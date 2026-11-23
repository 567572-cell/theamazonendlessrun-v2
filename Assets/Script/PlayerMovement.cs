using UnityEngine;



[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Forward Speed")]
    public float startSpeed = 10f;
    public float maxSpeed = 30f;
    public float speedIncreasePerSecond = 0.15f;

    [Header("Lanes")]
    public float laneDistance = 2.5f;     // gap between lane centres
    public float laneSwitchSpeed = 18f;   // how fast you slide sideways

    [Header("Jump & Gravity")]
    public float jumpHeight = 2.2f;
    public float gravity = -45f;          // stronger than real gravity = snappier, arcade feel

    [Header("Roll")]
    public float rollDuration = 0.75f;
    public float rollHeight = 0.9f;       // collider height while rolling
    public float fastFallSpeed = -35f;    // downward speed when you roll in mid-air

    [Header("Swipe (touch / mouse)")]
    public float minSwipeDistance = 50f;  // in pixels

    [Header("Animation (optional)")]
    public Animator animator;

    CharacterController controller;

    int currentLane = 1;   // 0 = left, 1 = middle, 2 = right
    int previousLane = 1;
    float forwardSpeed;
    float verticalVelocity;

    bool isRolling;
    float rollTimer;
    float normalHeight;
    Vector3 normalCenter;

    Vector2 swipeStart;
    bool swiping;
    bool isDead;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        forwardSpeed = startSpeed;
        normalHeight = controller.height;
        normalCenter = controller.center;
    }

    void Update()
    {
        if (isDead) return;

        HandleInput();

        // Speed creeps up over time, like the real game
        forwardSpeed = Mathf.Min(forwardSpeed + speedIncreasePerSecond * Time.deltaTime, maxSpeed);

        UpdateRoll();
        Move();
    }

    // ---------------- INPUT ----------------

    void HandleInput()
    {
        // Keyboard
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) ChangeLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) ChangeLane(1);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) Jump();
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Roll();

        // Swipes. Unity turns phone touches into mouse input by default,
        // so this works on mobile AND lets you test by click-dragging in the editor.
        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = Input.mousePosition;
            swiping = true;
        }

        if (swiping && Input.GetMouseButton(0))
        {
            Vector2 delta = (Vector2)Input.mousePosition - swipeStart;

            // Trigger as soon as the finger has moved far enough (feels more responsive than waiting for release)
            if (delta.magnitude >= minSwipeDistance)
            {
                swiping = false;

                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    ChangeLane(delta.x > 0 ? 1 : -1);
                else if (delta.y > 0)
                    Jump();
                else
                    Roll();
            }
        }

        if (Input.GetMouseButtonUp(0)) swiping = false;
    }

    // ---------------- ACTIONS ----------------

    void ChangeLane(int direction)
    {
        int target = Mathf.Clamp(currentLane + direction, 0, 2);
        if (target == currentLane) return; // already at the edge

        previousLane = currentLane;
        currentLane = target;
    }

    void Jump()
    {
        if (!controller.isGrounded) return;

        // Jumping cancels a roll, same as Subway Surfers
        if (isRolling) EndRoll();

        // Physics formula: velocity needed to reach jumpHeight with this gravity
        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        if (animator) animator.SetTrigger("Jump");
    }

    void Roll()
    {
        // Rolling in the air slams you straight down
        if (!controller.isGrounded) verticalVelocity = fastFallSpeed;

        isRolling = true;
        rollTimer = rollDuration;

        // Shrink the collider and keep its feet on the ground
        controller.height = rollHeight;
        controller.center = new Vector3(normalCenter.x,
                                        normalCenter.y - (normalHeight - rollHeight) / 2f,
                                        normalCenter.z);

        if (animator) animator.SetTrigger("Roll");
    }

    void UpdateRoll()
    {
        if (!isRolling) return;
        rollTimer -= Time.deltaTime;
        if (rollTimer <= 0f) EndRoll();
    }

    void EndRoll()
    {
        isRolling = false;
        controller.height = normalHeight;
        controller.center = normalCenter;
    }

    // ---------------- MOVEMENT ----------------

    void Move()
    {
        // Sideways: slide towards the X position of the current lane
        float targetX = (currentLane - 1) * laneDistance;
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        float deltaX = newX - transform.position.x;

        // Up/down: gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small push down keeps isGrounded reliable
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = new Vector3(deltaX,
                                   verticalVelocity * Time.deltaTime,
                                   forwardSpeed * Time.deltaTime);
        controller.Move(move);

        if (animator) animator.SetBool("Grounded", controller.isGrounded);
    }

    // ---------------- COLLISIONS ----------------

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDead || !hit.gameObject.CompareTag("Obstacle")) return;

        // Landing on top of a train is fine - you can run along the roof
        if (hit.normal.y > 0.5f) return;

        if (hit.normal.z < -0.5f)
        {
            // Hit the front of something
            Crash();
        }
        else if (Mathf.Abs(hit.normal.x) > 0.5f)
        {
            // Clipped the side while switching lanes - bounce back
            currentLane = previousLane;
            previousLane = currentLane;
            if (animator) animator.SetTrigger("Stumble");
        }
    }

    void Crash()
    {
        isDead = true;
        if (animator) animator.SetTrigger("Die");
        Debug.Log("Crashed! Final speed: " + forwardSpeed);
        // Hook your game over screen in here
    }
}