using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
public class VisualWebRenderer : NetworkBehaviour
{
    private LineRenderer line;
    private PlayerController pc;
    private Coroutine visualCoroutine;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        if (line != null) line.positionCount = 0;
        pc = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Owner için bu görsel component devre dýþý olabilir (owner kendi SimpleWebShooter ile çiziyor)
        if (IsOwner)
        {
            enabled = false;
            if (line != null) line.positionCount = 0;
            return;
        }

        // Ýlk hali uygula
        ApplyVisual(pc.netIsAttached.Value, pc.netAttachPoint.Value);

        // Deðiþiklikleri dinle
        pc.netIsAttached.OnValueChanged += OnAttachChanged;
        pc.netAttachPoint.OnValueChanged += OnAttachPointChanged;
    }

    private void OnAttachChanged(bool oldVal, bool newVal)
    {
        ApplyVisual(newVal, pc.netAttachPoint.Value);
    }

    private void OnAttachPointChanged(Vector3 oldVal, Vector3 newVal)
    {
        if (pc.netIsAttached.Value)
            ApplyVisual(true, newVal);
    }

    private void ApplyVisual(bool isAttached, Vector3 attachPoint)
    {
        if (line == null) return;

        if (isAttached)
        {
            if (visualCoroutine != null) StopCoroutine(visualCoroutine);
            line.positionCount = 2;
            visualCoroutine = StartCoroutine(UpdateLinePositionCoroutine(attachPoint));
        }
        else
        {
            if (visualCoroutine != null) StopCoroutine(visualCoroutine);
            visualCoroutine = null;
            line.positionCount = 0;
        }
    }

    private IEnumerator UpdateLinePositionCoroutine(Vector3 attachPoint)
    {
        while (true)
        {
            // pos0 = oyuncunun güncel pozisyonu (NetworkTransform tarafýndan güncellenir)
            line.SetPosition(0, transform.position);
            line.SetPosition(1, attachPoint);
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (pc != null)
        {
            pc.netIsAttached.OnValueChanged -= OnAttachChanged;
            pc.netAttachPoint.OnValueChanged -= OnAttachPointChanged;
        }
    }
}
