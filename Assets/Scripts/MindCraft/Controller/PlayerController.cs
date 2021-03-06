using System;
using Framewerk;
using Framewerk.StrangeCore;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.Model;
using MindCraft.Physics;
using MindCraft.View;
using strange.framework.api;
using UnityEngine;

namespace MindCraft.Controller
{
    public interface IPlayerController : IDestroyable
    {
        void Init(VoxelRigidBody playerBody, Transform camera, Transform pick);
        void SetEnabled(bool value);
        Vector3 PlayerPosition { get; }
        float Yaw { get; }
        float CameraPitch { get; }
    }

    public class PlayerController : IPlayerController
    {
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IVoxelPhysicsWorld PhysicsWorld { get; set; }
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IWorldRaycaster WorldRaycaster { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }
        [Inject] public ISaveLoadManager SaveLoadManager { get; set; }
        
        [Inject] public BlockTypeSelectedSignal BlockTypeSelectedSignal { get; set; }

        public bool Enabled => _enabled;
        public Vector3 PlayerPosition => _playerBody.Transform.position;
        public float Yaw => _yaw;
        public float CameraPitch => _cameraPitch;

        private bool _enabled;

        private PlayerSettings _playerSettings;

        private float _horizontal;
        private float _vertical;
        private float _mouseHorizontal;
        private float _mouseVertical;
        private float _moveSpeed;

        private bool _jumpRequest;

        private VoxelRigidBody _playerBody;
        private Transform _camera;
        
        private float _yaw = 0f;
        
        private float _cameraPitch = 0f;
        private float _minPitch = -90f;
        private float _maxPitch = 90f;

        [PostConstruct]
        public void PostConstruct()
        {
            _playerSettings = WorldSettings.PlayerSettings;
        }

        public void Init(VoxelRigidBody playerBody, Transform camera, Transform pick)
        {
            _playerBody = playerBody;
            _pick = pick;
            _camera = camera;
            
            if (SaveLoadManager.LoadedGame.IsLoaded)
            {
                _yaw = SaveLoadManager.LoadedGame.Yaw;
                _cameraPitch = SaveLoadManager.LoadedGame.CameraPitch;
                
                _playerBody.Transform.rotation = Quaternion.Euler(_yaw * Vector3.up);
                _camera.transform.localEulerAngles = new Vector3(_cameraPitch, 0f, 0f);
            }

            SetEnabled(true);
        }

        public void SetEnabled(bool value)
        {
            if (_enabled == value)
                return;

            _enabled = value;

            if (_enabled)
            {
                Updater.EveryFrame(UpdateInput);
                Updater.EveryStep(UpdatePlayer);
            }
            else
            {
                Updater.RemoveFrameAction(UpdateInput);
                Updater.RemoveStepAction(UpdatePlayer);
            }
        }

        public void Destroy()
        {
            SetEnabled(false);
            BlockTypeSelectedSignal.RemoveListener(BlockTypeSelectedHandler);  
        }

        private void UpdateInput()
        {
            _horizontal = Input.GetAxis("Horizontal");
            _vertical = Input.GetAxis("Vertical");
            _mouseHorizontal = Input.GetAxis("Mouse X");
            _mouseVertical = Input.GetAxis("Mouse Y");

            _moveSpeed = Input.GetButton("Sprint") ? _playerSettings.RunSpeed : _playerSettings.WalkSpeed;

            if (_playerBody.Grounded && Input.GetButtonDown("Jump"))
                _jumpRequest = true;
            
        }

        private void UpdatePlayer()
        {
            if (_jumpRequest)
                Jump();

            _yaw += _mouseHorizontal * _playerSettings.LookSpeed;
            _cameraPitch -= _mouseVertical * _playerSettings.LookSpeed;
            _cameraPitch = Mathf.Clamp(_cameraPitch, _minPitch, _maxPitch); 
            
            _playerBody.Transform.rotation = Quaternion.Euler(_yaw * Vector3.up);
            _camera.transform.localEulerAngles = new Vector3(_cameraPitch, 0f, 0f);

            //walk / sprint
            _playerBody.Velocity = ((_playerBody.Transform.forward * _vertical) + (_playerBody.Transform.right * _horizontal)) * Time.deltaTime * _moveSpeed;
        }

        private void Jump()
        {
            _playerBody.VerticalMomentum = _playerSettings.JumpForce;
            _jumpRequest = false;
        }
        
        /// TODO: separate controller just for block build / erase blocks
        
        [Inject] public IInventoryModel InventoryModel { get; set; }
        
        private bool _isMining;
        private float _miningStartedTime;
        private Vector3Int _lastMinePosition;
        private BlockDef _minedBlockType;

        private BlockMarker _placeBlockCursor;
        private BlockMarker _mineBlockCursor;
        private Transform _pick;
        private bool _isHit;
        private bool _collidesWithPlayer;

        [PostConstruct]
        public void DiggingPostConstructor()
        {
            _placeBlockCursor = InstanceProvider.GetInstance<BlockMarker>();
            _placeBlockCursor.Init("BuildCursor", WorldSettings.BuildMaterial);
            _placeBlockCursor.SetBlockId((byte)InventoryModel.SelectedBlockType);
            
            _mineBlockCursor = InstanceProvider.GetInstance<BlockMarker>();
            _mineBlockCursor.Init("MineCursor", WorldSettings.MineMaterial);
            _mineBlockCursor.SetMiningProgress(0);
            
            Updater.EveryFrame(UpdateCursorBlocks);
            Updater.EveryFrame(UpdateMining);
            
            BlockTypeSelectedSignal.AddListener(BlockTypeSelectedHandler);
        }

        private void BlockTypeSelectedHandler(BlockTypeId id)
        {
            _placeBlockCursor.SetBlockId((byte)id);    
        }

        private void UpdateCursorBlocks()
        {
            _isHit = WorldRaycaster.Raycast(_camera.position, _camera.forward);

            if (_isHit)
            {
                // ====== Update Place Block cursor ======

                var placeBlockPosition = WorldRaycaster.LastPosition;

                // don't show place block cursor if it collides with the player
                _collidesWithPlayer = PhysicsWorld.CheckBodyOnGlobalXyz(_playerBody, placeBlockPosition.x, placeBlockPosition.y, placeBlockPosition.z);

                _placeBlockCursor.Transform.position = placeBlockPosition;
                
                // ====== Update Mining cursor / position / time ======
                
                var hitPosition = WorldRaycaster.HitPosition;
                if (WorldRaycaster.HitPosition != _lastMinePosition)
                {

                    _mineBlockCursor.Transform.position = WorldRaycaster.HitPosition;
                    
                    //position changed => reset mining timer
                    _miningStartedTime = Time.time;

                    _minedBlockType = BlockDefs.GetDefinitionById((BlockTypeId) WorldModel.GetVoxel(hitPosition.x, hitPosition.y, hitPosition.z));
                    _lastMinePosition = hitPosition;
                }
            }

            UpdateCursorsVisibility();
        }

        private void UpdateMining()
        {
            if (WorldRaycaster.IsHit && Input.GetMouseButtonDown(1))
            {
                var playerPosition = WorldModelHelper.FloorPositionToVector3Int(_playerBody.Transform.position);
                if (WorldRaycaster.LastPosition != playerPosition && WorldRaycaster.LastPosition != playerPosition + Vector3Int.up)
                {
                    WorldModel.EditVoxel(WorldRaycaster.LastPosition, (byte) InventoryModel.SelectedBlockType);
                }
            }
            
            if (!_isMining)
            {
                //check if we could start mining
                if (WorldRaycaster.IsHit && Input.GetMouseButton(0))
                {
                    //START MINING
                    _isMining = true;
                    UpdateCursorsVisibility();
                    _miningStartedTime = Time.time;
                }
            }
            else
            {
                if (!WorldRaycaster.IsHit || !Input.GetMouseButton(0))
                {
                    //STOP MINING
                    _isMining = false;
                    UpdateCursorsVisibility();
                    _mineBlockCursor.SetMiningProgress(0);
                    _pick.transform.localRotation = Quaternion.identity;
                    return;
                }

                var miningLength = Time.time - _miningStartedTime;
                var hits = Mathf.FloorToInt(miningLength / _playerSettings.MiningInterval);
                var blockHardness = _minedBlockType.Hardness;

                var animProgress = _playerSettings.PickMovementCurve.Evaluate((miningLength % _playerSettings.MiningInterval) / _playerSettings.MiningInterval);

                _pick.transform.localRotation = Quaternion.Euler(new Vector3(-45 * animProgress, 0 ,0));

                //blockHardness == means indestructible
                

                //finish mining (remove block)
                if (hits >= blockHardness && blockHardness != 0)
                {
                    WorldModel.EditVoxel(_lastMinePosition, BlockTypeByte.AIR);
                    UpdateCursorsVisibility();
                    return;
                }
                
                //anim block
                if(blockHardness > 0)
                    _mineBlockCursor.SetMiningProgress(hits / (float)blockHardness);
            }
            
        }
        
        private void UpdateCursorsVisibility()
        {
            _placeBlockCursor.SetActive(_isHit && !_collidesWithPlayer && !_isMining);
            _placeBlockCursor.SetActive(false);
            _mineBlockCursor.SetActive(_isHit && _isHit);    
        }
    }
}