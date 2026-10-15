using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ปุ่มสลับความเร็วเกม 1x/2x — กดครั้งแรก timeScale = 2, กดอีกครั้งกลับเป็น 1
// ไม่ทำงานตอนเกม Paused (ปุ่ม Pause ควบคุม timeScale=0 อยู่แล้ว กันชนกัน)
public class GameSpeedToggleUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;   // โชว์ "1x" / "2x" — เว้นว่างได้
    [SerializeField] private bool IsPauseButton;   // ถ้าเป็นปุ่ม Pause จะไม่สลับความเร็วเกม แต่จะสลับ pause/resume แทน
    [SerializeField] private GameObject Speed2XIcon1;
    [SerializeField] private GameObject Speed2XIcon2;       

    private bool _fast;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if(Speed2XIcon1 != null) Speed2XIcon1.SetActive(true);
        if(Speed2XIcon2 != null) Speed2XIcon2.SetActive(false);
    }

    void Start()
    {
        if (button != null) button.onClick.AddListener(OnClick);
        Refresh();
    }

    void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    void OnClick()
    {
        // ระหว่างเกม Pause อยู่ (timeScale=0) — แค่สลับสถานะที่จำไว้ ยังไม่ apply จริง
        // GameManager.ResumeGame() จะไป apply ให้เองตอนเลิก pause (ดู GameSpeed.Current)
        _fast = !_fast;
        if (IsPauseButton)
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameManager.GameState.Paused)
                {
                    this.gameObject.GetComponent<Image>().color = Color.white;
                    GameManager.Instance.ResumeGame();
                    if (label != null) label.text = "";
                }
                else
                {
                    this.gameObject.GetComponent<Image>().color = Color.red;
                    GameManager.Instance.PauseGame();
                    if (label != null) label.text = "Game is Paused!!!";
                }

            }
        }
        else
        GameSpeed.SetSpeed(_fast ? 2f : 1f);
        if (_fast)
        {
            if (Speed2XIcon2 != null) Speed2XIcon2.SetActive(true);
        }
        else
        {
            if (Speed2XIcon2 != null) Speed2XIcon2.SetActive(false);
            
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            Time.timeScale = 0f;

        Refresh();
    }

    void Refresh()
    {
        if (label != null)
        {
            if (IsPauseButton)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
                    label.text = "Game is Paused!!!";
                else
                    label.text = "";
            }
            else
                label.text = _fast ? "2" : "1";   
        }
    }
}
