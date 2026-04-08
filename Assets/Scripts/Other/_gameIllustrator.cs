using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class _gameIllustrator : MonoBehaviour
{
    private _gameManager _manager;

    public GameObject pauseMenuUI;
    public GameObject gameUI; // hide crosshair

    public GameObject gameOverUI;

    public Text deathMessage;
    public Text gameOverScoreText;


    public _sfxManager _sfxManager;
    public GameObject musicGO;

    public GameObject OptionsCanvasPrefab;

    [Header("Hotbar UI")]
    public _playerInventory _inventoryScript;
    public RawImage[] hotbarSlotImages = new RawImage[9]; //set effective blocks in hotbar
    public GameObject[] hotbarSlotHighlights = new GameObject[9]; // enable current slot, hide on others

    void Start()
    {
        _manager = FindObjectOfType<_gameManager>(); //grab game manager
        _manager.ResumeGame(); // make sure game is running
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

        //update hotbar no longer in update, call it everytime hotbar changes
        //UpdateHotbarUI();
    }

    //function to set the hotbar correctly based on inventory information
    public void UpdateHotbarUI() {
        for(int i = 0; i < 9; i++) {
            byte blockInSlot = _inventoryScript.hotbar[i];

            //if its air hide rawimage
            if(blockInSlot == 0) {
                hotbarSlotImages[i].enabled = false;
            } else {
                //show block
                hotbarSlotImages[i].enabled = true;

                Vector2 texturePos = VoxelData.GetTexturePosition(blockInSlot);

                float size = VoxelData.NormalizedBlockTextureSize;
                hotbarSlotImages[i].uvRect = new Rect(texturePos.x * size, texturePos.y * size, size, size);
            }

            hotbarSlotHighlights[i].SetActive(i == _inventoryScript.currentSlotIndex);//activate current slot border, hide rest of them
        }
    }

    //pause menu buttons logic
    public void BtnResume() {
        if (_manager != null) {
            pauseMenuUI.SetActive(false);
            gameUI.SetActive(true);
            _manager.ResumeGame();
        }
    }

    //instnatiate the options menu over the pause screen/title screen
    public void BtnOptions() {
        Instantiate(OptionsCanvasPrefab);
    }

    public void BtnExitToMenu() {
        _manager.ResumeGame(); // so we are not frozen in time

        Time.timeScale = 1f;

        //unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Title Screen");
    }

    //reload game
    public void BtnTryAgain() {
        //UNPAUSE GAME
        _manager.ResumeGame();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowGameOver() {
        //set game over score text
        gameOverScoreText.text = "Final score: " + _manager.playerScore;

        //if time is not up, show lava text
        if(_manager.timeRemaining > 0) {
            deathMessage.text = "You took a hot bath in lava!";
        }

        //disable hud, enable game over hud , freze time
        gameOverUI.SetActive(true);
        gameUI.SetActive(false);

        //unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //stop music
        if (musicGO != null) {
            Destroy(musicGO);
        }

        //play death sound
        if (_sfxManager != null) {
            _sfxManager.PlayDeath();
        }

        _manager.PauseGame();

    }
}
