// ═══════════════════════════════════════════════════════════════
//  GameEvents.cs — static event bus ของ Verdant
//  ระบบมลพิษ: ทุกระบบคุยกันผ่าน event ตรงนี้ ไม่อ้างอิงกันตรงๆ
// ═══════════════════════════════════════════════════════════════
using System;

public static class GameEvents
{
    // ── Pollution ──
    public static event Action<float> OnPollutionChanged;   // ค่าเกจใหม่ 0..1
    public static event Action OnPollutionCleared;           // หมอกเหลือ 0% → ชนะ
    public static event Action OnPollutionFull;              // หมอกเต็ม 100% → เกมโอเวอร์

    public static event Action<float> OnEnemyKilled;         // ส่งค่า cleanse ของตัวที่ตาย
    public static event Action<float> OnCrystalDamaged;      // ส่งดาเมจที่คริสตัลโดน

    public static void RaisePollutionChanged(float v) => OnPollutionChanged?.Invoke(v);
    public static void RaisePollutionCleared()        => OnPollutionCleared?.Invoke();
    public static void RaisePollutionFull()           => OnPollutionFull?.Invoke();
    public static void RaiseEnemyKilled(float cleanse) => OnEnemyKilled?.Invoke(cleanse);
    public static void RaiseCrystalDamaged(float dmg)  => OnCrystalDamaged?.Invoke(dmg);
}
