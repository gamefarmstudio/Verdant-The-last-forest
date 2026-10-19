using UnityEngine;
using UnityEngine.UI;

public class NextWaveButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }

    void Start()
    {
        if (button != null) button.onClick.AddListener(OnClick);
        SetVisible(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveEnd.AddListener(HandleWaveEnd);
            GameManager.Instance.OnWaveStart.AddListener(HandleWaveStart);
        }
        else
        {
            Debug.LogError("[NextWaveButtonUI] ไม่พบ GameManager.Instance — ปุ่มเริ่มเวฟถัดไปจะไม่ทำงาน");
        }
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveEnd.RemoveListener(HandleWaveEnd);
            GameManager.Instance.OnWaveStart.RemoveListener(HandleWaveStart);
        }
    }

    void HandleWaveEnd(int wave)
    {
        if (wave >= GameManager.Instance.totalWaves) return;
        SetVisible(true);
    }

    void HandleWaveStart(int wave) => SetVisible(false);

    void OnClick()
    {
        SetVisible(false);
        GameManager.Instance.StartNextWave();
    }

    void SetVisible(bool on)
    {
        if (button != null) button.gameObject.SetActive(on);
    }
}
