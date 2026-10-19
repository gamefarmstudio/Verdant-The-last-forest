using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // (fixed a typo: was "UntyEngine.UI")

/// <summary>
/// The knight — a melee unit summoned by MeleeTower.
///   Moves along the EnemyPath (it doesn't cut straight across the map).
///   Blocking: blocks several enemies that come within blockRadius (up to maxBlock at once); all of them attack the knight.
///   It hits one at a time (the nearest blocked enemy). When the knight dies every enemy is released and MeleeTower is told.
///   Its stats are overwritten by Configure() from towers.csv for the tower's level.
///   Shows its own green health bar and plays an attack sound on each hit.
///   Note: put the knight prefab on a layer that is NOT "Enemy".
/// </summary>
public class KnightUnit : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int damage = 25;
    [SerializeField] private float attackInterval = 1f;   // seconds between hits
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Block")]
    [SerializeField] private float blockRadius = 0.9f;   // blocking radius (overwritten by the CSV range in Configure)
    [SerializeField] private int maxBlock = 3;           // max enemies blocked at the same time

    [Header("Seek")]
    [SerializeField] private float detectRange = 6f;     // how far (around the rally point) it looks for enemies
    [SerializeField] private string enemyLayerName = "Enemy";

    [Header("Path Movement")]
    [SerializeField] private bool moveAlongPath = true;
    [SerializeField] private float leash = 4f;           // max distance from the rally point (measured along the path)

    [Header("Visual")]
    [SerializeField] private bool flipToFaceDirection = true;

    [Header("UI")]
    [Tooltip("Drag in the Canvas prefab that contains a Slider")]
    [SerializeField] private Canvas healthCanvasPrefab;
    [Tooltip("Local position above the knight's head where the health bar is placed")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Animations")]
    [SerializeField] private Animator animator;
    [SerializeField] private string TriggerAttack = "IsAttacking";   // Animator trigger fired on each hit
    [Header("Sound")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioSource audioSource;   // optional — found or added automatically in Awake

    public bool IsAlive => _hp > 0;
    public int CurrentHP => _hp;
    public System.Action OnDeath;   // MeleeTower subscribes to start its respawn timer

    [HideInInspector] public int _hp;
    private float _attackTimer;
    private Vector3 _rallyPoint;    // where the knight is meant to stand
    private ContactFilter2D _filter;
    private readonly List<Collider2D> _hits = new List<Collider2D>();
    private readonly List<EnemyBase> _blocked = new List<EnemyBase>();   // enemies currently being blocked

    // path movement
    private EnemyPath _path;
    private float _rallyDist;   // rally point's distance along the path
    private float _pathDist;    // knight's current distance along the path

    // UI
    private Canvas _healthCanvas;
    private Slider _healthSlider;

    /// <summary>
    /// Sets the stats from outside (MeleeTower loads them from towers.csv) — call before Init.
    /// Note: maxHP is fixed at 100 here (only the current HP comes from the CSV).
    /// </summary>
    public void Configure( int hp, int dmg, float atkInterval, float reach)
    {
        maxHP = 100;
        _hp = hp;
        damage = dmg;
        if (atkInterval > 0f) attackInterval = atkInterval;
        if (reach > 0f) blockRadius = reach;

        // Update the health bar in case Configure is called after Awake
        if (_healthSlider != null)
        {
            _healthSlider.maxValue = maxHP;
            _healthSlider.value = _hp;
        }
    }

    /// <summary>Sets the rally point and places the knight on the path at that point.</summary>
    public void Init(Vector3 rallyPoint)
    {
        _rallyPoint = rallyPoint;
        if (_path != null && _path.IsValid)
        {
            _rallyDist = _path.DistanceAlongPath(rallyPoint);
            _pathDist  = _rallyDist;
            transform.position = _path.PointAtDistance(_pathDist);
        }
    }

    void Awake()
    {
        _hp = maxHP;
        _rallyPoint = transform.position;
        _path = FindAnyObjectByType<EnemyPath>();

        // Only detect colliders on the Enemy layer (including triggers).
        int layer = LayerMask.NameToLayer(enemyLayerName);
        LayerMask mask = layer >= 0 ? (1 << layer) : LayerMask.GetMask(enemyLayerName);
        _filter = new ContactFilter2D();
        _filter.SetLayerMask(mask);
        _filter.useTriggers = true;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        SetupHealthBar();
    }

    private void SetupHealthBar()
    {
        if (healthCanvasPrefab == null)
        {
            Debug.LogError($"[KnightUnit] '{name}' no Canvas prefab has been assigned to the Health Canvas Prefab field", this);
            return;
        }

        // Clone a Canvas just for this knight
        _healthCanvas = Instantiate(healthCanvasPrefab);
        _healthCanvas.transform.SetParent(transform, worldPositionStays: false);
        _healthCanvas.transform.localPosition = healthBarOffset;
        _healthCanvas.transform.localRotation = Quaternion.identity;

        // Give the World Space Canvas a camera (avoids a warning)
        if (_healthCanvas.renderMode == RenderMode.WorldSpace && _healthCanvas.worldCamera == null)
        {
            _healthCanvas.worldCamera = Camera.main;
        }

        _healthSlider = _healthCanvas.GetComponentInChildren<Slider>(true);
        if (_healthSlider == null)
        {
            Debug.LogError($"[KnightUnit] '{name}' cloned the Canvas but found no Slider inside it", this);
            return;
        }

        // Make the health bar green (to tell it apart from enemies, which are usually red)
        var fillImage = _healthSlider.fillRect != null ? _healthSlider.fillRect.GetComponent<Image>() : null;
        if (fillImage != null) fillImage.color = Color.green;

        _healthSlider.interactable = false;

        // Initial values
        _healthSlider.minValue = 0;
        _healthSlider.maxValue = maxHP;
        _healthSlider.value = _hp;
    }

    void Update()
    {
        if (!IsAlive) return;

        UpdateBlockSet();   // add/release blocked enemies based on radius and the cap

        // Where to move: while blocking -> hold position at the nearest blocked enemy (B1 included, so the knight
        // doesn't walk away and lose the block when B1 is the only one); otherwise -> go intercept the nearest enemy.
        EnemyBase moveTarget = _blocked.Count > 0 ? NearestBlockedAny() : NearestEnemyInDetect();

        if (moveAlongPath && _path != null && _path.IsValid)
            MoveAlongPath(moveTarget);
        else
            MoveStraight(moveTarget != null ? moveTarget.transform.position : _rallyPoint);

        // Hit the nearest blocked enemy (one at a time)
        EnemyBase atk = NearestBlocked();
        if (atk != null)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                _attackTimer = attackInterval;
                atk.TakeDamage(damage);
                if (animator != null) animator.SetTrigger(TriggerAttack);
                if (attackSound != null && audioSource != null)
                {
                    audioSource.clip = attackSound;
                    audioSource.PlayOneShot(attackSound);
                }
            }
        }
        else
        {
            // Null-check the animator before using it
            if(animator != null) animator.ResetTrigger(TriggerAttack);
        }
    }

    void LateUpdate()
    {
        // Set the health bar position ourselves every frame so the Canvas doesn't flip when the knight turns (FlipX)
        if (_healthCanvas != null)
        {
            _healthCanvas.transform.position = transform.position + healthBarOffset;
            _healthCanvas.transform.rotation = Quaternion.identity;
        }
    }

    // Manages the blocked set: release dead / out-of-range enemies, then fill back up to the cap.
    void UpdateBlockSet()
    {
        for (int i = _blocked.Count - 1; i >= 0; i--)
        {
            var e = _blocked[i];
            bool out_ = e == null || e.IsDead ||
                        (e.transform.position - transform.position).sqrMagnitude > blockRadius * blockRadius;
            if (out_)
            {
                if (e != null && e.Blocker == this) e.Disengage();
                _blocked.RemoveAt(i);
            }
        }

        if (_blocked.Count >= maxBlock) return;

        int count = Physics2D.OverlapCircle(transform.position, blockRadius, _filter, _hits);
        for (int i = 0; i < count && _blocked.Count < maxBlock; i++)
        {
            var e = _hits[i].GetComponentInParent<EnemyBase>();
            if (e == null || e.IsDead || _blocked.Contains(e)) continue;
            if (e.IsBlocked && e.Blocker != this) continue;   // already blocked by another knight -> skip

            e.EngageWith(this);
            _blocked.Add(e);
        }
    }

    // Picks the attack target — the nearest blocked enemy, skipping B1 (the Smog Dragon boss)
    // while this knight's damage is 60 (the level-3 Forest Knight).
    EnemyBase NearestBlocked()
    {
        EnemyBase best = null; float bestSqr = float.MaxValue;
        for (int i = 0; i < _blocked.Count; i++)
        {
            var e = _blocked[i];
            if (e == null || e.IsDead) continue;
            if (e.gameObject.name.Contains("B1") && damage == 60) continue;   // cannot attack dragon — skip as a candidate entirely
            float sqr = ((Vector2)(e.transform.position - transform.position)).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = e; }
        }
        return best;
    }

    // Picks the target to "hold the line" against — includes B1, so the knight doesn't walk off
    // and lose the block when B1 is the only enemy it's blocking.
    EnemyBase NearestBlockedAny()
    {
        EnemyBase best = null; float bestSqr = float.MaxValue;
        for (int i = 0; i < _blocked.Count; i++)
        {
            var e = _blocked[i];
            if (e == null || e.IsDead) continue;
            float sqr = ((Vector2)(e.transform.position - transform.position)).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = e; }
        }
        return best;
    }

    // Nearest unblocked enemy within detectRange of the rally point (the one the knight walks out to meet).
    EnemyBase NearestEnemyInDetect()
    {
        int count = Physics2D.OverlapCircle(_rallyPoint, detectRange, _filter, _hits);
        EnemyBase best = null; float bestSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var e = _hits[i].GetComponentInParent<EnemyBase>();
            if (e == null || e.IsDead) continue;
            if (e.IsBlocked && e.Blocker != this) continue;
            float sqr = ((Vector2)(e.transform.position - transform.position)).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = e; }
        }
        return best;
    }

    // Walks along the path toward the target (or back to the rally point), never more than "leash" away from it.
    void MoveAlongPath(EnemyBase target)
    {
        float targetDist = _rallyDist;
        if (target != null)
        {
            float tDist = _path.DistanceAlongPath(target.transform.position);
            targetDist = Mathf.Clamp(tDist, _rallyDist - leash, _rallyDist + leash);
        }
        _pathDist = Mathf.MoveTowards(_pathDist, targetDist, moveSpeed * Time.deltaTime);
        Vector3 newPos = _path.PointAtDistance(_pathDist);
        FaceX(newPos.x - transform.position.x);
        transform.position = newPos;
    }

    // Fallback when there's no path: move in a straight line.
    void MoveStraight(Vector3 dest)
    {
        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(before, dest, moveSpeed * Time.deltaTime);
        FaceX(dest.x - before.x);
    }

    // Faces left/right by flipping the X scale.
    void FaceX(float dx)
    {
        if (!flipToFaceDirection || Mathf.Abs(dx) < 0.001f) return;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
        transform.localScale = s;
    }

    /// <summary>Called by blocked enemies when they hit the knight.</summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        _hp = Mathf.Max(0, _hp - amount);

        // Update the health bar when hit
        if (_healthSlider != null)
            _healthSlider.value = _hp;

        if (_hp <= 0) Die();
    }

    void Die()
    {
        ReleaseAll();
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    // Releases every blocked enemy (on death / destruction) -> they can keep walking.
    void ReleaseAll()
    {
        for (int i = 0; i < _blocked.Count; i++)
            if (_blocked[i] != null && _blocked[i].Blocker == this) _blocked[i].Disengage();
        _blocked.Clear();
    }

    void OnDestroy() => ReleaseAll();

    // Scene view: detect range (blue), block radius (orange) and a health-bar position preview (green).
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.35f);
        Gizmos.DrawWireSphere(Application.isPlaying ? _rallyPoint : transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.6f);   // block radius
        Gizmos.DrawWireSphere(transform.position, blockRadius);

        // Health bar preview
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
        Vector3 previewPos = transform.position + healthBarOffset;
        Vector3 gizmoPreviewSize = new Vector3(0.6f, 0.15f, 0.05f);
        Gizmos.DrawCube(previewPos, gizmoPreviewSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(previewPos, gizmoPreviewSize);
    }
}