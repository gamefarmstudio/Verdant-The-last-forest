using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// EnemyBase — ติดกับ Enemy Prefab ทุกตัว (ทั้ง normal enemy และ boss)
/// รับ stats จาก CSV ผ่าน ApplyRecord() ทันทีหลัง Instantiate
///
/// ยังไม่มี movement — ตัวนี้ทำแค่ HP, รับ damage, ตาย, แจ้ง GameManager
/// (เพิ่ม EnemyMovement.cs แยกทีหลัง)
/// </summary>
public class EnemyBase : MonoBehaviour
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
    public int   BaseDamage   { get; private set; }   // damage ให้ player base เมื่อถึงเส้นชัย
    public int   RewardKill   { get; private set; }   // crystal เมื่อ kill
    public bool  IsDead       { get; private set; } = false;

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

        MaxHP      = record.hp;
        CurrentHP  = record.hp;
        MoveSpeed  = record.speed;
        BaseDamage = record.baseDamage;
        RewardKill = record.rewardKill;
        IsDead     = false;

        gameObject.name = $"Enemy_{record.id}_{record.nameEN}";

        OnHPChanged.Invoke(CurrentHP);
    }

    /// <summary>ใช้กับ boss — ดึง stats จาก BossRecord</summary>
    public void ApplyBossRecord(BossRecord record)
    {
        bossRecord  = record;
        enemyRecord = null;

        MaxHP      = record.hp;
        CurrentHP  = record.hp;
        MoveSpeed  = record.speed;
        BaseDamage = record.damage;
        RewardKill = record.rewardCrystal;
        IsDead     = false;

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
            Die();
    }

    /// <summary>เรียกเมื่อ enemy ถูก kill (มี reward)</summary>
    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnDeath.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyEnemyKilled(RewardKill);

        Debug.Log($"[EnemyBase] {gameObject.name} died — reward {RewardKill} crystal");

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
