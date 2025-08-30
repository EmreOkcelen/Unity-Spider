using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float airControlMultiplier = 0.6f;

    [Header("Jump / Charge")]
    public float baseJumpForce = 7f;           // normal z�plama
    public float maxJumpForce = 16f;           // tam �arjl� z�plama
    public float maxChargeTime = 1.2f;         // en fazla ne kadar s�re �arj olabilir
    public float tapThreshold = 0.12f;         // �ok k�sa basma -> normal z�plama

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.15f;
    public LayerMask groundLayers;

    [Header("References")]
    public Camera playerCamera;
    public SimpleWebShooter webShooter;
    public JumpChargeUI jumpChargeUI; // ba�layaca��z (UI scripti a�a��da)

    [HideInInspector] public Rigidbody rb;

    // State machine and states
    public StateMachine stateMachine { get; private set; }
    public GroundedState groundedState { get; private set; }
    public AirState airState { get; private set; }
    public WebState webState { get; private set; }

    // input
    [HideInInspector] public Vector2 inputMove;
    [HideInInspector] public bool inputJumpPressed;
    [HideInInspector] public bool inputJumpHeld;
    [HideInInspector] public bool inputJumpReleased;
    [HideInInspector] public bool inputFire;

    // jump charge internal
    private float jumpCharge = 0f;
    private float jumpPressStartTime = 0f;

    // internal
    [HideInInspector] public bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        stateMachine = new StateMachine();
        groundedState = new GroundedState(this, stateMachine);
        airState = new AirState(this, stateMachine);
        webState = new WebState(this, stateMachine, webShooter);

        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Start()
    {
        stateMachine.Initialize(groundedState);
        if (jumpChargeUI != null) jumpChargeUI.SetVisible(false);
    }

    private void Update()
    {
        ReadInput();
        HandleJumpChargeUIAndLogic();
        stateMachine.LogicUpdate();
    }

    private void FixedUpdate()
    {
        GroundCheck();
        stateMachine.PhysicsUpdate();
    }

    private void ReadInput()
    {
        inputMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        inputFire = Input.GetMouseButtonDown(0);

        // jump inputs
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputJumpPressed = true;
            inputJumpHeld = true;
            inputJumpReleased = false;
            jumpPressStartTime = Time.time;
            jumpCharge = 0f;
        }
        else
        {
            inputJumpPressed = false;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            inputJumpHeld = true;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            inputJumpReleased = true;
            inputJumpHeld = false;
        }
        else
        {
            // reset release flag next frame; we will act on it in Update cycle
            // keep it true for this frame only when released
            if (!inputJumpReleased) inputJumpReleased = false;
        }
    }

    private void HandleJumpChargeUIAndLogic()
    {
        // Eğer yerdeysek ve space basılı tutuluyorsa şarj et
        // Ancak eğer web'e bağlıysak (attach) zıplama/şarj mantığını çalıştırma.
        bool attached = (webShooter != null && webShooter.IsAttached());
        if (isGrounded && inputJumpHeld && !attached)
        {
            jumpCharge += Time.deltaTime;
            jumpCharge = Mathf.Min(jumpCharge, maxChargeTime);

            // Update UI
            if (jumpChargeUI != null)
            {
                jumpChargeUI.SetVisible(true);
                jumpChargeUI.SetCharge(jumpCharge / maxChargeTime);
            }
        }

        // Eğer tuş bırakıldıysa -> zıpla (tap veya charged)
        if (inputJumpReleased)
        {
            float heldDuration = Time.time - jumpPressStartTime;

            // Eğer web'e bağlı ise jump yapma; tırmanma WebState tarafından handle ediliyor.
            if (isGrounded && !attached)
            {
                if (heldDuration <= tapThreshold)
                {
                    // kısa tap: normal zıpla
                    AddJumpForce(baseJumpForce);
                }
                else
                {
                    // charged jump: interpolate base -> max by charge ratio
                    float ratio = jumpCharge / maxChargeTime;
                    float jumpStrength = Mathf.Lerp(baseJumpForce, maxJumpForce, ratio);
                    AddJumpForce(jumpStrength);
                }

                // state değişimi
                stateMachine.ChangeState(airState);
            }

            // reset UI & charge (her durumda gizle ve sıfırla)
            jumpCharge = 0f;
            if (jumpChargeUI != null)
            {
                jumpChargeUI.SetVisible(false);
                jumpChargeUI.SetCharge(0f);
            }

            // reset release flag (we consumed it)
            inputJumpReleased = false;
        }
    }

    private void GroundCheck()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance + 0.01f, groundLayers);
    }

    // movement helper unchanged
    public void Move(Vector3 localMove, bool useAirControl)
    {
        Vector3 desired = transform.TransformDirection(localMove) * moveSpeed;
        Vector3 vel = rb.linearVelocity;
        Vector3 horizVel = new Vector3(vel.x, 0, vel.z);
        float control = useAirControl ? airControlMultiplier : 1f;
        Vector3 newHoriz = Vector3.Lerp(horizVel, new Vector3(desired.x, 0, desired.z), 0.2f * control);
        rb.linearVelocity = new Vector3(newHoriz.x, vel.y, newHoriz.z);
    }

    // AddJumpForce now accepts a strength value (velocity change)
    public void AddJumpForce(float strength)
    {
        Vector3 v = rb.linearVelocity;
        v.y = 0; // cancel vertical velocity for consistent jumps
        rb.linearVelocity = v;
        rb.AddForce(Vector3.up * strength, ForceMode.VelocityChange);
    }
}
