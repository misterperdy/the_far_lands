using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class _mainMenuManager : MonoBehaviour
{
    public Camera _camera;


    public GameObject OptionsCanvasPrefab;

    //camera rotate panoramic view
    public float cameraRotationSpeed = 2.0f;

    private void Update() {
        _camera.transform.Rotate(Vector3.up * cameraRotationSpeed * Time.deltaTime, Space.World);
    }

    //handling buttons
    public void BtnStartLevel() {
        SceneManager.LoadScene("Level");
    }

    public void BtnOptions() {
        Instantiate(OptionsCanvasPrefab);
    }

    public void BtnExit() {
        Application.Quit();
    }
}
