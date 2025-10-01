using UnityEngine;

public class LocalUIManager : MonoBehaviour
{
    public static LocalUIManager Instance { get; private set; }

    [Tooltip("JumpChargeUI prefab (Canvas + Slider). Prefab should have JumpChargeUI component.")]
    public GameObject jumpChargeUIPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Player için bir JumpChargeUI oluþturur ve InitForLocalPlayer ile local camera'yý atar.
    /// Döndürülen deðer JumpChargeUI component'idir (null olabilir).
    /// </summary>
    public JumpChargeUI CreateJumpChargeUIForPlayer(Camera playerCamera)
    {
        if (jumpChargeUIPrefab == null)
        {
            Debug.LogWarning("[LocalUIManager] jumpChargeUIPrefab not assigned.");
            return null;
        }

        var go = Instantiate(jumpChargeUIPrefab);
        var ui = go.GetComponent<JumpChargeUI>();
        if (ui != null)
        {
            ui.InitForLocalPlayer(playerCamera);
            ui.SetActiveForOwner(true); // baþlangýçta aktif hale getir (PlayerController tekrar ayarlar)
        }
        else
        {
            Debug.LogWarning("[LocalUIManager] Prefab does not have JumpChargeUI component.");
            Destroy(go);
        }

        return ui;
    }
}
