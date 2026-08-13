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

    // [เพิ่ม] จุดบนเส้นทางที่ใกล้ from สุด (= เลื่อน 0)
    public Vector3 ClosestPointOnPath(Vector3 from) => PointAlongPath(from, 0f);

    // [เพิ่ม] จุดบน path ที่ใกล้ from สุด แล้วเลื่อนตามแนว path เป็นระยะ shift (หน่วยโลก)
    //   shift > 0 = ไปทางฐาน, shift < 0 = ไปทางจุดเกิดมอน
    public Vector3 PointAlongPath(Vector3 from, float shift)
    {
        if (waypoints == null || waypoints.Length < 2) return from;
        float d = Mathf.Clamp(DistanceAlongPath(from) + shift, 0f, TotalLength);
        return PointAtDistance(d);
    }

    // [เพิ่ม] ระยะสะสมจากจุดเกิดถึง "จุดบน path ที่ใกล้ from สุด" (KnightUnit ใช้รู้ตำแหน่งตัวเอง/ศัตรูบน path)
    public float DistanceAlongPath(Vector3 from)
    {
        if (waypoints == null || waypoints.Length < 2) return 0f;

        float bestSqr = float.MaxValue, bestDist = 0f, accum = 0f;
        for (int i = 1; i < waypoints.Length; i++)
        {
            if (waypoints[i - 1] == null || waypoints[i] == null) continue;
            Vector3 a = waypoints[i - 1].position, b = waypoints[i].position, ab = b - a;
            float segLen = ab.magnitude;
            float t = segLen > 1e-4f ? Mathf.Clamp01(Vector3.Dot(from - a, ab) / (segLen * segLen)) : 0f;
            float sqr = ((a + ab * t) - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; bestDist = accum + t * segLen; }
            accum += segLen;
        }
        return bestDist;
    }

    // [เพิ่ม] จุดบน path ที่ระยะสะสม dist จากจุดเกิด (public — KnightUnit เดินตามแนว path)
    public Vector3 PointAtDistance(float dist)
    {
        Vector3 prev = waypoints[0] != null ? waypoints[0].position : transform.position;
        float remaining = dist;
        for (int i = 1; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Vector3 cur = waypoints[i].position;
            float segLen = Vector3.Distance(prev, cur);
            if (remaining <= segLen || i == waypoints.Length - 1)
                return Vector3.Lerp(prev, cur, segLen > 1e-4f ? Mathf.Clamp01(remaining / segLen) : 0f);
            remaining -= segLen;
            prev = cur;
        }
        return prev;
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
