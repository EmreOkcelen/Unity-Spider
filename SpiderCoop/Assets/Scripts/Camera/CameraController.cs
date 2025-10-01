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
        // Eðer PlayerController varsa ve Network'de ise IsOwner kontrolü yap.
        // PlayerController, NetworkBehaviour'den türetilmiþse IsOwner kullanýlabilir.
        bool isOwner = true; // fallback single-player

        if (playerController != null)
        {
            // Güvenli ve doðrudan: PlayerController.IsOwner (PlayerController NetworkBehaviour ise eriþilebilir)
            // Eðer PlayerController aðlý deðilse bu property yine false/true sorununa yol açmaz — ama genelde network refactor'ýnda PlayerController NetworkBehaviour olacak.
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
            // non-owner'larda kamera ve audio kapat, script'i devre dýþý býrak
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            if (cam != null) cam.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
            enabled = false;
            return;
        }

        // owner ise cursor ayarlarýný uygula
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
        // ekstra güvenlik: update sýrasýnda da owner deðilsek hiçbir iþlem yapma
        if (playerController != null && !playerController.IsOwner) return;

        if (Input.GetMouseButton(1)) // 1 = sað mouse tuþu
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
