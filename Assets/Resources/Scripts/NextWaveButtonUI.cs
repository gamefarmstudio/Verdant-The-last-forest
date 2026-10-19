using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "Next Wave" button.
///   - Appears when a wave ends (GameManager.OnWaveEnd), except after the last wave (that's a win, no button needed).
///   - Clicking it calls GameManager.StartNextWave().
///   - Hides again when the next wave starts (GameManager.OnWaveStart).
/// Usage: put this on an HUD object that is always active and drag the (child) Button into the button field.
/// </summary>
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
            Debug.LogError("[NextWaveButtonUI] GameManager.Instance not found — the Next Wave button will not work");
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
        // The last wave just ended = level won, so no "next wave" button.
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
