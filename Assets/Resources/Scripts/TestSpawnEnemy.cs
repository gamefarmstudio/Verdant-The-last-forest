using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug helper for spawning individual enemies by ID to test stats from enemies.csv.
/// Note: the keyboard shortcuts in Update() have been removed, so nothing currently calls
/// SpawnEnemy — add key checks back in Update() to use it again.
/// </summary>
public class TestSpawnEnemy : MonoBehaviour
{
    [Header("One prefab per Enemy ID — each prefab already has EnemyBase.cs attached")]
    [SerializeField] private GameObject prefab_E1;
    [SerializeField] private GameObject prefab_E2;
    [SerializeField] private GameObject prefab_E3;
    [SerializeField] private GameObject prefab_E4;

    [Header("Spawn point — uses this object's position if not set")]
    [SerializeField] private Transform spawnPoint;

    [Header("Random spread between spawns (so they don't overlap exactly)")]
    [SerializeField] private float spawnSpread = 0.5f;

    void Update()
    {
        if (Keyboard.current == null) return;


    }

    // Spawns one enemy prefab near the spawn point and loads its stats from EnemyDatabase.
    private void SpawnEnemy(GameObject prefab, string enemyID)
    {
        print($"[TestSpawnEnemy] spawn key pressed for {enemyID} — prefab: {prefab?.name ?? "null"}");
        if (prefab == null)
        {
            Debug.LogError($"[TestSpawnEnemy] no Prefab for {enemyID} has been assigned in the Inspector");
            return;
        }

        if (EnemyDatabase.Instance == null || !EnemyDatabase.Instance.IsLoaded)
        {
            Debug.LogWarning("[TestSpawnEnemy] EnemyDatabase has not finished loading — wait a moment and try again");
            return;
        }

        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;

        Vector3 offset = new Vector3(Random.Range(-spawnSpread, spawnSpread),
                                       Random.Range(-spawnSpread, spawnSpread), 0f);

        GameObject obj = Instantiate(prefab, basePos + offset, Quaternion.identity);

        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogError($"[TestSpawnEnemy] Prefab '{prefab.name}' does not have EnemyBase.cs attached");
            Destroy(obj);
            return;
        }

        enemy.Setup(enemyID);
        enemy.CurrentHP = enemy.MaxHP;

        Debug.Log($"[TestSpawnEnemy] Spawned {enemyID} → {enemy.enemyRecord?.nameEN} | HP:{enemy.MaxHP} SPD:{enemy.MoveSpeed}");
    }
}