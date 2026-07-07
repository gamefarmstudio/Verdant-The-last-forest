// ═══════════════════════════════════════════════════════════════
//  PollutionFogController.cs — แปลงค่าเกจเป็นกล่องหมอกใน VFX Graph
//  ตรึงขอบซ้าย เลื่อนขอบขวาตามเกจ + smoothing ด้วย MoveTowards
//  ต้องมี exposed property บน Blackboard: FogCenter, FogAreaSize, Density
//  วางบน GameObject ที่มีคอมโพเนนต์ Visual Effect
// ═══════════════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.VFX;

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

    static readonly int CenterID  = Shader.PropertyToID("FogCenter");
    static readonly int SizeID    = Shader.PropertyToID("FogAreaSize");
    static readonly int DensityID = Shader.PropertyToID("Density");

    private float _targetGauge, _shownGauge;

    void OnEnable()  => GameEvents.OnPollutionChanged += SetGauge;
    void OnDisable() => GameEvents.OnPollutionChanged -= SetGauge;

    public void SetGauge(float g)
    {
        _targetGauge = Mathf.Clamp01(g);
        if (Time.frameCount <= 1) _shownGauge = _targetGauge;  // ค่าแรกตอนเริ่มด่าน ไม่ต้องไล่
    }

    /// แปลงตำแหน่ง world X → ค่าเกจที่หมอก "ยังคลุมถึง" จุดนั้นพอดี (TowerSpot ใช้)
    public float GaugeAtX(float worldX)
        => Mathf.InverseLerp(mapLeftX, mapRightX, worldX);

    void Update()
    {
        _shownGauge = Mathf.MoveTowards(_shownGauge, _targetGauge,
                                        smoothSpeed * Time.deltaTime);

        float edgeX = Mathf.Lerp(mapLeftX, mapRightX, _shownGauge);
        float width = Mathf.Max(0.01f, edgeX - mapLeftX);

        fogVFX.SetVector3(CenterID, new Vector3((mapLeftX + edgeX) * 0.5f, mapCenterY, 0f));
        fogVFX.SetVector3(SizeID,   new Vector3(width, mapHeight, 0.1f));
        fogVFX.SetFloat(DensityID,  Mathf.Lerp(densityMin, densityMax, _shownGauge));
    }
}
