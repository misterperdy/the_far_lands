using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class _viewmodelController : MonoBehaviour
{
    public GameObject playerHand;
    public GameObject heldBlock; //assign in inspector
    public Transform handPosition;

    public _worldManager _worldManager;
    public CharacterController _playerCharController; // assign in inspector
    private MeshRenderer heldBlockRenderer;
    private Vector3 initialPosition;
    private float bobTimer = 0f;

    [Header("Sway, Bobbing & Swap Settings")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmoothness = 6f;
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;
    public float swapDropDistance = -0.8f;
    public float swapSpeed = 5f;

    //swap variables
    private float currentSwapY = 0f;
    private float targetSwapY = 0f;
    private bool isSwapping = false;
    private byte pendingBlockID = 0;
    private byte currentBlockID = 0;
    private Vector3 currentSmoothedSwayPos; //to not mix up the animations

    private void Start() {
        if (heldBlock != null) {
            heldBlockRenderer = heldBlock.GetComponent<MeshRenderer>();
        }

        //remember default hand position
        if (handPosition != null) {
            initialPosition = handPosition.localPosition;
            currentSmoothedSwayPos = handPosition.localPosition;
        }
    }

    //bobbing and sway logic
    private void Update() {
        if (handPosition == null) return;

        //determine Sway (moving mouse inertia when staying still)
        float mouseX = Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = Input.GetAxis("Mouse Y") * swayAmount;

        //limit movement
        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

        //reverse direction, so hand will remain in the past a little bit
        Vector3 swayOffset = new Vector3(-mouseX, -mouseY, 0);

        //determine bobbing(caused by moving translating player position)
        Vector3 bobOffset = Vector3.zero;

        //read horizontal velocity (without Y)
        Vector3 horizontalVeclocity = new Vector3(_playerCharController.velocity.x, 0, _playerCharController.velocity.z);
        float speed = horizontalVeclocity.magnitude;

        //if moving and on ground do the swing
        if(speed > 0.1f && _playerCharController.isGrounded) {
            //calculate based on speed+time
            bobTimer += Time.deltaTime * bobSpeed * (speed / 4f);

            //math formula to draw 8-s
            float bobX = Mathf.Cos(bobTimer / 2f) * bobAmount;
            float bobY = Mathf.Sin(bobTimer) * bobAmount;

            bobOffset = new Vector3(bobX, bobY, 0);
        } else {
            //not moving
            bobTimer = 0f;
        }

        //remember both movements position
        Vector3 targetSwayPos = initialPosition + swayOffset + bobOffset;

        currentSmoothedSwayPos = Vector3.Lerp(currentSmoothedSwayPos, targetSwayPos, swaySmoothness * Time.deltaTime);

        //see if we need to switch block animation
        currentSwapY = Mathf.MoveTowards(currentSwapY, targetSwapY, swapSpeed * Time.deltaTime); // use move towards cause its faster than lerp(don't need smoothing here)

        //if block went off-screen change it
        if (isSwapping && currentSwapY <= swapDropDistance + 0.01f) {
            PerformActualSwap(); //replace block
            targetSwapY = 0f;
            isSwapping = false;
        }

        //apply final position with both offsets
        handPosition.localPosition = currentSmoothedSwayPos + new Vector3(0, currentSwapY, 0);
    }

    //function to be called by inventory
    public void UpdateViewmodel(byte newBlockID) {
        if (newBlockID == currentBlockID) return; //don't do animation if it's same block

        pendingBlockID = newBlockID;
        targetSwapY = swapDropDistance; //start falling
        isSwapping = true; // green light for the update to start the swap animation
    }

    //function to actually replace block/hand
    private void PerformActualSwap() {
        currentBlockID = pendingBlockID; //set new block

        if (currentBlockID == 0) {
            //no block selected, show hand
            playerHand.SetActive(true);
            heldBlock.SetActive(false);
        } else {
            playerHand.SetActive(false);
            heldBlock.SetActive(true);

            //look for this block pmaterial from PARTICLES
            if (_worldManager.blockBreakParticlePrefabs[currentBlockID] != null) {
                ParticleSystemRenderer psrenderer = _worldManager.blockBreakParticlePrefabs[currentBlockID].GetComponent<ParticleSystemRenderer>();

                if(psrenderer != null && heldBlockRenderer != null) {
                    heldBlockRenderer.material = psrenderer.sharedMaterial;
                }
            }

        }
    }

}
