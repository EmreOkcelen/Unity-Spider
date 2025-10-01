using System.Globalization;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class CameraController : MonoBehaviour
{
    public Transform playerRoot; // usually the Player transform
    public Transform cameraTransform; // usually the child camera

    public float mouseSensitivity = 1.8f;
    public float pitchMin = -40f;
    public float pitchMax = 60f;

    public bool lockCursor = false; // default false -> cursor visible

    private float yaw;
    private float pitch;
    private PlayerController playerController;
    private Camera cam;
    private AudioListener audioListener;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (cameraTransform == null)
        {
            cam = Camera.main;
            cameraTransform = cam != null ? cam.transform : null;
        }
        else
        {
            cam = cameraTransform.GetComponent<Camera>();
        }

        audioListener = GetComponentInChildren<AudioListener>();
    }
    void Start()
    {
        // E�er PlayerController varsa ve Network'de ise IsOwner kontrol� yap.
        // PlayerController, NetworkBehaviour'den t�retilmi�se IsOwner kullan�labilir.
        bool isOwner = true; // fallback single-player

        if (playerController != null)
        {
            // G�venli ve do�rudan: PlayerController.IsOwner (PlayerController NetworkBehaviour ise eri�ilebilir)
            // E�er PlayerController a�l� de�ilse bu property yine false/true sorununa yol a�maz � ama genelde network refactor'�nda PlayerController NetworkBehaviour olacak.
            try
            {
                isOwner = playerController.IsOwner;
            }
            catch
            {
                isOwner = true;
            }
        }

        if (!isOwner)
        {
            // non-owner'larda kamera ve audio kapat, script'i devre d��� b�rak
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            if (cam != null) cam.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            enabled = false;
            return;
        }

        // owner ise cursor ayarlar�n� uygula
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        // ekstra g�venlik: update s�ras�nda da owner de�ilsek hi�bir i�lem yapma
        if (playerController != null && !playerController.IsOwner) return;

        if (Input.GetMouseButton(1)) // 1 = sa� mouse tu�u
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

            if (playerRoot != null)
                playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
