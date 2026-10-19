using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that loads and caches CSV files from Assets/Resources/Data/.
///
/// Usage:
///   var rows = DataManager.Instance.GetTableData("towers");
///   // rows = one entry per CSV row; each row maps column header -> cell value
///
/// Put the CSV at:  Assets/Resources/Data/towers.csv
/// Load it with:    DataManager.Instance.GetTableData("towers")
/// TowerDatabase, EnemyDatabase and WaveDatabase all read their data through this class.
/// </summary>
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // Cache of already-loaded files — a file is only parsed once.
    private Dictionary<string, List<Dictionary<string, string>>> _cache
        = new Dictionary<string, List<Dictionary<string, string>>>();


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }


    /// <summary>
    /// Loads a CSV and returns its rows. Each row is a Dictionary: column name -> cell value.
    /// Example: rows[0]["ID"] -> "T1A1", rows[0]["Cost (Crystal)"] -> "75".
    /// </summary>
    /// <param name="fileName">File name inside Resources/Data/, without ".csv".</param>
    /// <returns>The rows, or null if the file doesn't exist.</returns>
    public List<Dictionary<string, string>> GetTableData(string fileName)
    {
        // Return the cached copy if this file was already loaded.
        if (_cache.TryGetValue(fileName, out var cached))
            return cached;

        // Load from Resources/Data/<fileName>
        TextAsset asset = Resources.Load<TextAsset>($"Data/{fileName}");

        if (asset == null)
        {
            Debug.LogError($"[DataManager] File not found: Resources/Data/{fileName}.csv");
            return null;
        }

        var result = ParseCSV(asset.text);
        _cache[fileName] = result;

        Debug.Log($"[DataManager] Loaded '{fileName}' successfully — {result.Count} rows");
        return result;
    }

    /// <summary>Forgets the cached copy of one file, so the next GetTableData reloads it.</summary>
    public void ClearCache(string fileName)
    {
        if (_cache.ContainsKey(fileName))
            _cache.Remove(fileName);
    }

    /// <summary>Forgets every cached file.</summary>
    public void ClearAllCache()
    {
        _cache.Clear();
    }



    // Turns CSV text into rows keyed by the header row.
    private List<Dictionary<string, string>> ParseCSV(string text)
    {
        var result = new List<Dictionary<string, string>>();

        // Strip the UTF-8 BOM (U+FEFF) at the start of the file. Excel's "CSV UTF-8" adds one,
        // which would put an invisible character in front of the first header so row["ID"] is never found.
        text = text.TrimStart('﻿');

        // Parse the whole file at once, so newlines inside quoted cells are handled.
        var rows = ParseRows(text);
        if (rows.Count < 2)
        {
            Debug.LogWarning("[DataManager] CSV has fewer than 2 lines (header + data)");
            return result;
        }

        var headers = rows[0];
        for (int i = 0; i < headers.Count; i++) headers[i] = headers[i].Trim();

        for (int i = 1; i < rows.Count; i++)
        {
            var values = rows[i];

            // Skip blank lines (a single empty field).
            if (values.Count == 1 && string.IsNullOrEmpty(values[0].Trim())) continue;

            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count; j++)
                row[headers[j]] = j < values.Count ? values[j].Trim() : "";

            result.Add(row);
        }

        return result;
    }

    /// <summary>
    /// Splits the whole CSV into rows of fields.
    /// Handles: commas/newlines inside quoted cells, "" as an escaped quote, and both \r\n and \n.
    /// e.g.  T2A1,"Wildfire Tower",2,"30% Cleanse,\nclears tile"
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
                    current.Append('"');   // "" = an escaped quote character
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
                // End of row on a newline outside quotes — skip the \n of a \r\n pair.
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                fields.Add(current.ToString());
                current.Clear();
                rows.Add(fields);
                fields = new List<string>();
            }
            else current.Append(c);
        }

        // Last field/row (for files that don't end with a newline).
        if (current.Length > 0 || fields.Count > 0)
        {
            fields.Add(current.ToString());
            rows.Add(fields);
        }

        return rows;
    }
}