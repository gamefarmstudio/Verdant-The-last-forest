/// <summary>
/// Data class holding one boss row from bosses.csv.
/// Matches the columns: ID, WaveID, Boss Name (EN), Boss Name (TH), HP, Speed, Damage,
///                      Phases, Special Mechanic, Reward (Crystal), Theme
/// </summary>
[System.Serializable]
public class BossRecord
{
    public string id;               // B1, B2, ...
    public string waveID;           // wave the boss belongs to, e.g. "W5"
    public string nameEN;
    public string nameTH;
    public int    hp;
    public float  speed;
    public int    damage;           // damage dealt to the crystal when the boss reaches it
    public int    phases;
    public string specialMechanic;  // design text only (not implemented in code)
    public int    rewardCrystal;    // crystals awarded when the boss is killed
    public string theme;

    /// <summary>Wave number parsed from waveID (digits only, e.g. "W5" -> 5). 0 if none.</summary>
    public int WaveNumber
    {
        get
        {
            string digits = new string(System.Array.FindAll(waveID.ToCharArray(), char.IsDigit));
            return int.TryParse(digits, out int n) ? n : 0;
        }
    }

    public override string ToString() =>
        $"[{id}] {nameEN} | HP:{hp} Phases:{phases} | Wave:{WaveNumber}";
}
