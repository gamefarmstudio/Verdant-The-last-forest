using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WaveManager — spawn มอนเป็นเวฟตามข้อมูลใน EnemyDatabase (enemies.csv / bosses.csv)
/// ติดกับ: GameObject ในฉาก (จะเป็น "GameManager" หรือ object แยกก็ได้)
///
/// การไหลของเกม (ผูกกับ GameManager):
///   GameManager.StartGame() → StartNextWave() → OnWaveStart(wave)
///     → WaveManager spawn มอนของ wave นั้น
///     → รอจนมอนตายหมด → GameManager.NotifyWaveCompleted()
///     → ถ้าไม่ใช่ wave สุดท้ายและเปิด auto → หน่วงเวลาแล้ว StartNextWave()
///
/// หมายเหตุ: ยังไม่มีระบบเดิน — มอน spawn แล้วอยู่กับที่ ป้อมยิงจนตาย = จบ wave
///          พอเพิ่ม movement ทีหลัง ก็ยังใช้ flow เดิมได้ (เพิ่มเงื่อนไข reach base เอง)
/// </summary>
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyPrefabEntry
    {
        public string id;          // ID ตรงกับ enemies.csv เช่น E1
        public GameObject prefab;  // prefab ของมอนตัวนั้น (มี SpriteRenderer ฯลฯ)
    }

    [Header("Enemy Prefabs (map ID → prefab)")]
    [Tooltip("ระบุ prefab เฉพาะของแต่ละ Enemy ID — ID ไหนไม่มีในลิสต์จะใช้ defaultEnemyPrefab")]
    [SerializeField] private EnemyPrefabEntry[] enemyPrefabs;
    [SerializeField] private GameObject defaultEnemyPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawn")]
    [Tooltip("จุด spawn (สุ่มเลือก) — เว้นว่างจะ spawn ที่ตำแหน่ง WaveManager เอง")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 0.8f;

    [Header("องค์ประกอบเวฟ")]
    [Tooltip("จำนวนมอนใน wave 1")]
    [SerializeField] private int baseEnemiesPerWave = 5;
    [Tooltip("จำนวนมอนที่เพิ่มขึ้นต่อ wave")]
    [SerializeField] private int extraEnemiesPerWave = 2;

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
            Debug.LogError($"[WaveManager] ไม่พบ layer '{enemyLayerName}' — ป้อมจะตรวจจับมอนไม่เจอ (ไปเพิ่ม layer ใน Project Settings)");

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

    private void HandleWaveStart(int wave)
    {
        if (_waveRoutine != null) StopCoroutine(_waveRoutine);
        _waveRoutine = StartCoroutine(RunWave(wave));
    }

    private IEnumerator RunWave(int wave)
    {
        _alive.Clear();

        if (EnemyDatabase.Instance == null || !EnemyDatabase.Instance.IsLoaded)
        {
            Debug.LogError("[WaveManager] EnemyDatabase ยังไม่พร้อม — ข้าม wave นี้");
            yield break;
        }

        // 1) spawn มอนปกติที่ปลดล็อกแล้ว ณ wave นี้
        var pool = EnemyDatabase.Instance.GetEnemiesForWave(wave);
        int count = baseEnemiesPerWave + extraEnemiesPerWave * Mathf.Max(0, wave - 1);

        if (pool == null || pool.Count == 0)
            Debug.LogWarning($"[WaveManager] wave {wave} ไม่มีมอนใน pool (เช็ค 'First Appears (Wave)' ใน enemies.csv)");

        for (int i = 0; i < count && pool != null && pool.Count > 0; i++)
        {
            var rec = pool[Random.Range(0, pool.Count)];
            SpawnEnemy(rec.id, false);
            yield return new WaitForSeconds(spawnInterval);
        }

        // 2) spawn boss ถ้า wave นี้มี
        if (EnemyDatabase.Instance.HasBossOnWave(wave))
        {
            var boss = EnemyDatabase.Instance.GetBossForWave(wave);
            if (boss != null) SpawnEnemy(boss.id, true);
        }

        // 3) รอจนมอนตายหมด (เผื่อบางตัวถูกทำลายด้วยวิธีอื่น จึงเช็ค null ด้วย)
        while (true)
        {
            _alive.RemoveAll(e => e == null || e.IsDead);
            if (_alive.Count == 0) break;
            yield return null;
        }

        // 4) จบ wave — GameManager จะเช็คเองว่าเป็น wave สุดท้ายไหม (→ victory)
        GameManager.Instance.NotifyWaveCompleted();

        // 5) ต่อ wave ถัดไปอัตโนมัติ (ถ้ายังไม่ใช่ wave สุดท้ายและเกมยังเล่นอยู่)
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
        GameObject prefab = isBoss ? bossPrefab : ResolvePrefab(id);
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

        // ให้มอนเดินตาม EnemyPath (เติมให้ถ้า prefab ยังไม่มี)
        if (obj.GetComponent<EnemyMovement>() == null)
            obj.AddComponent<EnemyMovement>();

        _alive.Add(enemy);
    }

    private GameObject ResolvePrefab(string id)
    {
        if (enemyPrefabs != null)
            foreach (var e in enemyPrefabs)
                if (e != null && e.id == id && e.prefab != null)
                    return e.prefab;
        return defaultEnemyPrefab;
    }

    /// <summary>ให้ป้อมตรวจจับได้: ตั้ง layer Enemy + มี Collider2D อย่างน้อย 1</summary>
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
