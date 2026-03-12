using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class _gameIllustrator : MonoBehaviour
{
    private _gameManager _manager;

    public GameObject pauseMenuUI;
    public GameObject gameUI; // hide crosshair

    void Start()
    {
        _manager = FindObjectOfType<_gameManager>(); //grab game manager
    }

    // Update is called once per frame
    void Update()
    {
        //check for pause key
        if (Input.GetKeyDown(KeyCode.Escape)) {
            //if unpaused, pause, if paused, unpause

            if(_manager != null) {
                if (_manager.isPaused) {
                    //resume game
                    pauseMenuUI.SetActive(false);
                    gameUI.SetActive(true);
                    _manager.ResumeGame();
                } else {
                    //pause
                    pauseMenuUI.SetActive(true);
                    gameUI.SetActive(false);
                    _manager.PauseGame();
                }
            }
        }
    }

    //pause menu buttons logic
    public void BtnResume() {
        if (_manager != null) {
            pauseMenuUI.SetActive(false);
            _manager.ResumeGame();
        }
    }

    public void BtnOptions() {
        //tbd
    }

    public void BtnExitToMenu() {
        _manager.ResumeGame(); // so we are not frozen in time

        //unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Title Screen");
    }
}
