using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Audio;
using DG.Tweening;
using TMPro;

/// <summary>
/// The Settings menu: Music / SFX volume (through the "All" AudioMixer), fullscreen and screen resolution.
///   - Choices are saved to PlayerPrefs as soon as they change.
///   - Opening the menu freezes the game (timeScale 0); closing it restores the previous speed.
///   - It can't be opened while a Tutorial popup is showing (a Tutorial closes it if one appears).
/// The page hides itself in Awake; open it with ShowSettingsMenu() from a button.
/// </summary>
public class SettingsPage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject SettingsMenuPage;   // the panel that is shown/hidden (this object)
    [SerializeField] private Slider MusicVolumeSlider;
    [SerializeField] private Slider SFXVolumeSlider;
    [SerializeField] private Toggle FullscreenToggle;
    [SerializeField] private List<Toggle> ResolutionButtons;   // one toggle per entry in Resolutions, same order
    [Header("Settings")]
    [SerializeField] private AudioMixer AllMixer;

    // Must match the Exposed Parameter names in All.mixer
    private const string MusicParam = "MusicVolume";
    private const string SFXParam = "SFXVolume";

    // PlayerPrefs keys
    private const string MusicKey = "Settings.Music";
    private const string SFXKey = "Settings.SFX";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResolutionKey = "Settings.Resolution";
    private const string MixerResourcePath = "Sounds/Mixers/All";   // Assets/Resources/Sounds/Mixers/All.mixer

    // All 16:9 and whole-number multiples of 320x180 (the Pixel Perfect Camera reference) = crisp pixels
    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1280, 720),
        new Vector2Int(960, 540),
        new Vector2Int(2560, 1440),
    };

    private int _selectedResolution = 0;
    private bool _updatingToggles;   // stops our own isOn changes from firing the toggle events again

    public static SettingsPage Instance { get; private set; }
    public bool IsOpen { get; private set; }

    void Awake()
    {
        Instance = this;
        // Hide the page first — if anything below fails, the Settings page still won't stay on top of the other UI.
        this.gameObject.SetActive(false);

        // Hook up the listeners first, then set the starting values, so they reach the mixer straight away.
        MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        SFXVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        FullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        for (int i = 0; i < ResolutionButtons.Count; i++)
        {
            int index = i;
            ResolutionButtons[i].onValueChanged.AddListener((value) => OnResolutionButtonClicked(index, value));
        }

        // Starting values. Note: volumes always start at full here (the saved values aren't read back),
        // while fullscreen and resolution are loaded from PlayerPrefs.
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

    // Applies the saved volume as soon as any scene loads, even when no Settings page exists in that scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedVolumeOnLoad()
    {
        var mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (mixer == null) return;
        mixer.SetFloat(MusicParam, ToDecibels(PlayerPrefs.GetFloat(MusicKey, 1f)));
        mixer.SetFloat(SFXParam, ToDecibels(PlayerPrefs.GetFloat(SFXKey, 1f)));
    }

    // Fullscreen window, or windowed at the selected resolution.
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

    // Slider 0..1 -> decibels. Log10(0) is -infinity, which the mixer rejects, so it's floored at -80 dB (silent).
    private static float ToDecibels(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }

    private void SetMixerVolume(string param, float value)
    {
        if (!AllMixer.SetFloat(param, ToDecibels(value)))
            Debug.LogWarning($"[SettingsPage] Exposed Parameter '{param}' not found in the mixer", this);
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
        // Leaving fullscreen goes back to the selected resolution.
        ApplyDisplay(value);

        // Resolution choices only apply in windowed mode.
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
            // Clicking the already-selected toggle turned it off -> turn it back on (never leave none selected).
            if (index == _selectedResolution) RefreshResolutionToggles();
            return;
        }

        _selectedResolution = index;
        PlayerPrefs.SetInt(ResolutionKey, index);
        PlayerPrefs.Save();
        RefreshResolutionToggles();
        if (!FullscreenToggle.isOn) ApplyResolution(index);
    }

    // Ticks only the selected resolution toggle (without firing events) and locks them in fullscreen.
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

    // ── Freezing the game while the menu is open ──
    private float _prevTimeScale = 1f;
    private bool _frozen;

    private void Freeze()
    {
        if (_frozen) return;   // opened twice — don't overwrite the saved timeScale with 0
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _frozen = true;
    }

    private void Unfreeze()
    {
        if (!_frozen) return;
        Time.timeScale = _prevTimeScale;   // restore the previous value (2x, or 0 if the game had already ended/paused)
        _frozen = false;
    }

    void OnDestroy() => Unfreeze();   // don't leave timeScale stuck at 0 if a new scene loads while the menu is open

    /// <summary>Opens the menu with a pop-in animation and freezes the game.</summary>
    public void ShowSettingsMenu()
    {
        if (Tutorial.IsOpen) return;   // a Tutorial is open — it must be closed first, so the two windows never overlap
        IsOpen = true;
        Freeze();
        SettingsMenuPage.SetActive(true);
        DOTween.Kill(SettingsMenuPage.transform);
        SettingsMenuPage.transform.localScale = Vector3.zero;   // start from zero so the animation always plays
        SettingsMenuPage.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);   // SetUpdate(true) = runs even at timeScale 0
    }

    /// <summary>Closes the menu with a shrink animation and restores the game speed.</summary>
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

    /// <summary>Button shortcut: mutes the music (slider to 0).</summary>
    public void Shortcut_disableMusic()
    {
        MusicVolumeSlider.value = 0f;
    }

    /// <summary>Button shortcut: music back to full volume.</summary>
    public void Shortcut_UnDisableMusic()
    {
        MusicVolumeSlider.value = 1f;
    }
}
