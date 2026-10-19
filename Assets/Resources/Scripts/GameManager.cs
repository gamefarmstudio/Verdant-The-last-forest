using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameManager — controls the overall flow of Verdant: The Last Forest.
/// Attach to: the GameObject named "GameManager".
///
/// Components that must be on the same object:
///   CurrencyManager, TowerPlacementManager, PollutionManager
///
/// Win / lose:
///   Clear every wave (CurrentWave reaches totalWaves)  -> Victory (see NotifyWaveCompleted)
///   Crystal / base HP reaches 0                        -> Game Over (enemies that reach the base, see DamageBase)
///   Pollution gauge reaches 100%                       -> Game Over (GameEvents.OnPollutionFull)
/// Both endings pause the game (timeScale 0) and fire OnVictory / OnGameOver for the UI.
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

    // Clearing this many waves wins the level — set it in the Inspector to match the level's real wave count.
    [Header("Wave Settings")]
    public int totalWaves = 6;

    // Start the game automatically when Play is pressed — for testing a level scene directly (no menu/button needed).
    // Untick it if a menu calls StartGame() itself.
    [SerializeField] private bool autoStartOnPlay = true;
    private bool _autoStarted;

    // The crystal / base "HP" — separate from the pollution gauge.
    // An enemy reaching the base -> HP goes down · HP = 0 -> game over.
    [Header("Base / Crystal HP")]
    public int baseMaxHP = 20;
    public int BaseHP { get; private set; }

    // ─────────────────────────────────────────────
    // RUNTIME STATE
    // ─────────────────────────────────────────────

    public int CurrentWave { get; private set; } = 0;   // 0 = not started yet
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
    public UnityEvent<int> OnBaseHPChanged = new UnityEvent<int>();   // int = HP left (for the HP bar UI)
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
        // A 0% gauge is no longer a win — only listen for a full (100%) gauge, which loses.
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
        // Auto-start once on the first frame — done in Update, not Start(),
        // so every other Start() has run first (e.g. WaveManager subscribing to OnWaveStart).
        // Starting in Start() could fire the wave-1 event before WaveManager is listening.
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

    /// <summary>Starts the game — called by a Start button / UI, or automatically (autoStartOnPlay).</summary>
    public void StartGame()
    {
        if (CurrentState != GameState.Idle) return;

        SessionTime = 0f;
        EnemiesKilled = 0;
        TotalCrystalEarned = 0;
        CurrentWave = 0;

        // Reset HP and restore timeScale (in case it was left at 0 by a previous game's pause/ending).
        BaseHP = baseMaxHP;
        OnBaseHPChanged.Invoke(BaseHP);
        if (HealthBarSlider != null) HealthBarSlider.value = 1f; // Reset the health bar slider
        GameSpeed.RestoreCurrent();

        _timerRunning = true;
        SetState(GameState.Playing);

        StartNextWave();
    }

    /// <summary>Freezes the game (timeScale 0).</summary>
    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        _timerRunning = false;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    /// <summary>Unfreezes the game at the speed the player chose (1x/2x).</summary>
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

    /// <summary>Called by WaveManager when a wave has finished spawning and every enemy is gone.</summary>
    public void NotifyWaveCompleted()
    {
        OnWaveEnd.Invoke(CurrentWave);

        if (CurrentWave >= totalWaves)
        {
            // Every wave has been cleared — win the level straight away.
            TriggerVictory();
        }
        else
        {
            // Gap before the next wave — WaveManager handles the delay (if autoStartNextWave is on),
            // otherwise the player presses the "Next Wave" button, which calls StartNextWave().
        }
    }

    /// <summary>Starts the next wave. Called by WaveManager or the "Next Wave" button.</summary>
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
    // CRYSTAL DAMAGE -> base HP goes down
    // ─────────────────────────────────────────────

    /// <summary>
    /// An enemy reached the base -> lower the crystal's HP (this used to raise the fog gauge instead).
    /// HP 0 = game over.
    /// </summary>
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
    /// Called by EnemyBase when the player gets rid of an enemy (killed by damage, or cleansed).
    /// crystalReward = rewardKill or rewardCleanse, depending on how it was removed.
    /// </summary>
    public void NotifyEnemyKilled(int crystalReward = 0)
    {
        EnemiesKilled++;

        if (crystalReward > 0)
        {
            TotalCrystalEarned += crystalReward;
            CurrencyManager.Instance.Earn(crystalReward);
        }

        // Broadcast the kill. (Nothing listens to this any more — kills no longer lower pollution;
        // only the Cleanse button does.)
        GameEvents.RaiseEnemyKilled(0f);
    }

    // ─────────────────────────────────────────────
    // WIN / LOSE
    // ─────────────────────────────────────────────

    // (A 0%-gauge win handler used to live here; it was removed — winning only comes from clearing every wave.)
    private void HandlePollutionFull() => TriggerGameOver();   // fog gauge full at 100% -> lose

    private void TriggerVictory()
    {
        if (CurrentState != GameState.Playing) return;

        _timerRunning = false;
        Time.timeScale = 0f;   // freeze the game while the victory popup is shown
        SetState(GameState.Victory);
        OnVictory.Invoke();
        Debug.Log($"[GameManager] Victory! Time: {FormatTime(SessionTime)} | Killed: {EnemiesKilled}");
    }

    private void TriggerGameOver()
    {
        if (CurrentState != GameState.Playing) return;

        _timerRunning = false;
        Time.timeScale = 0f;   // freeze the game while the game-over popup is shown
        SetState(GameState.GameOver);
        OnGameOver.Invoke();
        Debug.Log("[GameManager] Game Over — base HP ran out, or the fog reached 100%");
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged.Invoke(newState);
    }

    /// <summary>Formats seconds as "m:ss", e.g. 154 -> "2:34".</summary>
    public string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m}:{s:00}";
    }
}