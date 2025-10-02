using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerSpawnHandler : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Sadece kendi (local) oyuncusu pozisyonunu ayarla � herkesin kendi pozisyonunu belirlemesi yeterli.
        // (Alternatif: server-side spawn istersen a�a��daki y�ntemi kullanma, server spawn daha sa�lamd�r.)
        if (!IsOwner)
            return;

        if (SceneSpawnPoints.Instance == null)
        {
            Debug.LogWarning("[PlayerSpawnHandler] SceneSpawnPoints.Instance yok. Spawn noktas� atanmad�.");
            return;
        }

        Transform spawn = SceneSpawnPoints.Instance.GetSpawnPoint(NetworkObject.OwnerClientId);
        if (spawn != null)
        {
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            // E�er NetworkTransform varsa: pozisyonu an�nda setlemek istiyorsan NetworkTransform interpolasyonunu
            // biraz bekleyip resetlemek gerekebilir; ama �o�u durumda bu yeterlidir.
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnHandler] Spawn point for clientId {NetworkObject.OwnerClientId} is null.");
        }
    }
}
