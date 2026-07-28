namespace Verdant.Combat
{
    /// <summary>
    /// รูปแบบการเลือกเป้าหมายของป้อม (มาตรฐานเกมแนว Tower Defense)
    /// </summary>
    public enum TargetingMode
    {
        First,     // เดินหน้าไปไกลสุด (ใกล้ฐานเรามากสุด) — ค่ามาตรฐานของ TD
        Last,      // เพิ่งเข้ามาในระยะ
        Closest,   // อยู่ใกล้ป้อมมากสุด
        Strongest  // เลือดเยอะสุด
    }
}
