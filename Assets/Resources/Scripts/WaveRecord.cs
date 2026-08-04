using System.Collections.Generic;

[System.Serializable]
public class WaveSpawnEntry
{
    public string enemyID;    
    public int    count;      
    public int    spawnOrder; 

    public bool IsBoss => !string.IsNullOrEmpty(enemyID) &&
                          (enemyID[0] == 'B' || enemyID[0] == 'b');
}

[System.Serializable]
public class WaveRecord
{
    public string waveID;         
    public int    waveNumber;     
    public string type;           
    public float  spawnInterval;  
    public int    totalEnemies;   
    public int    reward;         
    public string difficulty;     
    public string notes;

    public List<WaveSpawnEntry> spawns = new List<WaveSpawnEntry>();

    public bool IsBossWave => !string.IsNullOrEmpty(type) &&
                              type.ToUpperInvariant().Contains("BOSS");

    public bool HasComposition => spawns != null && spawns.Count > 0;

    public override string ToString() =>
        $"[{waveID}] {type} | spawns:{spawns.Count} groups | reward:{reward}";
}
