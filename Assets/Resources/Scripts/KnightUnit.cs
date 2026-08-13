using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // [Fix] แก้ไข Typo จาก UntyEngine.UI เป็น UnityEngine.UI

// [เพิ่ม] อัศวิน — unit ประชิดที่ MeleeTower summon
//   เคลื่อนที่ตามแนว EnemyPath (ไม่ตัดเส้นตรง)
//   [แก้] block แบบ B + เพดาน: กั้นมอน "หลายตัว" ที่เข้ามาในรัศมี (สูงสุด maxBlock) — ทุกตัวรุมตีอัศวิน
//   ตีทีละตัว (ตัวที่กั้นอยู่ใกล้สุด) · อัศวินตาย → ปลดบล็อกทุกตัว + แจ้ง MeleeTower
//   สเตตัสถูกทับด้วย Configure() จาก towers.csv ตามเลเวลป้อม
//   หมายเหตุ: วาง prefab อัศวินบน layer ที่ไม่ใช่ Enemy
public class KnightUnit : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int damage = 25;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Block (แบบ B)")]
    [SerializeField] private float blockRadius = 0.9f;   // รัศมีที่กั้นมอน (Configure ทับด้วย range จาก CSV)
    [SerializeField] private int maxBlock = 3;           // กั้นได้สูงสุดกี่ตัวพร้อมกัน

    [Header("Seek")]
    [SerializeField] private float detectRange = 6f;     // ระยะมองหามอน (รอบจุดรวมพล)
    [SerializeField] private string enemyLayerName = "Enemy";

    [Header("Path Movement")]
    [SerializeField] private bool moveAlongPath = true;
    [SerializeField] private float leash = 4f;           // ห่างจากจุดรวมพลได้สูงสุด (ตามแนว path)

    [Header("Visual")]
    [SerializeField] private bool flipToFaceDirection = true;
    
    [Header("UI")]
    [Tooltip("ลาก Canvas prefab ที่มี Slider อยู่ข้างในมาใส่ตรงนี้")]
    [SerializeField] private Canvas healthCanvasPrefab;
    [Tooltip("ตำแหน่ง local เหนือหัวอัศวินที่จะวาง health bar")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);
    
    [Header("Animations")]
    [SerializeField] private Animator animator;
    [SerializeField] private string TriggerAttack = "IsAttacking";   // bool param — true = กำลังตี, false = เดิน/ยืน

    public bool IsAlive => _hp > 0;
    public int CurrentHP => _hp;
    public System.Action OnDeath;

    private int _hp;
    private float _attackTimer;
    private Vector3 _rallyPoint;
    private ContactFilter2D _filter;
    private readonly List<Collider2D> _hits = new List<Collider2D>();
    private readonly List<EnemyBase> _blocked = new List<EnemyBase>();   // มอนที่กำลังกั้นอยู่

    // path movement
    private EnemyPath _path;
    private float _rallyDist;
    private float _pathDist;
    
    // UI
    private Canvas _healthCanvas;
    private Slider _healthSlider;

    // [เพิ่ม] ตั้งสเตตัสจากภายนอก (MeleeTower โหลดจาก towers.csv) — เรียกก่อน Init
    public void Configure(int hp, int dmg, float atkInterval, float reach)
    {
        maxHP = hp;
        _hp = hp;
        damage = dmg;
        if (atkInterval > 0f) attackInterval = atkInterval;
        if (reach > 0f) blockRadius = reach;

        // [Fix] อัปเดตหลอดเลือดหาก Configure ถูกเรียกหลังจาก Awake
        if (_healthSlider != null) 
        {
            _healthSlider.maxValue = maxHP;
            _healthSlider.value = _hp;
        }
    }

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

        int layer = LayerMask.NameToLayer(enemyLayerName);
        LayerMask mask = layer >= 0 ? (1 << layer) : LayerMask.GetMask(enemyLayerName);
        _filter = new ContactFilter2D();
        _filter.SetLayerMask(mask);
        _filter.useTriggers = true;

        SetupHealthBar();
    }

    private void SetupHealthBar()
    {
        if (healthCanvasPrefab == null)
        {
            Debug.LogError($"[KnightUnit] '{name}' ยังไม่ได้ลาก Canvas prefab มาใส่ในช่อง Health Canvas Prefab", this);
            return;
        }

        // Clone Canvas แยกให้อัศวินตัวนี้
        _healthCanvas = Instantiate(healthCanvasPrefab);
        _healthCanvas.transform.SetParent(transform, worldPositionStays: false);
        _healthCanvas.transform.localPosition = healthBarOffset;
        _healthCanvas.transform.localRotation = Quaternion.identity;

        // ตั้งกล้องให้ World Space Canvas (กัน Warning)
        if (_healthCanvas.renderMode == RenderMode.WorldSpace && _healthCanvas.worldCamera == null)
        {
            _healthCanvas.worldCamera = Camera.main;
        }

        _healthSlider = _healthCanvas.GetComponentInChildren<Slider>(true);
        if (_healthSlider == null)
        {
            Debug.LogError($"[KnightUnit] '{name}' Clone Canvas แล้วแต่ไม่พบ Slider อยู่ข้างใน", this);
            return;
        }

        // เปลี่ยนสีหลอดเลือดเป็นสีเขียว/ฟ้า (เพื่อแยกความต่างจากศัตรูที่มักเป็นสีแดง)
        var fillImage = _healthSlider.fillRect != null ? _healthSlider.fillRect.GetComponent<Image>() : null;
        if (fillImage != null) fillImage.color = Color.green;

        _healthSlider.interactable = false;
        
        // ตั้งค่าเริ่มต้น
        _healthSlider.minValue = 0;
        _healthSlider.maxValue = maxHP;
        _healthSlider.value = _hp;
    }

    void Update()
    {
        if (!IsAlive) return;

        UpdateBlockSet();   // เพิ่ม/ปลด มอนที่กั้นอยู่ตามรัศมี + เพดาน

        // เป้าเคลื่อนที่: ถ้ากำลังกั้นอยู่ → ยืนกับที่ (กั้นตัวใกล้สุด), ถ้าว่าง → เดินไปดักมอนใกล้สุด
        EnemyBase moveTarget = _blocked.Count > 0 ? NearestBlocked() : NearestEnemyInDetect();

        if (moveAlongPath && _path != null && _path.IsValid)
            MoveAlongPath(moveTarget);
        else
            MoveStraight(moveTarget != null ? moveTarget.transform.position : _rallyPoint);

        // ตีตัวที่กั้นอยู่ใกล้สุด (ทีละตัว)
        EnemyBase atk = NearestBlocked();
        if (atk != null)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                if(animator != null) animator.SetTrigger(TriggerAttack);
                _attackTimer = attackInterval;
                atk.TakeDamage(damage);
            }
        }
        else
        {
            // [Fix] ตรวจสอบ null ก่อนเรียก animator
            if(animator != null) animator.ResetTrigger(TriggerAttack);
        }
    }

    void LateUpdate()
    {
        // อัปเดตตำแหน่งเองทุกเฟรมกัน Canvas พลิกซ้ายขวาเวลาอัศวินหันหน้า (FlipX)
        if (_healthCanvas != null)
        {
            _healthCanvas.transform.position = transform.position + healthBarOffset;
            _healthCanvas.transform.rotation = Quaternion.identity;
        }
    }

    // จัดการชุดที่กั้น: ปลดตัวตาย/หลุดรัศมี → เติมตัวใหม่จนเต็มเพดาน
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
            if (e.IsBlocked && e.Blocker != this) continue;   // ตัวอื่นกั้นอยู่ → ข้าม
            
            e.EngageWith(this);
            _blocked.Add(e);
        }
    }

    EnemyBase NearestBlocked()
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

    void MoveStraight(Vector3 dest)
    {
        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(before, dest, moveSpeed * Time.deltaTime);
        FaceX(dest.x - before.x);
    }

    void FaceX(float dx)
    {
        if (!flipToFaceDirection || Mathf.Abs(dx) < 0.001f) return;
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
        transform.localScale = s;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        _hp = Mathf.Max(0, _hp - amount);
        
        // [Fix] อัปเดตหลอดเลือดเมื่อโดนโจมตี
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

    // ปลดบล็อกมอนทุกตัว (ตอนตาย/ถูกทำลาย) → มอนเดินต่อได้
    void ReleaseAll()
    {
        for (int i = 0; i < _blocked.Count; i++)
            if (_blocked[i] != null && _blocked[i].Blocker == this) _blocked[i].Disengage();
        _blocked.Clear();
    }

    void OnDestroy() => ReleaseAll();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.35f);
        Gizmos.DrawWireSphere(Application.isPlaying ? _rallyPoint : transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.6f);   // รัศมีกั้น
        Gizmos.DrawWireSphere(transform.position, blockRadius);

        // วาดพรีวิว Health bar
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f); 
        Vector3 previewPos = transform.position + healthBarOffset;
        Vector3 gizmoPreviewSize = new Vector3(0.6f, 0.15f, 0.05f);
        Gizmos.DrawCube(previewPos, gizmoPreviewSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(previewPos, gizmoPreviewSize);
    }
}