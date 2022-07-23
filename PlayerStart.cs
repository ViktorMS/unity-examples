using Cinemachine;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStart : NetworkBehaviour
{
    void Start()
    {
        // Other players will not have input / camera
        if (!IsOwner) return;

        // Enable cameras
        GameObject main = gameObject.transform.Find("MainCamera").gameObject;
        GameObject follow = gameObject.transform.Find("PlayerFollowCamera").gameObject;

        main.GetComponent<Camera>().enabled = true;
        main.GetComponent<AudioListener>().enabled = true;
        main.GetComponent<CinemachineBrain>().enabled = true;
        follow.GetComponent<CinemachineVirtualCamera>().enabled = true;

        // Enable player input after components are enabled
        GetComponent<PlayerInput>().enabled = true;
        GetComponent<NetworkThirdPersonController>().enabled = true;
    }
}
