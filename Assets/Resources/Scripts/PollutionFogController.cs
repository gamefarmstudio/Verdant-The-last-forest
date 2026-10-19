using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

/// <summary>
/// Drives the pollution fog visual effect. The fog covers the map from the left edge up to
/// a point proportional to the pollution gauge (0 = no fog, 1 = whole map), and moves smoothly
/// toward the new value whenever GameEvents.OnPollutionChanged fires.
/// Can optionally also drive a gauge Slider and colour its fill with a gradient.
/// Runs in Edit mode too ([ExecuteAlways]) so the fog can be previewed with editorPreviewGauge.
/// Also used by TowerSpot to work out at what gauge value a tower slot becomes uncovered.
/// </summary>
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
    [SerializeField] private float smoothSpeed = 3f;    // how fast the shown fog catches up to the real gauge
    [SerializeField] private float densityMin = 50f;
    [SerializeField] private float densityMax = 300f;

    [Header("Editor Preview")]
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewGauge = 0.7f;   // gauge shown while not in Play mode


    [Header("Gauge UI (optional)")]
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient dangerColor;

    // Exposed property names on the fog VFX Graph.
    static readonly int CenterID  = Shader.PropertyToID("FogCenter");
    static readonly int SizeID    = Shader.PropertyToID("FogAreaSize");
    static readonly int DensityID = Shader.PropertyToID("Density");

    private float _targetGauge, _shownGauge;   // real gauge value vs. the smoothed one being displayed

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

    /// <summary>Sets the target gauge (0..1). On the first frame it snaps instead of animating.</summary>
    public void SetGauge(float g)
    {
        _targetGauge = Mathf.Clamp01(g);
        if (Time.frameCount <= 1) _shownGauge = _targetGauge;
    }

    /// <summary>Converts a world X position into the gauge value whose fog edge sits exactly there.</summary>
    public float GaugeAtX(float worldX)
        => Mathf.InverseLerp(mapLeftX, mapRightX, worldX);

    void Update()
    {
        // In Play mode ease toward the real value; in Edit mode just show the preview value.
        if (Application.isPlaying)
            _shownGauge = Mathf.MoveTowards(_shownGauge, _targetGauge, smoothSpeed * Time.deltaTime);
        else
            _shownGauge = _targetGauge = editorPreviewGauge;

        // The fog spans from the left edge of the map to edgeX.
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
