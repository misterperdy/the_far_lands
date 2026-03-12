using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _gameManager : MonoBehaviour
{
    public bool isPaused = false;

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
}
