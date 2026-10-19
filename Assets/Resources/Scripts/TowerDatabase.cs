using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads towers.csv through DataManager and indexes it so tower stats are easy to look up.
///
/// Usage:
///   TowerDatabase.Instance.GetBaseLevel(tier)    -> every level-1 tower of that tier
///   TowerDatabase.Instance.GetByID("T1A1")       -> a single tower level
///   TowerDatabase.Instance.GetNextLevel("T1A1")  -> T1A2 (its upgrade)
/// </summary>
public class TowerDatabase : MonoBehaviour
{
    public static TowerDatabase Instance { get; private set; }

    [Header("CSV Settings")]
    [Tooltip("CSV file name in Resources/Data/ (without .csv)")]
    [SerializeField] private string csvFileName = "towers";

    // Main lookup: ID -> TowerRecord
    private Dictionary<string, TowerRecord> _byID = new Dictionary<string, TowerRecord>();

    // Index: tier -> list of level-1 towers (what the player can buy directly)
    private Dictionary<int, List<TowerRecord>> _baseLevelByTier = new Dictionary<int, List<TowerRecord>>();

    public bool IsLoaded { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Load in Start, not Awake — so DataManager.Awake() has already run.
    private void Start()
    {
        LoadFromCSV();
    }


    private void LoadFromCSV()
    {
        var rawRows = DataManager.Instance.GetTableData(csvFileName);

        if (rawRows == null || rawRows.Count == 0)
        {
            Debug.LogError($"[TowerDatabase] Could not load CSV '{csvFileName}' — check DataManager");
            return;
        }

        _byID.Clear();
        _baseLevelByTier.Clear();

        foreach (var row in rawRows)
        {
            var record = ParseRow(row);
            if (record == null) continue;

            _byID[record.id] = record;

            // Index level-1 towers by tier.
            if (record.IsBaseLevel)
            {
                if (!_baseLevelByTier.ContainsKey(record.tier))
                    _baseLevelByTier[record.tier] = new List<TowerRecord>();
                _baseLevelByTier[record.tier].Add(record);
            }
        }

        IsLoaded = true;
        Debug.Log($"[TowerDatabase] Loaded successfully: {_byID.Count} rows | " +
                  $"Tier1:{GetBaseLevel(1).Count} Tier2:{GetBaseLevel(2).Count} Tier3:{GetBaseLevel(3).Count}");
    }

    // Converts one CSV row into a TowerRecord. Returns null (and logs) if a required column is missing or malformed.
    private TowerRecord ParseRow(Dictionary<string, string> row)
    {
        try
        {
            return new TowerRecord
            {
                id = row["ID"].Trim(),
                nameEN = row["Name (EN)"].Trim(),
                nameTH = row["Name (TH)"].Trim(),
                tier = int.Parse(row["Tier"]),
                level = int.Parse(row["Level"]),
                cost = int.Parse(row["Cost (Crystal)"]),
                cumulativeCost = ParseIntSafe(row["Cumulative Cost"]),
                damage = ParseFloatSafe(row["Damage"]),
                atkSpeed = ParseFloatSafe(row["Atk Speed (s)"]),
                dps = ParseFloatSafe(row["DPS"]),
                range = ParseFloatSafe(row["Range (tiles)"]),
                hp = ParseIntSafe(row["HP"]),
                specialAbility = row["Special Ability"].Trim(),
                assetName = row["Asset (Tiny Sword)"].Trim(),
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TowerDatabase] Parse error on row {row.GetValueOrDefault("ID", "?")}: {e.Message}");
            return null;
        }
    }


    /// <summary>Every level-1 tower of the given tier (empty list if none).</summary>
    public List<TowerRecord> GetBaseLevel(int tier)
    {
        return _baseLevelByTier.TryGetValue(tier, out var list) ? list : new List<TowerRecord>();
    }

    /// <summary>Every level-1 tower across all tiers, sorted by tier then ID.</summary>
    public List<TowerRecord> GetAllBaseLevel()
    {
        return _byID.Values.Where(r => r.IsBaseLevel).OrderBy(r => r.tier).ThenBy(r => r.id).ToList();
    }

    /// <summary>The tower with exactly this ID, or null.</summary>
    public TowerRecord GetByID(string id)
    {
        return _byID.TryGetValue(id, out var r) ? r : null;
    }

    /// <summary>The next upgrade level, e.g. T1A1 -> T1A2. Returns null at max level.</summary>
    public TowerRecord GetNextLevel(string currentID)
    {
        var current = GetByID(currentID);
        if (current == null) return null;
        string nextID = current.BaseID + (current.level + 1);
        return GetByID(nextID);
    }

    /// <summary>Every level of one tower, e.g. "T1A" -> [T1A1, T1A2, T1A3].</summary>
    public List<TowerRecord> GetAllLevels(string baseID)
    {
        return _byID.Values
            .Where(r => r.BaseID == baseID)
            .OrderBy(r => r.level)
            .ToList();
    }


    // Lenient number parsing: thousands separators are ignored, invalid text becomes 0.
    private int ParseIntSafe(string s)
    {
        return int.TryParse(s.Replace(",", ""), out int v) ? v : 0;
    }

    private float ParseFloatSafe(string s)
    {
        return float.TryParse(s, out float v) ? v : 0f;
    }
}