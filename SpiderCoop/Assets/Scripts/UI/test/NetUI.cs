using UnityEngine;
using Unity.Netcode;
using Unity.Networking.Transport; // e�er UnityTransport kullan�yorsan
using Unity.Netcode.Transports.UTP;

public class NetUI : MonoBehaviour
{
    public string connectAddress = "127.0.0.1";
    public ushort connectPort = 7777;

    // Host ba�lat
    public void StartHost()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.StartHost();
        Debug.Log("[NetUI] StartHost()");
    }

    // Client ba�lat (UnityTransport kullan�yorsan connection data ayarla)
    public void StartClient()
    {
        if (NetworkManager.Singleton == null) return;

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (utp != null)
        {
            utp.SetConnectionData(connectAddress, connectPort);
            Debug.Log($"[NetUI] UnityTransport set to {connectAddress}:{connectPort}");
        }

        NetworkManager.Singleton.StartClient();
        Debug.Log("[NetUI] StartClient()");
    }

    // Server (headless) ba�latmak istersen
    public void StartServer()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.StartServer();
        Debug.Log("[NetUI] StartServer()");
    }
}
