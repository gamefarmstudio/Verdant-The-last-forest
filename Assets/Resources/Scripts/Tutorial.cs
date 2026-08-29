using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    [Header("Tutorial Content Data")]
    [Tooltip("ใช้สัญลักษณ์ | เพื่อแยกส่วนหัวข้อและเนื้อหาออกจากกัน เช่น HEADER | BODY TEXT")]
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
    [SerializeField] private TMP_Text tutorialHeaderTMPText; // แปะกล่องข้อความสำหรับ หัวข้อ (Header)
    [SerializeField] private TMP_Text tutorialBodyTMPText;   // แปะกล่องข้อความสำหรับ เนื้อหา (Body)
    [SerializeField] private List<GameObject> tutorialImages; 
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button closeButton;
    
    [Header("Animation Settings")]
    [SerializeField] private CanvasGroup tutorialCanvasGroup; 
    
    [Header("Time Settings")]
    [Tooltip("0 = Paused, 1 = Normal Speed")]
    [Range(0f, 1f)]
    [SerializeField] private float tutorialTimeScale = 0.1f; 

    private int currentStepIndex = 0;

    void Awake()
    {

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

    void OnEnable()
    {
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
        Time.timeScale = 1f;
    }

    public void ShowTutorialStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= tutorialSteps.Count)
        {
            return;
        }

        currentStepIndex = stepIndex;

        // แยกข้อความหัวข้อและเนื้อหาออกจากกันผ่านตัวแยก '|'
        string rawText = tutorialSteps[stepIndex];
        string headerString = "TUTORIAL"; // ค่าเริ่มต้นหากไม่มีตัวแยก
        string bodyString = rawText;

        if (rawText.Contains("|"))
        {
            string[] splitText = rawText.Split('|');
            headerString = splitText[0].Trim();
            bodyString = splitText[1].Trim();
        }

        // 1. Text Animation: Fade ทั้งหัวข้อและเนื้อหาพร้อมกัน
        AnimateTextField(tutorialHeaderTMPText, headerString);
        AnimateTextField(tutorialBodyTMPText, bodyString);

        // 2. Picture Animation: Spring Effect เหมือนเดิม
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

    // ฟังก์ชันช่วยจัดการ Fade ตัวอักษรแบบ Real-time ป้องกันโค้ดซ้ำซ้อน
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
