using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }
    private Dictionary<string, List<Dictionary<string, string>>> _cache
        = new Dictionary<string, List<Dictionary<string, string>>>();


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }


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

    public void ClearCache(string fileName)
    {
        if (_cache.ContainsKey(fileName))
            _cache.Remove(fileName);
    }
    public void ClearAllCache()
    {
        _cache.Clear();
    }



    private List<Dictionary<string, string>> ParseCSV(string text)
    {
        var result = new List<Dictionary<string, string>>();

        text = text.TrimStart('﻿');

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

            if (values.Count == 1 && string.IsNullOrEmpty(values[0].Trim())) continue;

            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count; j++)
                row[headers[j]] = j < values.Count ? values[j].Trim() : "";

            result.Add(row);
        }

        return result;
    }

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
                    current.Append('"');  
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
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                fields.Add(current.ToString());
                current.Clear();
                rows.Add(fields);
                fields = new List<string>();
            }
            else current.Append(c);
        }

        if (current.Length > 0 || fields.Count > 0)
        {
            fields.Add(current.ToString());
            rows.Add(fields);
        }

        return rows;
    }
}