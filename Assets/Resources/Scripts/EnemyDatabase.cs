using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// โหลด enemies.csv และ bosses.csv ผ่าน DataManager ที่มีอยู่แล้ว
/// แล้วจัด index ให้ query ได้ง่ายจาก EnemyBase / WaveManager
///
/// วิธีใช้:
///   EnemyDatabase.Instance.GetEnemyByID("E1")
///   EnemyDatabase.Instance.GetBossByID("B1")
///   EnemyDatabase.Instance.GetEnemiesForWave(3)   → enemy ที่ปลดล็อกแล้ว ณ wave นี้
///   EnemyDatabase.Instance.GetBossForWave(5)      → boss ของ wave นี้ (ถ้ามี)
/// </summary>
public class EnemyDatabase : MonoBehaviour
{
    public static EnemyDatabase Instance { get; private set; }

    [Header("CSV Settings")]
    [Tooltip("ชื่อไฟล์ใน Resources/Data/ (ไม่ต้องใส่ .csv)")]
    [SerializeField] private string enemiesCsvName = "enemies";
    [SerializeField] private string bossesCsvName   = "bosses";

    private Dictionary<string, EnemyRecord> _enemyByID = new Dictionary<string, EnemyRecord>();
    private Dictionary<string, BossRecord>  _bossByID  = new Dictionary<string, BossRecord>();
    private Dictionary<int, BossRecord>     _bossByWave= new Dictionary<int, BossRecord>();

    public bool IsLoaded { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // โหลดใน Start ไม่ใช่ Awake — รอให้ DataManager.Awake() วิ่งเสร็จก่อน
        LoadEnemies();
        LoadBosses();
        IsLoaded = true;
    }

    // ─────────────────────────────────────────────────
    // LOAD: ENEMIES
    // ─────────────────────────────────────────────────

    private void LoadEnemies()
    {
        var rawRows = DataManager.Instance.GetTableData(enemiesCsvName);

        if (rawRows == null || rawRows.Count == 0)
        {
            Debug.LogError($"[EnemyDatabase] โหลด '{enemiesCsvName}' ไม่ได้ — ตรวจสอบไฟล์ใน Resources/Data/");
            return;
        }

        _enemyByID.Clear();

        foreach (var row in rawRows)
        {
            var record = ParseEnemyRow(row);
            if (record != null) _enemyByID[record.id] = record;
        }

        Debug.Log($"[EnemyDatabase] โหลด enemies สำเร็จ {_enemyByID.Count} ตัว");
    }

    private EnemyRecord ParseEnemyRow(Dictionary<string, string> row)
    {
        try
        {
            return new EnemyRecord
            {
                id               = row["ID"].Trim(),
                nameEN           = row["Name (EN)"].Trim(),
                nameTH           = row["Name (TH)"].Trim(),
                hp               = ParseIntSafe(row["HP"]),
                speed            = ParseFloatSafe(row["Speed (tiles/s)"]),
                baseDamage       = ParseIntSafe(row["Base Damage"]),
                rewardKill       = ParseIntSafe(row["Reward Kill (Crystal)"]),
                rewardCleanse    = ParseIntSafe(row["Reward Cleanse (Crystal)"]),
                pollutionDrop    = ParseIntSafe(row["Pollution Drop"]),
                firstAppearsWave = ParseIntSafe(row["First Appears (Wave)"]),
                specialAbility   = row["Special Ability"].Trim(),
                assetName        = row["Asset (Tiny Sword)"].Trim(),
                realWorldSymbol  = row.ContainsKey("Real-World Symbol") ? row["Real-World Symbol"].Trim() : "",
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnemyDatabase] Parse error บน enemy row {row.GetValueOrDefault("ID", "?")}: {e.Message}");
            return null;
        }
    }

    // ─────────────────────────────────────────────────
    // LOAD: BOSSES
    // ─────────────────────────────────────────────────

    private void LoadBosses()
    {
        var rawRows = DataManager.Instance.GetTableData(bossesCsvName);

        if (rawRows == null || rawRows.Count == 0)
        {
            Debug.LogWarning($"[EnemyDatabase] ไม่พบ '{bossesCsvName}' — ข้ามการโหลด boss (อาจยังไม่มีไฟล์)");
            return;
        }

        _bossByID.Clear();
        _bossByWave.Clear();

        foreach (var row in rawRows)
        {
            var record = ParseBossRow(row);
            if (record == null) continue;

            _bossByID[record.id] = record;
            _bossByWave[record.WaveNumber] = record;
        }

        Debug.Log($"[EnemyDatabase] โหลด bosses สำเร็จ {_bossByID.Count} ตัว");
    }

    private BossRecord ParseBossRow(Dictionary<string, string> row)
    {
        try
        {
            return new BossRecord
            {
                id               = row["ID"].Trim(),
                waveID           = row["WaveID"].Trim(),
                nameEN           = row["Boss Name (EN)"].Trim(),
                nameTH           = row["Boss Name (TH)"].Trim(),
                hp               = ParseIntSafe(row["HP"]),
                speed            = ParseFloatSafe(row["Speed"]),
                damage           = ParseIntSafe(row["Damage"]),
                phases           = ParseIntSafe(row["Phases"]),
                specialMechanic  = row["Special Mechanic"].Trim(),
                rewardCrystal    = ParseIntSafe(row["Reward (Crystal)"]),
                theme            = row.ContainsKey("Theme") ? row["Theme"].Trim() : "",
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EnemyDatabase] Parse error บน boss row {row.GetValueOrDefault("ID", "?")}: {e.Message}");
            return null;
        }
    }

    // ─────────────────────────────────────────────────
    // QUERY API — ENEMY
    // ─────────────────────────────────────────────────

    /// <summary>ดึง enemy ตัวเดียวจาก ID</summary>
    public EnemyRecord GetEnemyByID(string id)
    {
        return _enemyByID.TryGetValue(id, out var r) ? r : null;
    }

    /// <summary>ดึง enemy ทั้งหมดที่ปลดล็อกแล้ว ณ wave นี้ (firstAppearsWave <= wave)</summary>
    public List<EnemyRecord> GetEnemiesForWave(int wave)
    {
        return _enemyByID.Values
            .Where(e => e.firstAppearsWave <= wave)
            .OrderBy(e => e.firstAppearsWave)
            .ToList();
    }

    /// <summary>ดึง enemy ทั้งหมด</summary>
    public List<EnemyRecord> GetAllEnemies()
    {
        return _enemyByID.Values.OrderBy(e => e.firstAppearsWave).ToList();
    }

    // ─────────────────────────────────────────────────
    // QUERY API — BOSS
    // ─────────────────────────────────────────────────

    /// <summary>ดึง boss ตัวเดียวจาก ID</summary>
    public BossRecord GetBossByID(string id)
    {
        return _bossByID.TryGetValue(id, out var r) ? r : null;
    }

    /// <summary>ดึง boss ของ wave นี้ — null ถ้า wave นี้ไม่มี boss</summary>
    public BossRecord GetBossForWave(int wave)
    {
        return _bossByWave.TryGetValue(wave, out var r) ? r : null;
    }

    /// <summary>เช็คว่า wave นี้มี boss ไหม</summary>
    public bool HasBossOnWave(int wave) => _bossByWave.ContainsKey(wave);

    /// <summary>ดึง boss ทั้งหมด เรียงตาม wave</summary>
    public List<BossRecord> GetAllBosses()
    {
        return _bossByID.Values.OrderBy(b => b.WaveNumber).ToList();
    }

    // ─────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────

    private int ParseIntSafe(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        return int.TryParse(s.Replace(",", ""), out int v) ? v : 0;
    }

    private float ParseFloatSafe(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0f;
        return float.TryParse(s, out float v) ? v : 0f;
    }
}
