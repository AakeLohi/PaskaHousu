using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Called by Play button
    public void Play()
    {
        Invoke("StartGame", 0.5f);
    }

    private void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // Called by Exit button
    public void ExitGame()
    {
        Debug.Log("Exiting game...");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
