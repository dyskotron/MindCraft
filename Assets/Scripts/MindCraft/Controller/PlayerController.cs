using System;
using Framewerk;
using MindCraft.Data;
using MindCraft.Data.Defs;
using MindCraft.Model;
using MindCraft.Physics;
using MindCraft.View;
using strange.framework.api;
using Temari.Common;
using UnityEngine;

namespace MindCraft.Controller
{
    public interface IPlayerController
    {
        void Init(VoxelRigidBody playerBody, Transform camera, Transform pick);
        void SetEnabled(bool value);
        void Destroy();
    }

    public class PlayerController : IPlayerController
    {
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IInstanceProvider InstanceProvider { get; set; }
        [Inject] public IWorldModel WorldModel { get; set; }
        [Inject] public IWorldSettings WorldSettings { get; set; }
        [Inject] public IWorldRaycaster WorldRaycaster { get; set; }
        [Inject] public IBlockDefs BlockDefs { get; set; }
        [Inject] public ViewConfig ViewConfig { get; set; }

        public bool Enabled => _enabled;
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

            _playerBody.Transform.Rotate(_mouseHorizontal * _playerSettings.LookSpeed * Vector3.up);
            _camera.transform.Rotate(-_mouseVertical * _playerSettings.LookSpeed * Vector3.right);

            //walk / sprint
            _playerBody.Velocity = ((_playerBody.Transform.forward * _vertical) + (_playerBody.Transform.right * _horizontal)) * Time.deltaTime * _moveSpeed;
        }

        private void Jump()
        {
            _playerBody.VerticalMomentum = _playerSettings.JumpForce;
            _playerBody.Grounded = false; //TODO: make only physics responsible for determining if the player is grounded
            _jumpRequest = false;
        }


        /// separate controller just for block build / erase blocks?
        private bool _validMiningPosition;

        private bool _isMining;
        private float _miningStartedTime;
        private Vector3Int _lastMinePosition;
        private BlockDef _minedBlockType;

        private BlockMarker _placeBlockCursor;
        private BlockMarker _mineBlockCursor;
        private Transform _pick;
        private byte _placeBlockType;

        [PostConstruct]
        public void DiggingPostConstructor()
        {
            _placeBlockCursor = InstanceProvider.GetInstance<BlockMarker>();
            _placeBlockCursor.Init("BuildCursor", WorldSettings.BuildMaterial);
            _placeBlockCursor.SetBlockId((byte)BlockTypeId.Rock);
            
            
            _mineBlockCursor = InstanceProvider.GetInstance<BlockMarker>();
            _mineBlockCursor.Init("MineCursor", WorldSettings.MineMaterial);
            _mineBlockCursor.SetMiningProgress(0);
            
            Updater.EveryFrame(PlaceCursorBlocks);
            Updater.EveryFrame(UpdateMining);
        }

        private void PlaceCursorBlocks()
        {
            var isHit = WorldRaycaster.Raycast(_camera.position, _camera.forward);
            
            _mineBlockCursor.Transform.position = WorldRaycaster.HitPosition;
            _placeBlockCursor.Transform.position = WorldRaycaster.LastPosition;
            
            _placeBlockCursor.SetActive(isHit);
            _mineBlockCursor.SetActive(isHit);
            
            if(isHit)
                SetMiningPosition(WorldRaycaster.HitPosition);
        }

        private void UpdateMining()
        {
            if (!_isMining)
            {
                //check if we could start mining
                if (WorldRaycaster.IsHit && Input.GetMouseButton(0))
                {
                    //START MINING
                    _isMining = true;
                    _miningStartedTime = Time.time;
                }
            }
            else
            {
                if (!WorldRaycaster.IsHit || !Input.GetMouseButton(0))
                {
                    //STOP MINING
                    _isMining = false;
                    _mineBlockCursor.SetMiningProgress(0);
                    _pick.transform.localRotation = Quaternion.identity;
                    return;
                }

                //block can't be mined (bedrock)
                if (_minedBlockType.Hardness == 0)
                    return;

                var miningLength = Time.time - _miningStartedTime;
                var hits = Mathf.FloorToInt(miningLength / _playerSettings.MiningInterval);

                var animProgress = _playerSettings.PickMovementCurve.Evaluate((miningLength % _playerSettings.MiningInterval) / _playerSettings.MiningInterval);

                _pick.transform.localRotation = Quaternion.Euler(new Vector3(-45 * animProgress, 0 ,0));

                _mineBlockCursor.SetMiningProgress(hits);

                //finish mining (remove block)
                if (hits >= _minedBlockType.Hardness)
                    WorldModel.EditVoxel(_lastMinePosition, VoxelTypeByte.AIR);
            }
            
            if (WorldRaycaster.IsHit)
            {
                if (Input.GetMouseButtonDown(1))
                    WorldModel.EditVoxel(WorldRaycaster.LastPosition, (byte)(_placeBlockType + 2));
            
            }

            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            {
                //temp hack to avoid empty + air blocks (1, 2)
                var numBlocks = Enum.GetValues(typeof(BlockTypeId)).Length - 2;

                byte someVar = 2;
                someVar = (byte) (someVar - 3);

                var dir = (int)Mathf.Sign(Input.mouseScrollDelta.y);
                _placeBlockType = (byte) ((_placeBlockType + numBlocks + dir) % numBlocks);
                _placeBlockCursor.SetBlockId(_placeBlockType + 2);
            }  
        }

        private void SetMiningPosition(Vector3Int position)
        {
            if (position == _lastMinePosition)
                return;

            _mineBlockCursor.Transform.position = position;
            _mineBlockCursor.SetActive(true);
            _validMiningPosition = true;

            //position changed => reset mining timer
            _miningStartedTime = Time.time;

            _minedBlockType = BlockDefs.GetDefinitionById((BlockTypeId) WorldModel.GetVoxel(position.x, position.y, position.z));

            Debug.LogWarning($"<color=\"aqua\">Player.SetMiningPosition() : WE ARE GOING TO MINE {_minedBlockType.Name}</color>");

            _lastMinePosition = position;
        }
    }
}