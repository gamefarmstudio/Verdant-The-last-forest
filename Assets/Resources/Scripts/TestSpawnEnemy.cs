using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script ทดสอบ — กดปุ่ม Q/W/E/R เพื่อ spawn enemy E1-E4 ที่ตำแหน่งตัวเอง
/// ติดกับ: GameObject ว่างๆ ใน Scene (เช่นจุด spawn ทดสอบ)
///
/// เวอร์ชันนี้ใช้ Prefab แยกตัวต่อ Enemy ID — ลาก Prefab ที่ต่างกัน 4 ตัวมาใส่
/// (ถ้าต้องการ Prefab เดียวใช้ร่วมกันทุกตัว ดู TestSpawnEnemy เวอร์ชันก่อนหน้า)
///
/// ใช้ Input System (new) — ต้องติดตั้ง package "Input System" ก่อน
/// (Window > Package Manager > Input System)
/// </summary>
public class TestSpawnEnemy : MonoBehaviour
{
    [Header("Prefab แยกตัวต่อ Enemy ID — แต่ละ Prefab มี EnemyBase.cs ติดอยู่แล้ว")]
    [SerializeField] private GameObject prefab_E1;   // Smog Skull
    [SerializeField] private GameObject prefab_E2;   // Toxic Drum
    [SerializeField] private GameObject prefab_E3;   // Hex Shaman
    [SerializeField] private GameObject prefab_E4;   // Oil Lighter

    [Header("จุด spawn — ถ้าไม่ตั้งจะใช้ตำแหน่งตัวเอง")]
    [SerializeField] private Transform spawnPoint;

    [Header("ระยะห่างระหว่างตัวที่ spawn ติดกัน (กันซ้อนกันเป๊ะ)")]
    [SerializeField] private float spawnSpread = 0.5f;

    void Update()
    {
        // Keyboard.current เป็น null ได้ถ้าไม่มี keyboard เชื่อมต่อ (เช่น build บนมือถือ) — กันไว้ก่อน
        if (Keyboard.current == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame) SpawnEnemy(prefab_E1, "E1");
        if (Keyboard.current.wKey.wasPressedThisFrame) SpawnEnemy(prefab_E2, "E2");
        if (Keyboard.current.eKey.wasPressedThisFrame) SpawnEnemy(prefab_E3, "E3");
        if (Keyboard.current.rKey.wasPressedThisFrame) SpawnEnemy(prefab_E4, "E4");
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

        // สุ่ม offset เล็กน้อยกันตัวที่ spawn ซ้อนกันเป๊ะจนมองไม่เห็นว่ามีกี่ตัว
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
        enemy.CurrentHP = enemy.MaxHP;  // เริ่มต้น HP เต็ม (ปกติ EnemyBase.Setup() จะทำให้ CurrentHP = MaxHP อยู่แล้ว แต่กันไว้ก่อน)

        Debug.Log($"[TestSpawnEnemy] Spawned {enemyID} → {enemy.enemyRecord?.nameEN} | HP:{enemy.MaxHP} SPD:{enemy.MoveSpeed}");
    }
}