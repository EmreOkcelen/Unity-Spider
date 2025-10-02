using System.Collections;
using UnityEngine;
using Unity.Netcode;

[DisallowMultipleComponent]
public class VisualWebRenderer : NetworkBehaviour
{
    public LineRenderer sourceLine; // optional inspector assign (webEmitter child)
    private LineRenderer visualLine; // non-owner clone
    private PlayerController pc;
    private Coroutine visualCoroutine;

    private string dbgPrefix => $"[VisualWebRenderer][Local:{NetworkManager.Singleton?.LocalClientId ?? 0}][Owner:{NetworkObject?.OwnerClientId ?? 0}]";

    void Awake()
    {
        if (sourceLine == null)
        {
            sourceLine = GetComponentInChildren<LineRenderer>(true);
            if (sourceLine != null) Debug.Log($"{dbgPrefix} Found sourceLine: {sourceLine.gameObject.name}");
        }
        pc = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"{dbgPrefix} OnNetworkSpawn IsOwner={IsOwner}");

        if (IsOwner)
        {
            // owner uses its own child line; we do not interfere
            enabled = false;
            return;
        }

        if (pc == null) pc = GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning($"{dbgPrefix} PlayerController missing - disabling");
            enabled = false;
            return;
        }

        // subscribe to net changes
        pc.netIsAttached.OnValueChanged += OnAttachChanged;
        pc.netAttachPoint.OnValueChanged += OnAttachPointChanged;

        // create clone line immediately (so we have it ready)
        CreateVisualLineFromSource(sourceLine);

        // apply initial state
        OnAttachChanged(false, pc.netIsAttached.Value);
        OnAttachPointChanged(Vector3.zero, pc.netAttachPoint.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Cleanup();
    }

    void OnDestroy() { Cleanup(); }

    private void Cleanup()
    {
        if (pc != null)
        {
            pc.netIsAttached.OnValueChanged -= OnAttachChanged;
            pc.netAttachPoint.OnValueChanged -= OnAttachPointChanged;
        }
        if (visualCoroutine != null) StopCoroutine(visualCoroutine);
        visualCoroutine = null;
        if (visualLine != null) Destroy(visualLine.gameObject);
        visualLine = null;
    }

    private void OnAttachChanged(bool oldVal, bool newVal)
    {
        Debug.Log($"{dbgPrefix} OnAttachChanged {oldVal} -> {newVal}");
        ApplyVisual(newVal, pc.netAttachPoint.Value);
    }

    private void OnAttachPointChanged(Vector3 oldVal, Vector3 newVal)
    {
        Debug.Log($"{dbgPrefix} OnAttachPointChanged {newVal}");
        if (pc.netIsAttached.Value) ApplyVisual(true, newVal);
    }

    private void ApplyVisual(bool isAttached, Vector3 attachPoint)
    {
        if (visualLine == null) CreateVisualLineFromSource(sourceLine);
        if (visualLine == null) { Debug.LogError($"{dbgPrefix} visualLine null - cannot draw"); return; }

        EnsureVisualLineSettings();

        if (isAttached)
        {
            if (visualCoroutine != null) StopCoroutine(visualCoroutine);
            visualCoroutine = StartCoroutine(UpdateVisualCoroutine(attachPoint));
            Debug.Log($"{dbgPrefix} visual started attachPoint={attachPoint}");
        }
        else
        {
            if (visualCoroutine != null) StopCoroutine(visualCoroutine);
            visualCoroutine = null;
            visualLine.positionCount = 0;
            visualLine.enabled = false;
            Debug.Log($"{dbgPrefix} visual stopped");
        }
    }

    private IEnumerator UpdateVisualCoroutine(Vector3 attachPoint)
    {
        visualLine.enabled = true;
        visualLine.positionCount = 2;
        while (true)
        {
            Vector3 pos0 = transform.position; // player world pos
            Vector3 pos1 = attachPoint; // world pos from netvar

            if (float.IsNaN(pos1.x) || float.IsInfinity(pos1.x))
            {
                Debug.LogWarning($"{dbgPrefix} invalid attachPoint {pos1}");
                yield break;
            }

            visualLine.SetPosition(0, pos0);
            visualLine.SetPosition(1, pos1);

            // debug camera culling
            var cam = Camera.main;
            if (cam != null)
            {
                bool sees = (cam.cullingMask & (1 << visualLine.gameObject.layer)) != 0;
                if (!sees) Debug.LogWarning($"{dbgPrefix} Camera cullingMask does NOT include layer {visualLine.gameObject.layer}");
            }

            yield return null;
        }
    }

    private void CreateVisualLineFromSource(LineRenderer src)
    {
        if (visualLine != null) { Destroy(visualLine.gameObject); visualLine = null; }

        GameObject go = new GameObject($"VisualLine_Owner{NetworkObject.OwnerClientId}");
        go.transform.SetParent(null); // put in root so world-space is direct
        visualLine = go.AddComponent<LineRenderer>();

        visualLine.useWorldSpace = true;
        if (src != null)
        {
            visualLine.startWidth = Mathf.Max(0.01f, src.startWidth);
            visualLine.endWidth = Mathf.Max(0.01f, src.endWidth);
            visualLine.numCapVertices = src.numCapVertices;
            visualLine.numCornerVertices = src.numCornerVertices;
            visualLine.material = src.material != null ? new Material(src.material) : CreateFallbackMaterial();
            visualLine.gameObject.layer = this.gameObject.layer;
        }
        else
        {
            visualLine.startWidth = 0.05f;
            visualLine.endWidth = 0.05f;
            visualLine.material = CreateFallbackMaterial();
            visualLine.gameObject.layer = this.gameObject.layer;
        }

        visualLine.positionCount = 0;
        visualLine.enabled = false;
        Debug.Log($"{dbgPrefix} Created visualLine (layer {visualLine.gameObject.layer})");
    }

    private Material CreateFallbackMaterial()
    {
        Material mat = null;
        string[] candidates = { "Universal Render Pipeline/Unlit", "Sprites/Default", "Unlit/Color", "Standard" };
        foreach (var s in candidates)
        {
            var sh = Shader.Find(s);
            if (sh != null) { mat = new Material(sh); break; }
        }
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        return mat;
    }

    private void EnsureVisualLineSettings()
    {
        if (visualLine == null) return;
        visualLine.useWorldSpace = true;
        if (visualLine.startWidth <= 0f) visualLine.startWidth = 0.05f;
        if (visualLine.endWidth <= 0f) visualLine.endWidth = 0.05f;
        if (visualLine.material == null) visualLine.material = CreateFallbackMaterial();
        visualLine.enabled = true;
    }
}
