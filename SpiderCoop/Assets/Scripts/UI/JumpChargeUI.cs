using UnityEngine;
using UnityEngine.UI;

public class JumpChargeUI : MonoBehaviour
{
    [Tooltip("Baðlayacaðýnýz Slider komponenti")]
    public Slider slider;

    [Tooltip("Root GameObject (panel) - SetActive ile gizle/göster için)")]
    public GameObject root;

    private void Reset()
    {
        // otomatik baðlama denemesi
        if (slider == null) slider = GetComponent<Slider>();
        if (root == null) root = gameObject;
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

        // baþlangýçta gizli olmasýný istiyorsan root.SetActive(false) býrak
        if (root != null) root.SetActive(false);
    }

    // PlayerController tarafýndan çaðrýlacak
    public void SetCharge(float t)
    {
        if (slider != null)
        {
            slider.value = Mathf.Clamp01(t);
        }
    }

    // PlayerController tarafýndan çaðrýlacak
    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
