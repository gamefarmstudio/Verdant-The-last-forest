using UnityEngine;

// [แก้] ขับ shader น้ำจากเกจ PollutionManager (เกจเดียวกับด่าน 1 — ขึ้นเองตามเวลา + ปุ่มชำระล้างลด)
//   - _Pollution : สีน้ำ ไล่ ฟ้า(สะอาด) → ม่วง(พิษ)
//   - [เพิ่ม] _SurfaceX : ขอบพิษ/ความลึก คืบตามเกจ (พิษมาก = ขอบเลื่อนลามไปทั่ว) เหมือนหมอกด่านแรก
//   set ค่าลง material ตรงๆ (เสถียรกว่า MaterialPropertyBlock กับ shader เขียนมือ)
[ExecuteAlways]
public class WaterPollutionShaderDriver : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("TilemapRenderer/SpriteRenderer ของน้ำ (ที่ใช้ material WaterClear2D)")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Pollution → สีน้ำ")]
    [SerializeField] private string pollutionProp = "_Pollution";

    [Header("Pollution → ขอบพิษ (Surface X)")]
    [SerializeField] private bool driveSurfaceX = true;
    [SerializeField] private string surfaceXProp = "_SurfaceX";
    [Tooltip("ตำแหน่ง Surface X ตอนสะอาด (pollution = 0) — พิษน้อย")]
    [SerializeField] private float surfaceXClean = -10f;
    [Tooltip("ตำแหน่ง Surface X ตอนพิษเต็ม (pollution = 1) — พิษลามไปทั่ว")]
    [SerializeField] private float surfaceXPolluted = 10f;

    [Header("Response")]
    [SerializeField] private float smoothSpeed = 2f;

    [Header("Editor Preview")]
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewGauge = 0.7f;

    private float _target, _shown;
    private Material _mat;

    void OnEnable()
    {
        _mat = null;
        GameEvents.OnPollutionChanged += SetGauge;
        if (PollutionManager.Instance != null) SetGauge(PollutionManager.Instance.Gauge);
    }

    void OnDisable() => GameEvents.OnPollutionChanged -= SetGauge;

    public void SetGauge(float g)
    {
        _target = Mathf.Clamp01(g);
        if (Time.frameCount <= 1) _shown = _target;
    }

    Material GetMat()
    {
        if (_mat != null) return _mat;
        if (targetRenderer == null) return null;
        // edit mode ใช้ sharedMaterial (ห้าม instantiate) / play mode ใช้ instance
        _mat = Application.isPlaying ? targetRenderer.material : targetRenderer.sharedMaterial;
        return _mat;
    }

    void Update()
    {
        var m = GetMat();
        if (m == null) return;

        if (Application.isPlaying)
            _shown = Mathf.MoveTowards(_shown, _target, smoothSpeed * Time.deltaTime);
        else
            _shown = _target = editorPreviewGauge;

        m.SetFloat(pollutionProp, _shown);
        if (driveSurfaceX)
            m.SetFloat(surfaceXProp, Mathf.Lerp(surfaceXClean, surfaceXPolluted, _shown));
    }
}
