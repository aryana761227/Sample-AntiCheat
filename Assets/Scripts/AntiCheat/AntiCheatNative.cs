using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AntiCheat
{
    public static class AntiCheatNative
    {
        // Library name (without lib prefix on Android/Linux)
        private const string LibraryName = "anticheat";
        
        // Callback delegates
        public delegate void OnDetectionDelegate([MarshalAs(UnmanagedType.LPStr)] string message);
        public delegate void OnLogDelegate([MarshalAs(UnmanagedType.LPStr)] string message);
        public delegate void OnPermissionDelegate([MarshalAs(UnmanagedType.LPStr)] string permission);
        
        // Native function imports
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InitializeAntiCheat(
            IntPtr onDetection,
            IntPtr onLog,
            IntPtr onPermission
        );
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool StartMonitoring();
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void StopMonitoring();
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Shutdown();
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsMonitoring();
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetTickRate(int milliseconds);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void AddSuspiciousProcess([MarshalAs(UnmanagedType.LPStr)] string processName);
        
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ClearSuspiciousProcesses();
        
        // Static callback instances to prevent garbage collection
        private static OnDetectionDelegate s_onDetectionCallback;
        private static OnLogDelegate s_onLogCallback;
        private static OnPermissionDelegate s_onPermissionCallback;
        
        // Events
        public static event Action<string> OnDetection;
        public static event Action<string> OnLog;
        public static event Action<string> OnPermissionRequested;
        
        /// <summary>
        /// Initialize the anti-cheat system
        /// </summary>
        /// <param name="enableLogging">Enable detailed logging</param>
        /// <returns>True if initialization was successful</returns>
        public static bool Initialize(bool enableLogging = true)
        {
            try
            {
                // Create callback delegates
                s_onDetectionCallback = OnDetectionCallback;
                s_onLogCallback = OnLogCallback;
                s_onPermissionCallback = OnPermissionCallback;
                
                // Get function pointers
                IntPtr detectionPtr = Marshal.GetFunctionPointerForDelegate(s_onDetectionCallback);
                IntPtr logPtr = Marshal.GetFunctionPointerForDelegate(s_onLogCallback);
                IntPtr permissionPtr = Marshal.GetFunctionPointerForDelegate(s_onPermissionCallback);
                
                // Initialize native library
                bool result = InitializeAntiCheat(detectionPtr, logPtr, permissionPtr);
                
                if (result)
                {
                    Debug.Log("[AntiCheat] Native library initialized successfully");
                }
                else
                {
                    Debug.LogError("[AntiCheat] Failed to initialize native library");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception during anti-cheat initialization: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Start monitoring for cheats
        /// </summary>
        /// <returns>True if monitoring started successfully</returns>
        public static bool Start()
        {
            try
            {
                bool result = StartMonitoring();
                if (result)
                {
                    Debug.Log("[AntiCheat] Monitoring started");
                }
                else
                {
                    Debug.LogError("[AntiCheat] Failed to start monitoring");
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception starting monitoring: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Stop monitoring
        /// </summary>
        public static void Stop()
        {
            try
            {
                StopMonitoring();
                Debug.Log("[AntiCheat] Monitoring stopped");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception stopping monitoring: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shutdown the anti-cheat system
        /// </summary>
        public static void ShutdownAntiCheat()
        {
            try
            {
                Shutdown();
                Debug.Log("[AntiCheat] Anti-cheat shutdown complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception during shutdown: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if monitoring is active
        /// </summary>
        /// <returns>True if monitoring is active</returns>
        public static bool GetIsMonitoring()
        {
            try
            {
                return IsMonitoring();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception checking monitoring status: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Set the monitoring tick rate
        /// </summary>
        /// <param name="milliseconds">Milliseconds between monitoring checks</param>
        public static void SetMonitoringTickRate(int milliseconds)
        {
            try
            {
                SetTickRate(milliseconds);
                Debug.Log($"[AntiCheat] Tick rate set to {milliseconds}ms");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception setting tick rate: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Add a process name to the suspicious processes list
        /// </summary>
        /// <param name="processName">Name of the suspicious process</param>
        public static void AddSuspiciousProcessName(string processName)
        {
            try
            {
                if (string.IsNullOrEmpty(processName))
                {
                    Debug.LogError("[AntiCheat] Process name cannot be null or empty");
                    return;
                }
                
                AddSuspiciousProcess(processName);
                Debug.Log($"[AntiCheat] Added suspicious process: {processName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception adding suspicious process: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clear all suspicious processes
        /// </summary>
        public static void ClearAllSuspiciousProcesses()
        {
            try
            {
                ClearSuspiciousProcesses();
                Debug.Log("[AntiCheat] Cleared suspicious processes list");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception clearing suspicious processes: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Quick start with default settings
        /// </summary>
        /// <param name="tickRateMs">Monitoring tick rate in milliseconds</param>
        /// <param name="enableLogging">Enable logging</param>
        /// <returns>True if successfully started</returns>
        public static bool QuickStart(int tickRateMs = 1000, bool enableLogging = true)
        {
            try
            {
                if (!Initialize(enableLogging))
                {
                    Debug.LogError("[AntiCheat] Failed to initialize anti-cheat");
                    return false;
                }
                
                SetMonitoringTickRate(tickRateMs);
                
                if (!Start())
                {
                    Debug.LogError("[AntiCheat] Failed to start monitoring");
                    return false;
                }
                
                Debug.Log("[AntiCheat] Quick start successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception during quick start: {ex.Message}");
                return false;
            }
        }
        
        // Callback implementations
        [AOT.MonoPInvokeCallback(typeof(OnDetectionDelegate))]
        private static void OnDetectionCallback(string message)
        {
            try
            {
                Debug.LogWarning($"[AntiCheat] DETECTION: {message}");
                OnDetection?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception in detection callback: {ex.Message}");
            }
        }
        
        [AOT.MonoPInvokeCallback(typeof(OnLogDelegate))]
        private static void OnLogCallback(string message)
        {
            try
            {
                Debug.Log($"[AntiCheat] {message}");
                OnLog?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception in log callback: {ex.Message}");
            }
        }
        
        [AOT.MonoPInvokeCallback(typeof(OnPermissionDelegate))]
        private static void OnPermissionCallback(string permission)
        {
            try
            {
                Debug.Log($"[AntiCheat] Permission requested: {permission}");
                OnPermissionRequested?.Invoke(permission);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AntiCheat] Exception in permission callback: {ex.Message}");
            }
        }
    }
}