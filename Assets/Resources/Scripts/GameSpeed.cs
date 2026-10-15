using UnityEngine;

// เก็บ "ความเร็วเกมที่ผู้เล่นเลือก" (1x/2x) ไว้ที่เดียว
// ให้ที่อื่นที่ต้องคืนค่า Time.timeScale (เช่น TowerBuildPopup/TowerUpgradePopup ตอนปิดเมนู,
// GameManager ตอน Resume) อ้างอิงจากนี่แทนที่จะ hardcode 1f — กันสปีดที่เลือกไว้โดนรีเซ็ตทิ้ง
public static class GameSpeed
{
    public static float Current { get; private set; } = 1f;

    /// <summary>ตั้งสปีดใหม่ + apply ทันที (เรียกจากปุ่ม toggle)</summary>
    public static void SetSpeed(float speed)
    {
        Current = speed;
        Time.timeScale = speed;
    }

    /// <summary>คืน timeScale กลับไปที่สปีดปัจจุบัน — เรียกตอนปิดเมนู/popup ที่เคย override timeScale ไว้ชั่วคราว</summary>
    public static void RestoreCurrent() => Time.timeScale = Current;
}
