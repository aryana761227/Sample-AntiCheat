using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class AntiCheatNative
{
    #region Native Function Declarations
    
    // Callback delegates for native library
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DetectionCallbackDelegate(string processName, string executableName);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogCallbackDelegate(string processName, string executableName, int pid);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void PermissionCallbackDelegate(string message);

    // Native function imports
    [DllImport("anticheat")]
    private static extern bool InitializeAntiCheat(DetectionCallbackDelegate onDetection, 
                                                   LogCallbackDelegate onLog, 
                                                   PermissionCallbackDelegate onPermission);
    
    [DllImport("anticheat")]
    private static extern void SetTickRate(int milliseconds);
    
    [DllImport("anticheat")]
    private static extern void AddSuspiciousProcess(string processName);
    
    [DllImport("anticheat")]
    private static extern void ClearSuspiciousProcesses();
    
    [DllImport("anticheat")]
    private static extern void StartMonitoring();
    
    [DllImport("anticheat")]
    private static extern void StopMonitoring();
    
    [DllImport("anticheat")]
    private static extern void EnableLogging(bool enable);
    
    [DllImport("anticheat")]
    private static extern void Shutdown();
    
    [DllImport("anticheat")]
    private static extern bool detectCheater();
    
    [DllImport("anticheat")]
    private static extern int GetRunningProcessCount();
    
    [DllImport("anticheat")]
    private static extern bool IsMonitoringActive();
    
    #endregion

    #region Public Events
    
    /// <summary>
    /// Fired when a suspicious process is detected
    /// </summary>
    public static event Action<string, string> OnCheatDetected;
    
    /// <summary>
    /// Fired when logging is enabled and processes are being monitored
    /// </summary>
    public static event Action<string, string, int> OnProcessLogged;
    
    /// <summary>
    /// Fired when permission issues occur
    /// </summary>
    public static event Action<string> OnPermissionRequired;
    
    #endregion

    #region Private Fields
    
    private static readonly object lockObject = new object();
    private static Queue<System.Action> mainThreadActions = new Queue<System.Action>();
    
    // Keep references to prevent garbage collection
    private static DetectionCallbackDelegate detectionCallback;
    private static LogCallbackDelegate logCallback;
    private static PermissionCallbackDelegate permissionCallback;
    
    private static bool isInitialized = false;
    private static bool debugMode = true;
    private static MonoBehaviour coroutineRunner;
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize the anti-cheat system
    /// </summary>
    public static bool Initialize(bool enableDebugLogs = true)
    {
        if (isInitialized)
        {
            LogDebug("Anti-cheat already initialized");
            return true;
        }
        
        debugMode = enableDebugLogs;
        
        // Create a hidden GameObject to handle coroutines and main thread dispatching
        SetupCoroutineRunner();
        
        try
        {
            // Create callback delegates (keep references to prevent GC)
            detectionCallback = OnNativeCheatDetected;
            logCallback = OnNativeProcessLogged;
            permissionCallback = OnNativePermissionRequired;
            
            // Initialize native library
            bool success = InitializeAntiCheat(detectionCallback, logCallback, permissionCallback);
            
            if (success)
            {
                isInitialized = true;
                LogDebug("Anti-cheat initialized successfully");
                return true;
            }
            else
            {
                LogError("Failed to initialize anti-cheat native library");
                return false;
            }
        }
        catch (Exception e)
        {
            LogError($"Exception during anti-cheat initialization: {e.Message}");
            return false;
        }
    }
    
    private static void SetupCoroutineRunner()
    {
        if (coroutineRunner != null) return;
        
        GameObject runnerObject = new GameObject("AntiCheatRunner");
        runnerObject.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(runnerObject);
        
        coroutineRunner = runnerObject.AddComponent<AntiCheatRunner>();
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Quick setup and start monitoring
    /// </summary>
    public static void QuickStart(int tickRateMs = 1000, bool enableLogging = false)
    {
        if (!Initialize())
        {
            LogError("Failed to initialize anti-cheat");
            return;
        }
        
        SetTickRate(tickRateMs);
        StartAntiCheatMonitoring();
        
        if (enableLogging)
        {
            SetLogging(true);
        }
    }
    
    /// <summary>
    /// Start monitoring for suspicious processes
    /// </summary>
    public static void StartAntiCheatMonitoring()
    {
        if (!isInitialized)
        {
            LogError("Anti-cheat not initialized. Call Initialize() first.");
            return;
        }
        
        try
        {
            StartMonitoring();
            LogDebug("Anti-cheat monitoring started");
        }
        catch (Exception e)
        {
            LogError($"Error starting monitoring: {e.Message}");
        }
    }
    
    /// <summary>
    /// Set the monitoring tick rate in milliseconds
    /// </summary>
    public static void SetMonitoringTickRate(int milliseconds)
    {
        int tickRateMs = Mathf.Clamp(milliseconds, 100, 60000);
        
        if (isInitialized)
        {
            SetTickRate(tickRateMs);
            LogDebug($"Tick rate set to {tickRateMs}ms");
        }
    }
    
    /// <summary>
    /// Add a process name to the suspicious list
    /// </summary>
    public static void AddSuspiciousProcessName(string processName)
    {
        if (string.IsNullOrEmpty(processName))
        {
            LogError("Process name cannot be null or empty");
            return;
        }
        
        if (isInitialized)
        {
            AddSuspiciousProcess(processName);
            LogDebug($"Added suspicious process: {processName}");
        }
        else
        {
            LogError("Anti-cheat not initialized. Call Initialize() first.");
        }
    }
    
    /// <summary>
    /// Clear all suspicious processes
    /// </summary>
    public static void ClearAllSuspiciousProcesses()
    {
        if (isInitialized)
        {
            ClearSuspiciousProcesses();
            LogDebug("Cleared all suspicious processes");
        }
    }
    
    /// <summary>
    /// Enable or disable detailed logging
    /// </summary>
    public static void SetLogging(bool enabled)
    {
        if (isInitialized)
        {
            EnableLogging(enabled);
            LogDebug($"Logging {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Check for cheats once (synchronous)
    /// </summary>
    public static bool CheckForCheats()
    {
        if (!isInitialized)
        {
            LogError("Anti-cheat not initialized");
            return false;
        }
        
        try
        {
            return detectCheater();
        }
        catch (Exception e)
        {
            LogError($"Error checking for cheats: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get current number of running processes
    /// </summary>
    public static int GetProcessCount()
    {
        if (!isInitialized) return 0;
        
        try
        {
            return GetRunningProcessCount();
        }
        catch (Exception e)
        {
            LogError($"Error getting process count: {e.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Check if monitoring is currently active
    /// </summary>
    public static bool IsMonitoring()
    {
        if (!isInitialized) return false;
        
        try
        {
            return IsMonitoringActive();
        }
        catch (Exception e)
        {
            LogError($"Error checking monitoring status: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Check if the anti-cheat system is initialized
    /// </summary>
    public static bool IsInitialized => isInitialized;
    
    /// <summary>
    /// Shutdown the anti-cheat system
    /// </summary>
    public static void ShutdownAntiCheat()
    {
        if (!isInitialized) return;
        
        try
        {
            Shutdown();
            isInitialized = false;
            
            // Clean up coroutine runner
            if (coroutineRunner != null)
            {
                UnityEngine.Object.Destroy(coroutineRunner.gameObject);
                coroutineRunner = null;
            }
            
            LogDebug("Anti-cheat shutdown complete");
        }
        catch (Exception e)
        {
            LogError($"Error during shutdown: {e.Message}");
        }
    }
    
    #endregion

    #region Native Callbacks
    
    private static void OnNativeCheatDetected(string processName, string executableName)
    {
        // Execute on main thread
        EnqueueMainThreadAction(() =>
        {
            LogDebug($"CHEAT DETECTED: Process={processName}, Executable={executableName}");
            OnCheatDetected?.Invoke(processName, executableName);
        });
    }
    
    private static void OnNativeProcessLogged(string processName, string executableName, int pid)
    {
        // Execute on main thread
        EnqueueMainThreadAction(() =>
        {
            OnProcessLogged?.Invoke(processName, executableName, pid);
        });
    }
    
    private static void OnNativePermissionRequired(string message)
    {
        // Execute on main thread
        EnqueueMainThreadAction(() =>
        {
            LogError($"Permission required: {message}");
            OnPermissionRequired?.Invoke(message);
        });
    }
    
    #endregion

    #region Main Thread Dispatching
    
    private static void EnqueueMainThreadAction(System.Action action)
    {
        lock (lockObject)
        {
            mainThreadActions.Enqueue(action);
        }
    }
    
    internal static void ProcessMainThreadActions()
    {
        lock (lockObject)
        {
            while (mainThreadActions.Count > 0)
            {
                var action = mainThreadActions.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    LogError($"Error executing main thread action: {e.Message}");
                }
            }
        }
    }
    
    #endregion

    #region Logging
    
    private static void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[AntiCheat] {message}");
        }
    }
    
    private static void LogError(string message)
    {
        Debug.LogError($"[AntiCheat] {message}");
    }
    
    #endregion
}

// Helper MonoBehaviour for main thread dispatching