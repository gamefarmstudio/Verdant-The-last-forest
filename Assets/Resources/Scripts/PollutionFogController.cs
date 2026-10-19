using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.VFX;

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

    [Header("Editor Preview")]
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewGauge = 0.7f;


    [Header("Gauge UI (optional)")]
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private Image fillImage;  
    [SerializeField] private Gradient dangerColor;

    static readonly int CenterID  = Shader.PropertyToID("FogCenter");
    static readonly int SizeID    = Shader.PropertyToID("FogAreaSize");
    static readonly int DensityID = Shader.PropertyToID("Density");

    private float _targetGauge, _shownGauge;

    void OnEnable()
    {
        GameEvents.OnPollutionChanged += SetGauge;


        if (gaugeSlider != null)
        {
            gaugeSlider.minValue = 0f;
            gaugeSlider.maxValue = 1f;
            gaugeSlider.interactable = false;
        }

        if (PollutionManager.Instance != null) SetGauge(PollutionManager.Instance.Gauge);
    }

    void OnDisable() => GameEvents.OnPollutionChanged -= SetGauge;

    public void SetGauge(float g)
    {
        _targetGauge = Mathf.Clamp01(g);
        if (Time.frameCount <= 1) _shownGauge = _targetGauge;
    }

    public float GaugeAtX(float worldX)
        => Mathf.InverseLerp(mapLeftX, mapRightX, worldX);

    void Update()
    {
        
        if (Application.isPlaying)
            _shownGauge = Mathf.MoveTowards(_shownGauge, _targetGauge, smoothSpeed * Time.deltaTime);
        else
            _shownGauge = _targetGauge = editorPreviewGauge;

        float edgeX = Mathf.Lerp(mapLeftX, mapRightX, _shownGauge);
        float width = Mathf.Max(0.01f, edgeX - mapLeftX);

        
        if (fogVFX != null)
        {
            fogVFX.SetVector3(CenterID, new Vector3((mapLeftX + edgeX) * 0.5f, mapCenterY, 0f));
            fogVFX.SetVector3(SizeID,   new Vector3(width, mapHeight, 0.1f));
            
        }

        if (gaugeSlider != null) gaugeSlider.value = _shownGauge;
        if (fillImage != null && dangerColor != null) fillImage.color = dangerColor.Evaluate(_shownGauge);
    }
}
