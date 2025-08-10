using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Transform _transform;
    private Rigidbody rb;

    //--�ړ��֌W--
    [SerializeField] private PlayerInput input;
    [SerializeField] private float speed;
    private Vector2 direction;

    //--�X�^�~�i�֘A--
    [SerializeField] private float minusRate;
    [SerializeField] private float plusRate;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float maxStamina;
    private float stamina;
    private bool isDashing;

    //--�W�����v�֌W--
    [SerializeField] private float jumpForce;
    [SerializeField] private LayerMask groundLayer;
    private bool isGround;

    //--���_�֌W--
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
        //�n�ʔ�����s��
        isGround = Physics.CheckSphere(_transform.position, 0.1f, groundLayer);

        LookInput();
        DashFunction();
        Debug.Log(stamina);
        Debug.Log(isDashing);
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
    }

    private void MoveInput()
    {
        //���͕��������[�J�����W�ɕϊ�
        Vector3 movement = new Vector3(direction.x, 0, direction.y);

        //�L�����N�^�[�̌������l�����Čv�Z
        Vector3 moveDirection = (_transform.forward * movement.z + _transform.right * movement.x).normalized;

        //�_�b�V�����ł�������dashSpeed���g��
        float getSpeed = isDashing ? dashSpeed : speed;

        moveDirection.x = moveDirection.x * getSpeed * Time.fixedDeltaTime;
        moveDirection.z = moveDirection.z * getSpeed * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
    }

    private void ChangeDirection(InputAction.CallbackContext context)
    {
        direction = context.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGround)
        {
            rb.AddForce(Vector3.up * jumpForce,ForceMode.Impulse);
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

        //���E�̉�] (Yaw)
        _transform.Rotate(Vector3.up * xDirection);

        //�㉺�̉�] (Pitch)
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
}
