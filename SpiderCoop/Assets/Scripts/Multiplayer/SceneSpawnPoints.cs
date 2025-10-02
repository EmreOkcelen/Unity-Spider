using UnityEngine;

public class SceneSpawnPoints : MonoBehaviour
{
    public static SceneSpawnPoints Instance { get; private set; }

    [Tooltip("Sahnedeki Host spawn noktasý (Editor'dan sürükle)")]
    public Transform hostSpawnPoint;

    [Tooltip("Sahnedeki Client spawn noktasý (Editor'dan sürükle)")]
    public Transform clientSpawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ýstersen sahne deðiþince yok etme:
        // DontDestroyOnLoad(gameObject);
    }

    public Transform GetSpawnPoint(ulong clientId)
    {
        // Host genelde clientId == 0
        if (clientId == 0ul)
            return hostSpawnPoint;
        else
            return clientSpawnPoint;
    }
}
