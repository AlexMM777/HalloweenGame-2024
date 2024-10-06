using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayersManager : Singleton<PlayersManager>
{
    private NetworkVariable<int> playersInGame = new NetworkVariable<int>();
    private NetworkVariable<bool> isWerewolf = new NetworkVariable<bool>();

    public int PlayersInGame
    {
        get { return playersInGame.Value; }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if (IsServer)
            {
                print($"{id} just connected...");
                playersInGame.Value++;
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            if (IsServer)
            {
                print($"{id} just disconnected..."); playersInGame.Value--;
            }
        };
    }

}
