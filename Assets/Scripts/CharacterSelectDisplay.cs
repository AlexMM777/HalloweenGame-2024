using UnityEngine;
using TMPro;
using Unity.Netcode;

public class CharacterSelectDisplay : NetworkBehaviour
{
    private NetworkList<CharacterSelectState> players;

    private void Awake()
    {
        players = new NetworkList<CharacterSelectState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;

            foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                HandleClientConnected(client.ClientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleClientConnected(ulong cliendId)
    {
        players.Add(new CharacterSelectState(cliendId));
    }
    
    void HandleClientDisconnected(ulong cliendId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId == cliendId)
            {
                players.RemoveAt(i);
                break;
            }
        }
    }
}
