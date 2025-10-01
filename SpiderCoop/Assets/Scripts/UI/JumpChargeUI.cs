using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class JumpChargeUI : MonoBehaviour
{
    [Tooltip("Baðlayacaðýnýz Slider komponenti")]
    public Slider slider;

    [Tooltip("Root GameObject (panel) - SetActive ile gizle/göster için)")]
    public GameObject root;

    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private void Reset()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>(true);
        if (root == null) root = gameObject;
    }

    private void Awake()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>(true);
        if (root == null) root = gameObject;
        canvas = GetComponentInChildren<Canvas>(true);
        canvasGroup = GetComponentInChildren<CanvasGroup>(true);
    }

    private void Start()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.wholeNumbers = false;
            slider.interactable = false;
        }

        if (root != null) root.SetActive(false);
    }

    // Eðer prefab Canvas içermiyorsa runtime'da bir Overlay Canvas oluþturur ve slider'ý altýna taþýr.
    private void EnsureCanvasExists()
    {
        if (canvas != null) return;

        // Eðer root null ise yapacak þey yok
        if (root == null)
        {
            Debug.LogWarning("[JumpChargeUI] EnsureCanvasExists: root is null.");
            return;
        }

        // Yeni Canvas GameObject oluþtur
        GameObject canvasGO = new GameObject("JumpChargeCanvas");
        canvasGO.transform.SetParent(root.transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        // Ek bileþenler
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Eðer slider varsa onu Canvas altýna taþý
        if (slider != null)
        {
            // slider GameObject'i UI hiyerarþisine uygun hale getir
            slider.transform.SetParent(canvasGO.transform, false);
        }

        // CanvasGroup ekle
        canvasGroup = canvasGO.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvasGO.AddComponent<CanvasGroup>();
    }

    // InitForLocalPlayer içinde canvas atamadan sonra ekle:
    public void InitForLocalPlayer(Camera cam)
    {
        EnsureCanvasExists();

        if (canvas == null) return;

        if (cam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
        }

        // ÖNEMLÝ: planeDistance kameranýn clipping aralýðýna uygun mu kontrol et ve uygula
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera != null)
        {
            Camera c = canvas.worldCamera;
            // planeDistance MUST be between nearClipPlane and farClipPlane
            float desiredPlane = Mathf.Clamp(c.nearClipPlane + 0.5f, c.nearClipPlane + 0.001f, c.farClipPlane - 0.001f);
            canvas.planeDistance = desiredPlane;

            Debug.Log($"[JumpChargeUI] InitForLocalPlayer: assigned worldCamera '{c.name}', near={c.nearClipPlane}, far={c.farClipPlane}, planeDistance={canvas.planeDistance}");
        }
        else
        {
            Debug.Log($"[JumpChargeUI] InitForLocalPlayer: using Overlay mode");
        }

        canvas.sortingOrder = 5000;

        if (canvasGroup == null)
        {
            canvasGroup = canvas.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        canvas.enabled = true;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Slider transform ayarlarý - güvenlik
        if (slider != null && slider.gameObject != null)
        {
            var rt = slider.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(300, 30); // makul bir default
            }
            slider.gameObject.SetActive(false); // baþlangýçta gizli; SetVisible(true) ile aktive edilecek
        }
    }

    public void SetCharge(float t)
    {
        if (slider != null)
        {
            slider.value = Mathf.Clamp01(t);
        }
    }

    public void SetVisible(bool visible)
    {
        if (root == null)
        {
            Debug.LogWarning("[JumpChargeUI] root is null.");
            return;
        }

        root.SetActive(visible);

        if (visible && canvas == null)
        {
            EnsureCanvasExists();
        }

        if (canvas != null)
        {
            if (visible && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // Eðer kamera atanmýþsa planeDistance'ý tekrar doðrula
                if (canvas.worldCamera != null)
                {
                    Camera c = canvas.worldCamera;
                    float desiredPlane = Mathf.Clamp(c.nearClipPlane + 0.5f, c.nearClipPlane + 0.001f, c.farClipPlane - 0.001f);
                    canvas.planeDistance = desiredPlane;
                    Debug.Log($"[JumpChargeUI] SetVisible(true): canvas planeDistance set to {canvas.planeDistance} (cam {c.name}, near {c.nearClipPlane}, far {c.farClipPlane})");
                }
                else
                {
                    Debug.LogWarning("[JumpChargeUI] SetVisible: canvas.worldCamera is null while in ScreenSpaceCamera mode");
                }
            }

            canvas.enabled = visible;
        }

        if (slider != null && slider.gameObject != null)
        {
            slider.gameObject.SetActive(visible);

            // görsel hala görünmüyorsa scale/pos kontrolü
            var rt = slider.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                // Ekranda görünmeme durumunda anchoredPosition'ý merkeze ayarla
                if (visible)
                {
                    rt.anchoredPosition = Vector2.zero;
                }
            }
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        Debug.Log($"[JumpChargeUI] SetVisible({visible}) -> rootActive={root.activeInHierarchy} canvasMode={(canvas != null ? canvas.renderMode.ToString() : "null")} worldCamAssigned={(canvas != null && canvas.worldCamera != null)} worldCamActive={(canvas != null && canvas.worldCamera != null ? canvas.worldCamera.gameObject.activeInHierarchy : false)} sliderActive={(slider != null ? slider.gameObject.activeInHierarchy : false)}");
    }


    public void SetActiveForOwner(bool isOwner)
    {
        if (gameObject != null)
            gameObject.SetActive(isOwner);
    }
}
