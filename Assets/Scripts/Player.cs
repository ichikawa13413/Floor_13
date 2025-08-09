using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Transform _transform;
    private Rigidbody rb;

    //--移動関係--
    [SerializeField] private PlayerInput input;
    [SerializeField] private float speed;
    private Vector2 direction;

    //--ジャンプ関係--
    [SerializeField] private float jumpForce;
    [SerializeField] private LayerMask groundLayer;
    private bool isGround;

    //--視点関係--
    [SerializeField] private Camera currentCamera;
    [SerializeField] private float lookSpeed;
    [SerializeField] private float lookLimit;
    private Vector2 cameraDirection;
    private float xRotation = 0;

    private void Awake()
    {
        _transform = transform;
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        MoveInput();
    }

    private void Update()
    {
        //地面判定を行う
        isGround = Physics.CheckSphere(_transform.position, 0.1f, groundLayer);

        LookInput();
    }

    private void OnEnable()
    {
        if(input == null) return;
        input.actions["Move"].performed += ChangeDirection;
        input.actions["Move"].canceled += ChangeDirection;
        input.actions["Jump"].performed += OnJump;
        input.actions["Look"].performed += OnLook;
        input.actions["Look"].canceled += OnLook;
    }

    private void OnDisable()
    {
        if (input == null) return;
        input.actions["Move"].performed -= ChangeDirection;
        input.actions["Move"].canceled -= ChangeDirection;
        input.actions["Jump"].performed -= OnJump;
        input.actions["Look"].performed -= OnLook;
        input.actions["Look"].canceled -= OnLook;
    }

    private void MoveInput()
    {
        //入力方向をローカル座標に変換
        Vector3 movement = new Vector3(direction.x, 0, direction.y);

        //キャラクターの向きを考慮して計算
        Vector3 moveDirection = (_transform.forward * movement.z + _transform.right * movement.x).normalized;
        
        rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z) * speed * Time.fixedDeltaTime;
    }

    private void ChangeDirection(InputAction.CallbackContext context)
    {
        direction = context.ReadValue<Vector2>().normalized;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGround)
        {
            rb.linearVelocity = new Vector3(0, jumpForce, 0);
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if(input == null) return;
        cameraDirection = context.ReadValue<Vector2>().normalized;
    }

    private void LookInput()
    {
        float xDirection = cameraDirection.x * lookSpeed * Time.deltaTime;
        float yDirection = cameraDirection.y * lookSpeed * Time.deltaTime;

        //左右の回転 (Yaw)
        _transform.Rotate(Vector3.up * xDirection);

        //上下の回転 (Pitch)
        xRotation -= yDirection;
        xRotation = Mathf.Clamp(xRotation, -lookLimit, lookLimit);
        currentCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}
