using UnityEngine;
using UnityEngine.UI;   // [เพิ่ม] ต้องใช้ Slider / Image ในไฟล์นี้
using UnityEngine.VFX;

// [แก้] เขียนใหม่ (ล้างคอมเมนต์เดิม) + [เพิ่ม] รับ Slider และสีเกจในตัว
//   เป้าหมาย: แทน FogController เก่าได้ครบในคอมโพเนนต์เดียว (คุมทั้ง VFX หมอก + แถบเกจ)
//   แหล่งความจริงของค่าเกจยังคือ PollutionManager — ฟังผ่าน GameEvents.OnPollutionChanged
// [เพิ่ม] ExecuteAlways — ให้ Update รันใน edit mode ด้วย จะได้พรีวิวหมอกก่อนกด Play
//        และภาพก่อน/หลัง Play ตรงกัน ไม่กระโดด/หด (สาเหตุเดิม: edit mode ไม่รัน Update เลย)
[ExecuteAlways]
public class PollutionFogController : MonoBehaviour
{
    [SerializeField] private VisualEffect fogVFX;

    [Header("Map bounds (world space)")]
    [SerializeField] private float mapLeftX   = -8f;
    [SerializeField] private float mapRightX  =  8f;
    [SerializeField] private float mapCenterY =  0f;
    [SerializeField] private float mapHeight  =  9f;

    [Header("Response")]
    [SerializeField] private float smoothSpeed = 3f;
    [SerializeField] private float densityMin = 50f;
    [SerializeField] private float densityMax = 300f;

    // [เพิ่ม] ค่าหมอกสำหรับ "พรีวิวใน edit mode เท่านั้น" (ตอนเล่นจริงใช้ค่าจาก PollutionManager)
    //        ตั้งให้เท่ากับ PollutionManager.startGauge (0.7) แล้วภาพก่อน/หลัง Play จะตรงกัน
    [Header("Editor Preview")]
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewGauge = 0.7f;

    // [เพิ่ม] แถบเกจในตัว (ย้ายแนวคิดมาจาก FogController เก่า) — ลาก Slider มาใส่ได้เลย
    //         ไม่ต้องพึ่ง PollutionGaugeUI แยกอีก (อย่าใส่ทั้งสองตัวคุม slider เดียวกัน)
    [Header("Gauge UI (optional)")]
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private Image fillImage;       // ปล่อยว่างได้ถ้าไม่ต้องการเปลี่ยนสี
    [SerializeField] private Gradient dangerColor;  // 0 = เขียวสะอาด, 1 = ม่วงพิษ

    static readonly int CenterID  = Shader.PropertyToID("FogCenter");
    static readonly int SizeID    = Shader.PropertyToID("FogAreaSize");
    static readonly int DensityID = Shader.PropertyToID("Density");

    private float _targetGauge, _shownGauge;

    void OnEnable()
    {
        GameEvents.OnPollutionChanged += SetGauge;

        // [เพิ่ม] ตั้งช่วง slider เป็น 0..1 และปิด interactable (โชว์อย่างเดียว)
        if (gaugeSlider != null)
        {
            gaugeSlider.minValue = 0f;
            gaugeSlider.maxValue = 1f;
            gaugeSlider.interactable = false;
        }

        // [เพิ่ม] sync ค่าเริ่มต้นทันทีถ้า PollutionManager พร้อมแล้ว
        if (PollutionManager.Instance != null) SetGauge(PollutionManager.Instance.Gauge);
    }

    void OnDisable() => GameEvents.OnPollutionChanged -= SetGauge;

    public void SetGauge(float g)
    {
        _targetGauge = Mathf.Clamp01(g);
        if (Time.frameCount <= 1) _shownGauge = _targetGauge;
    }

    // TowerSpot ใช้ค่านี้เพื่อรู้ว่าจุดวางป้อมของตัวเองพ้นหมอกที่เกจเท่าไร
    public float GaugeAtX(float worldX)
        => Mathf.InverseLerp(mapLeftX, mapRightX, worldX);

    void Update()
    {
        // [เพิ่ม] edit mode: ใช้ editorPreviewGauge เห็นผลทันที (ไม่ smooth)
        //        play mode: ไล่ค่าจริงจาก PollutionManager แบบ smooth เหมือนเดิม
        if (Application.isPlaying)
            _shownGauge = Mathf.MoveTowards(_shownGauge, _targetGauge, smoothSpeed * Time.deltaTime);
        else
            _shownGauge = _targetGauge = editorPreviewGauge;

        float edgeX = Mathf.Lerp(mapLeftX, mapRightX, _shownGauge);
        float width = Mathf.Max(0.01f, edgeX - mapLeftX);

        // [แก้] เช็ค null ก่อนใช้ fogVFX กันพังถ้าลืมลาก VisualEffect
        if (fogVFX != null)
        {
            fogVFX.SetVector3(CenterID, new Vector3((mapLeftX + edgeX) * 0.5f, mapCenterY, 0f));
            fogVFX.SetVector3(SizeID,   new Vector3(width, mapHeight, 0.1f));
            //fogVFX.SetFloat(DensityID,  Mathf.Lerp(densityMin, densityMax, _shownGauge));
        }

        // [เพิ่ม] ขับแถบเกจตามค่าที่ smooth แล้ว ให้ภาพหมอกกับเกจตรงกัน
        if (gaugeSlider != null) gaugeSlider.value = _shownGauge;
        if (fillImage != null && dangerColor != null) fillImage.color = dangerColor.Evaluate(_shownGauge);
    }
}
