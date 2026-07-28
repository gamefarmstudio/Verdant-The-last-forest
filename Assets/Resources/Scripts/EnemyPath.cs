using UnityEngine;

/// <summary>
/// EnemyPath — เส้นทางเดินของมอนจากจุดเกิดไปยัง Crystal (ฐานผู้เล่น)
/// วางบน GameObject เปล่าในฉาก แล้วลาก Transform (จุดหมาย) ใส่ waypoints ตามลำดับ
///   waypoints[0]           = จุดเกิด (มอน spawn ที่นี่)
///   waypoints[length - 1]  = Crystal (มอนถึงตรงนี้ = ReachBase)
///
/// EnemyMovement จะค้นหา EnemyPath นี้เองด้วย FindAnyObjectByType (1 เส้นต่อด่าน)
/// </summary>
public class EnemyPath : MonoBehaviour
{
    [Tooltip("จุดหมายตามลำดับ — ตัวแรก = จุดเกิด, ตัวสุดท้าย = Crystal")]
    [SerializeField] private Transform[] waypoints;

    public Transform[] Waypoints => waypoints;

    public bool IsValid => waypoints != null && waypoints.Length >= 2;

    /// <summary>ตำแหน่งจุดเกิด (waypoint แรก)</summary>
    public Vector3 StartPoint =>
        (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
            ? waypoints[0].position
            : transform.position;

    /// <summary>ความยาวรวมของเส้นทาง (ใช้คำนวณ PathProgress 0..1)</summary>
    public float TotalLength
    {
        get
        {
            float len = 0f;
            if (waypoints == null) return 0f;
            for (int i = 1; i < waypoints.Length; i++)
                if (waypoints[i - 1] != null && waypoints[i] != null)
                    len += Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
            return len;
        }
    }

    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = new Color(0.15f, 0.8f, 1f, 1f);
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.18f);
            if (i > 0 && waypoints[i - 1] != null)
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }
    }
}
