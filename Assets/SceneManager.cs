using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene handling

public class RestartManager : MonoBehaviour
{
    // Function to restart the scene
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void maingame()
    {

        // Load Scene 2
        SceneManager.LoadScene(1);  // Ensure Scene 2 is in Build Settings
    }

    // Function to exit the application
    public void ExitApplication()
    {
        Application.Quit();
    }
}
