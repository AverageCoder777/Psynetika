using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIScript : MonoBehaviour
{
    [SerializeField] CanvasGroup pauseMenu;
    [SerializeField] CanvasGroup GameOverMenu;
    //[SerializeField] TeleportManager teleportManager;
    public void Start()
    {
        pauseMenu.alpha = 0f;
        pauseMenu.interactable = false;
        GameOverMenu.alpha = 0f;
        GameOverMenu.interactable = false;
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
        Time.timeScale = 1f;
        pauseMenu.alpha = 0f;
        pauseMenu.interactable = false;
    }
    public void RestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level 1");
    }
    public void Open()
    {
        Time.timeScale = 0f; 
        pauseMenu.alpha = 1f;
        pauseMenu.interactable = true;
    }

    public void GameOver()
    {
        Debug.Log("ПРАИПАЛ КАТОЧКУ!");
        GameOverMenu.alpha = 1f;
        GameOverMenu.interactable = true;
        Time.timeScale = 0f; 
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