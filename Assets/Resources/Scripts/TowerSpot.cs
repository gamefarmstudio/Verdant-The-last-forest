using UnityEngine;

/// <summary>
/// Locks a tower slot while it is covered by pollution fog.
/// At start it asks PollutionFogController which gauge value puts the fog edge just past this
/// slot; whenever the gauge drops below that value the slot unlocks (and can be built on),
/// and it locks again if the fog grows back. Swaps between a locked and an unlocked visual.
/// TowerSlot checks IsUnlocked before opening the build menu.
/// </summary>
public class TowerSpot : MonoBehaviour
{
    [SerializeField] private float clearMargin = 0.5f;   // how far past the slot the fog must recede before it unlocks
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    public bool IsUnlocked { get; private set; }
    private float _requiredGauge;   // gauge must be below this for the slot to be clear of fog
    private TowerSlot _slot;
    private bool _initialized;      // forces the first Check() to apply the visuals

    void Start()
    {
        _slot = GetComponent<TowerSlot>();

        var fog = FindAnyObjectByType<PollutionFogController>();
        if (fog == null)
        {
            Debug.LogError($"[TowerSpot] PollutionFogController not found in the scene — '{name}' will stay locked permanently", this);
            return;
        }


        _requiredGauge = fog.GaugeAtX(transform.position.x - clearMargin);

        GameEvents.OnPollutionChanged += Check;

        if (PollutionManager.Instance != null)
            Check(PollutionManager.Instance.Gauge);
        else
            Debug.LogWarning($"[TowerSpot] PollutionManager not found — '{name}' will wait to sync from the first event", this);
    }

    void OnDestroy() => GameEvents.OnPollutionChanged -= Check;

    void Check(float gauge)
    {
        // A slot that already has a tower stays as it is.
        if (_slot != null && _slot.isOccupied) return;

        bool unlocked = gauge < _requiredGauge;

        if (_initialized && unlocked == IsUnlocked) return;   // no change
        _initialized = true;

        IsUnlocked = unlocked;
        if (lockedVisual)   lockedVisual.SetActive(!unlocked);
        if (unlockedVisual) unlockedVisual.SetActive(unlocked);
    }
}
