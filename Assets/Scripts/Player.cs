using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using R3;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class Player : MonoBehaviour
{
    private Transform _transform;
    private Rigidbody rb;

    //--�ړ��֌W--
    [SerializeField] private PlayerInput input;
    public PlayerInput MyInput { get => input; }
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
    private const int X_ROTATION_CENTER = 0;
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
    [SerializeField] private int limitItem;//�v���C���[�������ł���item���̏��
    public int playerLimitItem { get => limitItem; }
    private bool isOpenInventory;
    private List<Item> itemList;
    public List<Item> playerItemList { get => itemList; }
    private ItemObject currentLookItem;
    private Subject<Item> getItemSubject;//�v���C���[���A�C�e����get������ʒm
    public Observable<Item> getItemObservable => getItemSubject;
    public event Action OnInventoryOpen;
    public event Action OnInventoryClose;
    private enum InventoryState
    {
        active,     //�C���x���g�����J���Ă�����
        inactive    //�C���x���g������Ă�����
    }
    private InventoryState CurrentInventoryState;

    //--�C���x���g���R���g���[���[�œ��������Ɏg���C�x���g�ꗗ--
    public event Action OnPlayerChoiceUP;
    public event Action OnPlayerChoiceDOWN;
    public event Action OnPlayerChoiceLEFT;
    public event Action OnPlayerChoiceRIGHT;
    public event Action OnPlayerDecisionButton;
    public event Action OnPlayerQuitChoice;

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
    [SerializeField] private float maxLife;
    [SerializeField] private float healingPoint;
    [SerializeField] private float startWait;
    [SerializeField] private float healingRate;
    private CancellationTokenSource healingCTS;
    private const int MINIMUM_LIFE = 0;
    private GameUIManager _gameOverUIManager;
    private enum lifeState
    {
        alive,      //�ʏ���
        healing,    //�ْ����
        death       //���S���
    }
    private lifeState currentLifeState;

    //--�m�b�N�o�b�N�n--
    [SerializeField] private float invincibleTime;
    [SerializeField] private float knockbackForce;
    [SerializeField] private Vector3 knockbackUpDirection;
    [SerializeField] private float knockbackDuration;
    private Vector3 knockbackDirection;
    private const int RESET_Y_DIRECTION = 0;
    public enum invincibleState
    {
        normal,
        invincible
    }
    public invincibleState currentInvincible { get; private set; }
    private enum knockbackState
    {
        knockbackActive,   //�m�b�N�o�b�N��
        knockbackFinish    //�m�b�N�o�b�N�I��
    }
    private knockbackState currentKnockback;

    public event Action OnketteiAction;

    [Inject]
    public void Construct(GameUIManager gameOverUIManager)
    {
        _gameOverUIManager = gameOverUIManager;
    }

    private void Awake()
    {
        _transform = transform;
        rb = GetComponent<Rigidbody>();

        _animator = GetComponent<Animator>();

        xRotation = X_ROTATION_CENTER;

        OnChange = false;
        stamina = MaxStamina;

        isOpenInventory = false;
        itemList = new List<Item>();
        getItemSubject = new Subject<Item>();
        CurrentInventoryState = InventoryState.inactive;

        closeSubject = new Subject<Unit>();
        keyboardSubject = new Subject<Unit>();
        maxStaminaSubject = new Subject<Unit>();
        consumeSubject = new Subject<Unit>();

        currentLifeState = lifeState.alive;
        healingCTS = new CancellationTokenSource();

        currentInvincible = invincibleState.normal;
        currentKnockback = knockbackState.knockbackFinish;
    }

    private void Start()
    {
        //�}�E�X�J�[�\���𒆉��ɌŒ肵�i1�s�ځj�A�����i2�s�ځj
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        //�m�b�N�o�b�N���̏�����FixedUpdate�ł��
        if (currentKnockback == knockbackState.knockbackActive)
        {
            rb.AddForce(knockbackDirection,ForceMode.Impulse);
            input.enabled = false;
        }
        else
        {
            input.enabled = true;//�����̂����Ŏ���ł�������
        }

        //inventory�g�p���͑���s��
        if (!isOpenInventory)
        {
            MoveInput();
        }
    }

    private void Update()
    {
        Debug.Log(currentLifeState);

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
        Debug.Log(currentLifeState);
        Debug.Log(itemList.Count);

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
        input.actions["ChoiceUp"].started += OnChoiceUp;
        input.actions["ChoiceDown"].started += OnChoiceDown;
        input.actions["ChoiceLeft"].started += OnChoiceLeft;
        input.actions["ChoiceRight"].started += OnChoiceRight;
        input.actions["DecisionButton"].started += OnDecision;
        input.actions["QuitButton"].started += OnQuitChoice;

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
        input.actions["ChoiceUp"].started -= OnChoiceUp;
        input.actions["ChoiceDown"].started -= OnChoiceDown;
        input.actions["ChoiceLeft"].started -= OnChoiceLeft;
        input.actions["ChoiceRight"].started -= OnChoiceRight;
        input.actions["DecisionButton"].started -= OnDecision;
        input.actions["QuitButton"].started -= OnQuitChoice;
        
        //--�A�C�e�����E���n--
        input.actions["Get"].started -= OnGetItem;
    }

    private void Onkettei(InputAction.CallbackContext context)
    {
        if(currentLifeState != lifeState.death) return;

        OnketteiAction?.Invoke();
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

    //���_����
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

        if (itemList.Count == limitItem)
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

        if (CurrentInventoryState == InventoryState.active)
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
        OnInventoryOpen?.Invoke();
        CurrentInventoryState = InventoryState.active;

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
        OnInventoryClose?.Invoke();
        CurrentInventoryState = InventoryState.inactive;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isOpenInventory = false;

        _animator.speed = START_ANIMARTION;

        UnFreezePlayer();

        //�C���x���g�����������ʒm
        closeSubject.OnNext(Unit.Default);
    }

    /// <summary>
    /// �X���b�g���R���g���[���[�ő��삵������SlotGrid�N���X�֒ʒm���S���̃{�^���I���ɗ��p
    /// </summary>
    private void OnChoiceUp(InputAction.CallbackContext context)
    {
        OnPlayerChoiceUP?.Invoke();
    }

    private void OnChoiceDown(InputAction.CallbackContext context)
    {
        OnPlayerChoiceDOWN?.Invoke();
    }

    //���S���̃{�^���I���ɗ��p
    private void OnChoiceLeft(InputAction.CallbackContext context)
    {
        if (currentLifeState == lifeState.death)
        {
            OnketteiAction?.Invoke();
        }
        else
        {
            OnPlayerChoiceLEFT?.Invoke();
        }
    }

    //���S���̃{�^���I���ɗ��p
    private void OnChoiceRight(InputAction.CallbackContext context)
    {
        if (currentLifeState == lifeState.death)
        {
            OnketteiAction?.Invoke();
        }
        else
        {
            OnPlayerChoiceRIGHT?.Invoke();
        }
    }

    private void OnDecision(InputAction.CallbackContext context)
    {
        OnPlayerDecisionButton?.Invoke();
    }

    private void OnQuitChoice(InputAction.CallbackContext context)
    {
        OnPlayerQuitChoice?.Invoke();
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
    //todo
    //���n�C���C�g�����������炱�̃��\�b�h�ɒǉ�����
    /// <summary>
    /// ���C���΂�itemobject���`�F�b�N����
    /// </summary>
    /// <returns>�v���C���[�����Ă���̂�item��������true�A����ȊO������false</returns>
    private bool CanGetItem()
    { 
        RaycastHit hit;
        if(Physics.Raycast(currentCamera.ViewportPointToRay(CAMERA_CENTER), out hit, maxDistance))
        {
            if (hit.collider.CompareTag("Item"))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// �A�C�e�����E�������Bray�����������A�C�e�������擾���A�����slotGrid�֑���B
    /// ���̌�A�A�C�e���I�u�W�F�N�g��j�󂷂�B
    /// </summary>
    /// <param name="hit">OnGetItem���瑗���Ă���RaycastHit</param>
    private void PickUpItem(RaycastHit hit)
    {
        ItemObject holder = hit.collider.GetComponent<ItemObject>();
        Item getItem = holder.itemData;

        itemList.Add(getItem);
        getItemSubject.OnNext(getItem);
        Destroy(hit.collider.gameObject);
    }

    public void DropItem(Item item)
    {
        if (item != null)
        {
            itemList.Remove(item);
        }
    }

    public void UseItem(Item item)
    {
        if (item != null)
        {
            itemList.Remove(item);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            TakeDamage(enemy);
        }
    }

    /// <summary>
    /// �G�l�~�[�ɐڐG������_���[�W����������B���̌�A�m�b�N�o�b�N����������
    /// </summary>
    /// <param name="enemy">�ڐG�����G�l�~�[</param>
    private void TakeDamage(Enemy enemy)
    {
        if(enemy == null) return;

        //�v���C���[�̏�Ԃ����S�������͖��G��Ԃ������烊�^�[��
        if (currentLifeState == lifeState.death || currentInvincible == invincibleState.invincible)
        {
            return;
        }

        //���C�t�񕜒��ɍU�����󂯂���񕜂𒆒f
        if (currentLifeState == lifeState.healing)
        {
            healingCTS.Cancel();
            healingCTS = new CancellationTokenSource();
            Debug.Log("�񕜂𒆒f");
        }

        life -= enemy.EnemyDamagePower;

        if (life <= 0)
        {
            ChangeLifeState(lifeState.death);
            Debug.Log("���C�t��0�ɂȂ�܂���");
            return;
        }

        ChangeLifeState(lifeState.healing);
        TakeKnockBack(enemy).Forget();
    }

    /// <summary>
    /// �m�b�N�o�b�N����������擾���A�t�B�[���h�ɐ錾�����ϐ��Ɋi�[����
    /// �i�[��A�X�e�[�g�ύX���A�w�肵�����ԑҋ@���ăX�e�[�g��߂�
    /// </summary>
    /// <param name="enemy">�v���C���[�����������G�l�~�[</param>>
    private async UniTask TakeKnockBack(Enemy enemy)
    {
        //�v���C���[�̏�Ԃ����S�������͖��G��Ԃ������烊�^�[��
        if (currentLifeState == lifeState.death || currentInvincible == invincibleState.invincible)
        {
            return;
        }

        rb.linearVelocity = Vector3.zero;

        Vector3 direction = _transform.position - enemy.transform.position;
        direction.y = RESET_Y_DIRECTION;
        knockbackDirection = (direction.normalized * knockbackForce) + knockbackUpDirection;

        currentKnockback = knockbackState.knockbackActive;
        await UniTask.WaitForSeconds(knockbackDuration);
        currentKnockback = knockbackState.knockbackFinish;

        StartInvincible().Forget();
    }
    
    /// <summary>
    /// ��������Ă���w�肵�����ԑ҂��A���C�t������l�ȏ�܂ŉ񕜂�����X�e�[�g��alive�ɕύX
    /// </summary>
    /// <param name="token">���̃��\�b�h�����s���ꂽ����CancellationToken</param>
    /// <returns>�񕜒��ɍU�����ꂽ��unitask�I��</returns>
    private async UniTask HealingLife(CancellationToken token)
    {
        await UniTask.WaitForSeconds(startWait, cancellationToken: token);

        while (life <= maxLife)
        {
            if (token.IsCancellationRequested)
            {
                Debug.Log("�񕜂�cancel����܂���");
                return;
            }

            await UniTask.WaitForSeconds(healingRate);
            life += healingPoint;
            Debug.Log("���C�t�񕜒�");
        }

        life = Mathf.Clamp(life, MINIMUM_LIFE, maxLife);
        ChangeLifeState (lifeState.alive);
        Debug.Log("�񕜊���");
    }

    private void ChangeLifeState(lifeState state)
    {
        currentLifeState = state;

        switch (state)
        {
            case lifeState.alive:
                //alive�X�e�[�g�Ɉڍs�������Ɏ��s������������ǉ�
                break;
            case lifeState.healing:
                HealingLife(healingCTS.Token).Forget();
                break;
            case lifeState.death:
                //death�X�e�[�g�Ɉڍs�������Ɏ��s������������ǉ�
                _gameOverUIManager.CreateGameOverText();
                _gameOverUIManager.CreateContinueButton();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                //����enabled�ő���𖳌������Ă��邪�A�Q�[���I�[�o�[���̓R���e�j���[�Ȃǂ̑�����o����悤�ɂ��邽�ߕύX�\��
                input.enabled = false;
                break;
            default:
                break;
        }
    }

    //���G���Ԃ��X�^�[�g
    private async UniTask StartInvincible()
    {
        currentInvincible = invincibleState.invincible;
        await UniTask.WaitForSeconds(invincibleTime);
        currentInvincible = invincibleState.normal;
    }
}
