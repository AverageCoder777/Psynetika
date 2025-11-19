using UnityEngine;
using UnityEngine.SceneManagement;

public class UIScript : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject GameOverMenu;
    [SerializeField] PlayerController player;
    [SerializeField] TimeOfDie timeOfDie;

    public void StartGame()
    {
        SceneManager.LoadScene("Level1");
    }

    public void Continue()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
    }
    public void Restart()
    {
        Time.timeScale = 1f; 
        GameOverMenu.SetActive(false);

        if (player != null)
        {
            player.ResetHealth();
        }
        timeOfDie.ResetTimer();

    }

    public void Open()
    {
        Time.timeScale = 0f; 
        pauseMenu.SetActive(true);
    }

    public void GameOver()
    {
        Debug.Log("пРОИГРЫШ");
        GameOverMenu.SetActive(true);
        Time.timeScale = 0f; 
    }


    public void Exit()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }
}