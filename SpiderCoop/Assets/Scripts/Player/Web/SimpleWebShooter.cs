using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SimpleWebShooter : MonoBehaviour
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

    private void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        if (line != null) line.positionCount = 0;
    }

    // Called from WebState.Enter()
    public Vector3? TryShoot()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, grappleLayer))
        {
            attachPoint = hit.point;
            return attachPoint;
        }
        return null;
    }

    public void StartPulling(Rigidbody playerRb)
    {
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
    }

    // Yeni: web'e týrmanma. maxDistance'ý azaltarak oyuncuyu attach noktasýna yaklaþtýrýr.
    public void Climb(Rigidbody playerRb, float climbSpeed)
    {
        if (currentJoint == null) return;

        // Attach noktasýna doðru yön
        Vector3 dir = (attachPoint - playerRb.position).normalized;

        // Yukarý çýkma hareketi (sabit noktaya doðru)
        playerRb.MovePosition(playerRb.position + dir * climbSpeed * Time.deltaTime);

        // LineRenderer güncellemesi
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
