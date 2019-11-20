using MindCraft.MapGeneration;
using MindCraft.MapGeneration.Defs;
using MindCraft;
using MindCraft.GameObjects;
using MindCraft.Model;
using UnityEngine;

public class Player : MonoBehaviour
{
    public const float PLAYER_HEIGHT = 1.8f;
    
    //We'll get this from inventory later on
    public const float NUM_USABLE_BLOCK_TYPES = 4;

    public World World => Locator.World;
    public WorldModel WorldModel => Locator.WorldModel;
    

    public float MiningInterval = 0.8f; // Hits / s
    public float WalkSpeed = 3f;
    public float RunSpeed = 10f;
    public float LookSpeed = 3f;

    public float Gravity = -9.8f;
    public float JumpForce = 5f;

    public float playerSize = 0.3f;
    public Transform Pick;

    public AnimationCurve PickMovementCurve;

    private bool _inited;
    
    private float _horizontal;
    private float _vertical;
    private float _mouseHorizontal;
    private float _mouseVertical;
    private float _v;
    private Vector3 _velocity;
    private Transform _camera;

    private float _moveSpeed = 0;

    private float verticalMomentum; 
    private bool jumpRequest; 
    private bool isGrounded; 
    private Transform _transform;
    
    private EditCube _placeBlockMarker;
    private Transform _placeBlockMarkerTransform;
    
    private EditCube _mineBlockMarker;
    private Transform _mineBlockMarkerTransform;

    private byte _placeBlockType = 3;
    
    private Vector3Int _lastMinePosition;
    private bool _validMiningPosition;
    private bool _isMining;
    private float _miningStartedTime;
    private VoxelDef _minedBlockType;

    public void Init()
    {
        _camera = Camera.main.transform;
        _transform = transform;
        
        _placeBlockMarker = new EditCube();
        _placeBlockMarker.Init(World.PlaceBlockMaterial);
        _placeBlockMarker.SetVoxelType(_placeBlockType);
        _placeBlockMarkerTransform = _placeBlockMarker.GameObject.transform;
        
        _mineBlockMarker = new EditCube();
        _mineBlockMarker.Init(World.MineMaterial, 1.01f);
        _mineBlockMarker.SetMiningProgress(_placeBlockType);
        _mineBlockMarkerTransform = _mineBlockMarker.GameObject.transform;

        _inited = true;
    }

    private void Update()
    {
        if(!_inited)
            return;
        
        GetPLayerInputs();
        UpdateMining();
        PlaceCursorBlocks();
    }

    private void FixedUpdate()
    {
        if(!_inited)
            return;
        
        CalculateVelocity();
        if (jumpRequest)
            Jump();

        _transform.Rotate(_mouseHorizontal * LookSpeed * Vector3.up);
        _camera.transform.Rotate(-_mouseVertical * LookSpeed * Vector3.right);

        _transform.Translate(_velocity, Space.World);
    }

    private void Jump()
    {
        verticalMomentum = JumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void PlaceCursorBlocks()
    {
        float checkIncrement = 0.01f;
        float reach = 10;

        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 position = _camera.position + _camera.forward * step;

            if (WorldModel.CheckVoxelOnGlobalXyz(position.x, position.y, position.z))
            {
                _placeBlockMarkerTransform.position = lastPos;
                _placeBlockMarkerTransform.gameObject.SetActive(true);

                _validMiningPosition = true;
                SetMiningPosition(new Vector3Int(Mathf.FloorToInt(position.x),
                                                 Mathf.FloorToInt(position.y),
                                                 Mathf.FloorToInt(position.z)));

                
                

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(position.x),
                                  Mathf.FloorToInt(position.y),
                                  Mathf.FloorToInt(position.z));

            step += checkIncrement;
        }

        _placeBlockMarkerTransform.gameObject.SetActive(false);
        _mineBlockMarkerTransform.gameObject.SetActive(false);
        _validMiningPosition = false;
    }

    private void UpdateMining()
    {
        if (!_isMining)
        {
            //check if we could start mining
            if (_validMiningPosition && Input.GetMouseButton(0))
            {
                //START MINING
                _isMining = true;
                _miningStartedTime = Time.time;
            }   
        }
        else
        {
            if (!_validMiningPosition || !Input.GetMouseButton(0))
            {
                //STOP MINING
                _isMining = false;
                _mineBlockMarker.SetMiningProgress(0);
                Pick.transform.localRotation = Quaternion.identity;
                return;
            }
            
            //block can't be mined (bedrock)
            if(_minedBlockType.Hardness == 0)
                return;
            
            var miningLength = Time.time - _miningStartedTime;
            var hits = Mathf.FloorToInt(miningLength / MiningInterval);
            
            var animProgress = PickMovementCurve.Evaluate((miningLength % MiningInterval) / MiningInterval);

            Pick.transform.localRotation = Quaternion.Euler(new Vector3(-45 * animProgress, 0 ,0));
            
            _mineBlockMarker.SetMiningProgress(hits);
            
            //finish mining (remove block)
            if (hits >= _minedBlockType.Hardness)
                WorldModel.EditVoxel(_lastMinePosition, VoxelTypeByte.AIR);
        }
    }

    private void SetMiningPosition(Vector3Int position)
    {
        if(position == _lastMinePosition)
            return;
        
        _mineBlockMarkerTransform.position = position;
        _mineBlockMarkerTransform.gameObject.SetActive(true);
        _validMiningPosition = true;
        
        //position changed =>> reset mining timer
        _miningStartedTime = Time.time;
        
        _minedBlockType = World.VoxelDefs[WorldModel.GetVoxel(position.x, position.y, position.z)];

        Debug.LogWarning($"<color=\"aqua\">Player.SetMiningPosition() : WE ARE GOING TO MINE {_minedBlockType.Name}</color>");
        
        _lastMinePosition = position;
    }

    private void GetPLayerInputs()
    {
        _horizontal = Input.GetAxis("Horizontal");
        _vertical = Input.GetAxis("Vertical");
        _mouseHorizontal = Input.GetAxis("Mouse X");
        _mouseVertical = Input.GetAxis("Mouse Y");

        _moveSpeed = Input.GetButton("Sprint") ? RunSpeed : WalkSpeed;

        if (isGrounded && Input.GetButtonDown("Jump"))
            jumpRequest = true;

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

    }

    #region Physics

    private void CalculateVelocity()
    {
        //apply gravity
        if (verticalMomentum > Gravity)
            verticalMomentum += Time.fixedDeltaTime * Gravity;

        //walk / sprint
        _velocity = ((_transform.forward * _vertical) + (_transform.right * _horizontal)) * Time.deltaTime * _moveSpeed;

        //apply vertical momentum
        _velocity += verticalMomentum * Time.fixedDeltaTime * Vector3.up;

        if (_velocity.z > 0 && CheckFront() || (_velocity.z < 0 && CheckBack()))
            _velocity.z = 0;
        if (_velocity.x > 0 && CheckRight() || (_velocity.x < 0 && CheckLeft()))
            _velocity.x = 0;

        if (_velocity.y < 0)
            _velocity.y = CheckDownSpeed(_velocity.y);
        if (_velocity.y > 0)
            _velocity.y = CheckUpSpeed(_velocity.y);
    }

    private float CheckDownSpeed(float speed)
    {
        if (WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x - playerSize, transform.position.y + speed, transform.position.z - playerSize) ||
            WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x - playerSize, transform.position.y + speed, transform.position.z + playerSize) ||
            WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x + playerSize, transform.position.y + speed, transform.position.z - playerSize) ||
            WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x + playerSize, transform.position.y + speed, transform.position.z + playerSize))
        {
            isGrounded = true;

            return 0;
        }

        isGrounded = false;

        return speed;
    }

    private float CheckUpSpeed(float speed)
    {
        if (
            WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x - playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z - playerSize) ||
            WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x - playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z + playerSize) ||
            WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x + playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z - playerSize) ||
            WorldModel.CheckVoxelOnGlobalXyz(_transform.position.x + playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z + playerSize)
        )
            return 0;

        return speed;
    }

    private bool CheckFront()
    {
        return WorldModel.CheckVoxelOnGlobalXyz(transform.position.x, transform.position.y, transform.position.z + playerSize) ||
               WorldModel.CheckVoxelOnGlobalXyz(transform.position.x, transform.position.y, transform.position.z + playerSize);
    }

    private bool CheckBack()
    {
        return WorldModel.CheckVoxelOnGlobalXyz(transform.position.x, transform.position.y, transform.position.z - playerSize) ||
               WorldModel.CheckVoxelOnGlobalXyz(transform.position.x, transform.position.y, transform.position.z - playerSize);
    }

    private bool CheckLeft()
    {
        return WorldModel.CheckVoxelOnGlobalXyz(transform.position.x - playerSize, transform.position.y, transform.position.z) ||
               WorldModel.CheckVoxelOnGlobalXyz(transform.position.x - playerSize, transform.position.y, transform.position.z);
    }

    private bool CheckRight()
    {
        return WorldModel.CheckVoxelOnGlobalXyz(transform.position.x + playerSize, transform.position.y, transform.position.z) ||
               WorldModel.CheckVoxelOnGlobalXyz(transform.position.x + playerSize, transform.position.y, transform.position.z);
    }

    #endregion
}