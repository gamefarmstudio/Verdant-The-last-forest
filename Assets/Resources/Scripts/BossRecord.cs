/// <summary>
/// Data class เก็บข้อมูล boss 1 แถวจาก bosses.csv
/// ตรงกับ columns: ID,WaveID,Boss Name (EN),Boss Name (TH),HP,Speed,Damage,
///                  Phases,Special Mechanic,Reward (Crystal),Theme
/// </summary>
[System.Serializable]
public class BossRecord
{
    public string id;                  // B1, B2, ...
    public string waveID;              // "W5" หรือ "10" — wave ที่ boss นี้โผล่ (รูปแบบไม่สม่ำเสมอใน CSV ต้นฉบับ)
    public string nameEN;              // Oil Behemoth
    public string nameTH;              // มหึมาน้ำมัน
    public int    hp;
    public float  speed;
    public int    damage;
    public int    phases;              // จำนวน phase ของ boss นี้ (1-3)
    public string specialMechanic;     // ข้อความอธิบาย mechanic พิเศษ — parse เพิ่มเองถ้าต้องการ logic
    public int    rewardCrystal;
    public string theme;               // เชิงการศึกษา

    /// <summary>ดึงเลข wave ออกมาจาก waveID (รองรับทั้ง "W5" และ "10")</summary>
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
