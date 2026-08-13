using UnityEngine;

/// <summary>
/// EnemyMovement — เดินมอนตาม EnemyPath จากจุดเกิดไปหา Crystal
/// ความเร็วดึงจาก EnemyBase.MoveSpeed (มาจาก CSV) — ถึงปลายทางแล้วเรียก ReachBase()
/// อัปเดต EnemyBase.PathProgress (0..1) ให้ป้อมโหมด First/Last เลือกเป้าได้
///
/// WaveManager จะ AddComponent ตัวนี้ให้อัตโนมัติตอน spawn (ไม่ต้องใส่บน prefab ก็ได้)
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyMovement : MonoBehaviour
{
    [Tooltip("ระยะที่ถือว่า 'ถึง waypoint' แล้วไปข้อถัดไป")]
    [SerializeField] private float reachThreshold = 0.05f;
    [Tooltip("พลิกสไปรต์ให้หันตามทิศทางเดิน (สมมติ default หันขวา)")]
    [SerializeField] private bool flipToFaceDirection = true;

    private float offset_Y;
    private EnemyBase _enemy;
    private Transform[] _waypoints;
    private int _index;
    private float _totalLength;
    private float _traveled;

    void Awake() => _enemy = GetComponent<EnemyBase>();

    void Start()
    {
        offset_Y = Random.Range(-1f, 0.5f);  // random offset Y เพื่อไม่ให้มอนซ้อนกันเป๊ะ
        var path = FindAnyObjectByType<EnemyPath>();
        if (path == null || !path.IsValid)
        {
            Debug.LogWarning("[EnemyMovement] ไม่พบ EnemyPath ที่ถูกต้องในฉาก — มอนจะอยู่กับที่", this);
            enabled = false;
            return;
        }

        _waypoints  = path.Waypoints;
        _totalLength = path.TotalLength;

        transform.position = _waypoints[0].position + Vector3.up * offset_Y;  // เริ่มที่จุดเกิด
        _index = 1;                                     // มุ่งหน้า waypoint ถัดไป
    }

    void Update()
    {
        if (_enemy.IsDead || _waypoints == null || _index >= _waypoints.Length) return;
        if (_enemy.IsBlocked) return;   // [เพิ่ม] ถูกอัศวินบล็อก → หยุดเดิน (เดินต่อเมื่ออัศวินตาย)

        Transform wp = _waypoints[_index];
        if (wp == null) { _index++; return; }

        Vector3 target = wp.position + Vector3.up * offset_Y;
        Vector3 before = transform.position;
        float step = _enemy.MoveSpeed * _enemy.SpeedMultiplier * Time.deltaTime;   // [เพิ่ม] คูณ slow/freeze

        transform.position = Vector3.MoveTowards(before, target, step);
        _traveled += Vector3.Distance(before, transform.position);

        if (_totalLength > 0f)
            _enemy.SetPathProgress(_traveled / _totalLength);

        if (flipToFaceDirection)
        {
            float dx = target.x - before.x;
            if (Mathf.Abs(dx) > 0.001f)
            {
                Vector3 s = transform.localScale;
                s.x = Mathf.Abs(s.x) * (dx >= 0f ? 1f : -1f);
                transform.localScale = s;
            }
        }

        if (Vector3.Distance(transform.position, target) <= reachThreshold)
        {
            _index++;
            if (_index >= _waypoints.Length)
            {
                _enemy.SetPathProgress(1f);
                _enemy.ReachBase();   // ถึง Crystal → ทำ damage ให้ฐาน + ทำลายตัวเอง
            }
        }
    }
}
