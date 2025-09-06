using UnityEngine;
using System.Collections.Generic;

namespace AntiCheat
{
    public class AntiCheatUnity : MonoBehaviour
    {
        [Header("Anti-Cheat Settings")] [SerializeField]
        private bool autoStartOnAwake = true;

        [SerializeField] private bool enableLogging = true;
        [SerializeField] private int tickRateMs = 1000;

        [Header("Suspicious Processes")] [SerializeField]
        private List<string> suspiciousProcessNames = new List<string>();

        [Header("Events")] [SerializeField] private bool logDetections = true;
        [SerializeField] private bool logPermissionRequests = true;

        private bool isInitialized = false;

        private void Awake()
        {
            // Ensure this object persists across scenes
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoStartOnAwake)
            {
                InitializeAndStart();
            }
        }

        private void OnEnable()
        {
            // Subscribe to events
            AntiCheatNative.OnDetection += HandleDetection;
            AntiCheatNative.OnLog += HandleLog;
            AntiCheatNative.OnPermissionRequested += HandlePermissionRequest;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            AntiCheatNative.OnDetection -= HandleDetection;
            AntiCheatNative.OnLog -= HandleLog;
            AntiCheatNative.OnPermissionRequested -= HandlePermissionRequest;
        }

        private void OnDestroy()
        {
            try
            {
                if (isInitialized)
                {
                    AntiCheatNative.Stop();
                    AntiCheatNative.ShutdownAntiCheat();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception during cleanup: {ex.Message}");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            try
            {
                if (isInitialized)
                {
                    if (pauseStatus)
                    {
                        // App is being paused
                        AntiCheatNative.Stop();
                        Debug.Log("[AntiCheat] Stopped monitoring due to app pause");
                    }
                    else
                    {
                        // App is being resumed
                        if (!AntiCheatNative.Start())
                        {
                            Debug.LogError("[AntiCheat] Failed to restart monitoring after app resume");
                        }
                        else
                        {
                            Debug.Log("[AntiCheat] Resumed monitoring after app resume");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception handling app pause: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize and start the anti-cheat system
        /// </summary>
        public bool InitializeAndStart()
        {
            try
            {
                Debug.Log("[AntiCheat] Initializing anti-cheat system...");

                if (isInitialized)
                {
                    Debug.LogWarning("[AntiCheat] Anti-cheat is already initialized");
                    return true;
                }

                // Initialize the native library
                if (!AntiCheatNative.Initialize(enableLogging))
                {
                    Debug.LogError("[AntiCheat] Failed to initialize native library");
                    return false;
                }

                // Set tick rate
                AntiCheatNative.SetMonitoringTickRate(tickRateMs);

                // Add suspicious processes
                foreach (string processName in suspiciousProcessNames)
                {
                    if (!string.IsNullOrEmpty(processName))
                    {
                        AntiCheatNative.AddSuspiciousProcessName(processName.Trim());
                    }
                }

                // Start monitoring
                if (!AntiCheatNative.Start())
                {
                    Debug.LogError("[AntiCheat] Failed to start monitoring");
                    return false;
                }

                isInitialized = true;
                Debug.Log("[AntiCheat] Anti-cheat system initialized and started successfully");
                return true;

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception during initialization: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop the anti-cheat system
        /// </summary>
        public void StopAntiCheat()
        {
            try
            {
                if (!isInitialized)
                {
                    Debug.LogWarning("[AntiCheat] Anti-cheat not initialized. Call Initialize() first.");
                    return;
                }

                AntiCheatNative.Stop();
                Debug.Log("[AntiCheat] Anti-cheat monitoring stopped");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception stopping anti-cheat: {ex.Message}");
            }
        }

        /// <summary>
        /// Restart the anti-cheat monitoring
        /// </summary>
        public bool RestartAntiCheat()
        {
            try
            {
                if (!isInitialized)
                {
                    Debug.LogError("[AntiCheat] Anti-cheat not initialized. Call Initialize() first.");
                    return false;
                }

                AntiCheatNative.Stop();

                if (!AntiCheatNative.Start())
                {
                    Debug.LogError("[AntiCheat] Failed to restart monitoring");
                    return false;
                }

                Debug.Log("[AntiCheat] Anti-cheat monitoring restarted");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception restarting anti-cheat: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add a suspicious process name at runtime
        /// </summary>
        public void AddSuspiciousProcess(string processName)
        {
            try
            {
                if (string.IsNullOrEmpty(processName))
                {
                    Debug.LogError("[AntiCheat] Process name cannot be null or empty");
                    return;
                }

                if (!suspiciousProcessNames.Contains(processName))
                {
                    suspiciousProcessNames.Add(processName);
                }

                if (isInitialized)
                {
                    AntiCheatNative.AddSuspiciousProcessName(processName);
                }

                Debug.Log($"[AntiCheat] Added suspicious process: {processName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception adding suspicious process: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a suspicious process name
        /// </summary>
        public void RemoveSuspiciousProcess(string processName)
        {
            try
            {
                if (string.IsNullOrEmpty(processName))
                {
                    Debug.LogError("[AntiCheat] Process name cannot be null or empty");
                    return;
                }

                suspiciousProcessNames.Remove(processName);

                // Note: Native library doesn't have remove function, 
                // so we need to clear and re-add all processes
                if (isInitialized)
                {
                    AntiCheatNative.ClearAllSuspiciousProcesses();
                    foreach (string process in suspiciousProcessNames)
                    {
                        if (!string.IsNullOrEmpty(process))
                        {
                            AntiCheatNative.AddSuspiciousProcessName(process);
                        }
                    }
                }

                Debug.Log($"[AntiCheat] Removed suspicious process: {processName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception removing suspicious process: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all suspicious processes
        /// </summary>
        public void ClearSuspiciousProcesses()
        {
            try
            {
                suspiciousProcessNames.Clear();

                if (isInitialized)
                {
                    AntiCheatNative.ClearAllSuspiciousProcesses();
                }

                Debug.Log("[AntiCheat] Cleared all suspicious processes");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception clearing suspicious processes: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the monitoring tick rate
        /// </summary>
        public void SetTickRate(int milliseconds)
        {
            try
            {
                if (milliseconds < 100)
                {
                    Debug.LogError("[AntiCheat] Tick rate must be at least 100ms");
                    return;
                }

                tickRateMs = milliseconds;

                if (isInitialized)
                {
                    AntiCheatNative.SetMonitoringTickRate(milliseconds);
                }

                Debug.Log($"[AntiCheat] Tick rate set to {milliseconds}ms");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception setting tick rate: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current monitoring status
        /// </summary>
        public bool IsMonitoring()
        {
            try
            {
                if (!isInitialized)
                {
                    return false;
                }

                return AntiCheatNative.GetIsMonitoring();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception checking monitoring status: {ex.Message}");
                return false;
            }
        }

        // Event handlers
        private void HandleDetection(string message)
        {
            try
            {
                if (logDetections)
                {
                    Debug.LogWarning($"[AntiCheat] CHEAT DETECTED: {message}");
                }

                // You can add custom detection handling here
                // For example: pause game, show warning, report to server, etc.
                OnCheatDetected(message);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception handling detection: {ex.Message}");
            }
        }

        private void HandleLog(string message)
        {
            try
            {
                if (enableLogging)
                {
                    Debug.Log($"[AntiCheat] {message}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception handling log: {ex.Message}");
            }
        }

        private void HandlePermissionRequest(string permission)
        {
            try
            {
                if (logPermissionRequests)
                {
                    Debug.Log($"[AntiCheat] Permission requested: {permission}");
                }

                // You can add custom permission handling here
                OnPermissionRequested(permission);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception handling permission request: {ex.Message}");
            }
        }

        // Virtual methods that can be overridden in derived classes
        protected virtual void OnCheatDetected(string message)
        {
            // Override this method to implement custom cheat detection handling
            // Example: Show UI warning, pause game, report to analytics, etc.
        }

        protected virtual void OnPermissionRequested(string permission)
        {
            // Override this method to implement custom permission handling
            // Example: Show permission dialog, request Android permissions, etc.
        }

        // Public properties for runtime access
        public bool IsInitialized => isInitialized;
        public int TickRateMs => tickRateMs;
        public bool LoggingEnabled => enableLogging;
        public List<string> SuspiciousProcessNames => new List<string>(suspiciousProcessNames);
    }
}