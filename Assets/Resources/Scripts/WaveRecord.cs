using System.Collections.Generic;

/// <summary>
/// One spawn group inside a wave (one row of waves_enemies.csv),
/// e.g. "spawn 10 x E1".
/// </summary>
[System.Serializable]
public class WaveSpawnEntry
{
    public string enemyID;    // E1..E7 for enemies, B1..B4 for bosses
    public int    count;      // how many to spawn
    public int    spawnOrder; // groups spawn in ascending order

    /// <summary>Boss IDs start with "B".</summary>
    public bool IsBoss => !string.IsNullOrEmpty(enemyID) &&
                          (enemyID[0] == 'B' || enemyID[0] == 'b');
}

/// <summary>
/// One wave: its settings from waves.csv plus its spawn groups from waves_enemies.csv.
/// </summary>
[System.Serializable]
public class WaveRecord
{
    public string waveID;         // e.g. "W3"
    public int    waveNumber;     // parsed from waveID, e.g. 3
    public string type;           // "Standard", "BOSS", ...
    public float  spawnInterval;  // seconds between spawns (0 = use WaveManager's default)
    public int    totalEnemies;   // design note only — the real count comes from spawns
    public int    reward;         // crystals awarded when the wave is cleared
    public string difficulty;
    public string notes;

    public List<WaveSpawnEntry> spawns = new List<WaveSpawnEntry>();

    /// <summary>True when the wave type contains "BOSS".</summary>
    public bool IsBossWave => !string.IsNullOrEmpty(type) &&
                              type.ToUpperInvariant().Contains("BOSS");

    /// <summary>True when at least one spawn group is defined for this wave.</summary>
    public bool HasComposition => spawns != null && spawns.Count > 0;

    public override string ToString() =>
        $"[{waveID}] {type} | spawns:{spawns.Count} groups | reward:{reward}";
}
