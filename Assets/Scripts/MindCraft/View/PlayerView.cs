using Framewerk.UI;
using MindCraft.Controller;
using MindCraft.Data;
using MindCraft.Physics;
using strange.extensions.mediation.impl;
using Temari.Common;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerView : View
{
    [FormerlySerializedAs("CameraPlaceholder")] public Transform CameraContainer;
    public Transform PlayerPick;
    public bool Grounded;
    public float VerticalMomentum;
    public Vector3 Velocity;
    public Vector3 AntiForce;
}

public class PlayerMediator : ExtendedMediator
{
    [Inject] public IVoxelPhysicsWorld Physics{ get; set; }
    [Inject] public IPlayerController PlayerController{ get; set; }
    [Inject] public IWorldSettings WorldSettings{ get; set; }
    [Inject] public ViewConfig ViewConfig{ get; set; }
    
    [Inject] public PlayerView View { get; set; }

    private VoxelRigidBody _playerBody;
    
    public override void OnRegister()
    {
        base.OnRegister();
        
        _playerBody = new VoxelRigidBody(WorldSettings.PlayerSettings.Radius, 
                                            WorldSettings.PlayerSettings.Height, 
                                            View.transform);
        //Register player to Physics
        Physics.AddRigidBody(_playerBody);
            
        //Start PlayerController
        PlayerController.Init(_playerBody, View.CameraContainer, View.PlayerPick);
        
        //Reparent the camera under player
        var cameraTransform = ViewConfig.Camera3d.transform;
        cameraTransform.SetParent(View.CameraContainer);
        cameraTransform.localPosition = Vector3.zero;
    }
    
#if UNITY_EDITOR

    private void Update()
    {
        View.Grounded = _playerBody.Grounded;
        View.Velocity = _playerBody.Velocity;
        View.AntiForce = _playerBody.LostVelocity;
        View.VerticalMomentum = _playerBody.VerticalMomentum;
    }
    
#endif
}