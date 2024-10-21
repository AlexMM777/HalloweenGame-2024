using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayersManager : Singleton<PlayersManager>
{
    private NetworkVariable<int> playersInGame = new NetworkVariable<int>();
    public int PlayersInGame => playersInGame.Value;

    // Character selection
    public NetworkVariable<bool> someoneIsWerewolf = new NetworkVariable<bool>(); public NetworkVariable<bool> someoneIsVampire = new NetworkVariable<bool>();
    public NetworkVariable<bool> someoneIsZombie = new NetworkVariable<bool>(); public NetworkVariable<bool> someoneIsGhost = new NetworkVariable<bool>();
    public Button defaultBtn, werewolfBtn, vampireBtn, zombieBtn, ghostBtn;
    public GameObject player0Ready, player1Ready, player2Ready, player3Ready;
    public Button readyBtn;
    public List<GameObject> playerObjects = new List<GameObject>();
    public NetworkVariable<int> playersReady = new NetworkVariable<int>();
    private bool inLobby;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Character selection (Need to use PlayerAuthorative_New so it works) (Can set inLobby to false so that you can go straight to playing, but haven't tested)
        inLobby = true;
        if(inLobby)
        {
            player0Ready.SetActive(false); player1Ready.SetActive(false);
            player2Ready.SetActive(false); player3Ready.SetActive(false);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"{clientId} just connected...");
            playersInGame.Value++;
        }

        // Character selection
        if (inLobby)
        {
            for (int i = 0; i < playerObjects.Count; i++)
            {
                playerObjects[i].GetComponent<PlayerControlAuthorative>().locIndex = i;
                playerObjects[i].GetComponent<PlayerControlAuthorative>().UpdateSelectCharMenuPosition();
                playerObjects[i].GetComponent<PlayerControlAuthorative>().readyToggle.SetActive(true);
                print($"Player {i} is in idex {playerObjects[i].GetComponent<PlayerControlAuthorative>().locIndex}");
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"{clientId} just disconnected...");
            playersInGame.Value--;
        }
    }


    #region Character Selection
    // Werewolf
    [ServerRpc(RequireOwnership = false)]
    public void SetWerewolfServerRpc()
    {
        someoneIsWerewolf.Value = true; // Update the network var that werewolf was selected
    }
    [ServerRpc(RequireOwnership = false)]
    public void ClearWerewolfServerRpc()
    {
        someoneIsWerewolf.Value = false;
    }

    // Vampire
    [ServerRpc(RequireOwnership = false)]
    public void SetVampireServerRpc()
    {
        someoneIsVampire.Value = true;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ClearVampireServerRpc()
    {
        someoneIsVampire.Value = false;
    }

    // Zombie
    [ServerRpc(RequireOwnership = false)]
    public void SetZombieServerRpc()
    {
        someoneIsZombie.Value = true;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ClearZombieServerRpc()
    {
        someoneIsZombie.Value = false;
    }

    // Ghost
    [ServerRpc(RequireOwnership = false)]
    public void SetGhostServerRpc()
    {
        someoneIsGhost.Value = true;
    }
    [ServerRpc(RequireOwnership = false)]
    public void ClearGhostServerRpc()
    {
        someoneIsGhost.Value = false;
    }


    public void AddPlayerReady()
    {
        playersReady.Value++;
    }
    public void RemovePlayerReady()
    {
        playersReady.Value--;
    }
    #endregion
}
