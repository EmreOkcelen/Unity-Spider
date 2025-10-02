using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
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

    [Header("Networked Web")]
    // using Unity.Netcode;
    public NetworkVariable<bool> netIsAttached = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    public NetworkVariable<Vector3> netAttachPoint = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [HideInInspector]
    public NetworkVariable<Vector3> netVelocity = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);


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

        // Eğer inspector'da atanmadıysa, aynı GameObject veya child'larında Component var mı diye kontrol et (include inactive)
        if (webShooter == null)
        {
            webShooter = GetComponentInChildren<SimpleWebShooter>(true);
        }

        stateMachine = new StateMachine();
        groundedState = new GroundedState(this, stateMachine);
        airState = new AirState(this, stateMachine);

        // !!! webShooter artık doğru şekilde setlendikten sonra WebState oluştur !!!
        webState = new WebState(this, stateMachine, webShooter);

        if (playerCamera == null) playerCamera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        Debug.Log($"[PlayerController] OnNetworkSpawn - GO:{gameObject.name} OwnerClientId:{NetworkObject.OwnerClientId} LocalClientId:{NetworkManager.Singleton.LocalClientId} IsOwner:{IsOwner} IsServer:{NetworkManager.Singleton.IsServer}");
        // Eğer bu oyuncu local owner ise fizik simülasyonu devam etmeli.
        // Değilse rb kinematik yap (diğer clientlarda fizik hesaplamayın).

        if (rb != null)
        {
            Debug.Log($"[PlayerController] rb.isKinematic initially = {rb.isKinematic}");
        }

        if (!IsOwner)
        {
            rb.isKinematic = true;
        }
        else
        {
            rb.isKinematic = false;
        }

        // net variable değişimlerini dinle (ör: diğer clientlarda line renderer güncellemesi)
        netIsAttached.OnValueChanged += (oldVal, newVal) =>
        {
            // burada SimpleWebShooter veya WebState diğer clientlar için görsel güncelleme yapabilir
        };
        netAttachPoint.OnValueChanged += (oldVal, newVal) =>
        {
            // attach point değiştiğinde görsel güncelle
        };

        if (IsOwner)
        {
            // UI'ı sahneden instantiate et ve referansı kendine ata
            if (LocalUIManager.Instance != null)
            {
                var ui = LocalUIManager.Instance.CreateJumpChargeUIForPlayer(playerCamera);
                jumpChargeUI = ui; // PlayerController.jumpChargeUI artık sahneden oluşturulan UI'ya referans tutar
                if (jumpChargeUI != null) jumpChargeUI.SetVisible(false);
            }
        }
        else
        {
            // non-owner: jumpChargeUI prefab child olsa bile kapat. (ya da null bırak)
            if (jumpChargeUI != null) jumpChargeUI.gameObject.SetActive(false);
        }
    }

    // Owner -> Server isteği
    [ServerRpc(RequireOwnership = true)]
    public void RequestAttachServerRpc(Vector3 attachPoint)
    {
        netAttachPoint.Value = attachPoint;
        netIsAttached.Value = true;
        Debug.Log($"[PlayerController] Server set attach for Owner:{NetworkObject.OwnerClientId} at {attachPoint}");
    }

    [ServerRpc(RequireOwnership = true)]
    public void RequestDetachServerRpc()
    {
        netIsAttached.Value = false;
        Debug.Log($"[PlayerController] Server cleared attach for Owner:{NetworkObject.OwnerClientId}");
    }

    private void Start()
    {
        stateMachine.Initialize(groundedState);
        if (jumpChargeUI != null) jumpChargeUI.SetVisible(false);

        // Kamera kontrolü: sadece owner'da aktif et
        if (!IsOwner && playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }
    }


    private float dbgTimer = 0f;
    private void Update()
    {
        dbgTimer += Time.deltaTime;
        if (dbgTimer > 2f) // her 2 saniyede bir log
        {
            dbgTimer = 0f;
            Debug.Log($"[PlayerController] Update Debug - GO:{gameObject.name} OwnerClientId:{NetworkObject.OwnerClientId} LocalClientId:{NetworkManager.Singleton.LocalClientId} IsOwner:{IsOwner} rb.isKinematic={(rb != null ? rb.isKinematic.ToString() : "null")}");
        }

        // input ve UI sadece owner'da okunacak / güncellenecek
        if (IsOwner)
        {
            ReadInput();
            HandleJumpChargeUIAndLogic();
            stateMachine.LogicUpdate();
        }
    }

    private void FixedUpdate()
    {
        GroundCheck();

        if (IsOwner)
        {
            stateMachine.PhysicsUpdate();
        }
    }

    private void ReadInput()
    {
        // tamamen aynı input okuma kodu, sadece IsOwner olduğunda çağrılıyor
        inputMove = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        inputFire = Input.GetMouseButtonDown(0);

        // jump inputs (aynı)
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
        // Eğer Rigidbody kinematik ise (ör. şu anda climbing modunda) fiziksel velocity ayarlanamaz.
        // Bu durumda Move()'u atla — tırmanma kendi MovePosition ile kontrol ediliyor.
        if (rb == null || rb.isKinematic)
            return;

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

    public void SetNetAttachPoint(Vector3 point)
    {
        if (!IsOwner) return;
        netAttachPoint.Value = point;
        netIsAttached.Value = true;
    }

    public void ClearNetAttach()
    {
        if (!IsOwner) return;
        netIsAttached.Value = false;
    }

}
