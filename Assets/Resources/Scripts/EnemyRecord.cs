/// <summary>
/// Data class เก็บข้อมูล enemy ปกติ 1 แถวจาก enemies.csv
/// ตรงกับ columns: ID,Name (EN),Name (TH),HP,Speed (tiles/s),Base Damage,
///                  Reward Kill (Crystal),Reward Cleanse (Crystal),Pollution Drop,
///                  First Appears (Wave),Special Ability,Asset (Tiny Sword),Real-World Symbol
/// </summary>
[System.Serializable]
public class EnemyRecord
{
    public string id;                  // E1, E2, ...
    public string nameEN;              // Smog Skull
    public string nameTH;              // ก็อบลินควันพิษ
    public int    hp;
    public float  speed;               // tiles/s
    public int    baseDamage;          // damage ให้ base เมื่อถึงเส้นชัย
    public int    rewardKill;          // crystal เมื่อ kill ตรงๆ
    public int    rewardCleanse;       // crystal เมื่อ cleanse (เปลี่ยนเป็นพวกเดียวกัน — Wildfire Tower)
    public int    pollutionDrop;       // pollution ที่ปล่อยถ้าหลุดถึงเส้นชัย
    public int    firstAppearsWave;    // wave แรกที่ enemy ตัวนี้โผล่
    public string specialAbility;
    public string assetName;           // ชื่อ sprite ใน Tiny Sword asset
    public string realWorldSymbol;     // เชิงการศึกษา — สิ่งที่ enemy แทนความหมาย

    public override string ToString() =>
        $"[{id}] {nameEN} | HP:{hp} SPD:{speed} DMG:{baseDamage} | Wave:{firstAppearsWave}+";
}
