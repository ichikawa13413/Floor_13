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

    //--移動関係--
    [SerializeField] private PlayerInput input;
    public PlayerInput MyInput { get => input; private set => input = value; }
    [SerializeField] private float speed;
    private Vector2 direction;

    //--スタミナ、ダッシュ関連--
    [SerializeField] private float minusRate;
    [SerializeField] private float plusRate;
    [SerializeField] private float dashSpeed;
    [SerializeField] private int maxDashAngle;
    [SerializeField] private float maxStamina;
    public float MaxStamina { get => maxStamina; }
    public float stamina {  get; private set; }
    public bool isDashing { get; private set; }
    private Subject<Unit> maxStaminaSubject;//スタミナが満タンになったら通知
    public Observable<Unit> maxStaminaObservable => maxStaminaSubject;
    private Subject<Unit> consumeSubject;//スタミナが消費されたら通知
    public Observable<Unit> consumeObservable => consumeSubject;

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
    private float xRotation;
    private const int XROTATION_CENTER = 0;
    private bool OnChange;//trueの時はTPS視点になっている時、falseの時はFPS視点になっている時

    //--アニメーション関係--
    private Animator _animator;
    private static readonly int XHash = Animator.StringToHash("X");
    private static readonly int YHash = Animator.StringToHash("Y");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int isRunHash = Animator.StringToHash("isRun");
    private static readonly int jumpHash = Animator.StringToHash("Jump");
    private static readonly int isGroundHash = Animator.StringToHash("isGround");
    private const int STOP_ANIMARTION = 0;
    private const int START_ANIMARTION = 1;

    //--インベントリ関連--
    private SlotGrid _slotGrid;
    private bool isOpenInventory;

    //インベントリをクローズしたら通知
    private Subject<Unit> closeSubject;
    public Observable<Unit> closeObservable => closeSubject;

    //キーボードでインベントリを開いたら通知
    private Subject<Unit> keyboardSubject;
    public Observable<Unit> keyboardObservable => keyboardSubject;

    //--アイテムを拾う系--
    [SerializeField] private LayerMask itemLayer;
    [SerializeField] private float maxDistance;
    private static readonly Vector2 CAMERA_CENTER = new Vector2(0.5f, 0.5f);

    //--ライフ系--
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
        //マウスカーソルを中央に固定し（1行目）、消す（2行目）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        //inventory使用中は操作不可
        if (!isOpenInventory)
        {
            MoveInput();
        }
    }

    private void Update()
    {
        //地面判定を行う
        isGround = Physics.CheckSphere(_transform.position, 0.1f, groundLayer);

        //inventory使用中は操作不可
        if (!isOpenInventory)
        {
            LookInput();
            DashFunction();
            _animator.SetBool(isRunHash, isDashing);
            _animator.SetBool(isGroundHash, isGround);
        }

        //スタミナの状態をStaminaSliderへ通知
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
        //移動系
        input.actions["Move"].performed += ChangeDirection;
        input.actions["Move"].canceled += ChangeDirection;
        input.actions["Jump"].performed += OnJump;
        input.actions["Dash"].performed += OnDashStart;
        input.actions["Dash"].canceled += OnDashEnd;

        //視点系
        input.actions["Look"].performed += OnLook;
        input.actions["Look"].canceled += OnLook;
        input.actions["ChangeViwe"].performed += OnChangeViwe;

        //インベントリ系
        input.actions["Inventory"].started += OnInventory;
        
        //--コントローラーでインベントリを操作系--
        input.actions["ChoiceUp"].started += _slotGrid.OnChoiceUp;
        input.actions["ChoiceDown"].started += _slotGrid.OnChoiceDown;
        input.actions["ChoiceLeft"].started += _slotGrid.OnChoiceLeft;
        input.actions["ChoiceRight"].started += _slotGrid.OnChoiceRight;
        input.actions["DecisionButton"].started += _slotGrid.OnDecisionButton;
        input.actions["QuitButton"].started += _slotGrid.OnQuitChoice;

        //--アイテムを拾う系--
        input.actions["Get"].started += OnGetItem;
    }

    private void OnDisable()
    {
        if (input == null) return;
        //移動系
        input.actions["Move"].performed -= ChangeDirection;
        input.actions["Move"].canceled -= ChangeDirection;
        input.actions["Jump"].performed -= OnJump;
        input.actions["Dash"].performed -= OnDashStart;
        input.actions["Dash"].canceled -= OnDashEnd;

        //視点系
        input.actions["Look"].performed -= OnLook;
        input.actions["Look"].canceled -= OnLook;
        input.actions["ChangeViwe"].performed -= OnChangeViwe;

        //インベントリ系
        input.actions["Inventory"].started -= OnInventory;

        //--コントローラーでインベントリを操作系--
        input.actions["ChoiceUp"].started -= _slotGrid.OnChoiceUp;
        input.actions["ChoiceDown"].started -= _slotGrid.OnChoiceDown;
        input.actions["ChoiceLeft"].started -= _slotGrid.OnChoiceLeft;
        input.actions["ChoiceRight"].started -= _slotGrid.OnChoiceRight;
        input.actions["DecisionButton"].started -= _slotGrid.OnDecisionButton;
        input.actions["QuitButton"].started -= _slotGrid.OnQuitChoice;

        //--アイテムを拾う系--
        input.actions["Get"].started -= OnGetItem;
    }
    
    //プレイヤーの移動処理
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
        //インベントリを表示中は入力を受け付けない
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
            Debug.Log("これ以上アイテムを取れません");
            return;
        }

        RaycastHit hit;
        //画面の中央かつ指定した距離にItemタグを持っているオブジェクトがあったらPickUpItemメソッドを呼び出す
        if (Physics.Raycast(currentCamera.ViewportPointToRay(CAMERA_CENTER), out hit, maxDistance))
        {
            if (hit.collider.CompareTag("Item"))
            {
                PickUpItem(hit);
            }
        }
    }

    /// <summary>
    /// ダッシュできる入力方向を制限
    /// trueだったらダッシュ条件を満たしている
    /// </summary>
    /// <param name="direction">プレイヤー入力した方向</param>
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

        //キーボードで開いたら通知
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

    //インベントリ オープン時の処理
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

    //インベントリ クローズ時の処理
    public void CloseSlotGrid()
    {
        _slotGrid.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isOpenInventory = false;

        _animator.speed = START_ANIMARTION;

        UnFreezePlayer();

        //インベントリを閉じた事を通知
        closeSubject.OnNext(Unit.Default);
    }

    //プレイヤーの物理演算をすべて制限
    private void FreezePlayer()
    {
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    //プレイヤーの物理演算をオフ
    private void UnFreezePlayer()
    {
        rb.constraints = RigidbodyConstraints.None;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    /// <summary>
    /// アイテムを拾う処理。rayが当たったアイテムを情報取得し、それをslotGridへ送る。
    /// その後、アイテムオブジェクトを破壊する。
    /// </summary>
    /// <param name="hit">OnGetItemから送られてきたRaycastHit</param>
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

    //ライフの減少機能
    private void ReduceLife()
    {
        life -= reduceRate * Time.deltaTime;
    }

    //ライフの回復機能
    private void RecoverLife()
    {
        //ライフが上限まで回復する。
    }
}
