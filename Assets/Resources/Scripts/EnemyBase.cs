using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Verdant.Combat;   // IDamageable — lets towers/projectiles hit this enemy

/// <summary>
/// EnemyBase — on every enemy prefab (normal enemies and bosses).
/// Gets its stats from the CSV via Setup() / ApplyRecord() right after being instantiated.
///
/// Handles HP, taking damage (with a short hit flash), dying, reaching the base, being blocked
/// by a knight (and hitting it back), and slow/freeze status effects. It tells GameManager when
/// it dies or reaches the base. Walking along the path is done by EnemyMovement.
///
/// Implements IDamageable -> the tower system (Verdant.Combat.Tower) can detect and shoot it.
/// ⚠ The prefab needs a Collider2D and the "Enemy" layer for towers to see it
///   (WaveManager adds both automatically if they're missing).
/// </summary>
public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Runtime Data (set by ApplyRecord)")]
    public EnemyRecord enemyRecord;   // null if this is a boss
    public BossRecord  bossRecord;    // null if this is a normal enemy

    public bool IsBoss => bossRecord != null;

    // ─────────────────────────────────────────────
    // STATS (read from the record)
    // ─────────────────────────────────────────────

    public int   MaxHP        { get;  set; }
    public int   CurrentHP    { get;  set; }
    public float MoveSpeed    { get; private set; }
    public int   BaseDamage    { get; private set; }   // damage to the player's base when it reaches the end (also its melee damage)
    public int   RewardKill    { get; private set; }   // crystals when killed by damage (normal towers)
    public int   RewardCleanse { get; private set; }   // crystals when cleansed (Wildfire Tower) — 0 for bosses
    public bool  IsDead        { get; private set; } = false;

    // ─────────────────────────────────────────────
    // IDamageable — bridge to the tower system (Verdant.Combat)
    // ─────────────────────────────────────────────

    private float _pathProgress;   // 0..1 — updated by EnemyMovement

    public Transform Transform => transform;
    public bool      IsAlive   => !IsDead;

    float IDamageable.CurrentHealth => CurrentHP;
    float IDamageable.PathProgress  => _pathProgress;

    // HP is an int but tower damage is a float -> round to an int (at least 1, so a hit always does something)
    void IDamageable.TakeDamage(float amount) => TakeDamage(Mathf.Max(1, Mathf.RoundToInt(amount)));

    /// <summary>Called by the movement system to update progress along the path — used by the First/Last targeting modes.</summary>
    public void SetPathProgress(float t) => _pathProgress = Mathf.Clamp01(t);

    // ─────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────

    [Header("Events")]
    public UnityEvent<int> OnHPChanged = new UnityEvent<int>();  // int = new HP
    public UnityEvent      OnDeath     = new UnityEvent();
    public UnityEvent      OnReachedBase = new UnityEvent();

    // ─────────────────────────────────────────────
    // SETUP — called right after Instantiate
    // ─────────────────────────────────────────────

    /// <summary>For normal enemies — takes the stats from an EnemyRecord.</summary>
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

    /// <summary>For bosses — takes the stats from a BossRecord.</summary>
    public void ApplyBossRecord(BossRecord record)
    {
        bossRecord  = record;
        enemyRecord = null;

        MaxHP         = record.hp;
        CurrentHP     = record.hp;
        MoveSpeed     = record.speed;
        BaseDamage    = record.damage;
        RewardKill    = record.rewardCrystal;
        RewardCleanse = 0;                     // bosses have no cleanse column
        IsDead        = false;

        gameObject.name = $"Boss_{record.id}_{record.nameEN}";

        OnHPChanged.Invoke(CurrentHP);
    }

    /// <summary>
    /// Looks up the record in EnemyDatabase by ID and applies it to this enemy.
    /// Convenient when WaveManager spawns an enemy and only knows its ID.
    /// </summary>
    public void Setup(string id, bool isBoss = false)
    {
        if (isBoss)
        {
            var record = EnemyDatabase.Instance.GetBossByID(id);
            if (record == null)
            {
                Debug.LogError($"[EnemyBase] Boss ID not found: {id}");
                return;
            }
            ApplyBossRecord(record);
            MaxHP = record.hp;   // bosses have very high HP -> the health slider's maxValue must match
            CurrentHP = record.hp;
        }
        else
        {
            var record = EnemyDatabase.Instance.GetEnemyByID(id);
            if (record == null)
            {
                Debug.LogError($"[EnemyBase] Enemy ID not found: {id}");
                return;
            }
            ApplyRecord(record);
            MaxHP = record.hp;   // normal enemy -> the health slider's maxValue must match
            CurrentHP = record.hp;
        }

    }

    // ─────────────────────────────────────────────
    // DAMAGE / DEATH
    // ─────────────────────────────────────────────

    /// <summary>Called by a Tower, Projectile, KnightUnit or Wildfire when it hits this enemy.</summary>
    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged.Invoke(CurrentHP);

        if (CurrentHP <= 0)
        {
            RemoveByPlayer(RewardKill, "killed");   // died from damage -> kill reward
            return;
        }

        FlashOnHit();   // short white flash to show the hit (skipped on death, since the object is about to be destroyed)
    }

    // ─────────────────────────────────────────────
    // HIT FLASH — flashes the sprite when hit (shared by enemies and bosses)
    // ─────────────────────────────────────────────

    [Header("Hit Flash")]
    // Plain Color.white (1,1,1,1) is already the sprite's normal tint — multiplying by white changes nothing.
    // So use values above 1 (e.g. 2.2 or 6) — the sprite shader multiplies the texture by this,
    // which "over-exposes" every pixel that isn't pure black into bright white: a real white flash.
    // (Pure black pixels (0,0,0) always stay black however much they're multiplied — e.g. black outlines.)
    [SerializeField] private Color hitFlashColor = new Color(2.2f, 2.2f, 2.2f, 1f);
    [SerializeField] private float hitFlashDuration = 0.1f;

    private SpriteRenderer[] _spriteRenderers;
    private Color[] _originalColors;   // each renderer's colour before the first flash, restored afterwards
    private Coroutine _flashRoutine;
    void Start()
    {
        if (enemyRecord == null && bossRecord == null)
            Debug.LogWarning($"[EnemyBase] {gameObject.name} ApplyRecord() or ApplyBossRecord() has not been called yet");
            // Note: despite the indentation this line is NOT inside the if — it always runs,
            // so it overrides any Hit Flash Color set in the Inspector.
            hitFlashColor = new Color(2.2f, 2.2f, 2.2f, 1f);   // safety — use the default value if nothing was set in the Inspector
    }

    // Finds every SpriteRenderer on the enemy (including children) and remembers their colours. Runs once.
    private void CacheSpriteRenderers()
    {
        if (_spriteRenderers != null) return;   // cache once — the original colours must come from before the first flash

        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        _originalColors  = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _originalColors[i] = _spriteRenderers[i].color;
    }

    private void FlashOnHit()
    {
        CacheSpriteRenderers();
        if (_spriteRenderers.Length == 0) return;

        // Hit again while still flashing -> stop the old flash and start a new one (refreshes the timer, colours never get stuck)
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Setting the colour once and then waiting does NOT work here: these enemies are Animator rigs with
        // Write Defaults on (checked in the .controller), which write the colour back every frame the Animator
        // runs, so a colour set once is overwritten before it is ever visible.
        // Instead the colour is set again every frame for the whole flash — the script runs after the Animator
        // each frame, so it always wins, whether the cause is Write Defaults or something else.
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
    /// Kill by cleansing (meant to be called by the Wildfire Tower) — pays rewardCleanse instead of rewardKill,
    /// so the CSV's Reward Cleanse (Crystal) value is actually used.
    /// </summary>
    public void CleanseKill() => RemoveByPlayer(RewardCleanse, "cleansed");

    /// <summary>Death caused by the player (kill or cleanse) — pays the crystal reward and reports it to GameManager.</summary>
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
    /// Called by EnemyMovement when the enemy reaches the end of the path.
    /// Damages the base and destroys itself — no reward.
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
    // ENGAGEMENT — blocked by a knight (KnightUnit)
    //   Blocked -> EnemyMovement stops walking (checks IsBlocked) and the enemy hits the knight every meleeAttackInterval
    //   Knight dies -> the block is released and the enemy keeps walking
    // ─────────────────────────────────────────────

    [Header("Melee (while blocked by a knight)")]
    [SerializeField] private float meleeAttackInterval = 1f;   // seconds per hit (damage = BaseDamage)

    private KnightUnit _blocker;
    private float _meleeTimer;

    public bool IsBlocked => _blocker != null && _blocker.IsAlive;
    public KnightUnit Blocker => _blocker;

    /// <summary>Called by KnightUnit when it engages — the enemy stops and fights the knight.</summary>
    public void EngageWith(KnightUnit knight)
    {
        if (_blocker == knight) return;   // already engaged — don't reset the timer
        _blocker = knight;
        _meleeTimer = meleeAttackInterval;
    }

    public void Disengage() => _blocker = null;

    // ─────────────────────────────────────────────
    // STATUS — slow / freeze (from the Crystal Spring tower)
    // ─────────────────────────────────────────────

    private float _slowFactor = 1f;   // 1 = normal, 0.6 = 40% slower
    private float _slowTimer;
    private bool  _frozen;
    private float _freezeTimer;

    /// <summary>Speed multiplier (EnemyMovement multiplies by this) — 0 while frozen.</summary>
    public float SpeedMultiplier => _frozen ? 0f : _slowFactor;

    /// <summary>Applies a slow — slowPercent 0.4 = 40% slower. Keeps the stronger slow and refreshes the duration.</summary>
    public void ApplySlow(float slowPercent, float duration)
    {
        float factor = Mathf.Clamp01(1f - slowPercent);
        if (factor <= _slowFactor || _slowTimer <= 0f) _slowFactor = factor;
        _slowTimer = Mathf.Max(_slowTimer, duration);
    }

    /// <summary>Applies a freeze — stops the enemy completely for a while.</summary>
    public void ApplyFreeze(float duration)
    {
        _frozen = true;
        _freezeTimer = Mathf.Max(_freezeTimer, duration);
    }

    void Update()
    {
        if (IsDead) return;

        // Count down slow/freeze and return to normal when they run out
        if (_slowTimer > 0f)   { _slowTimer   -= Time.deltaTime; if (_slowTimer <= 0f)   _slowFactor = 1f; }
        if (_freezeTimer > 0f) { _freezeTimer -= Time.deltaTime; if (_freezeTimer <= 0f) _frozen = false; }

        // Hit the knight while blocked
        if (_blocker == null) return;
        if (!_blocker.IsAlive) { _blocker = null; return; }

        _meleeTimer -= Time.deltaTime;
        if (_meleeTimer <= 0f)
        {
            _meleeTimer = meleeAttackInterval;
            this.GetComponent<Animator>()?.SetTrigger("IsAttacking");   // trigger animation
            _blocker.TakeDamage(BaseDamage);   // hit the knight
        }
    }
}
