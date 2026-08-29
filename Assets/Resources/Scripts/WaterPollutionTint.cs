using UnityEngine;
using UnityEngine.Tilemaps;

// [เพิ่ม] มลพิษทางน้ำ (ด่าน 2) แบบไม่ใช้ shader — ย้อมสี Tilemap น้ำตามเกจมลพิษ
//   ฟัง GameEvents.OnPollutionChanged (ใช้ PollutionManager ตัวเดิมของด่าน 1)
//   เกจ 0% = น้ำสะอาด (cleanColor) → 100% = น้ำพิษ (toxicColor)
//   ใช้แทน PollutionFogController ในด่านน้ำ (ไม่ต้องมี VFX/shader)
[ExecuteAlways]
public class WaterPollutionTint : MonoBehaviour
{
    [Header("เป้าหมายที่จะย้อมสี")]
    [Tooltip("Tilemap ของน้ำ — จะถูกย้อมสีตามระดับมลพิษ")]
    [SerializeField] private Tilemap waterTilemap;
    [Tooltip("(ไม่บังคับ) คราบน้ำมัน/ฟองลอย — จางไปชัดขึ้นตามมลพิษ")]
    [SerializeField] private SpriteRenderer scumOverlay;

    [Header("สี")]
    [Tooltip("สีน้ำปกติ (คูณกับสี tile เดิม — ขาวล้วน = สีเดิม)")]
    [SerializeField] private Color cleanColor = Color.white;
    [Tooltip("สีน้ำตอนพิษเต็ม (เขียวพิษ/น้ำมัน)")]
    [SerializeField] private Color toxicColor = new Color(0.45f, 0.7f, 0.4f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float maxScumAlpha = 0.6f;

    [Header("Response")]
    [SerializeField] private float smoothSpeed = 2f;

    [Header("Editor Preview")]
    [Tooltip("พรีวิวใน edit mode — ตั้งเท่ากับ PollutionManager.startGauge (0.7)")]
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewGauge = 0.7f;

    private float _target, _shown;

    void OnEnable()
    {
        GameEvents.OnPollutionChanged += SetGauge;
        if (PollutionManager.Instance != null) SetGauge(PollutionManager.Instance.Gauge);
    }

    void OnDisable() => GameEvents.OnPollutionChanged -= SetGauge;

    public void SetGauge(float g)
    {
        _target = Mathf.Clamp01(g);
        if (Time.frameCount <= 1) _shown = _target;
    }

    void Update()
    {
        // edit mode: พรีวิวทันที / play mode: ไล่ค่าจริงแบบ smooth
        if (Application.isPlaying)
            _shown = Mathf.MoveTowards(_shown, _target, smoothSpeed * Time.deltaTime);
        else
            _shown = _target = editorPreviewGauge;

        if (waterTilemap != null)
            waterTilemap.color = Color.Lerp(cleanColor, toxicColor, _shown);

        if (scumOverlay != null)
        {
            var c = scumOverlay.color;
            c.a = Mathf.Lerp(0f, maxScumAlpha, _shown);
            scumOverlay.color = c;
        }
    }
}
