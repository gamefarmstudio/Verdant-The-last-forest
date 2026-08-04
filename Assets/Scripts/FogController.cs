using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

[ExecuteAlways]
public class FogController : MonoBehaviour
{
    [SerializeField] private VisualEffect FogEffect;
    [Header("Map Boundaries")]
    [SerializeField] private float MapLeftX = -8f;
    [SerializeField] private float MapRightX = 8f;
    [SerializeField] private float MapCenterY = 0f;
    [SerializeField] private float MapHeight = 9f;

    [Header("Fog Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float FogGauge = 0.5f;
    [SerializeField] private Slider FogSlider;

    static readonly int CenterID = Shader.PropertyToID("FogCenter");
    static readonly int SizeID = Shader.PropertyToID("FogAreaSize");
    [Range(0f, 10f)]
    [SerializeField] private float FogSmoothness = 0.5f;

    private float _showGuage;

    void OnEnable()
    {
        if (FogSlider != null)
        {
            FogSlider.minValue = 0f;
            FogSlider.maxValue = 1f;
            FogSlider.value = FogGauge;
        }
    }




    void Update()
    {
        if (FogEffect == null) return;
        _showGuage = Application.isPlaying ? Mathf.MoveTowards(_showGuage, FogGauge, Time.deltaTime * FogSmoothness) : FogGauge;

        if (_showGuage <= 0f)
        {
            FogEffect.enabled = false;
            return;
        }

        FogEffect.enabled = true;
        // 0 = no fog, 1 = fog covers entire map (left → right)
        float edgeX = Mathf.Lerp(MapLeftX, MapRightX, _showGuage);
        float width = edgeX - MapLeftX;
        float centerX = (MapLeftX + edgeX) * 0.5f;
        FogEffect.SetVector3(CenterID, new Vector3(centerX, MapCenterY, 0f));
        FogEffect.SetVector3(SizeID, new Vector3(width, MapHeight, 0.1f));
        FogSlider.value = _showGuage;
    }
    public void cleanse(float percent)
    {
        if (Object.FindAnyObjectByType<CurrencyManager>().Spend(10))   // ใช้เงิน 10 crystal ต่อการ cleanse 1%
        {
            FogGauge = Mathf.Clamp01(FogGauge - percent / 100f);
        }
    }
}
