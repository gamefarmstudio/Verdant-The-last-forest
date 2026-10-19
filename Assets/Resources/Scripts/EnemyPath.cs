using UnityEngine;

/// <summary>
/// EnemyPath — the route enemies walk from their spawn point to the Crystal (the player's base).
/// Put it on an empty GameObject in the scene and drag the waypoint Transforms into "waypoints" in order:
///   waypoints[0]           = spawn point (enemies start here)
///   waypoints[length - 1]  = the Crystal (an enemy reaching this = ReachBase)
///
/// A scene can have several paths: EnemyMovement picks one at random for each enemy.
/// KnightUnit and MeleeTower use the helper methods below to stand and move along a path.
/// </summary>
public class EnemyPath : MonoBehaviour
{
    [Tooltip("Waypoints in order — first = spawn point, last = Crystal")]
    [SerializeField] private Transform[] waypoints;

    public Transform[] Waypoints => waypoints;

    /// <summary>A path needs at least two waypoints.</summary>
    public bool IsValid => waypoints != null && waypoints.Length >= 2;

    /// <summary>Position of the spawn point (the first waypoint).</summary>
    public Vector3 StartPoint =>
        (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
            ? waypoints[0].position
            : transform.position;

    /// <summary>Total length of the path (used to calculate PathProgress 0..1).</summary>
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

    /// <summary>The point on the path closest to <paramref name="from"/> (a shift of 0).</summary>
    public Vector3 ClosestPointOnPath(Vector3 from) => PointAlongPath(from, 0f);

    /// <summary>
    /// The point on the path closest to <paramref name="from"/>, then moved along the path by
    /// <paramref name="shift"/> world units: shift &gt; 0 = toward the base, shift &lt; 0 = toward the enemy spawn.
    /// </summary>
    public Vector3 PointAlongPath(Vector3 from, float shift)
    {
        if (waypoints == null || waypoints.Length < 2) return from;
        float d = Mathf.Clamp(DistanceAlongPath(from) + shift, 0f, TotalLength);
        return PointAtDistance(d);
    }

    /// <summary>
    /// Distance along the path from the spawn point to the point on the path closest to <paramref name="from"/>.
    /// (KnightUnit uses this to know where itself and enemies are along the path.)
    /// </summary>
    public float DistanceAlongPath(Vector3 from)
    {
        if (waypoints == null || waypoints.Length < 2) return 0f;

        float bestSqr = float.MaxValue, bestDist = 0f, accum = 0f;
        for (int i = 1; i < waypoints.Length; i++)
        {
            if (waypoints[i - 1] == null || waypoints[i] == null) continue;
            // Project 'from' onto this segment and remember the closest projection so far.
            Vector3 a = waypoints[i - 1].position, b = waypoints[i].position, ab = b - a;
            float segLen = ab.magnitude;
            float t = segLen > 1e-4f ? Mathf.Clamp01(Vector3.Dot(from - a, ab) / (segLen * segLen)) : 0f;
            float sqr = ((a + ab * t) - from).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; bestDist = accum + t * segLen; }
            accum += segLen;
        }
        return bestDist;
    }

    /// <summary>The point on the path at distance <paramref name="dist"/> from the spawn point (KnightUnit walks along the path with this).</summary>
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

    // Draws the path (spheres at waypoints, lines between them) in the Scene view.
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
