using UnityEngine;
using UnityEngine.Events;

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

    [Header("Wave Settings")]
    public int totalWaves = 6;

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
        GameEvents.OnPollutionCleared += HandlePollutionCleared;
        GameEvents.OnPollutionFull    += HandlePollutionFull;
    }

    void OnDisable()
    {
        GameEvents.OnPollutionCleared -= HandlePollutionCleared;
        GameEvents.OnPollutionFull    -= HandlePollutionFull;
    }

    // ─────────────────────────────────────────────
    // UPDATE — SESSION TIMER
    // ─────────────────────────────────────────────

    void Update()
    {
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
        Time.timeScale = 1f;
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

        Debug.Log($"[GameManager] Wave {CurrentWave}/{totalWaves} started");
    }

    // ─────────────────────────────────────────────
    // CRYSTAL DAMAGE → เกจหมอกเพิ่มขึ้น
    // ─────────────────────────────────────────────

    /// <summary>เรียกจาก Enemy เมื่อถึงปลายทางแล้วโจมตีคริสตัล — ดาเมจแปลงเป็นเกจหมอกที่เพิ่มขึ้น (ดู PollutionManager)</summary>
    public void DamageBase(int amount)
    {
        if (CurrentState != GameState.Playing) return;

        Debug.Log($"[GameManager] Crystal took {amount} damage");
        GameEvents.RaiseCrystalDamaged(amount);
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

    private void HandlePollutionCleared() => TriggerVictory();   // เกจหมอกเหลือ 0%
    private void HandlePollutionFull()    => TriggerGameOver();  // เกจหมอกเต็ม 100%

    private void TriggerVictory()
    {
        if (CurrentState != GameState.Playing) return;

        _timerRunning = false;
        SetState(GameState.Victory);
        OnVictory.Invoke();
        Debug.Log($"[GameManager] Victory! Time: {FormatTime(SessionTime)} | Killed: {EnemiesKilled}");
    }

    private void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing) return;

        _timerRunning = false;
        SetState(GameState.GameOver);
        OnGameOver.Invoke();
        Debug.Log("[GameManager] Game Over — หมอกกลืนกินคริสตัลจนหมด");
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