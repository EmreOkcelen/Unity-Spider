using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnManager : NetworkBehaviour
{
    private Transform hostSpawnPoint;
    private Transform clientSpawnPoint;

    public override void OnNetworkSpawn()
    {
        // Sahnedeki spawn pointleri bul
        hostSpawnPoint = GameObject.FindWithTag("HostSpawn").transform;
        clientSpawnPoint = GameObject.FindWithTag("ClientSpawn").transform;

        if (!IsOwner) return;

        if (OwnerClientId == 0) // Host
        {
            transform.position = hostSpawnPoint.position;
            transform.rotation = hostSpawnPoint.rotation;
        }
        else // Client
        {
            transform.position = clientSpawnPoint.position;
            transform.rotation = clientSpawnPoint.rotation;
        }
    }
}
