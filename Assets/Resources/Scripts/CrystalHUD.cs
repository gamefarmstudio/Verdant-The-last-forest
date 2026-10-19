using UnityEngine;
using TMPro;

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

    public void UpdateText(int newAmount)
    {
        crystalText.text = newAmount.ToString();
    }
}