using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SimpleWebShooter : NetworkBehaviour
{
    [Header("Web Settings")]
    public float maxDistance = 20f;
    public LayerMask grappleLayer; // set to Grappleable
    public LineRenderer line;

    [Header("Spring Settings")]
    public float spring = 100f;
    public float damper = 7f;
    public float massScale = 1f;

    private SpringJoint currentJoint;
    private Vector3 attachPoint;
    private Coroutine lineCoroutine;

    private PlayerController pc;

    private void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        if (line != null) line.positionCount = 0;
        pc = GetComponent<PlayerController>();
    }

    // Called from WebState.Enter() (owner tarafýndan çaðrýlmalý)
    public Vector3? TryShoot()
    {
        if (!IsOwner) return null;

        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("SimpleWebShooter.TryShoot: Camera.main is null"); return null; }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, grappleLayer))
        {
            attachPoint = hit.point;

            var pc = GetComponent<PlayerController>();
            if (pc != null && pc.IsOwner)
            {
                // Server'a bildir
                pc.RequestAttachServerRpc(attachPoint);
                Debug.Log($"[SimpleWebShooter] Owner requested server attach at {attachPoint}");
            }
            else
            {
                Debug.LogWarning("[SimpleWebShooter] TryShoot: PlayerController missing or not owner.");
            }

            return attachPoint;
        }
        return null;
    }


    public void StartPulling(Rigidbody playerRb)
    {
        if (!IsOwner) return; // sadece owner fiziksel joint yapar

        if (currentJoint != null) StopPulling();

        currentJoint = playerRb.gameObject.AddComponent<SpringJoint>();
        currentJoint.autoConfigureConnectedAnchor = false;
        currentJoint.connectedAnchor = attachPoint;

        float distance = Vector3.Distance(playerRb.position, attachPoint);
        currentJoint.maxDistance = distance * 0.8f;
        currentJoint.minDistance = 0f;
        currentJoint.spring = spring;
        currentJoint.damper = damper;
        currentJoint.massScale = massScale;

        if (line != null)
        {
            line.positionCount = 2;
            UpdateLine(playerRb);
        }

        if (lineCoroutine != null) StopCoroutine(lineCoroutine);
        lineCoroutine = StartCoroutine(UpdateLineCoroutine(playerRb));
    }

    public void StopPulling()
    {
        if (!IsOwner) return;

        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
        }

        if (line != null)
        {
            line.positionCount = 0;
        }

        if (lineCoroutine != null)
        {
            StopCoroutine(lineCoroutine);
            lineCoroutine = null;
        }

        if (pc != null && pc.IsOwner)
        {
            pc.netIsAttached.Value = false;
        }
    }

    public void Climb(Rigidbody playerRb, float climbSpeed)
    {
        if (!IsOwner) return;
        if (currentJoint == null) return;

        Vector3 dir = (attachPoint - playerRb.position).normalized;
        playerRb.MovePosition(playerRb.position + dir * climbSpeed * Time.deltaTime);
        UpdateLine(playerRb);
    }

    // Yeni: baðlý mý diye dýþarýya bilgi ver.
    public bool IsAttached()
    {
        return currentJoint != null;
    }

    private IEnumerator UpdateLineCoroutine(Rigidbody playerRb)
    {
        while (currentJoint != null)
        {
            UpdateLine(playerRb);
            yield return null;
        }
    }

    private void UpdateLine(Rigidbody playerRb)
    {
        if (line == null || line.positionCount < 2) return;
        if (playerRb != null)
            line.SetPosition(0, playerRb.position);
        else
            line.SetPosition(0, transform.position);
        line.SetPosition(1, attachPoint);
    }
}
