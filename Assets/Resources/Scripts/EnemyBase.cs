using System.Collections;
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

    public int   MaxHP        { get;  set; }
    public int   CurrentHP    { get;  set; }
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
            MaxHP = record.hp;   // boss มี HP สูงมาก → slider ต้องปรับ maxValue ให้ถูกต้อง
            CurrentHP = record.hp;
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
            MaxHP = record.hp;   // enemy ปกติ → slider ต้องปรับ maxValue ให้ถูกต้อง
            CurrentHP = record.hp;
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
        {
            RemoveByPlayer(RewardKill, "killed");   // ตายจากดาเมจ → reward kill
            return;
        }

        FlashOnHit();   // [เพิ่ม] วาบสีขาวสั้นๆ ให้เห็นว่าโดนตี (ข้ามถ้าตายไปแล้ว เพราะ object กำลังจะถูก Destroy)
    }

    // ─────────────────────────────────────────────
    // [เพิ่ม] HIT FLASH — วาบสีตอนโดนโจมตี (enemy ปกติ + boss ใช้ร่วมกัน)
    // ─────────────────────────────────────────────

    [Header("Hit Flash")]
    // [แก้] Color.white เฉยๆ (1,1,1,1) เป็นค่า tint ปกติของ sprite อยู่แล้ว — คูณสีขาวเข้าไปเลยไม่เห็นผลอะไร
    //      ใช้ค่าเกิน 1 แทน (เช่น 6,6,6,1) — shader ของ sprite คูณ texture ด้วยค่านี้
    //      ทำให้พิกเซลที่ไม่ได้ดำสนิทถูก "โอเวอร์เอ็กซ์โพส" จนขาวจ้า ได้เอฟเฟกต์วาบขาวจริงๆ
    //      (พิกเซลที่ดำสนิท (0,0,0) จะยังดำอยู่เสมอ ไม่ว่าคูณเท่าไหร่ก็ตาม — เช่น เส้น outline สีดำ)
    [SerializeField] private Color hitFlashColor = new Color(2.2f, 2.2f, 2.2f, 1f);
    [SerializeField] private float hitFlashDuration = 0.1f;

    private SpriteRenderer[] _spriteRenderers;
    private Color[] _originalColors;
    private Coroutine _flashRoutine;
    void Start()
    {
        if (enemyRecord == null && bossRecord == null)
            Debug.LogWarning($"[EnemyBase] {gameObject.name} ยังไม่ได้ ApplyRecord() หรือ ApplyBossRecord()");
            hitFlashColor = new Color(2.2f, 2.2f, 2.2f, 1f);   // safety — ถ้าไม่ตั้งใน inspector ให้ใช้ค่า default
    }

    private void CacheSpriteRenderers()
    {
        if (_spriteRenderers != null) return;   // แคชครั้งเดียวพอ — สีต้นฉบับต้องมาจากก่อนวาบครั้งแรกเท่านั้น

        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        _originalColors  = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _originalColors[i] = _spriteRenderers[i].color;
    }

    private void FlashOnHit()
    {
        CacheSpriteRenderers();
        if (_spriteRenderers.Length == 0) return;

        // โดนซ้ำระหว่างที่ยังวาบอยู่ → หยุดรอบเก่าแล้วเริ่มใหม่ (รีเฟรชเวลา ไม่ซ้อนกันจนสีค้าง)
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // [แก้] เดิม set สีครั้งเดียวแล้ว WaitForSeconds — แต่ enemy พวกนี้เป็น rig ที่มี Animator
        // ตั้ง Write Defaults ไว้ (เช็คใน .controller แล้ว) ซึ่งจะ "เขียนสีทับกลับ" ทุกเฟรมที่ Animator
        // ประมวลผล ทำให้สีที่ set ไว้ครั้งเดียวโดนทับจนมองไม่เห็นเลยแม้แต่เฟรมเดียว
        // แก้โดย set สีซ้ำทุกเฟรมตลอดช่วง flash แทน — สคริปต์รันหลัง Animator ในแต่ละเฟรมเสมอ
        // จึงชนะการเขียนทับของ Animator ได้ทุกครั้ง ไม่ว่าสาเหตุจริงจะมาจาก Write Defaults หรือระบบอื่น
        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            for (int i = 0; i < _spriteRenderers.Length; i++)
                if (_spriteRenderers[i] != null) _spriteRenderers[i].color = hitFlashColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < _spriteRenderers.Length; i++)
            if (_spriteRenderers[i] != null) _spriteRenderers[i].color = _originalColors[i];

        _flashRoutine = null;
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

    // ─────────────────────────────────────────────
    // [เพิ่ม] ENGAGEMENT — ถูกอัศวิน (KnightUnit) บล็อก
    //   ถูกบล็อก → EnemyMovement หยุดเดิน (เช็ค IsBlocked) + หันมาตีอัศวินทุก meleeAttackInterval
    //   อัศวินตาย → หลุดบล็อก เดินต่อได้
    // ─────────────────────────────────────────────

    [Header("Melee (ตอนถูกอัศวินบล็อก)")]
    [SerializeField] private float meleeAttackInterval = 1f;   // วินาที/นัด (ดาเมจ = BaseDamage)

    private KnightUnit _blocker;
    private float _meleeTimer;

    public bool IsBlocked => _blocker != null && _blocker.IsAlive;
    public KnightUnit Blocker => _blocker;

    /// <summary>เรียกจาก KnightUnit เมื่อเข้าปะทะ — ศัตรูหยุดเดินแล้วตีอัศวิน</summary>
    public void EngageWith(KnightUnit knight)
    {
        if (_blocker == knight) return;   // ปะทะอยู่แล้ว ไม่รีเซ็ต timer
        _blocker = knight;
        _meleeTimer = meleeAttackInterval;
    }

    public void Disengage() => _blocker = null;

    // ─────────────────────────────────────────────
    // [เพิ่ม] STATUS — slow / freeze (จากป้อม Crystal Spring)
    // ─────────────────────────────────────────────

    private float _slowFactor = 1f;   // 1 = ปกติ, 0.6 = ช้าลง 40%
    private float _slowTimer;
    private bool  _frozen;
    private float _freezeTimer;

    /// <summary>ตัวคูณความเร็ว (EnemyMovement เอาไปคูณ) — 0 ตอน freeze</summary>
    public float SpeedMultiplier => _frozen ? 0f : _slowFactor;

    /// <summary>ติด slow — slowPercent 0.4 = ช้าลง 40% · เอาอันที่ช้ากว่า และรีเฟรชเวลา</summary>
    public void ApplySlow(float slowPercent, float duration)
    {
        float factor = Mathf.Clamp01(1f - slowPercent);
        if (factor <= _slowFactor || _slowTimer <= 0f) _slowFactor = factor;
        _slowTimer = Mathf.Max(_slowTimer, duration);
    }

    /// <summary>ติด freeze — หยุดนิ่งชั่วคราว</summary>
    public void ApplyFreeze(float duration)
    {
        _frozen = true;
        _freezeTimer = Mathf.Max(_freezeTimer, duration);
    }

    void Update()
    {
        if (IsDead) return;

        // [เพิ่ม] นับเวลา slow/freeze แล้วคืนสภาพเมื่อหมดเวลา
        if (_slowTimer > 0f)   { _slowTimer   -= Time.deltaTime; if (_slowTimer <= 0f)   _slowFactor = 1f; }
        if (_freezeTimer > 0f) { _freezeTimer -= Time.deltaTime; if (_freezeTimer <= 0f) _frozen = false; }

        // ตีอัศวินตอนถูกบล็อก (เดิม)
        if (_blocker == null) return;
        if (!_blocker.IsAlive) { _blocker = null; return; }

        _meleeTimer -= Time.deltaTime;
        if (_meleeTimer <= 0f)
        {
            _meleeTimer = meleeAttackInterval;
            this.GetComponent<Animator>()?.SetTrigger("IsAttacking");   // trigger animation
            _blocker.TakeDamage(BaseDamage);   // ตีอัศวิน
        }
    }
}
