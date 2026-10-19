using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Audio;
using DG.Tweening;
using TMPro;
public class SettingsPage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject SettingsMenuPage;
    [SerializeField] private Slider MusicVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    [SerializeField] private Toggle FullscreenToggle;
    [SerializeField] private List<Toggle> ResolutionButtons;
    [Header("Settings")]
    [SerializeField] private AudioMixer AllMixer;

    private const string MusicParam = "MusicVolume";
    private const string SFXParam = "SFXVolume";

    private const string MusicKey = "Settings.Music";
    private const string SFXKey = "Settings.SFX";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResolutionKey = "Settings.Resolution";
    private const string MixerResourcePath = "Sounds/Mixers/All";  

    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1280, 720),
        new Vector2Int(960, 540),
        new Vector2Int(2560, 1440),
    };   

    private int _selectedResolution = 0;
    private bool _updatingToggles;  

    public static SettingsPage Instance { get; private set; }
    public bool IsOpen { get; private set; }

    void Awake()
    {
        Instance = this;
        this.gameObject.SetActive(false);

        MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        SFXVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        FullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        for (int i = 0; i < ResolutionButtons.Count; i++)
        {
            int index = i;
            ResolutionButtons[i].onValueChanged.AddListener((value) => OnResolutionButtonClicked(index, value));
        }

        float music = 1f;
        float sfx = 1f;
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        _selectedResolution = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, 0), 0, Resolutions.Length - 1);

        MusicVolumeSlider.SetValueWithoutNotify(music);
        SFXVolumeSlider.SetValueWithoutNotify(sfx);
        SetMixerVolume(MusicParam, music);
        SetMixerVolume(SFXParam, sfx);

        FullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        RefreshResolutionToggles();
        ApplyDisplay(fullscreen);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedVolumeOnLoad()
    {
        var mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (mixer == null) return;
        mixer.SetFloat(MusicParam, ToDecibels(PlayerPrefs.GetFloat(MusicKey, 1f)));
        mixer.SetFloat(SFXParam, ToDecibels(PlayerPrefs.GetFloat(SFXKey, 1f)));
    }

    private void ApplyDisplay(bool fullscreen)
    {
        if (fullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        else
        {
            ApplyResolution(_selectedResolution);
        }
    }

    private static float ToDecibels(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }

    private void SetMixerVolume(string param, float value)
    {
        if (!AllMixer.SetFloat(param, ToDecibels(value)))
            Debug.LogWarning($"[SettingsPage] ไม่พบ Exposed Parameter '{param}' ใน mixer", this);
    }

    public void OnMusicVolumeChanged(float value)
    {
        SetMixerVolume(MusicParam, value);
        PlayerPrefs.SetFloat(MusicKey, value);
        PlayerPrefs.Save();
    }

    public void OnSFXVolumeChanged(float value)
    {
        SetMixerVolume(SFXParam, value);
        PlayerPrefs.SetFloat(SFXKey, value);
        PlayerPrefs.Save();
    }

    public void OnFullscreenToggleChanged(bool value)
    {
        ApplyDisplay(value);

        for (int i = 0; i < ResolutionButtons.Count; i++)
            ResolutionButtons[i].interactable = !value;

        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnResolutionButtonClicked(int index, bool value)
    {
        if (_updatingToggles) return;

        if (!value)
        {
            if (index == _selectedResolution) RefreshResolutionToggles();
            return;
        }

        _selectedResolution = index;
        PlayerPrefs.SetInt(ResolutionKey, index);
        PlayerPrefs.Save();
        RefreshResolutionToggles();
        if (!FullscreenToggle.isOn) ApplyResolution(index);
    }

    private void RefreshResolutionToggles()
    {
        _updatingToggles = true;
        for (int i = 0; i < ResolutionButtons.Count; i++)
        {
            ResolutionButtons[i].SetIsOnWithoutNotify(i == _selectedResolution);
            ResolutionButtons[i].interactable = !FullscreenToggle.isOn;
        }
        _updatingToggles = false;
    }

    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= Resolutions.Length) return;
        var r = Resolutions[index];
        Screen.SetResolution(r.x, r.y, FullScreenMode.Windowed);
    }

    private float _prevTimeScale = 1f;
    private bool _frozen;

    private void Freeze()
    {
        if (_frozen) return;   
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _frozen = true;
    }

    private void Unfreeze()
    {
        if (!_frozen) return;
        Time.timeScale = _prevTimeScale;   
        _frozen = false;
    }

    void OnDestroy() => Unfreeze();   

    public void ShowSettingsMenu()
    {
        if (Tutorial.IsOpen) return;   
        IsOpen = true;
        Freeze();
        SettingsMenuPage.SetActive(true);
        DOTween.Kill(SettingsMenuPage.transform);
        SettingsMenuPage.transform.localScale = Vector3.zero;  
        SettingsMenuPage.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);  
    }

    public void HideSettingsMenu()
    {
        IsOpen = false;
        Unfreeze();
        DOTween.Kill(SettingsMenuPage.transform);
        SettingsMenuPage.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            SettingsMenuPage.SetActive(false);
        });
    }

    public void Shortcut_disableMusic()
    {
        MusicVolumeSlider.value = 0f;
    }
    public void Shortcut_UnDisableMusic()
    {
        MusicVolumeSlider.value = 1f;
    }
}
