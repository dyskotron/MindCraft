using Framewerk;
using MindCraft.Data;
using MindCraft.Physics;
using UnityEngine;

namespace MindCraft.Controller
{
    public interface IPlayerController
    {
        void Init(VoxelRigidBody playerBody, Transform camera);
        void SetEnabled(bool value);
        void Destroy();
    }

    public class PlayerController : IPlayerController
    {
        [Inject] public IUpdater Updater { get; set; }
        [Inject] public IWorldSettings WorldSettings { get; set; }
        
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

        public void Init(VoxelRigidBody playerBody, Transform camera)
        {
            _playerBody = playerBody;
            _camera = camera;
            
            SetEnabled(true);
        }

        public void SetEnabled(bool value)
        {
            if(_enabled == value)
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
            
            /*

            if (_mineBlockMarkerTransform.gameObject.activeSelf)
            {
                if (Input.GetMouseButtonDown(1))
                    WorldModel.EditVoxel(_placeBlockMarkerTransform.position, _placeBlockType);
            
            }

            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            {
                //temp hack to avoid empty + air blocks (1, 2)
                _placeBlockType -= 2;
            
                var dir = Mathf.Sign(Input.mouseScrollDelta.y);
                _placeBlockType = (byte)((_placeBlockType + NUM_USABLE_BLOCK_TYPES + dir) % NUM_USABLE_BLOCK_TYPES);
            
                _placeBlockType += 2;
                _placeBlockMarker.SetVoxelType(_placeBlockType);
            }    
            */
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
    }
}