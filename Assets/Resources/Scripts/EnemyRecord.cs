/// <summary>
/// Data class holding one regular-enemy row from enemies.csv.
/// Matches the columns: ID, Name (EN), Name (TH), HP, Speed (tiles/s), Base Damage,
///                      Reward Kill (Crystal), Reward Cleanse (Crystal), Pollution Drop,
///                      First Appears (Wave), Special Ability, Asset (Tiny Sword), Real-World Symbol
/// </summary>
[System.Serializable]
public class EnemyRecord
{
    public string id;                  // E1, E2, ...
    public string nameEN;              // Smog Skull
    public string nameTH;              // Thai display name
    public int    hp;
    public float  speed;               // tiles/s
    public int    baseDamage;          // damage dealt to the base when it reaches the end of the path
    public int    rewardKill;          // crystals for killing it outright
    public int    rewardCleanse;       // crystals for cleansing it (turning it to our side — Wildfire Tower)
    public int    pollutionDrop;       // pollution released if it reaches the end of the path
    public int    firstAppearsWave;    // first wave this enemy appears in
    public string specialAbility;      // design text only (not implemented in code)
    public string assetName;           // sprite name in the Tiny Swords asset pack
    public string realWorldSymbol;     // educational — the real-world problem this enemy represents

    public override string ToString() =>
        $"[{id}] {nameEN} | HP:{hp} SPD:{speed} DMG:{baseDamage} | Wave:{firstAppearsWave}+";
}
