using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private Slider _volumeSlider;

    private Resolution[] _resolutions;

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        _volumeSlider.value = savedVolume;
        SetVolume(savedVolume);

        _fullscreenToggle.isOn = Screen.fullScreen;

        _resolutions = Screen.resolutions;
        _resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            string option = _resolutions[i].width + " x " + _resolutions[i].height;
            options.Add(option);

            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        _resolutionDropdown.AddOptions(options);

        int savedResolution = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        _resolutionDropdown.value = savedResolution;
        _resolutionDropdown.RefreshShownValue();
    }

    public void SetVolume(float volume)
    {
        if (volume <= 0) volume = 0.0001f;
        _audioMixer.SetFloat("MasterVol", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = _resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}