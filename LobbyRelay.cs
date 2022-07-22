using System;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using System.Collections;
using Newtonsoft.Json;
/// <summary>
/// A simple sample showing how to use the Relay Allocation package. As the host, you can authenticate, request a relay allocation, get a join code and join the allocation.
/// </summary>
/// <remarks>
/// The sample is limited to calling the Relay Allocation Service and does not cover connecting the host game client to the relay using Unity Transport Protocol.
/// This will cause the allocation to be reclaimed about 10 seconds after creating it.
/// </remarks>
public class LobbyRelay : MonoBehaviour
{
    /// <summary>
    /// The textbox displaying the Player Id.
    /// </summary>
    public Text PlayerIdText;

    /// <summary>
    /// The dropdown displaying the region.
    /// </summary>
    public Dropdown RegionsDropdown;

    /// <summary>
    /// The textbox displaying the Allocation Id.
    /// </summary>
    public Text HostAllocationIdText;

    /// <summary>
    /// The textbox displaying the Join Code.
    /// </summary>
    public Text JoinCodeText;

    /// <summary>
    /// The textbox displaying the Allocation Id of the joined allocation.
    /// </summary>
    public Text PlayerAllocationIdText;

    public bool isClone = false;
    private Player loggedInPlayer;

    private string RelayJoinCode = "";
    private string LobbyJoinCode = "n/a";

    private string lobbyName = Guid.NewGuid().ToString();
    private int maxPlayers = 8;

    Lobby currentLobby;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (isClone) AuthenticationService.Instance.SwitchProfile("Random");

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        loggedInPlayer = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>());

        RefreshUI();

    }

    public void Host()
    {
        Debug.Log("Host");
        StartCoroutine(Example_ConfigureTransportAndStartNgoAsHost());

    }

    public async void CreateLobby()
    {
        Debug.Log(loggedInPlayer.Id + " Attempting to create Lobby");
        // Create a new lobby
        currentLobby = await LobbyService.Instance.CreateLobbyAsync(
            lobbyName: lobbyName,
            maxPlayers: maxPlayers,
            options: new CreateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>()
                    {
                            {
                                "RelayJoinCode", new DataObject(
                                    visibility: DataObject.VisibilityOptions.Member,
                                    value: RelayJoinCode
                                    )
                            },
                    },
                IsPrivate = false,
                Player = loggedInPlayer
            });

        LobbyJoinCode = currentLobby.LobbyCode;

        Debug.Log($"Created new lobby {currentLobby.Name} ({LobbyJoinCode})");

        // Since we're the host, let's wait a second and then heartbeat the lobby
        await Task.Delay(1000);
        await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);

        // Let's print the updated lobby.  The LastUpdated time should be different.
        Debug.Log($"Heartbeated lobby {currentLobby.Name} ({currentLobby.Id})");
        Debug.Log("Updated lobby info:\n" + JsonConvert.SerializeObject(currentLobby));

        Debug.Log(LobbyJoinCode);

        RefreshUI();
    }

    public async void Join()
    {
        LobbyJoinCode = GameObject.FindGameObjectWithTag("LobbyJoinCode").GetComponent<TMPro.TMP_InputField>().text;
        Debug.Log(loggedInPlayer.Id + " - Joining lobby using join code: " + LobbyJoinCode);
        currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(LobbyJoinCode);

        if (currentLobby != null)
        {
            RelayJoinCode = currentLobby.Data["RelayJoinCode"].Value;
            Debug.Log("Relay - Attempting to join: " + RelayJoinCode);
            // Join relay
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(RelayJoinCode);
            var playerAllocationId = joinAllocation.AllocationId;
            Debug.Log("Client Allocation ID: " + playerAllocationId.ToString());

            try
            {
                var (ipv4Address, port, allocationIdBytes, connectionData, hostConnectionData, key) =
                    await JoinRelayServerFromJoinCode(RelayJoinCode);

                var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                utp.SetClientRelayData(ipv4Address, port, allocationIdBytes, key, connectionData, hostConnectionData, isSecure: true);

                Debug.Log("NetworkManager starting as Client");
                NetworkManager.Singleton.StartClient();
            }
            catch (Exception e)
            {
                Debug.Log("Client relay join code failure.");
                Debug.Log(e.Message);
                return;//not re-throwing, but still not allowing to connect
            }


        }

        //if (currentLobby != null)
        //{
        //    RelayJoinCode = currentLobby.Data["RelayJoinCode"].Value;

        //    if (RelayJoinCode != "")
        //    {
        //        Debug.Log("Relay - Got the Join code from Lobby - Attempting to join: " + RelayJoinCode);
        //        StartCoroutine(Example_ConfigureTransportAndStartNgoAsConnectingPlayer());
        //    } else
        //    {
        //        Debug.Log("Failed to get relay join code");
        //    }
        //}
    }

    public void SetJoinCode(string code)
    {
        LobbyJoinCode = code;
        RefreshUI();
    }

    void RefreshUI()
    {
        PlayerIdText.text = $"Player ID: {loggedInPlayer.Id ?? ""}";
        JoinCodeText.text = LobbyJoinCode ?? "";

    }

    IEnumerator Example_ConfigureTransportAndStartNgoAsHost()
    {
        Debug.Log("Starting relay");
        var serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(maxPlayers);
        while (!serverRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }
        if (serverRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to start Relay Server. Server not started. Exception: " + serverRelayUtilityTask.Exception.Message);
            yield break;
        }

        var (ipv4address, port, allocationIdBytes, connectionData, key, joinCode) = serverRelayUtilityTask.Result;

        // Display the join code to the user.
        Debug.Log("Setting relay join code: " + joinCode);

        RelayJoinCode = joinCode;

        // Players will join the lobby where they get the Relay Join Code
        CreateLobby();

        // The .GetComponent method returns a UTP NetworkDriver (or a proxy to it)
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ipv4address, port, allocationIdBytes, key, connectionData, true);
        NetworkManager.Singleton.StartHost();

        SwitchToPlayerCamera();

        yield return null;
    }

    IEnumerator Example_ConfigureTransportAndStartNgoAsConnectingPlayer()
    {
        // Populate RelayJoinCode beforehand through the UI
        var clientRelayUtilityTask = JoinRelayServerFromJoinCode(RelayJoinCode);

        while (!clientRelayUtilityTask.IsCompleted)
        {
            yield return null;
        }

        if (clientRelayUtilityTask.IsFaulted)
        {
            Debug.LogError("Exception thrown when attempting to connect to Relay Server. Exception: " + clientRelayUtilityTask.Exception.Message);
            yield break;
        }

        var (ipv4address, port, allocationIdBytes, connectionData, hostConnectionData, key) = clientRelayUtilityTask.Result;

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(ipv4address, port, allocationIdBytes, key, connectionData, hostConnectionData, true);

        Debug.Log("Starting network manager Client");
        NetworkManager.Singleton.StartClient();

        SwitchToPlayerCamera();

        yield return null;
    }

    private void SwitchToPlayerCamera()
    {
        // Switching from menu camera to player camera
        GetComponent<Camera>().enabled = false;
        GetComponent<AudioListener>().enabled = false;
        transform.Find("Canvas").gameObject.SetActive(false); // Exit menu
    }

    public static async Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] key, string joinCode)> AllocateRelayServerAndGetJoinCode(int maxConnections, string region = null)
    {
        Allocation allocation;
        string createJoinCode;
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }
        catch (Exception e)
        {
            Debug.LogError($"Relay create allocation request failed {e.Message}");
            throw;
        }

        Debug.Log($"server: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"server: {allocation.AllocationId}");

        try
        {
            createJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.Key, createJoinCode);
    }

    public static async Task<(string ipv4address, ushort port, byte[] allocationIdBytes, byte[] connectionData, byte[] hostConnectionData, byte[] key)> JoinRelayServerFromJoinCode(string joinCode)
    {
        JoinAllocation allocation;
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch
        {
            Debug.LogError("Relay create join code request failed");
            throw;
        }

        Debug.Log($"client: {allocation.ConnectionData[0]} {allocation.ConnectionData[1]}");
        Debug.Log($"host: {allocation.HostConnectionData[0]} {allocation.HostConnectionData[1]}");
        Debug.Log($"client: {allocation.AllocationId}");

        var dtlsEndpoint = allocation.ServerEndpoints.First(e => e.ConnectionType == "dtls");
        return (dtlsEndpoint.Host, (ushort)dtlsEndpoint.Port, allocation.AllocationIdBytes, allocation.ConnectionData, allocation.HostConnectionData, allocation.Key);
    }
}
