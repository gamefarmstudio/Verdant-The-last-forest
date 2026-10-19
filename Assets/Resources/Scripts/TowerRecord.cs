
/// <summary>
/// Data class holding one tower row (one tower at one upgrade level) from towers.csv.
/// IDs follow the pattern Tier + Type + Level, e.g. "T1B2" = Tier 1, type B (Greenshot), level 2.
/// </summary>
[System.Serializable]
public class TowerRecord
{
    public string id;               // e.g. T1A1
    public string nameEN;
    public string nameTH;
    public int tier;
    public int level;               // 1..3
    public int cost;                // price to build (level 1) or to upgrade into this level
    public int cumulativeCost;      // total spent to reach this level (used for sell refunds)
    public float damage;
    public float atkSpeed;          // seconds between attacks
    public float dps;
    public float range;             // in tiles / world units
    public int hp;
    public string specialAbility;   // design text only
    public string assetName;

    /// <summary>ID without the level digit, e.g. "T1B2" -> "T1B". Shared by every level of a tower.</summary>
    public string BaseID => id.Length >= 3 ? id.Substring(0, 3) : id;

    /// <summary>True for level 1 — the version the player can build directly.</summary>
    public bool IsBaseLevel => level == 1;

    /// <summary>Cost to upgrade into this level (same as cost).</summary>
    public int UpgradeCost => cost;

    public override string ToString() =>
        $"[{id}] {nameEN} Tier{tier} Lv{level} | Cost:{cost} | DMG:{damage} | Range:{range}";
}