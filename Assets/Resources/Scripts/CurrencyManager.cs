using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton จัดการ Crystal (เงิน) ของผู้เล่น
/// ติดกับ: GameObject ชื่อ "GameManager"
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Settings")]
    public int startingCrystal = 200;

    [Header("Passive Income")]
    public int incomePerTick = 2;       // crystal ที่ได้ต่อ tick
    public float tickInterval = 1f;     // ความถี่ (วินาที)

    // ── State ──
    private int _crystal;
    private float _timer;

    public int Crystal => _crystal;

    // ── Event — UI subscribe เพื่อ update text อัตโนมัติ ──
    public UnityEvent<int> OnCrystalChanged = new UnityEvent<int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _crystal = startingCrystal;
        _timer = tickInterval;
        OnCrystalChanged.Invoke(_crystal);
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer += tickInterval;   // +=  แทน = เพื่อไม่ให้ drift สะสม
            Earn(incomePerTick);
        }
    }

    public bool CanAfford(int cost) => _crystal >= cost;

    /// <summary>หัก crystal — return false ถ้าไม่พอ</summary>
    public bool Spend(int amount)
    {
        if (!CanAfford(amount)) return false;
        _crystal -= amount;
        OnCrystalChanged.Invoke(_crystal);
        return true;
    }

    public void Earn(int amount)
    {
        _crystal += amount;
        OnCrystalChanged.Invoke(_crystal);
    }
}