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

    // ต้องตรงกับชื่อ Exposed Parameter ใน All.mixer
    private const string MusicParam = "MusicVolume";
    private const string SFXParam = "SFXVolume";

    private const string MusicKey = "Settings.Music";
    private const string SFXKey = "Settings.SFX";
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResolutionKey = "Settings.Resolution";
    private const string MixerResourcePath = "Sounds/Mixers/All";   // Assets/Resources/Sounds/Mixers/All.mixer

    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1280, 720),
        new Vector2Int(960, 540),
        new Vector2Int(2560, 1440),
    };   // ทั้งหมดเป็น 16:9 และเป็นจำนวนเต็มเท่าของ 320x180 (Pixel Perfect Camera) = พิกเซลคม

    private int _selectedResolution = 0;
    private bool _updatingToggles;   // กัน event วนซ้ำตอนเราตั้ง isOn เอง

    public static SettingsPage Instance { get; private set; }
    public bool IsOpen { get; private set; }

    void Awake()
    {
        Instance = this;
        // ซ่อนหน้าก่อนเป็นอันดับแรก — ถ้าโค้ดด้านล่างพลาดตรงไหน หน้า Settings จะได้ไม่ค้างโชว์ทับ UI อื่น
        this.gameObject.SetActive(false);

        // ผูก listener ก่อน แล้วค่อยตั้งค่าเริ่มต้น เพื่อให้ค่าถูกส่งเข้า mixer ทันที
        MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        SFXVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        FullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        for (int i = 0; i < ResolutionButtons.Count; i++)
        {
            int index = i;
            ResolutionButtons[i].onValueChanged.AddListener((value) => OnResolutionButtonClicked(index, value));
        }

        // โหลดค่าที่เคยบันทึกไว้ (ไม่มี = ค่าเริ่มต้น)
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

    // ตั้งค่าเสียงที่บันทึกไว้ให้ทันทีตอนเปิดเกม แม้หน้า Settings จะยังไม่เคยถูกเปิด/ไม่มีใน scene นั้น
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

    // slider 0 -> Log10(0) = -infinity ซึ่ง mixer รับไม่ได้ จึงกันไว้ที่ -80 dB (เงียบสนิท)
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
        // ออกจาก fullscreen แล้วกลับไปใช้ความละเอียดที่เลือกไว้
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
            // กดปุ่มที่เลือกอยู่ซ้ำ -> ห้ามให้ไม่มีตัวไหนถูกเลือกเลย
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
        if (_frozen) return;   // กดเปิดซ้ำ ห้ามทับค่า timeScale เดิมด้วย 0
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _frozen = true;
    }

    private void Unfreeze()
    {
        if (!_frozen) return;
        Time.timeScale = _prevTimeScale;   // คืนค่าเดิม (รวม 2x หรือ 0 ถ้าเกมจบ/พักอยู่ก่อนแล้ว)
        _frozen = false;
    }

    void OnDestroy() => Unfreeze();   // กัน timeScale ค้างที่ 0 ถ้าโหลด scene ใหม่ตอน settings เปิดอยู่

    public void ShowSettingsMenu()
    {
        if (Tutorial.IsOpen) return;   // Tutorial เปิดอยู่ ต้องปิดก่อน ไม่ให้สองหน้าต่างซ้อนกัน
        IsOpen = true;
        Freeze();
        SettingsMenuPage.SetActive(true);
        DOTween.Kill(SettingsMenuPage.transform);
        SettingsMenuPage.transform.localScale = Vector3.zero;   // เริ่มจากศูนย์ทุกครั้ง animation จะเล่นเสมอ
        SettingsMenuPage.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);   // SetUpdate(true) = ทำงานได้แม้ timeScale = 0
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
