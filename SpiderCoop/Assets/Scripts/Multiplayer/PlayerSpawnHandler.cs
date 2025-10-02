using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerSpawnHandler : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Sadece kendi (local) oyuncusu pozisyonunu ayarla — herkesin kendi pozisyonunu belirlemesi yeterli.
        // (Alternatif: server-side spawn istersen aþaðýdaki yöntemi kullanma, server spawn daha saðlamdýr.)
        if (!IsOwner)
            return;

        if (SceneSpawnPoints.Instance == null)
        {
            Debug.LogWarning("[PlayerSpawnHandler] SceneSpawnPoints.Instance yok. Spawn noktasý atanmadý.");
            return;
        }

        Transform spawn = SceneSpawnPoints.Instance.GetSpawnPoint(NetworkObject.OwnerClientId);
        if (spawn != null)
        {
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            // Eðer NetworkTransform varsa: pozisyonu anýnda setlemek istiyorsan NetworkTransform interpolasyonunu
            // biraz bekleyip resetlemek gerekebilir; ama çoðu durumda bu yeterlidir.
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnHandler] Spawn point for clientId {NetworkObject.OwnerClientId} is null.");
        }
    }
}
