using UnityEngine;

namespace Verdant.Combat
{
    /// <summary>
    /// ค่าป้อม "สำรอง" (fallback) เก็บเป็น asset (ScriptableObject)
    ///
    /// หมายเหตุ: source of truth จริงของ Verdant คือ towers.csv (ผ่าน TowerDatabase)
    /// asset นี้ใช้เฉพาะตอนทดสอบป้อมเดี่ยว ๆ หรือกรณี CSV โหลดไม่ได้/หา ID ไม่เจอ
    /// (ดู Tower.cs — ถ้าเจอแถวใน CSV จะ override ค่าพวกนี้ทับ)
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData_GreenArrow", menuName = "Verdant/Tower Data (Fallback)")]
    public class TowerData : ScriptableObject
    {
        [Header("ข้อมูลทั่วไป")]
        public string towerName = "Green Arrow";
        [TextArea] public string description = "ป้อมธนูพื้นฐาน ยิงเร็ว ดาเมจกลาง";

        [Header("การโจมตี (fallback — CSV ทับได้)")]
        public float damage = 10f;
        public float range = 3.5f;
        [Tooltip("จำนวนนัดต่อวินาที")]
        public float fireRate = 1.5f;

        [Header("ลูกกระสุน (Arrow)")]
        public Projectile projectilePrefab;
        public float projectileSpeed = 10f;

        /// <summary>ช่วงเวลาพักระหว่างยิง (วินาที) คำนวณจาก fireRate</summary>
        public float FireCooldown => 1f / Mathf.Max(0.01f, fireRate);
    }
}
