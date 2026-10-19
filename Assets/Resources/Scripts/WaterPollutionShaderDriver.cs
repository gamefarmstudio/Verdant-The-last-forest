using UnityEngine;

/// <summary>
/// Drives the water shader from the PollutionManager gauge (the same gauge as level 1 —
/// rises over time, lowered by the Cleanse button). Used for the water level (level 2).
///   - _Pollution : water colour, from blue (clean) to purple (toxic)
///   - _SurfaceX  : the toxic edge / depth line, which creeps along with the gauge
///                  (more pollution = the edge spreads further), like the fog in level 1
/// Values are set on the material directly (more reliable than a MaterialPropertyBlock with a hand-written shader).
/// Runs in Edit mode too, using editorPreviewGauge.
/// </summary>
[ExecuteAlways]
public class WaterPollutionShaderDriver : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The water TilemapRenderer/SpriteRenderer (the one using the WaterClear2D material)")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Pollution -> water colour")]
    [SerializeField] private string pollutionProp = "_Pollution";

    [Header("Pollution -> toxic edge (Surface X)")]
    [SerializeField] private bool driveSurfaceX = true;
    [SerializeField] private string surfaceXProp = "_SurfaceX";
    [Tooltip("Surface X position when clean (pollution = 0) — little pollution")]
    [SerializeField] private float surfaceXClean = -10f;
    [Tooltip("Surface X position when fully polluted (pollution = 1) — pollution everywhere")]
    [SerializeField] private float surfaceXPolluted = 10f;

    [Header("Response")]
    [SerializeField] private float smoothSpeed = 2f;

    [Header("Editor Preview")]
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewGauge = 0.7f;

    private float _target, _shown;   // real gauge vs. the smoothed value being displayed
    private Material _mat;

    void OnEnable()
    {
        _mat = null;
        GameEvents.OnPollutionChanged += SetGauge;
        if (PollutionManager.Instance != null) SetGauge(PollutionManager.Instance.Gauge);
    }

    void OnDisable() => GameEvents.OnPollutionChanged -= SetGauge;

    /// <summary>Sets the target gauge (0..1). On the first frame it snaps instead of animating.</summary>
    public void SetGauge(float g)
    {
        _target = Mathf.Clamp01(g);
        if (Time.frameCount <= 1) _shown = _target;
    }

    Material GetMat()
    {
        if (_mat != null) return _mat;
        if (targetRenderer == null) return null;
        // Edit mode uses sharedMaterial (must not instantiate) / Play mode uses an instance
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
