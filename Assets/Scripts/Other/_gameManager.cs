using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class _gameManager : MonoBehaviour
{
    public bool isPaused = false;

    public GameObject gameGUI; //hotbar , other ui canvas to hide for screenshot


    [Header("Score&Time")]
    public float timeToPlayInSeconds = 600; //10 minutes
    public Text ScoreText;
    public Text TimeText;
    public int playerScore;
    public float timeRemaining;

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

    private void Start() {

        //set player score & time
        playerScore = 0;
        timeRemaining = timeToPlayInSeconds;
    }

    void Update() {
        if (isPaused) return; //dont take screenshots/hide gui when paused

        if (timeRemaining <= 0) {
            GameOver();
        }

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

        // time updating
        timeRemaining -= Time.deltaTime;

        //time & score text updating
        if (TimeText != null) {
            float minutes = Mathf.FloorToInt(timeRemaining / 60);
            float seconds = Mathf.FloorToInt(timeRemaining % 60);

            //format to always show 2 digits
            TimeText.text = string.Format("Time remaining: {0:00}:{1:00}", minutes, seconds);
        }
        if (ScoreText != null) {
            ScoreText.text = "Score: " + playerScore.ToString();
        }
    }

    //take screenshot and for build save in game directory,  for editor save in assets
    public void TakeScreenshot() {
        string timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = "Screenshot_" + timeStamp + ".png";

        ScreenCapture.CaptureScreenshot(fileName);

        Debug.Log("screenshot taken: " + fileName);
    }



    public void GameOver() {
        //unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //send to menu
        SceneManager.LoadScene("Title Screen");
    }
}
