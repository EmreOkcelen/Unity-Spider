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
    /// Player i�in bir JumpChargeUI olu�turur ve InitForLocalPlayer ile local camera'y� atar.
    /// D�nd�r�len de�er JumpChargeUI component'idir (null olabilir).
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
            ui.SetActiveForOwner(true); // ba�lang��ta aktif hale getir (PlayerController tekrar ayarlar)
        }
        else
        {
            Debug.LogWarning("[LocalUIManager] Prefab does not have JumpChargeUI component.");
            Destroy(go);
        }

        return ui;
    }
}
