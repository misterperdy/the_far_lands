using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _gameManager : MonoBehaviour
{
    public bool isPaused = false;

    public GameObject gameGUI; //hotbar , other ui canvas to hide for screenshot

    //helper pause/resume functions for pause menu
    public void PauseGame() {
        Time.timeScale = 0f;
        isPaused = true;

        //unlock cursor to click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame() {
        Time.timeScale = 1f;
        isPaused = false;

        //lock and hide cursor for FPS gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        if (isPaused) return; //dont take screenshots/hide gui when paused

        //listen for screenshot key (F2)
        if (Input.GetKeyDown(KeyCode.F2)) {
            TakeScreenshot();
        }

        //listen for hide/enable GUI key (F1)
        if (Input.GetKeyDown(KeyCode.F1)) {
            if(gameGUI!= null){
                //if hide enable, if enabled hide
                gameGUI.SetActive(!gameGUI.activeSelf);
            }
        }
    }

    //take screenshot and for build save in game directory,  for editor save in assets
    public void TakeScreenshot() {
        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = "Screenshot_" + timeStamp + ".png";

        ScreenCapture.CaptureScreenshot(fileName);

        Debug.Log("screenshot taken: " + fileName);
    }


}
