using UnityEngine;
using UnityEngine.UI;

public class JumpChargeUI : MonoBehaviour
{
    [Tooltip("Ba�layaca��n�z Slider komponenti")]
    public Slider slider;

    [Tooltip("Root GameObject (panel) - SetActive ile gizle/g�ster i�in)")]
    public GameObject root;

    private void Reset()
    {
        // otomatik ba�lama denemesi
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

        // ba�lang��ta gizli olmas�n� istiyorsan root.SetActive(false) b�rak
        if (root != null) root.SetActive(false);
    }

    // PlayerController taraf�ndan �a�r�lacak
    public void SetCharge(float t)
    {
        if (slider != null)
        {
            slider.value = Mathf.Clamp01(t);
        }
    }

    // PlayerController taraf�ndan �a�r�lacak
    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }
}
