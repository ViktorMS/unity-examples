using System;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    public string JoinCode = "";
    private Player loggedInPlayer;
    public GameObject menu;

    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (JoinCode != "") AuthenticationService.Instance.SwitchProfile("Random");

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        loggedInPlayer = new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>());
    }

    public void setJoinCode(string joinCode)
    {
        Debug.Log("Setting join code " + joinCode);
        JoinCode = joinCode;
    }

    void HideMenu()
    {
        menu.GetComponent<Camera>().enabled = false;
        menu.GetComponent<AudioListener>().enabled = false;
        menu.transform.Find("Canvas").gameObject.SetActive(false);
    }

    public void Host()
    {
        StartCoroutine(Example_ConfigureTransportAndStartNgoAsHost());
        HideMenu();
    }

    public void Join()
    {
        StartCoroutine(Example_ConfigureTransportAndStartNgoAsConnectingPlayer());
        HideMenu();
    }

    IEnumerator Example_ConfigureTransportAndStartNgoAsHost()
    {
        Debug.Log("Starting relay");
        var serverRelayUtilityTask = AllocateRelayServerAndGetJoinCode(8);
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

        // Players will join the lobby where they get the Relay Join Code
        // CreateLobby();

        // The .GetComponent method returns a UTP NetworkDriver (or a proxy to it)
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(ipv4address, port, allocationIdBytes, key, connectionData, true);
        NetworkManager.Singleton.StartHost();



        yield return null;
    }

    IEnumerator Example_ConfigureTransportAndStartNgoAsConnectingPlayer()
    {
        // Populate RelayJoinCode beforehand through the UI
        var clientRelayUtilityTask = JoinRelayServerFromJoinCode(JoinCode);

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

        yield return null;
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
