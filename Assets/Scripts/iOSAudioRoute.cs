using System.Runtime.InteropServices;
using UnityEngine;

public static class iOSAudioRoute
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void _ios_audio_set_playback_category();
#endif

    public static void SetPlaybackCategory()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _ios_audio_set_playback_category();
#endif
    }
}