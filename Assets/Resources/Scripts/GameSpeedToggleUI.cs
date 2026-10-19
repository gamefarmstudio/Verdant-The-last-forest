using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// HUD button that toggles game speed or pause.
///   - Speed button (IsPauseButton off): first click sets timeScale to 2, the next click back to 1.
///   - Pause button (IsPauseButton on): toggles GameManager pause/resume and shows "Game is Paused!!!".
/// While the game is paused, the speed choice is only remembered — GameManager.ResumeGame()
/// applies it later through GameSpeed.Current, so the speed button can't accidentally unpause the game.
/// </summary>
public class GameSpeedToggleUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;       // shows "1" / "2" (or the paused message) — optional
    [SerializeField] private bool IsPauseButton;   // if ticked, this button toggles pause/resume instead of game speed
    [SerializeField] private GameObject Speed2XIcon1;
    [SerializeField] private GameObject Speed2XIcon2;   // shown only while _fast is on

    private bool _fast;   // toggled on every click (also drives the 2x icon)

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
        // While the game is paused (timeScale = 0) the new speed is only remembered, not applied —
        // GameManager.ResumeGame() applies it when the game un-pauses (see GameSpeed.Current).
        _fast = !_fast;
        if (IsPauseButton)
        {
            if (GameManager.Instance != null)
            {
                // Toggle pause, tinting the button red while paused.
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

        // Show the second "2x" icon only while fast mode is on.
        if (_fast)
        {
            if (Speed2XIcon2 != null) Speed2XIcon2.SetActive(true);
        }
        else
        {
            if (Speed2XIcon2 != null) Speed2XIcon2.SetActive(false);

        }

        // If the game is paused, make sure SetSpeed didn't un-pause it.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            Time.timeScale = 0f;

        Refresh();
    }

    // Updates the label text for the current mode.
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
