using UnityEngine;
using UnityEngine.SceneManagement;

public class UIScript : MonoBehaviour
{
    [SerializeField] CanvasGroup pauseMenu;
    [SerializeField] CanvasGroup GameOverMenu;
    [SerializeField] GameObject _skillsUI;
    //[SerializeField] TeleportManager teleportManager;
    public void Start()
    {
        ResetPlayableState();
    }
    public void ResetPlayableState()
    {
        Time.timeScale = 1f;
        if (_skillsUI == null||GameOverMenu == null||pauseMenu == null) return;
        pauseMenu.alpha = 0f;
        pauseMenu.interactable = false;
        pauseMenu.blocksRaycasts = false;
        GameOverMenu.alpha = 0f;
        GameOverMenu.interactable = false;
        GameOverMenu.blocksRaycasts = false;
        _skillsUI.SetActive(true);
    }
    
    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level 1");
    }

    public void MainMenuButton()
    {
        SceneManager.LoadScene("Main menu");
    }

    public void ContinueButton()
    {
         ResetPlayableState();
    }
    public void RestartButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }
    public void Open()
    {
        Time.timeScale = 0f;
        if (_skillsUI == null||pauseMenu == null) return;
        _skillsUI.SetActive(false); 
        pauseMenu.alpha = 1f;
        pauseMenu.interactable = true;
        pauseMenu.blocksRaycasts = true;
    }

    public void GameOver()
    {
        Debug.Log("ПРАИПАЛ КАТОЧКУ!");
        Time.timeScale = 0f;
        if (_skillsUI == null||GameOverMenu == null) return;
        _skillsUI.SetActive(false);
        GameOverMenu.alpha = 1f;
        GameOverMenu.interactable = true;
        GameOverMenu.blocksRaycasts = true;
    }

    /*public void NextCheckPoint()
    {
        teleportManager.NextTeleportPoint();
    }

    public void BackCheckPoint()
    {
        teleportManager.BackTeleportPoint();
    }*/


    public void ExitButton()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }
}