using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioSource BGMClip;

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
    public void MuteBGM()
    {
        if (BGMClip != null)
        {
            BGMClip.volume = 0f;
        }
    }
    public void UnmuteBGM()
    {
        if (BGMClip != null)
        {
            BGMClip.volume = 1f;
        }
    }
}
