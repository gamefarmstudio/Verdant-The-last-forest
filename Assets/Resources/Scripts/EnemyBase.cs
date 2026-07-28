using UnityEngine;
using UnityEngine.Events;
using Verdant.Combat;   // IDamageable — ให้ป้อม/ลูกธนูยิงโดนได้

/// <summary>
/// EnemyBase — ติดกับ Enemy Prefab ทุกตัว (ทั้ง normal enemy และ boss)
/// รับ stats จาก CSV ผ่าน ApplyRecord() ทันทีหลัง Instantiate
///
/// ยังไม่มี movement — ตัวนี้ทำแค่ HP, รับ damage, ตาย, แจ้ง GameManager
/// (เพิ่ม EnemyMovement.cs แยกทีหลัง)
///
/// implement IDamageable → ระบบป้อม (Verdant.Combat.Tower) ตรวจจับและยิงได้
/// ⚠ ต้องมี Collider2D บน prefab และตั้ง layer เป็น "Enemy" ป้อมถึงจะเห็น
/// </summary>
public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Runtime Data (set by ApplyRecord)")]
    public EnemyRecord enemyRecord;   // null ถ้าเป็น boss
    public BossRecord  bossRecord;    // null ถ้าเป็น enemy ปกติ

    public bool IsBoss => bossRecord != null;

    // ─────────────────────────────────────────────
    // STATS (อ่านจาก record)
    // ─────────────────────────────────────────────

    public int   MaxHP        { get; private set; }
    public int   CurrentHP    { get; private set; }
    public float MoveSpeed    { get; private set; }
    public int   BaseDamage    { get; private set; }   // damage ให้ player base เมื่อถึงเส้นชัย
    public int   RewardKill    { get; private set; }   // crystal เมื่อฆ่าด้วยดาเมจ (ป้อมทั่วไป)
    public int   RewardCleanse { get; private set; }   // crystal เมื่อ cleanse (Wildfire Tower) — boss = 0
    public bool  IsDead        { get; private set; } = false;

    // ─────────────────────────────────────────────
    // IDamageable — สะพานเชื่อมระบบป้อม (Verdant.Combat)
    // ─────────────────────────────────────────────

    private float _pathProgress;   // 0..1 — ให้ระบบ movement ทีหลังอัปเดตค่านี้

    public Transform Transform => transform;
    public bool      IsAlive   => !IsDead;

    float IDamageable.CurrentHealth => CurrentHP;
    float IDamageable.PathProgress  => _pathProgress;

    // HP เป็น int แต่ดาเมจป้อมเป็น float → ปัดเป็น int (อย่างน้อย 1 กันยิงแล้วไม่ขยับ)
    void IDamageable.TakeDamage(float amount) => TakeDamage(Mathf.Max(1, Mathf.RoundToInt(amount)));

    /// <summary>ให้ระบบเดินทาง (movement/wave) เรียกอัปเดตความคืบหน้าบนเส้นทาง — ใช้กับโหมดเล็ง First/Last</summary>
    public void SetPathProgress(float t) => _pathProgress = Mathf.Clamp01(t);

    // ─────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────

    [Header("Events")]
    public UnityEvent<int> OnHPChanged = new UnityEvent<int>();  // int = HP ใหม่
    public UnityEvent      OnDeath     = new UnityEvent();
    public UnityEvent      OnReachedBase = new UnityEvent();

    // ─────────────────────────────────────────────
    // SETUP — เรียกทันทีหลัง Instantiate
    // ─────────────────────────────────────────────

    /// <summary>ใช้กับ enemy ปกติ — ดึง stats จาก EnemyRecord</summary>
    public void ApplyRecord(EnemyRecord record)
    {
        enemyRecord = record;
        bossRecord  = null;

        MaxHP         = record.hp;
        CurrentHP     = record.hp;
        MoveSpeed     = record.speed;
        BaseDamage    = record.baseDamage;
        RewardKill    = record.rewardKill;
        RewardCleanse = record.rewardCleanse;
        IsDead        = false;

        gameObject.name = $"Enemy_{record.id}_{record.nameEN}";

        OnHPChanged.Invoke(CurrentHP);
    }

    /// <summary>ใช้กับ boss — ดึง stats จาก BossRecord</summary>
    public void ApplyBossRecord(BossRecord record)
    {
        bossRecord  = record;
        enemyRecord = null;

        MaxHP         = record.hp;
        CurrentHP     = record.hp;
        MoveSpeed     = record.speed;
        BaseDamage    = record.damage;
        RewardKill    = record.rewardCrystal;
        RewardCleanse = 0;                     // boss ไม่มีคอลัมน์ cleanse
        IsDead        = false;

        gameObject.name = $"Boss_{record.id}_{record.nameEN}";

        OnHPChanged.Invoke(CurrentHP);
    }

    /// <summary>
    /// โหลด record จาก EnemyDatabase ด้วย ID แล้ว apply ให้ตัวเอง
    /// สะดวกเวลา spawn จาก WaveManager โดยรู้แค่ ID
    /// </summary>
    public void Setup(string id, bool isBoss = false)
    {
        if (isBoss)
        {
            var record = EnemyDatabase.Instance.GetBossByID(id);
            if (record == null)
            {
                Debug.LogError($"[EnemyBase] ไม่พบ Boss ID: {id}");
                return;
            }
            ApplyBossRecord(record);
        }
        else
        {
            var record = EnemyDatabase.Instance.GetEnemyByID(id);
            if (record == null)
            {
                Debug.LogError($"[EnemyBase] ไม่พบ Enemy ID: {id}");
                return;
            }
            ApplyRecord(record);
        }
    }

    // ─────────────────────────────────────────────
    // DAMAGE / DEATH
    // ─────────────────────────────────────────────

    /// <summary>เรียกจาก Tower หรือ Projectile เมื่อโจมตี enemy ตัวนี้</summary>
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged.Invoke(CurrentHP);

        if (CurrentHP <= 0)
            RemoveByPlayer(RewardKill, "killed");   // ตายจากดาเมจ → reward kill
    }

    /// <summary>
    /// ฆ่าแบบ cleanse (เรียกจาก Wildfire Tower) — ให้ crystal เป็น rewardCleanse แทน rewardKill
    /// ทำให้ค่า Reward Cleanse (Crystal) จาก CSV ถูกใช้จริง
    /// </summary>
    public void CleanseKill() => RemoveByPlayer(RewardCleanse, "cleansed");

    /// <summary>เส้นทางการตายที่ผู้เล่นเป็นคนกำจัด (kill/cleanse) — มี crystal reward + หมอกถอย</summary>
    private void RemoveByPlayer(int crystalReward, string cause)
    {
        if (IsDead) return;
        IsDead = true;

        OnDeath.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyEnemyKilled(crystalReward);

        Debug.Log($"[EnemyBase] {gameObject.name} {cause} — reward {crystalReward} crystal");

        Destroy(gameObject);
    }

    /// <summary>
    /// เรียกจาก EnemyMovement (ทีหลัง) เมื่อ enemy เดินถึงปลายทาง
    /// ทำ damage ให้ base แล้วทำลายตัวเอง — ไม่ได้ reward
    /// </summary>
    public void ReachBase()
    {
        if (IsDead) return;
        IsDead = true;

        OnReachedBase.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.DamageBase(BaseDamage);

        Debug.Log($"[EnemyBase] {gameObject.name} reached base — dealt {BaseDamage} damage");

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
