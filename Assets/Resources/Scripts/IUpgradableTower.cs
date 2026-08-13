using UnityEngine;

// [เพิ่ม] interface กลางของ "ป้อมที่อัพเกรด/ขายได้"
//   ให้ทั้ง Verdant.Combat.Tower (ยิงไกล) และ MeleeTower (ประชิด) implement
//   ระบบ TowerSlot / TowerPlacementManager / TowerUpgradePopup อ่านผ่าน interface นี้
//   → รองรับป้อมทั้ง 2 ชนิดโดยไม่ต้องรู้ว่าเป็นชนิดไหน
public interface IUpgradableTower
{
    string TowerId { get; }              // ID ใน towers.csv (เช่น T1B1 / T1A1)
    GameObject NextLevelPrefab { get; }  // prefab เลเวลถัดไป (null = เลเวลสูงสุด)
}
