using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// โหลด towers.csv ผ่าน DataManager ที่มีอยู่แล้ว
/// แล้วจัด index ให้ query ได้ง่ายจาก TowerSelectionUI
///
/// วิธีใช้:
///   TowerDatabase.Instance.GetBaseLevel(tier)    → ป้อม Lv1 ทั้งหมดของ tier นั้น
///   TowerDatabase.Instance.GetByID("T1A1")       → ป้อมตัวเดียว
///   TowerDatabase.Instance.GetNextLevel("T1A1")  → T1A2 (upgrade)
/// </summary>
public class TowerDatabase : MonoBehaviour
{
    public static TowerDatabase Instance { get; private set; }

    [Header("CSV Settings")]
    [Tooltip("ชื่อไฟล์ CSV ใน Resources/Data/ (ไม่ต้องใส่ .csv)")]
    [SerializeField] private string csvFileName = "towers";

    // Dictionary หลัก: ID → TowerRecord
    private Dictionary<string, TowerRecord> _byID = new Dictionary<string, TowerRecord>();

    // Index: Tier → List ของป้อม Lv1 (ที่ player ซื้อได้ตอนแรก)
    private Dictionary<int, List<TowerRecord>> _baseLevelByTier = new Dictionary<int, List<TowerRecord>>();

    public bool IsLoaded { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // โหลดใน Start ไม่ใช่ Awake — รอให้ DataManager.Awake() วิ่งเสร็จก่อน
        LoadFromCSV();
    }

    // ─────────────────────────────────────────────────
    // LOAD
    // ─────────────────────────────────────────────────

    private void LoadFromCSV()
    {
        // ใช้ DataManager ที่มีอยู่แล้วในโปรเจกต์
        var rawRows = DataManager.Instance.GetTableData(csvFileName);

        if (rawRows == null || rawRows.Count == 0)
        {
            Debug.LogError($"[TowerDatabase] โหลด CSV '{csvFileName}' ไม่ได้ — ตรวจสอบ DataManager");
            return;
        }

        _byID.Clear();
        _baseLevelByTier.Clear();

        foreach (var row in rawRows)
        {
            var record = ParseRow(row);
            if (record == null) continue;

            _byID[record.id] = record;

            // index Tier → base level
            if (record.IsBaseLevel)
            {
                if (!_baseLevelByTier.ContainsKey(record.tier))
                    _baseLevelByTier[record.tier] = new List<TowerRecord>();
                _baseLevelByTier[record.tier].Add(record);
            }
        }

        IsLoaded = true;
        Debug.Log($"[TowerDatabase] โหลดสำเร็จ {_byID.Count} rows | " +
                  $"Tier1:{GetBaseLevel(1).Count} Tier2:{GetBaseLevel(2).Count} Tier3:{GetBaseLevel(3).Count}");
    }

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
            Debug.LogWarning($"[TowerDatabase] Parse error บน row {row.GetValueOrDefault("ID", "?")}: {e.Message}");
            return null;
        }
    }

    // ─────────────────────────────────────────────────
    // QUERY API
    // ─────────────────────────────────────────────────

    /// <summary>ดึงป้อม Lv1 ทั้งหมดของ tier ที่ระบุ (ใช้ใน selection UI)</summary>
    public List<TowerRecord> GetBaseLevel(int tier)
    {
        return _baseLevelByTier.TryGetValue(tier, out var list) ? list : new List<TowerRecord>();
    }

    /// <summary>ดึงป้อม Lv1 ทุก tier รวมกัน</summary>
    public List<TowerRecord> GetAllBaseLevel()
    {
        return _byID.Values.Where(r => r.IsBaseLevel).OrderBy(r => r.tier).ThenBy(r => r.id).ToList();
    }

    /// <summary>ดึงป้อมจาก ID ตรงๆ</summary>
    public TowerRecord GetByID(string id)
    {
        return _byID.TryGetValue(id, out var r) ? r : null;
    }

    /// <summary>ดึง level ถัดไป เช่น T1A1 → T1A2, T1A3 → null (max level)</summary>
    public TowerRecord GetNextLevel(string currentID)
    {
        var current = GetByID(currentID);
        if (current == null) return null;
        string nextID = current.BaseID + (current.level + 1);
        return GetByID(nextID);
    }

    /// <summary>ดึงป้อมทุก level ของ base เดียวกัน เช่น T1A → [T1A1, T1A2, T1A3]</summary>
    public List<TowerRecord> GetAllLevels(string baseID)
    {
        return _byID.Values
            .Where(r => r.BaseID == baseID)
            .OrderBy(r => r.level)
            .ToList();
    }

    // ─────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────

    private int ParseIntSafe(string s)
    {
        return int.TryParse(s.Replace(",", ""), out int v) ? v : 0;
    }

    private float ParseFloatSafe(string s)
    {
        return float.TryParse(s, out float v) ? v : 0f;
    }
}