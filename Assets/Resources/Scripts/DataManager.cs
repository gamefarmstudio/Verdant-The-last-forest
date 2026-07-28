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

        // ตัด UTF-8 BOM (U+FEFF) ที่หัวไฟล์ (Excel เซฟ "CSV UTF-8" จะติด BOM มา
        // ทำให้ header ตัวแรกกลายเป็น "﻿ID" แล้ว row["ID"] หาไม่เจอทั้งไฟล์)
        text = text.TrimStart('﻿');

        // parse ทั้งไฟล์ทีเดียว → รองรับ newline ที่อยู่ในเซลล์ครอบ quote ได้
        var rows = ParseRows(text);
        if (rows.Count < 2)
        {
            Debug.LogWarning("[DataManager] CSV มีน้อยกว่า 2 บรรทัด (header + data)");
            return result;
        }

        var headers = rows[0];
        for (int i = 0; i < headers.Count; i++) headers[i] = headers[i].Trim();

        for (int i = 1; i < rows.Count; i++)
        {
            var values = rows[i];

            // ข้ามบรรทัดว่าง (มี field เดียวและว่าง)
            if (values.Count == 1 && string.IsNullOrEmpty(values[0].Trim())) continue;

            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count; j++)
                row[headers[j]] = j < values.Count ? values[j].Trim() : "";

            result.Add(row);
        }

        return result;
    }

    /// <summary>
    /// แยกทั้งไฟล์ CSV เป็น list ของแถว (แต่ละแถว = list ของ field)
    /// รองรับ: comma/newline ในเซลล์ครอบ quote, escape "" , ทั้ง \r\n และ \n
    /// เช่น  T2A1,"Wildfire Tower",2,"30% Cleanse,\nclears tile"
    /// </summary>
    private List<List<string>> ParseRows(string text)
    {
        var rows = new List<List<string>>();
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuote = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuote && i + 1 < text.Length && text[i + 1] == '"')
                {
                    current.Append('"');   // "" = escape ของ "
                    i++;
                }
                else inQuote = !inQuote;
            }
            else if (c == ',' && !inQuote)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else if ((c == '\n' || c == '\r') && !inQuote)
            {
                // ปิดแถวเมื่อเจอ newline นอก quote — ข้าม \n ของคู่ \r\n
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                fields.Add(current.ToString());
                current.Clear();
                rows.Add(fields);
                fields = new List<string>();
            }
            else current.Append(c);
        }

        // field/แถวสุดท้าย (ไฟล์ที่ไม่ได้ลงท้ายด้วย newline)
        if (current.Length > 0 || fields.Count > 0)
        {
            fields.Add(current.ToString());
            rows.Add(fields);
        }

        return rows;
    }
}