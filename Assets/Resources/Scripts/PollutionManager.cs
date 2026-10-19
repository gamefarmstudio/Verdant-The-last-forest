using UnityEngine;

/// <summary>
/// Singleton that owns the pollution gauge (0 = clean, 1 = full).
///   - The gauge rises on its own over time: +passiveStep every passiveInterval seconds, only while playing.
///   - TryCleanse() spends cleanseCost crystals to lower it by cleansePercent (called by the Cleanse button).
///   - Reaching 100% raises GameEvents.OnPollutionFull, which GameManager treats as game over.
/// Every change is broadcast through GameEvents.OnPollutionChanged for the UI, fog and tower spots.
/// </summary>
public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Config")]
    [Range(0f, 1f)]
    [SerializeField] private float startGauge = 0.7f;

    // How the fog rises over time.
    [Header("Passive Rise")]
    [SerializeField] private float passiveInterval = 2f;   // seconds between each rise
    [SerializeField] private float passiveStep = 0.01f;    // amount per rise (0.01 = 1%)

    // Cleanse button settings.
    [Header("Cleanse Button")]
    [SerializeField] private int cleanseCost = 15;          // crystals per press
    [SerializeField] private float cleansePercent = 0.05f;  // gauge removed per press (0.05 = 5%)

    public float Gauge { get; private set; }
    public int CleanseCost => cleanseCost;   // read by the UI to show the price / enable the button

    private bool _ended;          // true once the gauge has hit 100% — stops all further changes
    private float _passiveTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Gauge = startGauge;
    }


    void Start()
    {
        GameEvents.RaisePollutionChanged(Gauge);
    }

    // Passive rise — only while the game is actually being played.
    void Update()
    {
        if (_ended) return;
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        _passiveTimer += Time.deltaTime;
        while (_passiveTimer >= passiveInterval)
        {
            _passiveTimer -= passiveInterval;
            ApplyDelta(passiveStep);
        }
    }

    /// <summary>
    /// Called by the Cleanse button. Returns true if crystals were spent and the gauge went down;
    /// false if the player can't afford it or the game is over.
    /// </summary>
    public bool TryCleanse()
    {
        if (_ended) return false;
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.CanAfford(cleanseCost))
            return false;

        CurrencyManager.Instance.Spend(cleanseCost);
        ApplyDelta(-cleansePercent);
        return true;
    }

    private void ApplyDelta(float delta)
    {
        if (_ended) return;

        Gauge = Mathf.Clamp01(Gauge + delta);
        GameEvents.RaisePollutionChanged(Gauge);

        // 0% is not a win condition (winning comes from clearing all waves) — only 100% matters: game over.
        if (Gauge >= 1f) { _ended = true; GameEvents.RaisePollutionFull(); }
    }
}
