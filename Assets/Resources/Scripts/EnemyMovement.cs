using UnityEngine;

/// <summary>
/// EnemyMovement — walks an enemy along an EnemyPath from its spawn point to the Crystal.
/// Speed comes from EnemyBase.MoveSpeed (from the CSV), multiplied by any slow/freeze.
/// When it reaches the last waypoint it calls ReachBase().
/// It keeps EnemyBase.PathProgress (0..1) updated so towers in First/Last mode can pick targets.
/// Each enemy picks one of the scene's EnemyPaths at random and stops while a knight blocks it.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyMovement : MonoBehaviour
{
    [Tooltip("Distance that counts as 'reached the waypoint', then it moves on to the next one")]
    [SerializeField] private float reachThreshold = 0.05f;
    [Tooltip("Flip the sprite to face the walking direction (assumes it faces right by default)")]
    [SerializeField] private bool flipToFaceDirection = true;

    private float offset_Y;          // small random vertical offset so enemies don't stack exactly
    private EnemyBase _enemy;
    private Transform[] _waypoints;
    private int _index;              // waypoint we're currently walking toward
    private float _totalLength;
    private float _traveled;

    void Awake() => _enemy = GetComponent<EnemyBase>();

    void Start()
    {
        offset_Y = Random.Range(-1f, 0.5f);  // random Y offset so enemies don't overlap exactly

        // Find every path in the scene
        EnemyPath[] allPaths = FindObjectsByType<EnemyPath>(FindObjectsSortMode.None);

        if (allPaths == null || allPaths.Length == 0)
        {
            Debug.LogWarning("[EnemyMovement] No valid EnemyPath found in the scene — the enemy will stand still", this);
            enabled = false;
            return;
        }

        // Pick a path completely at random, regardless of where the enemy spawned
        int randomIndex = Random.Range(0, allPaths.Length);
        EnemyPath chosenPath = allPaths[randomIndex];

        if (chosenPath != null && chosenPath.IsValid)
        {
            _waypoints  = chosenPath.Waypoints;
            _totalLength = chosenPath.TotalLength;

            // Move the enemy straight to the start of the chosen path
            transform.position = _waypoints[0].position + Vector3.up * offset_Y;
            _index = 1;
        }
        else
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (_enemy.IsDead || _waypoints == null || _index >= _waypoints.Length) return;
        if (_enemy.IsBlocked) return;   // blocked by a knight -> stop walking (resumes when the knight dies)

        Transform wp = _waypoints[_index];
        if (wp == null) { _index++; return; }

        Vector3 target = wp.position + Vector3.up * offset_Y;
        Vector3 before = transform.position;
        float step = _enemy.MoveSpeed * _enemy.SpeedMultiplier * Time.deltaTime;   // multiplied by slow/freeze

        transform.position = Vector3.MoveTowards(before, target, step);
        _traveled += Vector3.Distance(before, transform.position);

        if (_totalLength > 0f)
            _enemy.SetPathProgress(_traveled / _totalLength);

        // Face the direction of travel by flipping the X scale.
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
                _enemy.ReachBase();   // reached the Crystal -> damage the base and destroy itself
            }
        }
    }
}