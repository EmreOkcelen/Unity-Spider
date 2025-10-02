using UnityEngine;

public class SceneSpawnPoints : MonoBehaviour
{
    public static SceneSpawnPoints Instance { get; private set; }

    [Tooltip("Sahnedeki Host spawn noktas� (Editor'dan s�r�kle)")]
    public Transform hostSpawnPoint;

    [Tooltip("Sahnedeki Client spawn noktas� (Editor'dan s�r�kle)")]
    public Transform clientSpawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // �stersen sahne de�i�ince yok etme:
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
