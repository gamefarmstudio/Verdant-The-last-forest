using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton that owns the player's crystals (the in-game money).
/// Lives on the "GameManager" GameObject. Crystals come only from kills and wave rewards —
/// there is no passive income. UI listens to OnCrystalChanged to update itself.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Settings")]
    public int startingCrystal = 200;

    private int _crystal;

    public int Crystal => _crystal;

    /// <summary>Fired with the new total every time crystals are spent or earned.</summary>
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


    public bool CanAfford(int cost) => _crystal >= cost;

    /// <summary>Deducts crystals. Returns false (and changes nothing) if the player can't afford it.</summary>
    public bool Spend(int amount)
    {
        if (!CanAfford(amount)) return false;
        _crystal -= amount;
        OnCrystalChanged.Invoke(_crystal);
        return true;
    }

    /// <summary>Adds crystals (kill rewards, wave rewards, sell refunds).</summary>
    public void Earn(int amount)
    {
        _crystal += amount;
        OnCrystalChanged.Invoke(_crystal);
    }

    /// <summary>Same as Spend, but logs the result. Handy for wiring to buttons in the Inspector.</summary>
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