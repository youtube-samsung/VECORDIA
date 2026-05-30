using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private GameObject _pauseMenuPanel;
    [SerializeField] private GameObject _settingsPanel;

    private bool _isPaused = false;
    private List<IRitualController> _cachedRituals = new List<IRitualController>();

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
        CacheAllRituals();
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
            if (GetActiveRitual() != null) return;

            Pause();
        }
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        _pauseMenuPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);

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

    public void OpenSettings()
    {
        _pauseMenuPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        _settingsPanel.SetActive(false);
        _pauseMenuPanel.SetActive(true);
    }

    private void CacheAllRituals()
    {
        _cachedRituals.Clear();
        MonoBehaviour[] monoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour mono in monoBehaviours)
        {
            if (mono is IRitualController ritual)
            {
                _cachedRituals.Add(ritual);
            }
        }
    }

    private IRitualController GetActiveRitual()
    {
        for (int i = 0; i < _cachedRituals.Count; i++)
        {
            if (_cachedRituals[i] is MonoBehaviour mono && mono != null)
            {
                if (_cachedRituals[i].IsRitualActive)
                {
                    return _cachedRituals[i];
                }
            }
        }
        return null;
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