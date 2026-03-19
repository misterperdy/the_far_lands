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

    [Header("Sway/Bobbing Settings")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmoothness = 6f;
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;

    private void Start() {
        if (heldBlock != null) {
            heldBlockRenderer = heldBlock.GetComponent<MeshRenderer>();
        }

        //remember default hand position
        if (handPosition != null) {
            initialPosition = handPosition.localPosition;
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

        //apply both movements
        Vector3 targetPosition = initialPosition + swayOffset + bobOffset;

        handPosition.localPosition = Vector3.Lerp(handPosition.localPosition, targetPosition, swaySmoothness * Time.deltaTime);
    }

    public void UpdateViewmodel(byte blockID) {
        if (blockID == 0) {
            //no block selected, show hand
            playerHand.SetActive(true);
            heldBlock.SetActive(false);
        } else {
            playerHand.SetActive(false);
            heldBlock.SetActive(true);

            //look for this block pmaterial from PARTICLES
            if (_worldManager.blockBreakParticlePrefabs[blockID] != null) {
                ParticleSystemRenderer psrenderer = _worldManager.blockBreakParticlePrefabs[blockID].GetComponent<ParticleSystemRenderer>();

                if(psrenderer != null && heldBlockRenderer != null) {
                    heldBlockRenderer.material = psrenderer.sharedMaterial;
                }
            }

        }
    }

}
