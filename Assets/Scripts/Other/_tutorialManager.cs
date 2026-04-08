using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using UnityEngine.UI;

public class _tutorialManager : MonoBehaviour
{
    public GameObject tutorialPanel;
    public Text titleUI;
    public Text contentUI;

    public TutorialMessage[] messages;

    public _sfxManager _sfxManager;

    private int currentIndex = 0;
    private float timer = 0f;
    private bool isTutorialActive = false;

    public float initialDelay = 5f;
    private bool isWaitingForStart = false;
    private float startTimer = 0f;

    private void Start() {
        tutorialPanel.SetActive(false);

        if (messages.Length > 0) {
            isWaitingForStart = true;
        }
    }

    private void Update() {
        if ( Time.timeScale == 0f) return;

        //give a little delay at start for player to accomodate/load into game
        if (isWaitingForStart) {
            startTimer += Time.deltaTime;

            if(startTimer >= initialDelay) {
                isWaitingForStart = false;
                StartTutorial();
            }

            return;
        }

        if (!isTutorialActive) return;

        timer += Time.deltaTime;

        if(timer >= messages[currentIndex].displayDuration) {
            timer = 0f;
            currentIndex++; //go to next message

            if(currentIndex < messages.Length) {
                UpdateUI(currentIndex); //message exists, send to update ui
            } else {
                EndTutorial(); //reached the end
            }
        }
    }

    private void StartTutorial() {
        currentIndex = 0;
        timer = 0f;
        isTutorialActive = true;
        tutorialPanel.SetActive(true);

        UpdateUI(currentIndex); //show first text
    }

    private void EndTutorial() {
        isTutorialActive = false;
        tutorialPanel.SetActive(false);


    }

    //grab title and content of message from scriptable object array
    private void UpdateUI(int index) {
        titleUI.text = messages[index].titleText;
        contentUI.text = messages[index].contentText;

        //also play short ding
        if(_sfxManager != null) {
            _sfxManager.PlayDingShort();
        }

    }
}
