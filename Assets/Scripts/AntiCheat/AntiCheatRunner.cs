using UnityEngine;

public class AntiCheatRunner : MonoBehaviour
{
    private void Update()
    {
        AntiCheatNative.ProcessMainThreadActions();
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App going to background - completely shutdown anti-cheat
            if (AntiCheatNative.IsInitialized)
            {
                AntiCheatNative.ShutdownAntiCheat();
            }
        }
        else
        {
            // App resuming from background - reinitialize and restart
            if (!AntiCheatNative.IsInitialized)
            {
                if (AntiCheatNative.Initialize())
                {
                    AntiCheatNative.StartAntiCheatMonitoring();
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        AntiCheatNative.ShutdownAntiCheat();
    }
}