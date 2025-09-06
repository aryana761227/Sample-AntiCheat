using UnityEngine;
using AntiCheat;

public class AntiCheatExample : MonoBehaviour
{
    void Start()
    {
        // Initialize the anti-cheat system
        bool initialized = AntiCheatNative.Initialize(true);
        if (!initialized)
        {
            Debug.LogError("Failed to initialize anti-cheat!");
            return;
        }
        
        // Enable detailed logging to see ALL processes
        AntiCheatNative.EnableDetailedLogging(true);
        
        // Add suspicious process names (needles to search for in haystacks)
        AntiCheatNative.AddSuspiciousProcessName("cheat");
        AntiCheatNative.AddSuspiciousProcessName("injector");
        
        // Set monitoring frequency (every 2 seconds)
        AntiCheatNative.SetMonitoringTickRate(2000);
        
        // Start monitoring
        bool started = AntiCheatNative.Start();
        if (started)
        {
            Debug.Log("Anti-cheat monitoring started successfully!");
        }
        else
        {
            Debug.LogError("Failed to start anti-cheat monitoring!");
        }
        
        // Log all processes immediately for testing
        AntiCheatNative.LogAllCurrentProcesses();
        
        // Get current process count
        int processCount = AntiCheatNative.GetCurrentProcessCount();
        Debug.Log($"Currently running processes: {processCount}");
        
        // Search for specific processes
        AntiCheatNative.SearchForProcess("chrome");
        AntiCheatNative.SearchForProcess("unity");
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App paused - stop monitoring
            AntiCheatNative.Stop();
            Debug.Log("Anti-cheat monitoring paused");
        }
        else
        {
            // App resumed - restart monitoring
            if (AntiCheatNative.Start())
            {
                Debug.Log("Anti-cheat monitoring resumed");
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean shutdown
        AntiCheatNative.ShutdownAntiCheat();
        Debug.Log("Anti-cheat system shutdown");
    }
    
    // Test functions you can call from UI buttons
    public void TestLogAllProcesses()
    {
        AntiCheatNative.LogAllCurrentProcesses();
    }
    
    public void TestSearchChrome()
    {
        AntiCheatNative.SearchForProcess("chrome");
    }
    
    public void TestAddSuspiciousProcess()
    {
        AntiCheatNative.AddSuspiciousProcessName("test");
        Debug.Log("Added 'test' to suspicious processes");
    }
    
    public void ToggleDetailedLogging()
    {
        // This would require tracking the current state
        AntiCheatNative.EnableDetailedLogging(true);
    }
}