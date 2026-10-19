using UnityEngine;
using TMPro;

/// <summary>
/// Shows the player's current crystal (money) amount on the HUD and keeps it in sync
/// with CurrencyManager.OnCrystalChanged.
/// </summary>
public class CrystalHUD : MonoBehaviour
{
    [SerializeField] TMP_Text crystalText;

    void Start()
    {
        crystalText.text = CurrencyManager.Instance.Crystal.ToString();

        CurrencyManager.Instance.OnCrystalChanged.AddListener(OnCrystalChanged);
    }

    void OnCrystalChanged(int newAmount)
    {
        crystalText.text = newAmount.ToString();
    }

    void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCrystalChanged.RemoveListener(OnCrystalChanged);
    }

    /// <summary>Sets the displayed amount directly (can be wired to a UnityEvent in the Inspector).</summary>
    public void UpdateText(int newAmount)
    {
        crystalText.text = newAmount.ToString();
    }
}