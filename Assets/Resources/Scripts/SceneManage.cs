using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Small helper for UI buttons: load a scene by name or quit the game.
/// Hook these methods up in a Button's OnClick list in the Inspector.
/// </summary>
public class SceneManage : MonoBehaviour
{
    /// <summary>Loads the scene with this name (it must be in the Build Settings scene list).</summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Quits the built game (does nothing in the Editor).</summary>
    public void QuitGame()
    {
        Application.Quit();
    }
}
