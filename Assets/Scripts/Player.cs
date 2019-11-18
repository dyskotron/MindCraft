using MapGeneration;
using MapGeneration.Defs;
using Model;
using UnityEngine;

public class Player : MonoBehaviour
{
    public const float PLAYER_HEIGHT = 1.8f;
    
    //We'll get this from inventory later on
    public const float NUM_USABLE_BLOCK_TYPES = 4;

    public World World => Locator.World;
    public WorldModel WorldModel => Locator.WorldModel;

    public GameObject AddHighlightBlock;
    public GameObject RemoveHighlightBlock;

    public float WalkSpeed = 3f;
    public float RunSpeed = 10f;
    public float LookSpeed = 3f;

    public float Gravity = -9.8f;
    public float JumpForce = 5f;

    public float playerSize = 0.3f;

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
    private EditCube _editCube;
    private bool _inited;

    private byte _placeBlockType = 0;

    public void Init()
    {
        _camera = Camera.main.transform;
        _transform = transform;
        
        _editCube = new EditCube();
        _editCube.SetVoxelType(_placeBlockType);
        AddHighlightBlock = _editCube.GameObject;

        _inited = true;
    }

    private void Update()
    {
        if(!_inited)
            return;
        
        GetPLayerInputs();
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
                AddHighlightBlock.transform.position = lastPos;
                RemoveHighlightBlock.transform.position = new Vector3(Mathf.FloorToInt(position.x),
                                                                      Mathf.FloorToInt(position.y),
                                                                      Mathf.FloorToInt(position.z));

                AddHighlightBlock.SetActive(true);
                RemoveHighlightBlock.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(position.x),
                                  Mathf.FloorToInt(position.y),
                                  Mathf.FloorToInt(position.z));

            step += checkIncrement;
        }

        AddHighlightBlock.SetActive(false);
        RemoveHighlightBlock.SetActive(false);
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

        if (RemoveHighlightBlock.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
                WorldModel.EditVoxel(RemoveHighlightBlock.transform.position, VoxelTypeByte.AIR);
            else if (Input.GetMouseButtonDown(1))
                WorldModel.EditVoxel(AddHighlightBlock.transform.position, _placeBlockType);
            
        }

        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0)
        {
            //temp hack to avoid empty + air blocks (1, 2)
            _placeBlockType -= 2;
            
            var dir = Mathf.Sign(Input.mouseScrollDelta.y);
            _placeBlockType = (byte)((_placeBlockType + NUM_USABLE_BLOCK_TYPES + dir) % NUM_USABLE_BLOCK_TYPES);
            
            _placeBlockType += 2;
            _editCube.SetVoxelType(_placeBlockType);
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