/// <summary>
/// Data class เก็บข้อมูลป้อม 1 แถวจาก CSV
/// ตรงกับ columns: ID,Name (EN),Name (TH),Tier,Level,Cost (Crystal),
///                  Cumulative Cost,Damage,Atk Speed (s),DPS,Range (tiles),
///                  HP,Special Ability,Asset (Tiny Sword)
/// </summary>
[System.Serializable]
public class TowerRecord
{
    public string id;               // T1A1, T1A2, ...
    public string nameEN;           // Forest Knight
    public string nameTH;           // อัศวินป่า
    public int tier;             // 1, 2, 3
    public int level;            // 1, 2, 3
    public int cost;             // Cost (Crystal)
    public int cumulativeCost;   // Cumulative Cost
    public float damage;
    public float atkSpeed;         // seconds per attack
    public float dps;
    public float range;            // tiles
    public int hp;
    public string specialAbility;
    public string assetName;        // ชื่อ sprite ใน Tiny Sword asset

    /// <summary>Base ID ของป้อมนี้ (ตัด level ออก) เช่น T1A1→T1A, T2B3→T2B</summary>
    public string BaseID => id.Length >= 3 ? id.Substring(0, 3) : id;

    /// <summary>เป็น Level 1 (ป้อมเริ่มต้นที่ซื้อได้) ไหม</summary>
    public bool IsBaseLevel => level == 1;

    /// <summary>ราคา upgrade จาก level ก่อนหน้า</summary>
    public int UpgradeCost => level == 1 ? cost : cost; // cost column คือราคา upgrade แต่ละ level อยู่แล้ว

    public override string ToString() =>
        $"[{id}] {nameEN} Tier{tier} Lv{level} | Cost:{cost} | DMG:{damage} | Range:{range}";
}