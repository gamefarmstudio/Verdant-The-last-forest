using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("Reference to the EnemyBase script")]
    public EnemyBase enemyBase;

    [Tooltip("ลาก Canvas prefab ที่มี Slider อยู่ข้างในมาใส่ตรงนี้ — จะถูก Clone แยกให้ enemy แต่ละตัว")]
    [SerializeField] private Canvas healthCanvasPrefab;

    [Tooltip("ตำแหน่ง local เหนือหัว enemy ที่จะวาง health bar")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

    [Header("พรีวิวใน Scene view")]
    [Tooltip("ขนาดกล่องพรีวิวตำแหน่ง offset (แค่ช่วยดูใน Editor ไม่มีผลกับเกมจริง)")]
    [SerializeField] private Vector3 gizmoPreviewSize = new Vector3(0.6f, 0.15f, 0.05f);

    private Canvas healthCanvas;
    private Slider healthSlider;

    void Awake()
    {
        if (enemyBase == null)
            enemyBase = GetComponent<EnemyBase>();

        if (enemyBase == null)
        {
            Debug.LogError($"[EnemyHealth] '{name}' has no EnemyBase — can't set up health bar.", this);
            return;
        }

        if (healthCanvasPrefab == null)
        {
            Debug.LogError($"[EnemyHealth] '{name}' ยังไม่ได้ลาก Canvas prefab มาใส่ในช่อง Health Canvas Prefab", this);
            return;
        }

        // Clone Canvas แยกให้ enemy ตัวนี้โดยเฉพาะ กันหลาย enemy แย่งใช้ Slider ตัวเดียวกัน
        healthCanvas = Instantiate(healthCanvasPrefab);
        healthCanvas.transform.SetParent(transform, worldPositionStays: false);
        healthCanvas.transform.localPosition = offset;
        healthCanvas.transform.localRotation = Quaternion.identity; // กันไม่ให้เอียง/พลิกตามตัว enemy

        // World Space Canvas ต้องมี worldCamera ไม่งั้น Unity เตือนเรื่อง UI events
        // (ไม่จำเป็นมากเพราะ Slider นี้ไม่ interactable แต่ตั้งให้ถูกไว้เลย)
        if (healthCanvas.renderMode == RenderMode.WorldSpace && healthCanvas.worldCamera == null)
        {
            healthCanvas.worldCamera = Camera.main;
            if (healthCanvas.worldCamera == null)
            {
                Debug.LogWarning($"[EnemyHealth] '{name}' หา Camera.main ไม่เจอ — " +
                                 "ตรวจสอบว่ากล้องหลักในฉากติด tag 'MainCamera' อยู่", this);
            }
        }

        healthSlider = healthCanvas.GetComponentInChildren<Slider>(true); // true = หาแม้ถูกปิดอยู่
        if (healthSlider == null)
        {
            Debug.LogError($"[EnemyHealth] '{name}' Clone Canvas แล้วแต่ไม่พบ Slider อยู่ข้างใน", this);
            return;
        }

        // Tint the fill image red (fillColor doesn't exist on Slider itself)
        var fillImage = healthSlider.fillRect != null ? healthSlider.fillRect.GetComponent<Image>() : null;
        if (fillImage != null) fillImage.color = Color.red;

        healthSlider.interactable = false;
    }

    void Start()
    {
        if (enemyBase == null || healthSlider == null) return;

        // Awake ทุกตัวใน scene ทำงานเสร็จหมดแล้วตอนนี้ — ค่า MaxHP/CurrentHP ควรพร้อมแล้ว
        healthSlider.maxValue = enemyBase.MaxHP;
        healthSlider.value = enemyBase.CurrentHP;
        Debug.Log($"[EnemyHealth] '{name}' health bar set up with MaxHP={enemyBase.MaxHP}, CurrentHP={enemyBase.CurrentHP}", this);

        enemyBase.OnHPChanged.AddListener(UpdateHealthUI);
    }

    void UpdateHealthUI(int currentHealth)
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;
    }

    void LateUpdate()
    {
        // อัปเดตตำแหน่งเองทุกเฟรมแทนที่จะพึ่ง parenting อย่างเดียว
        // - กัน Canvas เอียง/หมุนตาม enemy เวลา enemy หัน/flip sprite
        // - LateUpdate เพื่อให้รันหลังจาก enemy ขยับใน Update แล้ว กันอาการสั่น/ล้าเฟรม
        if (healthCanvas == null) return;

        healthCanvas.transform.position = transform.position + offset;
        healthCanvas.transform.rotation = Quaternion.identity;
    }

    // วาดกล่องพรีวิวตำแหน่ง offset ใน Scene view — ช่วยจัดตำแหน่ง health bar
    // ก่อนกด Play โดยไม่ต้องรัน Instantiate จริง
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f); // แดงโปร่งแสง ให้เห็นชัดว่าเป็นพรีวิว
        Vector3 previewPos = transform.position + offset;
        Gizmos.DrawCube(previewPos, gizmoPreviewSize);

        // เส้นขอบทึบ ช่วยให้เห็นขอบเขตชัดขึ้นเวลากล่องเล็ก
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(previewPos, gizmoPreviewSize);
    }
}