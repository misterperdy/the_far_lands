using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//this script, attached on player, will handle dimming the enviromental lighting when you enter a cave( when you have a block above you)
public class _caveLightingController : MonoBehaviour
{
    [Header("Transition Parameters")]
    public float transitionSpeed = 1.5f;
    public LayerMask terrainLayer;
    public float roofCheckDistance = 40f;

    [Header("Illumination")]
    public float outdoorAmbientIntensity = 1f;
    public float caveAmbientIntensity = 0.5f;
    public int hitsRequiredForCave = 4;

    private float targetIntensity; //for lerp

    private void Start() {
        targetIntensity = outdoorAmbientIntensity;
    }

    private void Update() {
        int hitCount = 0;

        //try raycast above to see if you are underground
        if (Physics.Raycast(transform.position, Vector3.up, roofCheckDistance, terrainLayer)) hitCount++;

        //also check diagonal directions
        if (Physics.Raycast(transform.position, (Vector3.up + Vector3.forward).normalized, roofCheckDistance, terrainLayer)) hitCount++;
        if (Physics.Raycast(transform.position, (Vector3.up + Vector3.back).normalized, roofCheckDistance, terrainLayer)) hitCount++;
        if (Physics.Raycast(transform.position, (Vector3.up + Vector3.left).normalized, roofCheckDistance, terrainLayer)) hitCount++;
        if (Physics.Raycast(transform.position, (Vector3.up + Vector3.right).normalized, roofCheckDistance, terrainLayer)) hitCount++;

        bool isUnderground = (hitCount >= hitsRequiredForCave);

        targetIntensity = isUnderground ? caveAmbientIntensity : outdoorAmbientIntensity;

        //smooth transiton through intensities
        RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, targetIntensity, Time.deltaTime * transitionSpeed);    
    }
}
