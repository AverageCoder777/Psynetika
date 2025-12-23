using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIScript : MonoBehaviour
{
    [SerializeField] GameObject pauseMenu;
    [SerializeField] GameObject GameOverMenu;
    [SerializeField] Player player;
    [SerializeField] TextMeshProUGUI characterText;
    
    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level1");
    }

    public void Menu()
    {
        SceneManager.LoadScene("Main menu");
    }

    public void Continue()
    {
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level1");
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
    public void UpdateText(string text){
        characterText.text = text;
    }
}