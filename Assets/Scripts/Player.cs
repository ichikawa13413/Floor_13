using System.Linq;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class Player : MonoBehaviour
{
    private Transform _transform;
    private Rigidbody rb;

    //--�ړ��֌W--
    [SerializeField] private PlayerInput input;
    public PlayerInput MyInput { get => input; private set => input = value; }
    [SerializeField] private float speed;
    private Vector2 direction;

    //--�X�^�~�i�A�_�b�V���֘A--
    [SerializeField] private float minusRate;
    [SerializeField] private float plusRate;
    [SerializeField] private float dashSpeed;
    [SerializeField] private int maxDashAngle;
    [SerializeField] private float maxStamina;
    public float MaxStamina { get => maxStamina; }
    public float stamina {  get; private set; }
    public bool isDashing { get; private set; }
    private Subject<Unit> maxStaminaSubject;//�X�^�~�i�����^���ɂȂ�����ʒm
    public Observable<Unit> maxStaminaObservable => maxStaminaSubject;
    private Subject<Unit> consumeSubject;//�X�^�~�i������ꂽ��ʒm
    public Observable<Unit> consumeObservable => consumeSubject;

    //--�W�����v�֌W--
    [SerializeField] private float jumpForce;
    [SerializeField] private LayerMask groundLayer;
    private bool isGround;

    //--���_�֌W--
    [SerializeField] private Camera currentCamera;
    [SerializeField] private float lookSpeed;
    [SerializeField] private float lookLimit;
    [SerializeField] private Vector3 fpsViwe;
    [SerializeField] private Vector3 tpsViwe;
    private Vector2 cameraDirection;
    private float xRotation;
    private const int XROTATION_CENTER = 0;
    private bool OnChange;//true�̎���TPS���_�ɂȂ��Ă��鎞�Afalse�̎���FPS���_�ɂȂ��Ă��鎞

    //--�A�j���[�V�����֌W--
    private Animator _animator;
    private static readonly int XHash = Animator.StringToHash("X");
    private static readonly int YHash = Animator.StringToHash("Y");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int isRunHash = Animator.StringToHash("isRun");
    private static readonly int jumpHash = Animator.StringToHash("Jump");
    private static readonly int isGroundHash = Animator.StringToHash("isGround");
    private const int STOP_ANIMARTION = 0;
    private const int START_ANIMARTION = 1;

    //--�C���x���g���֘A--
    private SlotGrid _slotGrid;
    private bool isOpenInventory;

    //�C���x���g�����N���[�Y������ʒm
    private Subject<Unit> closeSubject;
    public Observable<Unit> closeObservable => closeSubject;

    //�L�[�{�[�h�ŃC���x���g�����J������ʒm
    private Subject<Unit> keyboardSubject;
    public Observable<Unit> keyboardObservable => keyboardSubject;

    //--�A�C�e�����E���n--
    [SerializeField] private LayerMask itemLayer;
    [SerializeField] private float maxDistance;
    private static readonly Vector2 CAMERA_CENTER = new Vector2(0.5f, 0.5f);

    //--���C�t�n--
    [SerializeField] private float life;
    [SerializeField] private float reduceRate;
    [SerializeField] private float recoverRate;

    [Inject]
    public void Construct(SlotGrid slotGrid)
    {
        _slotGrid = slotGrid;
    }

    private void Awake()
    {
        _transform = transform;
        rb = GetComponent<Rigidbody>();

        _animator = GetComponent<Animator>();

        xRotation = XROTATION_CENTER;

        OnChange = false;
        stamina = MaxStamina;

        isOpenInventory = false;

        closeSubject = new Subject<Unit>();
        keyboardSubject = new Subject<Unit>();
        maxStaminaSubject = new Subject<Unit>();
        consumeSubject = new Subject<Unit>();
    }

    private void Start()
    {
        //�}�E�X�J�[�\���𒆉��ɌŒ肵�i1�s�ځj�A�����i2�s�ځj
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        //inventory�g�p���͑���s��
        if (!isOpenInventory)
        {
            MoveInput();
        }
    }

    private void Update()
    {
        //�n�ʔ�����s��
        isGround = Physics.CheckSphere(_transform.position, 0.1f, groundLayer);

        //inventory�g�p���͑���s��
        if (!isOpenInventory)
        {
            LookInput();
            DashFunction();
            _animator.SetBool(isRunHash, isDashing);
            _animator.SetBool(isGroundHash, isGround);
        }

        //�X�^�~�i�̏�Ԃ�StaminaSlider�֒ʒm
        if (stamina == MaxStamina)
        {
            maxStaminaSubject.OnNext(Unit.Default);
        }
        else if(stamina != MaxStamina)
        {
            consumeSubject.OnNext(Unit.Default);
        }

        Debug.Log(life);
    }

    private void OnEnable()
    {
        if(input == null) return;
        //�ړ��n
        input.actions["Move"].performed += ChangeDirection;
        input.actions["Move"].canceled += ChangeDirection;
        input.actions["Jump"].performed += OnJump;
        input.actions["Dash"].performed += OnDashStart;
        input.actions["Dash"].canceled += OnDashEnd;

        //���_�n
        input.actions["Look"].performed += OnLook;
        input.actions["Look"].canceled += OnLook;
        input.actions["ChangeViwe"].performed += OnChangeViwe;

        //�C���x���g���n
        input.actions["Inventory"].started += OnInventory;
        
        //--�R���g���[���[�ŃC���x���g���𑀍�n--
        input.actions["ChoiceUp"].started += _slotGrid.OnChoiceUp;
        input.actions["ChoiceDown"].started += _slotGrid.OnChoiceDown;
        input.actions["ChoiceLeft"].started += _slotGrid.OnChoiceLeft;
        input.actions["ChoiceRight"].started += _slotGrid.OnChoiceRight;
        input.actions["DecisionButton"].started += _slotGrid.OnDecisionButton;
        input.actions["QuitButton"].started += _slotGrid.OnQuitChoice;

        //--�A�C�e�����E���n--
        input.actions["Get"].started += OnGetItem;
    }

    private void OnDisable()
    {
        if (input == null) return;
        //�ړ��n
        input.actions["Move"].performed -= ChangeDirection;
        input.actions["Move"].canceled -= ChangeDirection;
        input.actions["Jump"].performed -= OnJump;
        input.actions["Dash"].performed -= OnDashStart;
        input.actions["Dash"].canceled -= OnDashEnd;

        //���_�n
        input.actions["Look"].performed -= OnLook;
        input.actions["Look"].canceled -= OnLook;
        input.actions["ChangeViwe"].performed -= OnChangeViwe;

        //�C���x���g���n
        input.actions["Inventory"].started -= OnInventory;

        //--�R���g���[���[�ŃC���x���g���𑀍�n--
        input.actions["ChoiceUp"].started -= _slotGrid.OnChoiceUp;
        input.actions["ChoiceDown"].started -= _slotGrid.OnChoiceDown;
        input.actions["ChoiceLeft"].started -= _slotGrid.OnChoiceLeft;
        input.actions["ChoiceRight"].started -= _slotGrid.OnChoiceRight;
        input.actions["DecisionButton"].started -= _slotGrid.OnDecisionButton;
        input.actions["QuitButton"].started -= _slotGrid.OnQuitChoice;

        //--�A�C�e�����E���n--
        input.actions["Get"].started -= OnGetItem;
    }
    
    //�v���C���[�̈ړ�����
    private void MoveInput()
    {
        Vector3 movement = new Vector3(direction.x, 0, direction.y);
        Vector3 moveDirection = (_transform.forward * movement.z + _transform.right * movement.x).normalized;

        float getSpeed = isDashing ? dashSpeed : speed;

        moveDirection.x = moveDirection.x * getSpeed * Time.fixedDeltaTime;
        moveDirection.z = moveDirection.z * getSpeed * Time.fixedDeltaTime;
        rb.linearVelocity = new Vector3(moveDirection.x, rb.linearVelocity.y, moveDirection.z);
    }

    private void ChangeDirection(InputAction.CallbackContext context)
    {
        //�C���x���g����\�����͓��͂��󂯕t���Ȃ�
        if(isOpenInventory) return;

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
        if (isGround && !isOpenInventory)
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
            stamina = Mathf.Min(stamina,MaxStamina);
        }
    }

    private void OnDashStart(InputAction.CallbackContext context)
    {
        if (IsLimitDash(direction))
        {
            isDashing = true;
        }
        else
        {
            isDashing = false;
        }
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

    private void OnGetItem(InputAction.CallbackContext context)
    {
        if (currentCamera == null) return;

        if (!_slotGrid.CanGetItem())
        {
            Debug.Log("����ȏ�A�C�e�������܂���");
            return;
        }

        RaycastHit hit;
        //��ʂ̒������w�肵��������Item�^�O�������Ă���I�u�W�F�N�g����������PickUpItem���\�b�h���Ăяo��
        if (Physics.Raycast(currentCamera.ViewportPointToRay(CAMERA_CENTER), out hit, maxDistance))
        {
            if (hit.collider.CompareTag("Item"))
            {
                PickUpItem(hit);
            }
        }
    }

    /// <summary>
    /// �_�b�V���ł�����͕����𐧌�
    /// true��������_�b�V�������𖞂����Ă���
    /// </summary>
    /// <param name="direction">�v���C���[���͂�������</param>
    /// <returns></returns>
    private bool IsLimitDash(Vector2 direction)
    {
        float angle = Vector2.Angle(Vector2.up, direction);

        if (angle <= maxDashAngle)
        {
            return true;
        }

        return false;
    }

    private void OnInventory(InputAction.CallbackContext context)
    {
        InputDevice device = context.control.device;

        //�L�[�{�[�h�ŊJ������ʒm
        if (device is Keyboard) 
        {
            keyboardSubject.OnNext(Unit.Default);
        }

        if (_slotGrid.gameObject.activeSelf)
        {
            CloseSlotGrid();
        }
        else
        {
            OpenSlotGrid();
        }
    }

    //�C���x���g�� �I�[�v�����̏���
    public void OpenSlotGrid()
    {
        _slotGrid.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isOpenInventory = true;

        _animator.speed = STOP_ANIMARTION;
        _animator.SetFloat(SpeedHash, STOP_ANIMARTION);

        FreezePlayer();
        direction = Vector2.zero;
    }

    //�C���x���g�� �N���[�Y���̏���
    public void CloseSlotGrid()
    {
        _slotGrid.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isOpenInventory = false;

        _animator.speed = START_ANIMARTION;

        UnFreezePlayer();

        //�C���x���g�����������ʒm
        closeSubject.OnNext(Unit.Default);
    }

    //�v���C���[�̕������Z�����ׂĐ���
    private void FreezePlayer()
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    //�v���C���[�̕������Z���I�t
    private void UnFreezePlayer()
    {
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    /// <summary>
    /// �A�C�e�����E�������Bray�����������A�C�e�������擾���A�����slotGrid�֑���B
    /// ���̌�A�A�C�e���I�u�W�F�N�g��j�󂷂�B
    /// </summary>
    /// <param name="hit">OnGetItem���瑗���Ă���RaycastHit</param>
    private void PickUpItem(RaycastHit hit)
    {
        ItemHolder holder = hit.collider.GetComponent<ItemHolder>();
        Item getItem = holder.itemData;

        _slotGrid.SetItem(getItem);
        Destroy(hit.collider.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Enemy enemy = collision.collider.GetComponent<Enemy>();

        if (enemy != null)
        {
            ReduceLife();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        RecoverLife();
    }

    //���C�t�̌����@�\
    private void ReduceLife()
    {
        life -= reduceRate * Time.deltaTime;
    }

    //���C�t�̉񕜋@�\
    private void RecoverLife()
    {
        //���C�t������܂ŉ񕜂���B
    }
}
