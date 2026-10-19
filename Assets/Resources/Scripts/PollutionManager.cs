using UnityEngine;

public class PollutionManager : MonoBehaviour
{
    public static PollutionManager Instance { get; private set; }

    [Header("Config")]
    [Range(0f, 1f)]
    [SerializeField] private float startGauge = 0.7f;

    // [เพิ่ม] ค่าการเพิ่มหมอกตามเวลา
    [Header("Passive Rise")]
    [SerializeField] private float passiveInterval = 2f;
    [SerializeField] private float passiveStep = 0.01f; 

    // [เพิ่ม] ค่าปุ่มชำระล้าง
    [Header("Cleanse Button")]
    [SerializeField] private int cleanseCost = 15;  
    [SerializeField] private float cleansePercent = 0.05f;

    public float Gauge { get; private set; }
    public int CleanseCost => cleanseCost; 

    private bool _ended;
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

        if (Gauge >= 1f) { _ended = true; GameEvents.RaisePollutionFull(); } 
    }
}
