using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveDatabase : MonoBehaviour
{
    public static WaveDatabase Instance { get; private set; }

    [Header("CSV Settings")]
    [SerializeField] private string wavesCsvName = "waves";
    [SerializeField] private string wavesEnemiesCsvName = "waves_enemies";

    private readonly Dictionary<int, WaveRecord> _byNumber = new Dictionary<int, WaveRecord>();

    public bool IsLoaded { get; private set; }
    public int  TotalWaves { get; private set; }        // เวฟมากสุดที่มี metadata
    public int  LastComposedWave { get; private set; }  // เวฟมากสุดที่ "มีองค์ประกอบ" จริง

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
        // 1) metadata ของแต่ละเวฟ
        var waveRows = DataManager.Instance.GetTableData(wavesCsvName);
        if (waveRows == null || waveRows.Count == 0)
        {
            Debug.LogError($"[WaveDatabase] โหลด '{wavesCsvName}' ไม่ได้ — ตรวจสอบ Resources/Data/");
            return;
        }

        _byNumber.Clear();
        foreach (var row in waveRows)
        {
            var rec = ParseWave(row);
            if (rec != null) _byNumber[rec.waveNumber] = rec;
        }

        // 2) องค์ประกอบการ spawn (แต่ละเวฟมีหลายแถว)
        var compRows = DataManager.Instance.GetTableData(wavesEnemiesCsvName);
        if (compRows != null)
        {
            foreach (var row in compRows)
            {
                var entry = ParseEntry(row, out int waveNum);
                if (entry == null) continue;   // ข้ามแถวว่าง/แถว note (count ไม่เป็นตัวเลข)

                if (_byNumber.TryGetValue(waveNum, out var rec))
                    rec.spawns.Add(entry);
                else
                    Debug.LogWarning($"[WaveDatabase] waves_enemies อ้างเวฟ {waveNum} ที่ไม่มีใน waves.csv");
            }

            // เรียง spawn ตาม spawnOrder ในแต่ละเวฟ
            foreach (var rec in _byNumber.Values)
                rec.spawns.Sort((a, b) => a.spawnOrder.CompareTo(b.spawnOrder));
        }
        else
        {
            Debug.LogWarning($"[WaveDatabase] ไม่พบ '{wavesEnemiesCsvName}' — เวฟจะไม่มีองค์ประกอบ");
        }

        TotalWaves = _byNumber.Count > 0 ? _byNumber.Keys.Max() : 0;
        LastComposedWave = _byNumber.Values
            .Where(r => r.HasComposition)
            .Select(r => r.waveNumber)
            .DefaultIfEmpty(0)
            .Max();

        IsLoaded = true;
        Debug.Log($"[WaveDatabase] โหลด {_byNumber.Count} เวฟ | มีองค์ประกอบถึงเวฟ {LastComposedWave}/{TotalWaves}");
    }

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

    /// <summary>แปลง 1 แถวของ waves_enemies เป็น entry — คืน null ถ้าเป็นแถวว่าง/note</summary>
    private WaveSpawnEntry ParseEntry(Dictionary<string, string> row, out int waveNum)
    {
        waveNum = 0;
        string wid = Get(row, "waveID");
        string eid = Get(row, "enemyID");
        if (string.IsNullOrEmpty(wid) || string.IsNullOrEmpty(eid)) return null;

        // count ต้องเป็นตัวเลขบวก (แถว note เช่น "1× The Hollow..." จะไม่ผ่าน)
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

    public WaveRecord GetWave(int waveNumber) =>
        _byNumber.TryGetValue(waveNumber, out var r) ? r : null;

    public bool HasWave(int waveNumber) => _byNumber.ContainsKey(waveNumber);

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private static string Get(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var v) ? v.Trim() : "";

    /// <summary>ดึงเฉพาะตัวเลขจากข้อความ เช่น "W5" → 5</summary>
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
