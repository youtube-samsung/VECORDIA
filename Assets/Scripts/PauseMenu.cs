using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private GameObject _pauseMenuPanel;

    private bool _isPaused = false;

    private void OnEnable()
    {
        _inputReader.OnPausePerformed += TogglePause;
        _inputReader.OnUnpausePerformed += TogglePause;
    }

    private void OnDisable()
    {
        _inputReader.OnPausePerformed -= TogglePause;
        _inputReader.OnUnpausePerformed -= TogglePause;
    }
    private void Start()
    {
        Resume();
    }

    private void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f; 
        _pauseMenuPanel.SetActive(false); 

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _inputReader.SwitchToGameplay();
    }

    private void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f; 
        _pauseMenuPanel.SetActive(true); 

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _inputReader.SwitchToUI();
    }

    public void LoadMainMenu()
    {

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }


    public void QuitGame()
    {
        Application.Quit();
    }
}
