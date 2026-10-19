using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns each wave's enemies. When GameManager fires OnWaveStart it:
///   1) reads the wave from WaveDatabase (spawn groups, spawn interval, reward);
///   2) spawns every enemy/boss one by one at the spawn points, set up from EnemyDatabase
///      (adds EnemyBase / EnemyMovement / a collider and the Enemy layer if the prefab lacks them);
///   3) waits until every spawned enemy is dead or has reached the base;
///   4) pays the wave reward and tells GameManager the wave is complete
///      (and starts the next wave automatically if autoStartNextWave is on).
/// </summary>
public class WaveManager : MonoBehaviour
{
    /// <summary>Maps an enemy/boss ID from the CSV to the prefab to spawn for it.</summary>
    [System.Serializable]
    public class EnemyPrefabEntry
    {
        public string id;   // e.g. "E1" or "B1"
        public GameObject prefab;
    }

    [Header("Enemy Prefabs (map ID → prefab)")]
    [Tooltip("A specific prefab for each ID — IDs not in this list use the default/boss prefab")]
    [SerializeField] private EnemyPrefabEntry[] enemyPrefabs;
    [SerializeField] private GameObject defaultEnemyPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawn Points / Paths")]
    [Tooltip("All spawn points (0 = first lane, 1 = second lane, 2 = third lane, etc.)")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Custom Spawn Pattern")]
    [Tooltip("The order of spawn point indexes to use, e.g. [0, 1, 0, 2] — leave empty to cycle through them in order")]
    [SerializeField] private int[] spawnPattern;

    [Tooltip("Used when the Spawn Interval in the CSV is 0")]
    [SerializeField] private float defaultSpawnInterval = 0.8f;

    [Header("Wave flow")]
    [SerializeField] private float delayBetweenWaves = 3f;
    [SerializeField] private bool autoStartNextWave = false;   // off = the player starts each wave with the Next Wave button

    [Header("Make enemies detectable by towers")]
    [Tooltip("Enemy layer — must match the enemyLayer set on the towers")]
    [SerializeField] private string enemyLayerName = "Enemy";
    [Tooltip("If an enemy has no Collider2D, a trigger CircleCollider2D of this radius is added automatically")]
    [SerializeField] private float autoColliderRadius = 0.4f;

    private readonly List<EnemyBase> _alive = new List<EnemyBase>();   // enemies spawned this wave that are still around
    private int _patternStepIndex = 0;
    private int _enemyLayer = -1;
    private Coroutine _waveRoutine;

    public int AliveCount => _alive.Count;

    void Start()
    {
        _enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        if (_enemyLayer < 0)
            Debug.LogError($"[WaveManager] Layer '{enemyLayerName}' not found — towers will not be able to detect enemies");

        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStart.AddListener(HandleWaveStart);
        else
            Debug.LogError("[WaveManager] GameManager.Instance not found — WaveManager cannot work");
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStart.RemoveListener(HandleWaveStart);
    }

    /// <summary>Starts running the given wave (stops any wave coroutine still running).</summary>
    public void HandleWaveStart(int wave)
    {
        if (_waveRoutine != null) StopCoroutine(_waveRoutine);
        _waveRoutine = StartCoroutine(RunWave(wave));
    }

    // Spawns every group in the wave, then waits for the wave to finish.
    private IEnumerator RunWave(int wave)
    {
        _alive.Clear();

        if (WaveDatabase.Instance == null || !WaveDatabase.Instance.IsLoaded ||
            EnemyDatabase.Instance == null || !EnemyDatabase.Instance.IsLoaded)
        {
            Debug.LogError("[WaveManager] Databases not ready — Aborting wave start.");
            yield break;
        }

        // A wave missing from the CSV is skipped (completes immediately, no reward).
        var rec = WaveDatabase.Instance.GetWave(wave);
        if (rec == null)
        {
            Debug.LogWarning($"[WaveManager] No data for wave {wave} in waves.csv — skipping the wave");
            yield return FinishWave(wave, null);
            yield break;
        }

        float interval = rec.spawnInterval > 0f ? rec.spawnInterval : defaultSpawnInterval;
        bool bossSpawned = false;

        foreach (var entry in rec.spawns)
        {
            for (int k = 0; k < entry.count; k++)
            {
                SpawnEnemy(entry.enemyID, entry.IsBoss);
                if (entry.IsBoss) bossSpawned = true;
                yield return new WaitForSeconds(interval);
            }
        }

        // A BOSS wave whose spawn list didn't include the boss: spawn the boss bosses.csv assigns to this wave.
        if (rec.IsBossWave && !bossSpawned && EnemyDatabase.Instance.HasBossOnWave(wave))
        {
            var b = EnemyDatabase.Instance.GetBossForWave(wave);
            if (b != null) SpawnEnemy(b.id, true);
        }

        if (rec.spawns.Count == 0 && !rec.IsBossWave)
            Debug.LogWarning($"[WaveManager] Wave {wave} has no spawn groups in waves_enemies.csv yet");

        yield return FinishWave(wave, rec);
    }

    // Waits until every enemy from this wave is gone, pays the reward and reports the wave as complete.
    private IEnumerator FinishWave(int wave, WaveRecord rec)
    {
        yield return new WaitUntil(() =>
        {
            _alive.RemoveAll(e => e == null || e.IsDead);
            return _alive.Count == 0;
        });

        if (rec != null && rec.reward > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.Earn(rec.reward);
            Debug.Log($"[WaveManager] Wave {wave} finished — reward {rec.reward} crystal");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyWaveCompleted();

            if (autoStartNextWave &&
                GameManager.Instance.CurrentState == GameManager.GameState.Playing &&
                wave < GameManager.Instance.totalWaves)
            {
                yield return new WaitForSeconds(delayBetweenWaves);
                GameManager.Instance.StartNextWave();
            }
        }

        _waveRoutine = null;
    }

    // Instantiates one enemy or boss and makes sure it has everything it needs to work.
    private void SpawnEnemy(string id, bool isBoss)
    {
        GameObject prefab = ResolvePrefab(id, isBoss);
        if (prefab == null)
        {
            Debug.LogError($"[WaveManager] No prefab for {(isBoss ? "boss" : "enemy")} '{id}'");
            return;
        }

        Vector3 pos = GetPatternSpawnPosition();

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        var enemy = obj.GetComponent<EnemyBase>();
        if (enemy == null) enemy = obj.AddComponent<EnemyBase>();

        enemy.Setup(id, isBoss);   // load stats from enemies.csv / bosses.csv
        MakeDetectable(obj);

        if (obj.GetComponent<EnemyMovement>() == null)
            obj.AddComponent<EnemyMovement>();

        _alive.Add(enemy);
    }

    // ─────────────────────────────────────────────
    // CUSTOM PATTERN SPAWN LOGIC (e.g. 0, 1, 0, 2)
    // ─────────────────────────────────────────────
    private Vector3 GetPatternSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return transform.position;
        }

        int targetIndex;

        // 1. If a custom pattern array is provided, follow it in order
        if (spawnPattern != null && spawnPattern.Length > 0)
        {
            int rawIndex = spawnPattern[_patternStepIndex];

            // Safe clamp using modulo in case index is out of bounds
            targetIndex = Mathf.Abs(rawIndex) % spawnPoints.Length;

            // Move to next step in the pattern, looping back when reaching the end
            _patternStepIndex = (_patternStepIndex + 1) % spawnPattern.Length;
        }
        else
        {
            // 2. Default fallback: Sequential round-robin (0, 1, 2...)
            targetIndex = _patternStepIndex % spawnPoints.Length;
            _patternStepIndex = (_patternStepIndex + 1) % spawnPoints.Length;
        }

        Transform targetPath = spawnPoints[targetIndex];
        return targetPath != null ? targetPath.position : transform.position;
    }

    // Picks the prefab for an ID: a specific one from enemyPrefabs, otherwise the default/boss prefab.
    private GameObject ResolvePrefab(string id, bool isBoss)
    {
        if (enemyPrefabs != null)
        {
            foreach (var e in enemyPrefabs)
            {
                if (e != null && e.id == id && e.prefab != null)
                    return e.prefab;
            }
        }
        return isBoss ? bossPrefab : defaultEnemyPrefab;
    }

    // Puts the enemy on the Enemy layer and adds a trigger collider if it has none, so towers can find it.
    private void MakeDetectable(GameObject obj)
    {
        if (_enemyLayer >= 0) obj.layer = _enemyLayer;

        if (obj.GetComponentInChildren<Collider2D>() == null)
        {
            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = autoColliderRadius;
        }
    }
}