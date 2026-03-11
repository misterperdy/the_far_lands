using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class _playerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.3f;
    public float gravity = -19f;
    public float jumpHeight = 1.25f;
    public float airControl = 3f; // how hard to change direction mid air( smalleer -> harder)

    [Header("Camera Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    private float cameraPitch = 0f; //to rotate on x axis

    [Header("Interaction Setttings")]
    public float reach = 4f;
    public Transform highlightBlock; // selected block highlighter
    public float interactionDelay = 0.15f; //delay when holding click to break/place
    private float interactionTimer = 0f;

    private _worldManager _world;
    private CharacterController _controller;

    private Vector3 velocity;
    private Vector3 currentMoveVelocity;

    private void Start() {
        _controller = GetComponent<CharacterController>();
        _world = FindObjectOfType<_worldManager>();

        //lock cursor in middle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update() {
        HandleMouseLook();
        HandleMovement();
        HandleInteraction();
    }

    //get mouse input, rotate pitch/yaw
    private void HandleMouseLook() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        //rotate camera on X axis (up and down for camera)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f); // to not go around our head

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        //rotate left right player body - y axis
        transform.Rotate(Vector3.up * mouseX);
    }
    
    //get axis movements/space and move player controller
    private void HandleMovement() {
        //get raw input
        float x = Input.GetAxisRaw("Horizontal"); //A,D
        float z = Input.GetAxisRaw("Vertical"); //W,S
        Vector3 targetMove = (transform.right * x + transform.forward * z).normalized * walkSpeed;

        if (_controller.isGrounded) { // on ground keep raw speed
            currentMoveVelocity = targetMove;
        } else { // on air lerp to have some inertia in changing direction
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetMove, airControl * Time.deltaTime);
        }

        //ground check
        if(_controller.isGrounded && velocity.y < 0) {
            velocity.y = -2f; //pull down to be connected 100% to the ground
        }

        //jump
        if(Input.GetButton("Jump") && _controller.isGrounded) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        //apply gravity
        velocity.y += gravity * Time.deltaTime;

        //create vector with both horizontal and vertical movement

        Vector3 finalMove = new Vector3(currentMoveVelocity.x, velocity.y, currentMoveVelocity.z);

        //move controller, we are multiplying with time so it's not dependant on FPS, but rather on time
        _controller.Move(finalMove * Time.deltaTime);
    }

    private void HandleInteraction() {
        //raycast
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        //if we hit a collider
        if(Physics.Raycast(ray, out hit, reach)) {
            //we are right on block edge
            //if we go a little bit in direction of ray direction we are inside the block we can break
            //and if we go a little bit in the opposite direction of ray direction we are outside where we need to place
            //and rounding to int we get the exact block coordinates we need to operate on

            Vector3 pointInSolidBlock = hit.point + (ray.direction * 0.01f);
            Vector3Int breakCoord = new Vector3Int(Mathf.FloorToInt(pointInSolidBlock.x), Mathf.FloorToInt(pointInSolidBlock.y), Mathf.FloorToInt(pointInSolidBlock.z));

            Vector3 pointInEmptyAir = hit.point - (ray.direction * 0.01f);
            Vector3Int placeCoord = new Vector3Int(Mathf.FloorToInt(pointInEmptyAir.x), Mathf.FloorToInt(pointInEmptyAir.y), Mathf.FloorToInt(pointInEmptyAir.z));

            //outline
            if (highlightBlock != null) {
                highlightBlock.gameObject.SetActive(true);
                highlightBlock.position = new Vector3(breakCoord.x + 0.5f, breakCoord.y + 0.5f, breakCoord.z + 0.5f);
            }

            //update interaction timer
            if(interactionTimer > 0) {
                interactionTimer -= Time.deltaTime;
            }

            //break block logic
            if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && interactionTimer<=0f)) {
                _world.SetVoxelGlobal(breakCoord, (byte)BlockType.Air); // we replace block with air
                interactionTimer = interactionDelay; // init the timer for hold to break
            }

            //place block logic
            if (Input.GetMouseButtonDown(1) || (Input.GetMouseButton(1) && interactionTimer <= 0f)) {

                //logic to prevent placing block inside player using BOUNDS
                //each object has BOUNDS = "collision box" enclosing itself, we will create bounds for a block and check if it overlaps with character controller's bounds box

                Vector3 blockCenter = new Vector3(placeCoord.x + 0.5f, placeCoord.y + 0.5f, placeCoord.z + 0.5f);
                Bounds blockBonds = new Bounds(blockCenter, Vector3.one);
                Bounds playerBounds = _controller.bounds;

                //check if they overlap
                if (!playerBounds.Intersects(blockBonds)) {
                    //place block if they dont

                    _world.SetVoxelGlobal(placeCoord, (byte)BlockType.Stone); // place stone for now
                } else {
                    Debug.Log("overlap player");
                }

                interactionTimer = interactionDelay; // init the timer for hold to place
            }
        } else {
            //no raycasthit , hide block outline
            if (highlightBlock != null) {
                highlightBlock.gameObject.SetActive(false);
            }
        }
    }
}
