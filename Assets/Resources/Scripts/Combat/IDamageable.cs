using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// อินเทอร์เฟซกลางสำหรับ "ทุกสิ่งที่รับดาเมจได้" (ศัตรู ฯลฯ)
    /// ป้อมและลูกธนูจะคุยกับศัตรูผ่านอินเทอร์เฟซนี้เท่านั้น
    /// ทำให้ระบบยิงไม่ผูกกับคลาสศัตรูตัวใดตัวหนึ่ง — เปลี่ยนศัตรูได้อิสระ
    ///
    /// ในโปรเจกต์ Verdant: EnemyBase เป็นตัว implement อินเทอร์เฟซนี้
    /// </summary>
    public interface IDamageable
    {
        /// <summary>ตำแหน่งของเป้าหมาย (ให้ลูกธนูเล็งตาม)</summary>
        Transform Transform { get; }

        /// <summary>ยังมีชีวิตอยู่ไหม</summary>
        bool IsAlive { get; }

        /// <summary>เลือดปัจจุบัน (ใช้ตอนเล็งโหมด Strongest)</summary>
        float CurrentHealth { get; }

        /// <summary>ความคืบหน้าบนเส้นทาง 0..1 (มาก = ใกล้ฐานเรา) ใช้ตอนเล็งโหมด First/Last</summary>
        float PathProgress { get; }

        /// <summary>รับความเสียหาย</summary>
        void TakeDamage(float amount);
    }
}
