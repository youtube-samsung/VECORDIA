using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        Debug.Log("Загружаем игровую сцену...");
        SceneManager.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        Debug.Log("настройки потом");
    }

    public void QuitGame()
    {
        Debug.Log("в редакторе юнити не работает но при сборке выйдет");
        Application.Quit();
    }
}
