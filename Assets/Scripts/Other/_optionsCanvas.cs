using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class _optionsCanvas : MonoBehaviour
{
    //this script will grab all current VALUES in start and dynamically update them in UPDATE listening to the sliders

    public AudioMixer mainMixer;

    public Slider sensitivitySlider; // in _playerController
    public Slider fovSlider; // in _playerController
    public Slider musicSlider; // mixer
    public Slider sfxSlider; // mixer

    public _playerController _controller; // find if it exists, otherwise don't change them

    // Start is called before the first frame update
    void Start()
    {
        _controller = GameObject.FindAnyObjectByType<_playerController>();

        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        float savedSFX = PlayerPrefs.GetFloat("SfxVolume", 1.0f);
        float savedSens = PlayerPrefs.GetFloat("Sensitivity", 2f);
        float savedFOV = PlayerPrefs.GetFloat("FOV", 60f);

        //music and volume we can set
        SetMusicVolume(savedMusic);
        SetSoundVolume(savedSFX);
        musicSlider.value = savedMusic;
        sfxSlider.value = savedSFX;

        if (_controller == null) {
            //we are in HOME SCREEN

            sensitivitySlider.value = savedSens;
            fovSlider.value = savedFOV;

        }

        //we are in game, we should load from in-game values
        if(_controller != null) {
            //set fov & sens

            sensitivitySlider.value = _controller.mouseSensitivity;
            fovSlider.value = _controller.normalFOV;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            this.gameObject.SetActive(false); // also hide settings panel when pressing escape
        }
    }

    //update values in realtime, functions called by sliders
    public void SetMusicVolume(float volume) {
        mainMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);

        //also save in prefs
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
    
    public void SetSoundVolume(float volume) {
        mainMixer.SetFloat("SfxVolume", Mathf.Log10(volume) * 20);

        //also save in prefs
        PlayerPrefs.SetFloat("SfxVolume", volume);
    }

    public void SetSensitivity(float sensitivity) {
        if(_controller != null) {
            _controller.mouseSensitivity = sensitivity;
        }

        //also save in prefs
        PlayerPrefs.SetFloat("Sensitivity", sensitivity);
    }

    public void SetFov(float fov) {
        if (_controller != null) {
            _controller.normalFOV = fov;
        }

        //also save in prefs
        PlayerPrefs.SetFloat("FOV", fov);
    }

    public void BtnDestroy() {
        GameObject.Destroy(this.gameObject);
    }
}
