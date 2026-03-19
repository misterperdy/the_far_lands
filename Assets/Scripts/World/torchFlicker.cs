using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//flicker torch light

public class torchFlicker : MonoBehaviour
{
    private Light torchLight;
    public float baseIntensity = 3.5f;
    public float flickerSpeed = 10f;
    public float flickerAmount = 0.5f;

    private void Start() {
        torchLight = GetComponent<Light>();
    }

    private void Update() {
        //use perlin noise
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);

        torchLight.intensity = baseIntensity + (noise * flickerAmount) - (flickerAmount / 2f); //both make it brighter and darker
    }
}
