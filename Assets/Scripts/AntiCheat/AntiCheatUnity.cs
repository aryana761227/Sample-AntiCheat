using UnityEngine;

namespace AntiCheat
{
    public class AntiCheatUnity : MonoBehaviour
    {
        // Simple integration example with all callbacks:
        void Start()
        {
            // Initialize anti-cheat
            AntiCheatNative.QuickStart(tickRateMs: 1000, enableLogging: true);
    
            // Add suspicious processes
            AntiCheatNative.AddSuspiciousProcessName("gameguardian");
            DontDestroyOnLoad(this);
            // Subscribe to all events
            AntiCheatNative.OnCheatDetected += OnPlayerCheating;
            AntiCheatNative.OnProcessLogged += OnProcessLogged;
            AntiCheatNative.OnPermissionRequired += OnPermissionRequired;
        }

        private void OnPlayerCheating(string processName, string executableName)
        {
            // Main cheat detection - ban the player
            Debug.LogError($"Cheat detected: {processName}");
            BanPlayer();
        }

        private void BanPlayer()
        {
        }

        private void OnProcessLogged(string processName, string executableName, int pid)
        {
            // Called for every running process when logging is enabled
            Debug.Log($"Running process: {processName} (PID: {pid})");
    
            // Optional: Log to your analytics system
            // LogProcessToAnalytics(processName, pid);
        }

        private void OnPermissionRequired(string message)
        {
            // Called when anti-cheat can't access process info due to permissions
            Debug.LogWarning($"Permission issue: {message}");
    
            // Optional: Show permission dialog to user
            // ShowPermissionDialog(message);
    
            // Or log the issue for debugging
            // LogPermissionIssue(message);
        }
    }
}