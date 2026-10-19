using UnityEngine;

/// <summary>
/// Plays the background music when the scene starts and lets buttons mute / unmute it.
/// Note: master Music/SFX volume is handled by SettingsPage through the "All" audio mixer.
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioSource BGMClip;   // AudioSource that holds the music track

    void Start()
    {
        PlayBGM();
    }

    void PlayBGM()
    {
        if (BGMClip != null && !BGMClip.isPlaying)
        {
            BGMClip.Play();
        }
    }

    /// <summary>Silences the music (volume 0) without stopping it.</summary>
    public void MuteBGM()
    {
        if (BGMClip != null)
        {
            BGMClip.volume = 0f;
        }
    }

    /// <summary>Restores the music to full volume.</summary>
    public void UnmuteBGM()
    {
        if (BGMClip != null)
        {
            BGMClip.volume = 1f;
        }
    }
}
