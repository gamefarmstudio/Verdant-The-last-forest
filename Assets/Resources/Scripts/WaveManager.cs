using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyPrefabEntry
    {
        public string id; 
        public GameObject prefab;
    }

    [Header("Enemy Prefabs (map ID → prefab)")]
    [Tooltip("ระบุ prefab เฉพาะของแต่ละ ID — ID ไหนไม่มีในลิสต์จะใช้ default/boss prefab")]
    [SerializeField] private EnemyPrefabEntry[] enemyPrefabs;
    [SerializeField] private GameObject defaultEnemyPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawn")]
    [Tooltip("จุด spawn (สุ่มเลือก) — เว้นว่างจะ spawn ที่ตำแหน่ง WaveManager เอง")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("ใช้เมื่อ Spawn Interval ใน CSV เป็น 0")]
    [SerializeField] private float defaultSpawnInterval = 0.8f;

    [Header("การไหลของเวฟ")]
    [SerializeField] private float delayBetweenWaves = 3f;
    [SerializeField] private bool autoStartNextWave = true;

    [Header("ให้ป้อมตรวจจับได้")]
    [Tooltip("layer ของมอน — ต้องตรงกับ enemyLayer ที่ตั้งบน Tower")]
    [SerializeField] private string enemyLayerName = "Enemy";
    [Tooltip("ถ้ามอนไม่มี Collider2D จะเพิ่ม CircleCollider2D (trigger) ให้อัตโนมัติ")]
    [SerializeField] private float autoColliderRadius = 0.4f;

    private readonly List<EnemyBase> _alive = new List<EnemyBase>();
    private int _enemyLayer = -1;
    private Coroutine _waveRoutine;

    public int AliveCount => _alive.Count;

    // ─────────────────────────────────────────────

    void Start()
    {
        _enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        if (_enemyLayer < 0)
            Debug.LogError($"[WaveManager] ไม่พบ layer '{enemyLayerName}' — ป้อมจะตรวจจับมอนไม่เจอ");

        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStart.AddListener(HandleWaveStart);
        else
            Debug.LogError("[WaveManager] ไม่พบ GameManager.Instance — WaveManager ทำงานไม่ได้");
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStart.RemoveListener(HandleWaveStart);
    }

    // ─────────────────────────────────────────────
    // WAVE FLOW
    // ─────────────────────────────────────────────

    public void HandleWaveStart(int wave)
    {
        if (_waveRoutine != null) StopCoroutine(_waveRoutine);
        _waveRoutine = StartCoroutine(RunWave(wave));
    }

    private IEnumerator RunWave(int wave)
    {
        _alive.Clear();

        if (WaveDatabase.Instance == null || !WaveDatabase.Instance.IsLoaded)
        {
            Debug.LogError("[WaveManager] WaveDatabase ยังไม่พร้อม — ข้ามเวฟ");
            yield return FinishWave(wave, null);
            yield break;
        }
        if (EnemyDatabase.Instance == null || !EnemyDatabase.Instance.IsLoaded)
        {
            Debug.LogError("[WaveManager] EnemyDatabase ยังไม่พร้อม — ข้ามเวฟ");
            yield return FinishWave(wave, null);
            yield break;
        }

        var rec = WaveDatabase.Instance.GetWave(wave);
        if (rec == null)
        {
            Debug.LogWarning($"[WaveManager] ไม่มีข้อมูลเวฟ {wave} ใน waves.csv — ผ่านเวฟ");
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

        // เวฟบอสที่ยังไม่มีบอสในองค์ประกอบ → เติมบอสจาก bosses.csv
        if (rec.IsBossWave && !bossSpawned &&
            EnemyDatabase.Instance.HasBossOnWave(wave))
        {
            var b = EnemyDatabase.Instance.GetBossForWave(wave);
            if (b != null) SpawnEnemy(b.id, true);
        }

        if (rec.spawns.Count == 0 && !rec.IsBossWave)
            Debug.LogWarning($"[WaveManager] เวฟ {wave} ยังไม่มีองค์ประกอบใน waves_enemies.csv — ยังไม่ได้ใส่ข้อมูล");

        yield return FinishWave(wave, rec);
    }


    private IEnumerator FinishWave(int wave, WaveRecord rec)
    {
        
        while (true)
        {
            _alive.RemoveAll(e => e == null || e.IsDead);
            if (_alive.Count == 0) break;
            yield return null;
        }

        
        if (rec != null && rec.reward > 0 && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.Earn(rec.reward);
            Debug.Log($"[WaveManager] จบเวฟ {wave} — reward {rec.reward} crystal");
        }

        GameManager.Instance.NotifyWaveCompleted();

        if (autoStartNextWave &&
            GameManager.Instance.CurrentState == GameManager.GameState.Playing &&
            wave < GameManager.Instance.totalWaves)
        {
            yield return new WaitForSeconds(delayBetweenWaves);
            GameManager.Instance.StartNextWave();
        }

        _waveRoutine = null;
    }

    // ─────────────────────────────────────────────
    // SPAWN
    // ─────────────────────────────────────────────

    private void SpawnEnemy(string id, bool isBoss)
    {
        GameObject prefab = ResolvePrefab(id, isBoss);
        if (prefab == null)
        {
            Debug.LogError($"[WaveManager] ไม่มี prefab สำหรับ {(isBoss ? "boss" : "enemy")} '{id}' " +
                           $"(ตั้ง defaultEnemyPrefab / bossPrefab หรือเพิ่มใน enemyPrefabs)");
            return;
        }

        Vector3 pos = (spawnPoints != null && spawnPoints.Length > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Length)].position
            : transform.position;

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        var enemy = obj.GetComponent<EnemyBase>();
        if (enemy == null) enemy = obj.AddComponent<EnemyBase>();

        enemy.Setup(id, isBoss);
        MakeDetectable(obj);

        if (obj.GetComponent<EnemyMovement>() == null)
            obj.AddComponent<EnemyMovement>();

        _alive.Add(enemy);
    }

    
    private GameObject ResolvePrefab(string id, bool isBoss)
    {
        if (enemyPrefabs != null)
            foreach (var e in enemyPrefabs)
                if (e != null && e.id == id && e.prefab != null)
                    return e.prefab;

        return isBoss ? bossPrefab : defaultEnemyPrefab;
    }

    
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
