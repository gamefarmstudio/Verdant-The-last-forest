using UnityEngine;
using UnityEngine.Events;


public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Settings")]
    public int startingCrystal = 200;

    private int _crystal;

    public int Crystal => _crystal;

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