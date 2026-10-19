using UnityEngine;

public class TowerSpot : MonoBehaviour
{
    [SerializeField] private float clearMargin = 0.5f;  
    [SerializeField] private GameObject lockedVisual; 
    [SerializeField] private GameObject unlockedVisual;

    public bool IsUnlocked { get; private set; }
    private float _requiredGauge;
    private TowerSlot _slot;
    private bool _initialized;   

    void Start()
    {
        _slot = GetComponent<TowerSlot>();

        var fog = FindAnyObjectByType<PollutionFogController>();
        if (fog == null)
        {
            Debug.LogError($"[TowerSpot] ไม่พบ PollutionFogController ในฉาก — '{name}' จะถูกล็อกถาวร", this);
            return;  
        }


        _requiredGauge = fog.GaugeAtX(transform.position.x - clearMargin);

        GameEvents.OnPollutionChanged += Check;

        if (PollutionManager.Instance != null)
            Check(PollutionManager.Instance.Gauge);   
        else
            Debug.LogWarning($"[TowerSpot] ไม่พบ PollutionManager — '{name}' จะรอ sync จาก event แรก", this);
    }

    void OnDestroy() => GameEvents.OnPollutionChanged -= Check;

    void Check(float gauge)
    {
        if (_slot != null && _slot.isOccupied) return;

        bool unlocked = gauge < _requiredGauge;

        if (_initialized && unlocked == IsUnlocked) return;
        _initialized = true;

        IsUnlocked = unlocked;
        if (lockedVisual)   lockedVisual.SetActive(!unlocked);
        if (unlockedVisual) unlockedVisual.SetActive(unlocked);
    }
}
