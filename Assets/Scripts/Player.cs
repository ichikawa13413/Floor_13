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

    //--スタミナ関連--
    [SerializeField] private float minusRate;
    [SerializeField] private float plusRate;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float maxStamina;
    private float stamina;
    private bool isDashing;

    //--ジャンプ関係--
    [SerializeField] private float jumpForce;
    [SerializeField] private LayerMask groundLayer;
    private bool isGround;

    //--視点関係--
    [SerializeField] private Camera currentCamera;
    [SerializeField] private float lookSpeed;
    [SerializeField] private float lookLimit;
    [SerializeField] private Vector3 fpsViwe;
    [SerializeField] private Vector3 tpsViwe;
    private Vector2 cameraDirection;
    private float xRotation = 0;
    private bool OnChange;//trueの時はTPS視点になっている時、falseの時はFPS視点になっている時

    //--アニメーション関係--
    private Animator _animator;
    private static readonly int XHash = Animator.StringToHash("X");
    private static readonly int YHash = Animator.StringToHash("Y");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int isRunHash = Animator.StringToHash("isRun");
    private static readonly int jumpHash = Animator.StringToHash("Jump");
    private static readonly int isGroundHash = Animator.StringToHash("isGround");

    private void Awake()
    {
        _transform = transform;
        rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        OnChange = false;
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
        DashFunction();
        Debug.Log(stamina);
        Debug.Log(isDashing);
        _animator.SetBool(isRunHash, isDashing);
        _animator.SetBool(isGroundHash, isGround);
    }

    private void OnEnable()
    {
        if(input == null) return;
        input.actions["Move"].performed += ChangeDirection;
        input.actions["Move"].canceled += ChangeDirection;
        input.actions["Jump"].performed += OnJump;
        input.actions["Look"].performed += OnLook;
        input.actions["Look"].canceled += OnLook;
        input.actions["Dash"].performed += OnDashStart;
        input.actions["Dash"].canceled += OnDashEnd;
        input.actions["ChangeViwe"].performed += OnChangeViwe;
    }

    private void OnDisable()
    {
        if (input == null) return;
        input.actions["Move"].performed -= ChangeDirection;
        input.actions["Move"].canceled -= ChangeDirection;
        input.actions["Jump"].performed -= OnJump;
        input.actions["Look"].performed -= OnLook;
        input.actions["Look"].canceled -= OnLook;
        input.actions["Dash"].performed -= OnDashStart;
        input.actions["Dash"].canceled -= OnDashEnd;
        input.actions["ChangeViwe"].performed -= OnChangeViwe;
    }

    private void MoveInput()
    {
        //入力方向をローカル座標に変換
        Vector3 movement = new Vector3(direction.x, 0, direction.y);

        //キャラクターの向きを考慮して計算
        Vector3 moveDirection = (_transform.forward * movement.z + _transform.right * movement.x).normalized;

        //ダッシュ中であったらdashSpeedを使う
        float getSpeed = isDashing ? dashSpeed : speed;

        moveDirection.x = moveDirection.x * getSpeed * Time.fixedDeltaTime;
        moveDirection.z = moveDirection.z * getSpeed * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
    }

    private void ChangeDirection(InputAction.CallbackContext context)
    {
        var vector2 = context.ReadValue<Vector2>();
        direction = vector2;

        _animator.SetFloat(SpeedHash, vector2.magnitude);
        if (vector2 != Vector2.zero)
        {
            _animator.SetFloat(XHash, vector2.x);
            _animator.SetFloat(YHash, vector2.y);
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGround)
        {
            rb.AddForce(Vector3.up * jumpForce,ForceMode.Impulse);
            _animator.SetTrigger(jumpHash);
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

    private void DashFunction()
    {
        if (isDashing)
        {
            stamina -= minusRate * Time.deltaTime;
            if (stamina <= 0)
            {
                isDashing = false;
                stamina = 0;
            }
        }
        else if (!isDashing)
        {
            stamina += plusRate * Time.deltaTime;
            stamina = Mathf.Min(stamina,maxStamina);
        }
    }

    private void OnDashStart(InputAction.CallbackContext context)
    {
        isDashing = true;
    }

    private void OnDashEnd(InputAction.CallbackContext context)
    {
        isDashing = false;
    }

    private void OnChangeViwe(InputAction.CallbackContext context)
    {
        if (OnChange)
        {
#if UNITY_EDITOR
            currentCamera.transform.localPosition = fpsViwe;
            OnChange = false;
#endif
        }
        else
        {
#if UNITY_EDITOR
            currentCamera.transform.localPosition = tpsViwe;
            OnChange =true;
#endif
        }
    }
}
