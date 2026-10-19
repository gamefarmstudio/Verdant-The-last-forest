using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;           
using Verdant.Combat;

public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] TowerBuildPopup popup;                    
    [SerializeField] TowerUpgradePopup upgradePopup;          

    [Header("Sell")]
    [Range(0f, 1f)]
    [SerializeField] float sellRefundPercent = 0.6f;          

    TowerSlot _activeSlot = null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OpenPopup(TowerSlot slot)
    {
        if (_activeSlot != null && _activeSlot != slot) _activeSlot.SetSelected(false);
        _activeSlot = slot;
        slot.SetSelected(true);
        popup.Show(slot);
    }

    public void OpenUpgrade(TowerSlot slot)
    {
        if (_activeSlot != null && _activeSlot != slot) _activeSlot.SetSelected(false);
        _activeSlot = slot;
        slot.SetSelected(true);
        upgradePopup.Show(slot);
    }

    public void ClosePopup()
    {
        if (popup != null) popup.Hide();
        if (upgradePopup != null) upgradePopup.Hide();
        if (_activeSlot != null)
        {
            _activeSlot.SetSelected(false);
            _activeSlot = null;
        }
    }

    public void OnTowerSelected(GameObject towerPrefab, int cost)
    {
        if (_activeSlot == null) { Debug.LogError("[TowerPlacement] _activeSlot is NULL"); return; }
        if (!CurrencyManager.Instance.CanAfford(cost)) { Debug.LogWarning("ไม่มีเงินพอ"); return; }

        CurrencyManager.Instance.Spend(cost);
        _activeSlot.Build(towerPrefab);
        _activeSlot = null;
        popup.Hide();
    }

    public void UpgradeActiveTower()
    {
        if (_activeSlot == null) return;

        var tower = _activeSlot.TowerComponent;
        if (tower == null) { Debug.LogWarning("[Upgrade] ไม่พบ Tower ใน slot"); return; }

        var next = TowerDatabase.Instance != null ? TowerDatabase.Instance.GetNextLevel(tower.TowerId) : null;
        GameObject nextPrefab = tower.NextLevelPrefab;   
        if (next == null || nextPrefab == null)
        {
            Debug.Log($"[Upgrade] '{tower.TowerId}' เลเวลสูงสุด หรือไม่ได้ตั้ง Next Level Prefab");
            return;
        }

        if (!CurrencyManager.Instance.CanAfford(next.cost)) { Debug.LogWarning("[Upgrade] เงินไม่พอ"); return; }

        CurrencyManager.Instance.Spend(next.cost);
        _activeSlot.ReplaceTower(nextPrefab,(this.gameObject),true); 

        // feedback: เด้งป้อมใหม่นิดๆ
        if (_activeSlot.placedTower != null)
        {
            _activeSlot.placedTower.transform.DOKill();
            _activeSlot.placedTower.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f).SetLink(_activeSlot.placedTower);
        }

        upgradePopup.Refresh();       
        Debug.Log($"[Upgrade] → {next.id} (จ่าย {next.cost})");
    }

    public void SellActiveTower()
    {
        if (_activeSlot == null) return;

        int refund = SellRefund(_activeSlot.TowerComponent);
        if (refund > 0) CurrencyManager.Instance.Earn(refund);

        _activeSlot.ClearTower();
        _activeSlot = null;
        upgradePopup.Hide();
        Debug.Log($"[Sell] คืน {refund} crystal");
    }

    public int SellRefund(IUpgradableTower tower)
    {
        if (tower == null || TowerDatabase.Instance == null) return 0;
        var rec = TowerDatabase.Instance.GetByID(tower.TowerId);
        if (rec == null) return 0;
        return Mathf.RoundToInt(rec.cumulativeCost * sellRefundPercent);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ClosePopup();   
    }
}
