using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStart : NetworkBehaviour
{
    public GameObject main;
    public GameObject follow;

    void Start()
    {
        // Other players will not have input / camera
        if (!IsOwner) return;


        // Enable cameras
        main.GetComponent<Camera>().enabled = true;
        main.GetComponent<AudioListener>().enabled = true;
        main.GetComponent<CinemachineBrain>().enabled = true;
        follow.GetComponent<CinemachineVirtualCamera>().enabled = true;

        // Enable player input after components are enabled
        GetComponent<PlayerInput>().enabled = true;
        GetComponent<NetworkController>().enabled = true;
    }
}