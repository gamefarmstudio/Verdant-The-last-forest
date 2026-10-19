using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads a level's wave data through DataManager:
///   1) the wave settings file (e.g. waves.csv) — interval, reward, type per wave;
///   2) the spawn-list file (e.g. waves_enemies.csv) — which enemies spawn in each wave.
/// The two file names are set per scene in the Inspector (LV1: waves / waves_enemies,
/// LV2: waves_2 / waves_2_enemies, LV3: waves_3 / waves_3_enemies).
/// WaveManager reads waves from here with GetWave(n).
/// </summary>
public class WaveDatabase : MonoBehaviour
{
    public static WaveDatabase Instance { get; private set; }

    [Header("CSV Settings")]
    [SerializeField] private string wavesCsvName = "waves";
    [SerializeField] private string wavesEnemiesCsvName = "waves_enemies";

    private readonly Dictionary<int, WaveRecord> _byNumber = new Dictionary<int, WaveRecord>();

    public bool IsLoaded { get; private set; }
    public int  TotalWaves { get; private set; }        // highest wave number that has settings
    public int  LastComposedWave { get; private set; }  // highest wave number that actually has spawn groups

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        Load();
    }

    // ─────────────────────────────────────────────
    // LOAD
    // ─────────────────────────────────────────────

    private void Load()
    {
        // 1) Settings for each wave
        var waveRows = DataManager.Instance.GetTableData(wavesCsvName);
        if (waveRows == null || waveRows.Count == 0)
        {
            Debug.LogError($"[WaveDatabase] Could not load '{wavesCsvName}' — check Resources/Data/");
            return;
        }

        _byNumber.Clear();
        foreach (var row in waveRows)
        {
            var rec = ParseWave(row);
            if (rec != null) _byNumber[rec.waveNumber] = rec;
        }

        // 2) Spawn groups (each wave can have several rows)
        var compRows = DataManager.Instance.GetTableData(wavesEnemiesCsvName);
        if (compRows != null)
        {
            foreach (var row in compRows)
            {
                var entry = ParseEntry(row, out int waveNum);
                if (entry == null) continue;   // skip blank / note rows (count isn't a number)

                if (_byNumber.TryGetValue(waveNum, out var rec))
                    rec.spawns.Add(entry);
                else
                    Debug.LogWarning($"[WaveDatabase] waves_enemies refers to wave {waveNum}, which is not in waves.csv");
            }

            // Sort each wave's groups by spawnOrder
            foreach (var rec in _byNumber.Values)
                rec.spawns.Sort((a, b) => a.spawnOrder.CompareTo(b.spawnOrder));
        }
        else
        {
            Debug.LogWarning($"[WaveDatabase] '{wavesEnemiesCsvName}' not found — waves will have no spawn groups");
        }

        TotalWaves = _byNumber.Count > 0 ? _byNumber.Keys.Max() : 0;
        LastComposedWave = _byNumber.Values
            .Where(r => r.HasComposition)
            .Select(r => r.waveNumber)
            .DefaultIfEmpty(0)
            .Max();

        IsLoaded = true;
        Debug.Log($"[WaveDatabase] Loaded {_byNumber.Count} waves | spawn groups defined up to wave {LastComposedWave}/{TotalWaves}");
    }

    // Converts one wave-settings row into a WaveRecord (spawns are added later). Null for rows with no waveID.
    private WaveRecord ParseWave(Dictionary<string, string> row)
    {
        try
        {
            string id = Get(row, "waveID");
            if (string.IsNullOrEmpty(id)) return null;

            return new WaveRecord
            {
                waveID        = id,
                waveNumber    = DigitsToInt(id),
                type          = Get(row, "Type"),
                spawnInterval = ParseFloatSafe(Get(row, "Spawn Interval (s)")),
                totalEnemies  = ParseIntSafe(Get(row, "Total Enemies")),
                reward        = ParseIntSafe(Get(row, "Wave Reward (Crystal)")),
                difficulty    = Get(row, "Difficulty Rating"),
                notes         = Get(row, "Notes"),
            };
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[WaveDatabase] parse wave error: {e.Message}");
            return null;
        }
    }

    /// <summary>Converts one waves_enemies row into a spawn entry — returns null for blank / note rows.</summary>
    private WaveSpawnEntry ParseEntry(Dictionary<string, string> row, out int waveNum)
    {
        waveNum = 0;
        string wid = Get(row, "waveID");
        string eid = Get(row, "enemyID");
        if (string.IsNullOrEmpty(wid) || string.IsNullOrEmpty(eid)) return null;

        // count must be a positive number (note rows such as "1× The Hollow..." are rejected)
        if (!int.TryParse(Get(row, "count"), out int cnt) || cnt <= 0) return null;

        waveNum = DigitsToInt(wid);
        return new WaveSpawnEntry
        {
            enemyID    = eid,
            count      = cnt,
            spawnOrder = ParseIntSafe(Get(row, "spawnOrder")),
        };
    }

    // ─────────────────────────────────────────────
    // QUERY
    // ─────────────────────────────────────────────

    /// <summary>The wave with this number, or null if it isn't defined.</summary>
    public WaveRecord GetWave(int waveNumber) =>
        _byNumber.TryGetValue(waveNumber, out var r) ? r : null;

    public bool HasWave(int waveNumber) => _byNumber.ContainsKey(waveNumber);

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    // Reads a cell by column name; missing columns become "".
    private static string Get(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var v) ? v.Trim() : "";

    /// <summary>Keeps only the digits of a string, e.g. "W5" -> 5.</summary>
    private static int DigitsToInt(string s)
    {
        string digits = new string(s.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out int n) ? n : 0;
    }

    private static int ParseIntSafe(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        return int.TryParse(s.Replace(",", ""), out int v) ? v : 0;
    }

    private static float ParseFloatSafe(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0f;
        return float.TryParse(s, out float v) ? v : 0f;
    }
}
