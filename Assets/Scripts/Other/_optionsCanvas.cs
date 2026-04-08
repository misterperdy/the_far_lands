using System.Collections;
using System.Collections.Generic;
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

        if(_controller != null) {
            //set fov & sens

            sensitivitySlider.value = _controller.mouseSensitivity;
            fovSlider.value = _controller.normalFOV;
        }

        if (mainMixer != null) {
            //set volumes
            float musicVolume;
            mainMixer.GetFloat("MusicVolume", out musicVolume);

            //convert to db
            musicSlider.value = Mathf.Pow(10f, musicVolume / 20f);

            float soundVolume;
            mainMixer.GetFloat("SfxVolume", out soundVolume);

            //convert to db
            musicSlider.value = Mathf.Pow(10f, soundVolume / 20f);
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
    }
    
    public void SetSoundVolume(float volume) {
        mainMixer.SetFloat("SfxVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSensitivity(float sensitivity) {
        if(_controller != null) {
            _controller.mouseSensitivity = sensitivity;
        }
    }

    public void SetFov(float fov) {
        if (_controller != null) {
            _controller.normalFOV = fov;
        }
    }
}
