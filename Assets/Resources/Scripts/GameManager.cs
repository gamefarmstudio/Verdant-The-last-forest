using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameManager — ควบคุมภาพรวมของเกม Verdant: The Last Forest
/// ติดกับ: GameObject ชื่อ "GameManager"
///
/// Components ที่ต้องติดด้วยกัน:
///   CurrencyManager, TowerPlacementManager, PollutionManager
///
/// แพ้/ชนะด่านตัดสินจากเกจหมอก (PollutionManager เป็นสมองกลาง):
///   เกจหมอกเหลือ 0%   → ชนะ   (GameEvents.OnPollutionCleared)
///   เกจหมอกเต็ม 100%  → เกมโอเวอร์ (GameEvents.OnPollutionFull — เกิดจากคริสตัลโดนดาเมจสะสม)
/// หรือชนะทันทีถ้ากำจัดมอนสเตอร์ครบทุกเวฟ (ดู NotifyWaveCompleted)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // GAME STATE
    // ─────────────────────────────────────────────

    public enum GameState { Idle, Playing, Paused, Victory, GameOver }

    public GameState CurrentState { get; private set; } = GameState.Idle;

    // ─────────────────────────────────────────────
    // INSPECTOR SETTINGS
    // ─────────────────────────────────────────────

    // [แก้] จบครบทุกเวฟแล้วชนะด่าน — ตั้ง = 6 ตามสเปค (ปรับใน Inspector ให้ตรงจำนวนเวฟจริง)
    [Header("Wave Settings")]
    public int totalWaves = 6;

    // [เพิ่ม] เริ่มเกมเองตอนกด Play — สำหรับเทสต์ในซีนด่านโดยตรง (ไม่ต้องผ่านปุ่ม/เมนู)
    //        ถ้ามีเมนูเรียก StartGame() เองแล้ว ค่อยติ๊กออก
    [SerializeField] private bool autoStartOnPlay = true;
    private bool _autoStarted;

    // [เพิ่ม] ระบบ "เลือด" ของคริสตัล/ฐาน — แยกจากเกจหมอก
    //        มอนหลุดถึงฐาน → ลด HP · HP = 0 → เกมโอเวอร์
    [Header("Base / Crystal HP")]
    public int baseMaxHP = 20;
    public int BaseHP { get; private set; }

    // ─────────────────────────────────────────────
    // RUNTIME STATE
    // ─────────────────────────────────────────────

    public int CurrentWave { get; private set; } = 0;   // 0 = ยังไม่เริ่ม
    public int EnemiesKilled { get; private set; } = 0;
    public int TotalCrystalEarned { get; private set; } = 0;
    public float SessionTime { get; private set; } = 0f;

    private bool _timerRunning = false;

    // ─────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────

    [Header("Events")]
    public UnityEvent<int> OnWaveStart = new UnityEvent<int>();   // int = wave number
    public UnityEvent<int> OnWaveEnd = new UnityEvent<int>();   // int = wave number
    public UnityEvent OnVictory = new UnityEvent();
    public UnityEvent OnGameOver = new UnityEvent();
    public UnityEvent<GameState> OnStateChanged = new UnityEvent<GameState>();
    public UnityEvent<int> OnBaseHPChanged = new UnityEvent<int>();   // [เพิ่ม] int = HP ที่เหลือ (ให้ UI แถบเลือด)
    [Header("UI")]
    public Slider HealthBarSlider; // Reference to the UI Slider for health bar
    public TextMeshProUGUI WaveText; // Reference to the UI Text for wave information

    // ─────────────────────────────────────────────
    // SINGLETON SETUP
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SetState(GameState.Idle);
    }

    void OnEnable()
    {
        // [แก้] เกจ 0% ไม่ชนะแล้ว — ฟังแค่หมอกเต็ม 100% เพื่อแพ้
        GameEvents.OnPollutionFull += HandlePollutionFull;
    }

    void OnDisable()
    {
        GameEvents.OnPollutionFull -= HandlePollutionFull;
    }

    // ─────────────────────────────────────────────
    // UPDATE — SESSION TIMER
    // ─────────────────────────────────────────────

    void Update()
    {
        // [เพิ่ม] auto-start ครั้งเดียวในเฟรมแรก — ทำใน Update ไม่ใช่ Start()
        //        เพื่อรอให้ Start() ของตัวอื่นเสร็จก่อน (เช่น WaveManager subscribe OnWaveStart)
        //        ไม่งั้นถ้าเริ่มใน Start() อาจยิง event เวฟ 1 ตอนที่ WaveManager ยังไม่ฟัง
        if (autoStartOnPlay && !_autoStarted && CurrentState == GameState.Idle)
        {
            _autoStarted = true;
            StartGame();
        }

        if (_timerRunning)
            SessionTime += Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    // GAME STATE CONTROL
    // ─────────────────────────────────────────────

    /// <summary>เริ่มเกม — เรียกจากปุ่ม Start Wave หรือ UI</summary>
    public void StartGame()
    {
        if (CurrentState != GameState.Idle) return;

        SessionTime = 0f;
        EnemiesKilled = 0;
        TotalCrystalEarned = 0;
        CurrentWave = 0;

        // [เพิ่ม] รีเซ็ตเลือด + คืน timeScale (กันค้าง 0 จากรอบก่อนที่ pause ตอนจบเกม)
        BaseHP = baseMaxHP;
        OnBaseHPChanged.Invoke(BaseHP);
        if (HealthBarSlider != null) HealthBarSlider.value = 1f; // Reset the health bar slider
        GameSpeed.RestoreCurrent();

        _timerRunning = true;
        SetState(GameState.Playing);

        StartNextWave();
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        _timerRunning = false;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        _timerRunning = true;
        GameSpeed.RestoreCurrent();
        SetState(GameState.Playing);
    }

    // ─────────────────────────────────────────────
    // WAVE CONTROL
    // ─────────────────────────────────────────────

    /// <summary>เรียกจาก WaveManager เมื่อ wave นั้น spawn ครบและ enemy ตายหมดแล้ว</summary>
    public void NotifyWaveCompleted()
    {
        OnWaveEnd.Invoke(CurrentWave);

        if (CurrentWave >= totalWaves)
        {
            // กำจัดมอนสเตอร์ครบทุกเวฟแล้ว — ชนะด่านทันที ไม่ต้องรอเกจหมอกหมด
            TriggerVictory();
        }
        else
        {
            // เว้นช่วงก่อน wave ถัดไป — WaveManager จะเป็นคนจัดการ delay
            // แต่ถ้า auto-start ทันที ก็เรียก StartNextWave() ได้เลย
        }
    }

    /// <summary>เรียกจาก WaveManager หรือปุ่ม "Next Wave"</summary>
    public void StartNextWave()
    {
        if (CurrentState != GameState.Playing) return;
        if (CurrentWave >= totalWaves) return;

        CurrentWave++;
        OnWaveStart.Invoke(CurrentWave);

        if (WaveText != null) WaveText.text = $"{CurrentWave}/{totalWaves}";
        Debug.Log($"[GameManager] Wave {CurrentWave}/{totalWaves} started");
    }

    // ─────────────────────────────────────────────
    // CRYSTAL DAMAGE → เกจหมอกเพิ่มขึ้น
    // ─────────────────────────────────────────────

    // [แก้] มอนหลุดถึงฐาน → ลด "เลือด" ของคริสตัล (เดิมไปเพิ่มเกจหมอก) — เลือด 0 = เกมโอเวอร์
    public void DamageBase(int amount)
    {
        if (CurrentState != GameState.Playing) return;

        BaseHP = Mathf.Max(0, BaseHP - amount);
        OnBaseHPChanged.Invoke(BaseHP);
        Debug.Log($"[GameManager] Crystal took {amount} damage — HP {BaseHP}/{baseMaxHP}");
        if (HealthBarSlider != null) HealthBarSlider.value = (float)BaseHP / baseMaxHP; // Update the health bar slider

        if (BaseHP <= 0) TriggerGameOver();
    }

    // ─────────────────────────────────────────────
    // STATS TRACKING
    // ─────────────────────────────────────────────

    /// <summary>
    /// เรียกจาก EnemyBase เมื่อผู้เล่นกำจัด enemy (kill ด้วยดาเมจ หรือ cleanse)
    /// crystalReward = rewardKill หรือ rewardCleanse ตามวิธีกำจัด
    /// </summary>
    public void NotifyEnemyKilled(int crystalReward = 0)
    {
        EnemiesKilled++;

        if (crystalReward > 0)
        {
            TotalCrystalEarned += crystalReward;
            CurrencyManager.Instance.Earn(crystalReward);
        }

        // ฆ่ามอนแต่ละตัว ทำให้หมอกถอย (ส่ง 0 = ให้ PollutionManager ใช้ค่า defaultCleansePerKill)
        GameEvents.RaiseEnemyKilled(0f);
    }

    // ─────────────────────────────────────────────
    // WIN / LOSE
    // ─────────────────────────────────────────────

    // [ลบ] HandlePollutionCleared (เกจ 0% = ชนะ) — ตัดออก ชนะจากจบเวฟเท่านั้น
    private void HandlePollutionFull() => TriggerGameOver();   // เกจหมอกเต็ม 100% → แพ้

    private void TriggerVictory()
    {
        if (CurrentState != GameState.Playing) return;

        _timerRunning = false;
        Time.timeScale = 0f;   // [เพิ่ม] หยุดเกมตอนโชว์ popup ชนะ
        SetState(GameState.Victory);
        OnVictory.Invoke();
        Debug.Log($"[GameManager] Victory! Time: {FormatTime(SessionTime)} | Killed: {EnemiesKilled}");
    }

    private void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing) return;

        _timerRunning = false;
        Time.timeScale = 0f;   // [เพิ่ม] หยุดเกมตอนโชว์ popup แพ้
        SetState(GameState.GameOver);
        OnGameOver.Invoke();
        Debug.Log("[GameManager] Game Over — เลือดหมด หรือ หมอกเต็ม 100%");
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged.Invoke(newState);
    }

    /// <summary>แปลงวินาที → "2:34"</summary>
    public string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m}:{s:00}";
    }
}