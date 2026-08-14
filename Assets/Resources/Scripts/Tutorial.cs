using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    [Header("Tutorial Steps")]
    [SerializeField] private List<string> tutorialSteps; 
    [SerializeField] private List<GameObject> tutorialImages; // Changed from Sprite to GameObject

    [Header("UI References")]
    [SerializeField] private TMP_Text tutorialTMPText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button closeButton;
    
    [Header("Animation Settings")]
    [SerializeField] private CanvasGroup tutorialCanvasGroup; 

    private int currentStepIndex = 0;

    void Awake()
    {
        
        nextButton.onClick.AddListener(NextStep);
        prevButton.onClick.AddListener(PreviousStep);
        closeButton.onClick.AddListener(CloseTutorial);

        if (tutorialCanvasGroup == null)
        {
            tutorialCanvasGroup = GetComponent<CanvasGroup>();
        }

        ShowTutorialStep(0); 
    }

    public void ShowTutorialStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= tutorialSteps.Count || stepIndex >= tutorialImages.Count)
        {
            Debug.LogWarning("Invalid tutorial step index or missing assets: " + stepIndex);
            return;
        }

        currentStepIndex = stepIndex;

        // 1. Manage Text Content & Animation
        tutorialTMPText.text = tutorialSteps[stepIndex];
        tutorialTMPText.DOKill();
        tutorialTMPText.DOFade(1f, 0.3f).From(0f).SetEase(Ease.InOutQuad);

        // 2. Manage GameObject Panels (Deactivate all, then activate current)
        for (int i = 0; i < tutorialImages.Count; i++)
        {
            if (tutorialImages[i] != null)
            {
                // Kill any existing animations on the object before disabling/enabling
                tutorialImages[i].transform.DOKill(); 
                
                if (i == stepIndex)
                {
                    tutorialImages[i].SetActive(true);
                    // Optional pop-in effect for the active visual element
                    tutorialImages[i].transform.DOScale(1f, 0.3f).From(0f).SetEase(Ease.OutBack);
                }
                else
                {
                    tutorialImages[i].SetActive(false);
                }
            }
        }

        // 3. Dynamic Button States
        prevButton.interactable = (currentStepIndex > 0);
        nextButton.interactable = (currentStepIndex < tutorialSteps.Count - 1);
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

    void CloseTutorial()
    {
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.DOKill();
            tutorialCanvasGroup.DOFade(0f, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
