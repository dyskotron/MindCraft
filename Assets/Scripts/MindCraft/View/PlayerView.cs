using Framewerk.UI;
using MindCraft.Controller;
using MindCraft.Data;
using MindCraft.Physics;
using strange.extensions.mediation.impl;
using Temari.Common;
using UnityEngine;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

public class PlayerView : View
{
    [FormerlySerializedAs("CameraPlaceholder")] public Transform CameraContainer;
    public Transform PlayerPick;
}

public class PlayerMediator : ExtendedMediator
{
    [Inject] public IVoxelPhysicsWorld Physics{ get; set; }
    [Inject] public IPlayerController PlayerController{ get; set; }
    [Inject] public IWorldSettings WorldSettings{ get; set; }
    [Inject] public ViewConfig ViewConfig{ get; set; }
    
    [Inject] public PlayerView View { get; set; }

    public override void OnRegister()
    {
        base.OnRegister();
        
        var playerBody = new VoxelRigidBody(WorldSettings.PlayerSettings.Radius, 
                                            WorldSettings.PlayerSettings.Height, 
                                            View.transform);
        //Register player to Physics
        Physics.AddRigidBody(playerBody);
            
        //Start PlayerController
        PlayerController.Init(playerBody, View.CameraContainer, View.PlayerPick);
        
        //Reparent the camera under player
        var cameraTransform = ViewConfig.Camera3d.transform;
        cameraTransform.SetParent(View.CameraContainer);
        cameraTransform.localPosition = Vector3.zero;
    }
}