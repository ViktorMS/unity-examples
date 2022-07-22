# Convert 3D third person sample to Multiplayer
## Unity Netcode, Lobby, Relay

Install Netcode, Lobby, Relay *(Window - Package Manager)*

<sub>(if Visual Studio does not regognize Netcode, then open Unity - Preferences - External editors - Uncheck everything - Regenerate project files - Check everything - Regenerate project files)</sub>

- Open Prefabs folder, drag PlayerArmeture to prefabs folder and create Original Prefab
- Rename it NetworkPlayer (or anything you like) and open the prefab
- Drag the MainCamera and PlayerFollowCamera prefabs into the NetworkPlayer so each player spawns its own camera
- Disable all components in MainCamera and PlayerFollowCamera (we will enable them again only for the owner)
- Open PlayerFollowCamera and drag the PlayerCameraRoot gameobject into the Follow slot inside the Inspector


- Open the Scripts folder and duplicate ThirdPersonController and rename it NetworkThirdPersonController
- Add the NetworkThirdPersonController to the NetworkPlayer gameobject and disable all components except Animator and Character Controller (we will enable input for only the owner)
- ...
