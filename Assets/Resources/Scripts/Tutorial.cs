using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// In-level tutorial popup with several pages (Next / Previous / Close buttons).
/// Each page is one string written as "HEADER | BODY TEXT"; an optional picture per page pops in.
/// While it is open the game runs at tutorialTimeScale (slow motion) and the Settings menu can't be opened;
/// closing it restores the speed the player chose. The popup fades in when enabled and out when closed.
/// </summary>
public class Tutorial : MonoBehaviour
{
    [Header("Tutorial Content Data")]
    [Tooltip("Use the | symbol to separate the header from the body, e.g. HEADER | BODY TEXT")]
    [TextArea(3, 5)]
    [SerializeField] private List<string> tutorialSteps = new List<string>()
    {
        "NEW THREAT APPROACHING | Welcome to Level 2, the Coastal Reef. Water tiles block your ground towers, and enemy threats intensify. Watch out for Plastic Bombers entering the battlefield. They explode on impact, dealing heavy area-of-effect damage to your defenses.",
        "SUPPORT TOWER: CRYSTAL SPRING | The pollution is intensifying on the shores. Build a Crystal Spring tower near the path. It sprays pure water to activate an AoE Slow mechanic, slowing down fast-moving toxic groups and giving your defensive units more time to strike.",
        "HAZARD WARNING: OIL LIGHTERS | The enemy forces are pushing harder. Beware of Oil Lighters entering the fray. They will set active pollution puddles on fire, creating dangerous hazards that deal heavy damage-over-time to any units caught in the blaze.",
        "MANAGE POLLUTION LEVELS | Do not give up ground to the corruption. You must use your Cleanse mechanics to reduce the overall pollution levels before they max out. Keep the contamination meter down at all costs, or it is Game Over.",
        "BOSS WAVE: OIL BEHEMOTH | Warning. The final wave of this biome is here and the Oil Behemoth boss has arrived. This massive ground threat leaves a sticky oil trail that accelerates enemy movement and explodes with massive area damage upon death. Concentrate all your fire to bring it down."
    };

    [Header("UI Object References")]
    [SerializeField] private TMP_Text tutorialHeaderTMPText; // text box for the header
    [SerializeField] private TMP_Text tutorialBodyTMPText;   // text box for the body
    [SerializeField] private List<GameObject> tutorialImages;   // one picture per page (same index as tutorialSteps)
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private CanvasGroup tutorialCanvasGroup;   // faded in/out as a whole

    [Header("Time Settings")]
    [Tooltip("0 = Paused, 1 = Normal Speed")]
    [Range(0f, 1f)]
    [SerializeField] private float tutorialTimeScale = 0.1f;

    private int currentStepIndex = 0;

    void Awake()
    {
        // Clear any listeners set in the Inspector, then wire the buttons up in code (prevents double calls).
        if (nextButton != null) nextButton.onClick.RemoveAllListeners();
        if (prevButton != null) prevButton.onClick.RemoveAllListeners();
        if (closeButton != null) closeButton.onClick.RemoveAllListeners();

        if (nextButton != null) nextButton.onClick.AddListener(NextStep);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousStep);
        if (closeButton != null) closeButton.onClick.AddListener(CloseTutorial);

        if (tutorialCanvasGroup == null)
        {
            tutorialCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    /// <summary>True while a tutorial popup is showing (SettingsPage checks this).</summary>
    public static bool IsOpen { get; private set; }

    void OnEnable()
    {
        IsOpen = true;
        // If Settings is still open, close it first so the two windows don't overlap.
        if (SettingsPage.Instance != null && SettingsPage.Instance.IsOpen)
            SettingsPage.Instance.HideSettingsMenu();

        Time.timeScale = tutorialTimeScale;

        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.DOKill();
            tutorialCanvasGroup.alpha = 0f;
            tutorialCanvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        ShowTutorialStep(0);
    }

    void OnDisable()
    {
        IsOpen = false;
        GameSpeed.RestoreCurrent();   // back to the speed the player chose (1x/2x) instead of forcing 1x
    }

    /// <summary>Shows one page: fades in its header/body text and pops in its picture.</summary>
    public void ShowTutorialStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= tutorialSteps.Count)
        {
            return;
        }

        currentStepIndex = stepIndex;

        // Split the header and body on the '|' separator
        string rawText = tutorialSteps[stepIndex];
        string headerString = "TUTORIAL"; // default when there's no separator
        string bodyString = rawText;

        if (rawText.Contains("|"))
        {
            string[] splitText = rawText.Split('|');
            headerString = splitText[0].Trim();
            bodyString = splitText[1].Trim();
        }

        // 1. Text animation: fade the header and body in together
        AnimateTextField(tutorialHeaderTMPText, headerString);
        AnimateTextField(tutorialBodyTMPText, bodyString);

        // 2. Picture animation: spring/pop effect — only this page's picture is shown
        if (tutorialImages != null)
        {
            for (int i = 0; i < tutorialImages.Count; i++)
            {
                if (tutorialImages[i] != null)
                {
                    tutorialImages[i].transform.DOKill();

                    if (i == stepIndex)
                    {
                        tutorialImages[i].SetActive(true);
                        tutorialImages[i].transform.localScale = Vector3.one * 0.5f;
                        tutorialImages[i].transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
                    }
                    else
                    {
                        tutorialImages[i].SetActive(false);
                    }
                }
            }
        }

        UpdateButtonStates();
    }

    // Helper that sets the text and fades it in on unscaled time (avoids repeating the same code)
    private void AnimateTextField(TMP_Text targetTMP, string textContent)
    {
        if (targetTMP != null)
        {
            targetTMP.DOKill();
            targetTMP.text = textContent;

            Color txtColor = targetTMP.color;
            txtColor.a = 0f;
            targetTMP.color = txtColor;

            targetTMP.DOFade(1f, 0.35f).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    // Disables Previous on the first page and Next on the last page.
    private void UpdateButtonStates()
    {
        if (prevButton != null) prevButton.interactable = (currentStepIndex > 0);
        if (nextButton != null) nextButton.interactable = (currentStepIndex < tutorialSteps.Count - 1);
    }

    void NextStep()
    {
        if (currentStepIndex < tutorialSteps.Count - 1)
        {
            ShowTutorialStep(currentStepIndex + 1);
        }
    }

    void PreviousStep()
    {
        if (currentStepIndex > 0)
        {
            ShowTutorialStep(currentStepIndex - 1);
        }
    }

    /// <summary>Fades the popup out, then deactivates it (which restores the game speed in OnDisable).</summary>
    public void CloseTutorial()
    {
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.DOKill();
            tutorialCanvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Kill every tween so none runs on a destroyed object.
    void OnDestroy()
    {
        if (tutorialCanvasGroup != null) tutorialCanvasGroup.DOKill();
        if (tutorialHeaderTMPText != null) tutorialHeaderTMPText.DOKill();
        if (tutorialBodyTMPText != null) tutorialBodyTMPText.DOKill();

        if (tutorialImages != null)
        {
            foreach (GameObject img in tutorialImages)
            {
                if (img != null) img.transform.DOKill();
            }
        }
    }
}
