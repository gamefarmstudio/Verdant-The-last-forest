using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Water pollution (level 2) without a shader — tints the water Tilemap by the pollution gauge.
///   Listens to GameEvents.OnPollutionChanged (uses the same PollutionManager as level 1).
///   Gauge 0% = clean water (cleanColor) -> 100% = toxic water (toxicColor).
///   An alternative to PollutionFogController for water levels (no VFX/shader needed).
/// Runs in Edit mode too, using editorPreviewGauge.
/// </summary>
[ExecuteAlways]
public class WaterPollutionTint : MonoBehaviour
{
    [Header("What to tint")]
    [Tooltip("The water Tilemap — tinted according to the pollution level")]
    [SerializeField] private Tilemap waterTilemap;
    [Tooltip("(Optional) oil slick / floating scum — fades in as pollution rises")]
    [SerializeField] private SpriteRenderer scumOverlay;

    [Header("Colours")]
    [Tooltip("Normal water colour (multiplied with the tile colour — pure white = original colour)")]
    [SerializeField] private Color cleanColor = Color.white;
    [Tooltip("Water colour at full pollution (toxic green / oil)")]
    [SerializeField] private Color toxicColor = new Color(0.45f, 0.7f, 0.4f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float maxScumAlpha = 0.6f;   // scum opacity at 100% pollution

    [Header("Response")]
    [SerializeField] private float smoothSpeed = 2f;

    [Header("Editor Preview")]
    [Tooltip("Edit-mode preview — set it to PollutionManager.startGauge (0.7)")]
    [Range(0f, 1f)]
    [SerializeField] private float editorPreviewGauge = 0.7f;

    private float _target, _shown;   // real gauge vs. the smoothed value being displayed

    void OnEnable()
    {
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

    void Update()
    {
        // Edit mode: show the preview immediately / Play mode: ease toward the real value
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
