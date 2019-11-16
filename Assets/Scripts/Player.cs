using MapGeneration;
using UnityEngine;

public class Player : MonoBehaviour
{
    public const float PLAYER_HEIGHT = 1.8f;

    public World World => Locator.World;

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
    private Camera _camera;

    private float _moveSpeed = 0;

    private float verticalMomentum; //?? 
    private bool jumpRequest; //?? 
    private bool isGrounded; //?? 
    private Transform _transform;

    private void Start()
    {
        _camera = Camera.main;
        _transform = transform;
    }

    private void Update()
    {
        GetPLayerInputs();
    }

    private void FixedUpdate()
    {
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

    private void GetPLayerInputs()
    {
        _horizontal = Input.GetAxis("Horizontal");
        _vertical = Input.GetAxis("Vertical");
        _mouseHorizontal = Input.GetAxis("Mouse X");
        _mouseVertical = Input.GetAxis("Mouse Y");

        _moveSpeed = Input.GetButton("Sprint") ? RunSpeed : WalkSpeed;

        if (isGrounded && Input.GetButtonDown("Jump"))
            jumpRequest = true;
    }

    private float CheckDownSpeed(float speed)
    {
        if (World.CheckVoxel(_transform.position.x - playerSize, transform.position.y + speed, transform.position.z - playerSize) ||
            World.CheckVoxel(_transform.position.x - playerSize, transform.position.y + speed, transform.position.z + playerSize) ||
            World.CheckVoxel(_transform.position.x + playerSize, transform.position.y + speed, transform.position.z - playerSize) ||
            World.CheckVoxel(_transform.position.x + playerSize, transform.position.y + speed, transform.position.z + playerSize))
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
            World.CheckVoxel(_transform.position.x - playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z - playerSize) ||
            World.CheckVoxel(_transform.position.x - playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z + playerSize) ||
            World.CheckVoxel(_transform.position.x + playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z - playerSize) ||
            World.CheckVoxel(_transform.position.x + playerSize, transform.position.y + PLAYER_HEIGHT + speed, transform.position.z + playerSize)
        )
            return 0;

        return speed;
    }

    private bool CheckFront()
    {
        return World.CheckVoxel(transform.position.x, transform.position.y, transform.position.z + playerSize) ||
               World.CheckVoxel(transform.position.x, transform.position.y, transform.position.z + playerSize);
    }

    private bool CheckBack()
    {
        return World.CheckVoxel(transform.position.x, transform.position.y, transform.position.z - playerSize) ||
               World.CheckVoxel(transform.position.x, transform.position.y, transform.position.z - playerSize);
    }

    private bool CheckLeft()
    {
        return World.CheckVoxel(transform.position.x - playerSize, transform.position.y, transform.position.z) ||
               World.CheckVoxel(transform.position.x - playerSize, transform.position.y, transform.position.z);
    }

    private bool CheckRight()
    {
        return World.CheckVoxel(transform.position.x + playerSize, transform.position.y, transform.position.z) ||
               World.CheckVoxel(transform.position.x + playerSize, transform.position.y, transform.position.z);
    }
}