using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads enemies.csv and bosses.csv through the existing DataManager
/// and indexes them so EnemyBase / WaveManager can look them up easily.
///
/// Usage:
///   EnemyDatabase.Instance.GetEnemyByID("E1")
///   EnemyDatabase.Instance.GetBossByID("B1")
///   EnemyDatabase.Instance.GetEnemiesForWave(3)   -> enemies unlocked by this wave
///   EnemyDatabase.Instance.GetBossForWave(5)      -> this wave's boss (if any)
/// </summary>
public class EnemyDatabase : MonoBehaviour
{
    public static EnemyDatabase Instance { get; private set; }

    [Header("CSV Settings")]
    [Tooltip("File names in Resources/Data/ (without .csv)")]
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
        // Load in Start, not Awake — wait for DataManager.Awake() to finish first.
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
            Debug.LogError($"[EnemyDatabase] Could not load '{enemiesCsvName}' — check the file in Resources/Data/");
            return;
        }

        _enemyByID.Clear();

        foreach (var row in rawRows)
        {
            var record = ParseEnemyRow(row);
            if (record != null) _enemyByID[record.id] = record;
        }

        Debug.Log($"[EnemyDatabase] Loaded enemies successfully: {_enemyByID.Count}");
    }

    // Converts one enemies.csv row into an EnemyRecord. Returns null (and logs) on a bad row.
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
            Debug.LogWarning($"[EnemyDatabase] Parse error on enemy row {row.GetValueOrDefault("ID", "?")}: {e.Message}");
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
            Debug.LogWarning($"[EnemyDatabase] '{bossesCsvName}' not found — skipping boss loading (the file may not exist yet)");
            return;
        }

        _bossByID.Clear();
        _bossByWave.Clear();

        foreach (var row in rawRows)
        {
            var record = ParseBossRow(row);
            if (record == null) continue;

            _bossByID[record.id] = record;

            // Stop two bosses on the same wave from silently overwriting each other — keep the first, warn about the clash.
            if (_bossByWave.TryGetValue(record.WaveNumber, out var existing))
                Debug.LogWarning($"[EnemyDatabase] Wave {record.WaveNumber} has duplicate bosses: " +
                                 $"'{existing.id}' (kept) and '{record.id}' (skipped) — check bosses.csv");
            else
                _bossByWave[record.WaveNumber] = record;
        }

        Debug.Log($"[EnemyDatabase] Loaded bosses successfully: {_bossByID.Count}");
    }

    // Converts one bosses.csv row into a BossRecord. Returns null (and logs) on a bad row.
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
            Debug.LogWarning($"[EnemyDatabase] Parse error on boss row {row.GetValueOrDefault("ID", "?")}: {e.Message}");
            return null;
        }
    }

    // ─────────────────────────────────────────────────
    // QUERY API — ENEMY
    // ─────────────────────────────────────────────────

    /// <summary>Gets one enemy by ID, or null.</summary>
    public EnemyRecord GetEnemyByID(string id)
    {
        return _enemyByID.TryGetValue(id, out var r) ? r : null;
    }

    /// <summary>Gets every enemy unlocked by this wave (firstAppearsWave &lt;= wave).</summary>
    public List<EnemyRecord> GetEnemiesForWave(int wave)
    {
        return _enemyByID.Values
            .Where(e => e.firstAppearsWave <= wave)
            .OrderBy(e => e.firstAppearsWave)
            .ToList();
    }

    /// <summary>Gets every enemy, sorted by the wave they first appear in.</summary>
    public List<EnemyRecord> GetAllEnemies()
    {
        return _enemyByID.Values.OrderBy(e => e.firstAppearsWave).ToList();
    }

    // ─────────────────────────────────────────────────
    // QUERY API — BOSS
    // ─────────────────────────────────────────────────

    /// <summary>Gets one boss by ID, or null.</summary>
    public BossRecord GetBossByID(string id)
    {
        return _bossByID.TryGetValue(id, out var r) ? r : null;
    }

    /// <summary>Gets this wave's boss — null if the wave has no boss.</summary>
    public BossRecord GetBossForWave(int wave)
    {
        return _bossByWave.TryGetValue(wave, out var r) ? r : null;
    }

    /// <summary>Checks whether this wave has a boss.</summary>
    public bool HasBossOnWave(int wave) => _bossByWave.ContainsKey(wave);

    /// <summary>Gets every boss, sorted by wave.</summary>
    public List<BossRecord> GetAllBosses()
    {
        return _bossByID.Values.OrderBy(b => b.WaveNumber).ToList();
    }

    // ─────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────

    // Lenient number parsing: blank or invalid text becomes 0, thousands separators are ignored.
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
