using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DataManager — Singleton โหลดและ cache CSV จาก Assets/Resources/Data/
/// 
/// วิธีใช้:
///   var rows = DataManager.Instance.GetTableData("towers");
///   // rows = List ของแต่ละแถว, แต่ละแถว = Dictionary ชื่อ column → ค่า
/// 
/// วางไฟล์ CSV ที่: Assets/Resources/Data/towers.csv
/// เรียกใช้:        DataManager.Instance.GetTableData("towers")
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // cache เก็บข้อมูลที่โหลดแล้ว — ไม่โหลดซ้ำถ้าเคยโหลดแล้ว
    private Dictionary<string, List<Dictionary<string, string>>> _cache
        = new Dictionary<string, List<Dictionary<string, string>>>();

    // ─────────────────────────────────────────────
    // SINGLETON
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// โหลด CSV แล้ว return เป็น List ของแต่ละแถว
    /// แต่ละแถวเป็น Dictionary: ชื่อ column → ค่าในช่องนั้น
    ///
    /// ตัวอย่าง:
    ///   rows[0]["ID"]           → "T1A1"
    ///   rows[0]["Name (EN)"]    → "Forest Knight"
    ///   rows[0]["Cost (Crystal)"] → "75"
    /// </summary>
    /// <param name="fileName">ชื่อไฟล์ใน Resources/Data/ ไม่ต้องใส่ .csv</param>
    public List<Dictionary<string, string>> GetTableData(string fileName)
    {
        // return จาก cache ถ้าโหลดแล้ว
        if (_cache.TryGetValue(fileName, out var cached))
            return cached;

        // โหลดจาก Resources/Data/<fileName>
        TextAsset asset = Resources.Load<TextAsset>($"Data/{fileName}");

        if (asset == null)
        {
            Debug.LogError($"[DataManager] ไม่พบไฟล์: Resources/Data/{fileName}.csv");
            return null;
        }

        var result = ParseCSV(asset.text);
        _cache[fileName] = result;

        Debug.Log($"[DataManager] โหลด '{fileName}' สำเร็จ — {result.Count} rows");
        return result;
    }

    /// <summary>ล้าง cache ของไฟล์นั้น (ถ้าต้องการโหลดใหม่)</summary>
    public void ClearCache(string fileName)
    {
        if (_cache.ContainsKey(fileName))
            _cache.Remove(fileName);
    }

    /// <summary>ล้าง cache ทั้งหมด</summary>
    public void ClearAllCache()
    {
        _cache.Clear();
    }

    // ─────────────────────────────────────────────
    // CSV PARSER
    // ─────────────────────────────────────────────

    private List<Dictionary<string, string>> ParseCSV(string text)
    {
        var result = new List<Dictionary<string, string>>();

        // แยกบรรทัด รองรับทั้ง \r\n และ \n
        string[] lines = text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

        if (lines.Length < 2)
        {
            Debug.LogWarning("[DataManager] CSV มีน้อยกว่า 2 บรรทัด (header + data)");
            return result;
        }

        // บรรทัดแรก = header
        string[] headers = ParseLine(lines[0]);

        // บรรทัดที่เหลือ = data
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // ข้ามบรรทัดว่าง
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = ParseLine(line);

            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                string key = headers[j].Trim();
                string value = j < values.Length ? values[j].Trim() : "";
                row[key] = value;
            }

            result.Add(row);
        }

        return result;
    }

    /// <summary>
    /// แยก CSV line ทีละ field รองรับ cell ที่มี comma อยู่ในเครื่องหมาย "..."
    /// เช่น  T2A1,"Wildfire Tower",2,1,"30% Cleanse, clears tile"
    /// </summary>
    private string[] ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuote = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // "" ภายใน quoted field = escape สำหรับ "
                if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // ข้าม quote อันที่สอง
                }
                else
                {
                    inQuote = !inQuote;
                }
            }
            else if (c == ',' && !inQuote)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        // field สุดท้าย
        fields.Add(current.ToString());

        return fields.ToArray();
    }
}