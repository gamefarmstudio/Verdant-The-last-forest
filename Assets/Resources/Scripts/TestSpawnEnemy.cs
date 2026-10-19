using UnityEngine;
using UnityEngine.InputSystem;

public class TestSpawnEnemy : MonoBehaviour
{
    [Header("Prefab แยกตัวต่อ Enemy ID — แต่ละ Prefab มี EnemyBase.cs ติดอยู่แล้ว")]
    [SerializeField] private GameObject prefab_E1; 
    [SerializeField] private GameObject prefab_E2;   
    [SerializeField] private GameObject prefab_E3;  
    [SerializeField] private GameObject prefab_E4;  

    [Header("จุด spawn — ถ้าไม่ตั้งจะใช้ตำแหน่งตัวเอง")]
    [SerializeField] private Transform spawnPoint;

    [Header("ระยะห่างระหว่างตัวที่ spawn ติดกัน (กันซ้อนกันเป๊ะ)")]
    [SerializeField] private float spawnSpread = 0.5f;

    void Update()
    {
        if (Keyboard.current == null) return;


    }

    private void SpawnEnemy(GameObject prefab, string enemyID)
    {
        print($"[TestSpawnEnemy] กดปุ่ม spawn {enemyID} — prefab: {prefab?.name ?? "null"}");
        if (prefab == null)
        {
            Debug.LogError($"[TestSpawnEnemy] ยังไม่ได้ลาก Prefab ของ {enemyID} ใส่ Inspector");
            return;
        }

        if (EnemyDatabase.Instance == null || !EnemyDatabase.Instance.IsLoaded)
        {
            Debug.LogWarning("[TestSpawnEnemy] EnemyDatabase ยังโหลดไม่เสร็จ — รอสักครู่แล้วลองใหม่");
            return;
        }

        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;

        Vector3 offset = new Vector3(Random.Range(-spawnSpread, spawnSpread),
                                       Random.Range(-spawnSpread, spawnSpread), 0f);

        GameObject obj = Instantiate(prefab, basePos + offset, Quaternion.identity);

        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            Debug.LogError($"[TestSpawnEnemy] Prefab '{prefab.name}' ไม่มี EnemyBase.cs ติดอยู่");
            Destroy(obj);
            return;
        }

        enemy.Setup(enemyID);
        enemy.CurrentHP = enemy.MaxHP; 

        Debug.Log($"[TestSpawnEnemy] Spawned {enemyID} → {enemy.enemyRecord?.nameEN} | HP:{enemy.MaxHP} SPD:{enemy.MoveSpeed}");
    }
}