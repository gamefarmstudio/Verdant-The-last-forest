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

    // ── State ──
    private int _crystal;

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
        OnCrystalChanged.Invoke(_crystal);
    }

    // รายได้มาจากการฆ่ามอนสเตอร์เท่านั้น (เรียก Earn จาก GameManager.NotifyEnemyKilled)
    // — ไม่มี passive income อีกต่อไป

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
    public void SpendMoney(int amount)
    {
        if (Spend(amount))
        {
            Debug.Log($"Spend {amount} crystal. Remaining: {_crystal}");
        }
        else
        {
            Debug.Log($"Not enough crystal to spend {amount}. Remaining: {_crystal}");
        }
    }
}