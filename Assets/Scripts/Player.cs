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

    //--移動関係--
    [SerializeField] private PlayerInput input;
    public PlayerInput MyInput { get => input; }
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
    private const int X_ROTATION_CENTER = 0;
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
    [SerializeField] private int limitItem;//プレイヤーが所持できるitem数の上限
    public int playerLimitItem { get => limitItem; }
    private bool isOpenInventory;
    private List<Item> itemList;
    public List<Item> playerItemList { get => itemList; }
    private ItemObject currentLookItem;
    private Subject<Item> getItemSubject;//プレイヤーがアイテムをgetしたら通知
    public Observable<Item> getItemObservable => getItemSubject;
    public event Action OnInventoryOpen;
    public event Action OnInventoryClose;
    private enum InventoryState
    {
        active,     //インベントリを開いている状態
        inactive    //インベントリを閉じている状態
    }
    private InventoryState CurrentInventoryState;

    //--インベントをコントローラーで動かす時に使うイベント一覧--
    public event Action OnPlayerChoiceUP;
    public event Action OnPlayerChoiceDOWN;
    public event Action OnPlayerChoiceLEFT;
    public event Action OnPlayerChoiceRIGHT;
    public event Action OnPlayerDecisionButton;
    public event Action OnPlayerQuitChoice;

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
    [SerializeField] private float maxLife;
    [SerializeField] private float healingPoint;
    [SerializeField] private float startWait;
    [SerializeField] private float healingRate;
    private CancellationTokenSource healingCTS;
    private const int MINIMUM_LIFE = 0;
    private GameUIManager _gameOverUIManager;
    private enum lifeState
    {
        alive,      //通常状態
        healing,    //緊張状態
        death       //死亡状態
    }
    private lifeState currentLifeState;

    //--ノックバック系--
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
        knockbackActive,   //ノックバック中
        knockbackFinish    //ノックバック終了
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
        //マウスカーソルを中央に固定し（1行目）、消す（2行目）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        //ノックバック中の処理はFixedUpdateでやる
        if (currentKnockback == knockbackState.knockbackActive)
        {
            rb.AddForce(knockbackDirection,ForceMode.Impulse);
            input.enabled = false;
        }
        else
        {
            input.enabled = true;//こいつのせいで死んでも動ける
        }

        //inventory使用中は操作不可
        if (!isOpenInventory)
        {
            MoveInput();
        }
    }

    private void Update()
    {
        Debug.Log(currentLifeState);

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
        Debug.Log(currentLifeState);
        Debug.Log(itemList.Count);

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
        input.actions["ChoiceUp"].started += OnChoiceUp;
        input.actions["ChoiceDown"].started += OnChoiceDown;
        input.actions["ChoiceLeft"].started += OnChoiceLeft;
        input.actions["ChoiceRight"].started += OnChoiceRight;
        input.actions["DecisionButton"].started += OnDecision;
        input.actions["QuitButton"].started += OnQuitChoice;

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
        input.actions["ChoiceUp"].started -= OnChoiceUp;
        input.actions["ChoiceDown"].started -= OnChoiceDown;
        input.actions["ChoiceLeft"].started -= OnChoiceLeft;
        input.actions["ChoiceRight"].started -= OnChoiceRight;
        input.actions["DecisionButton"].started -= OnDecision;
        input.actions["QuitButton"].started -= OnQuitChoice;
        
        //--アイテムを拾う系--
        input.actions["Get"].started -= OnGetItem;
    }

    private void Onkettei(InputAction.CallbackContext context)
    {
        if(currentLifeState != lifeState.death) return;

        OnketteiAction?.Invoke();
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

    //視点操作
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

        if (itemList.Count == limitItem)
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

        if (CurrentInventoryState == InventoryState.active)
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

    //インベントリ クローズ時の処理
    public void CloseSlotGrid()
    {
        OnInventoryClose?.Invoke();
        CurrentInventoryState = InventoryState.inactive;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isOpenInventory = false;

        _animator.speed = START_ANIMARTION;

        UnFreezePlayer();

        //インベントリを閉じた事を通知
        closeSubject.OnNext(Unit.Default);
    }

    /// <summary>
    /// スロットをコントローラーで操作した時にSlotGridクラスへ通知死亡時のボタン選択に流用
    /// </summary>
    private void OnChoiceUp(InputAction.CallbackContext context)
    {
        OnPlayerChoiceUP?.Invoke();
    }

    private void OnChoiceDown(InputAction.CallbackContext context)
    {
        OnPlayerChoiceDOWN?.Invoke();
    }

    //死亡時のボタン選択に流用
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

    //死亡時のボタン選択に流用
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
    //todo
    //↓ハイライトを実装したらこのメソッドに追加する
    /// <summary>
    /// レイを飛ばしitemobjectかチェックする
    /// </summary>
    /// <returns>プレイヤーが見ているのがitemだったらtrue、それ以外だったfalse</returns>
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
    /// アイテムを拾う処理。rayが当たったアイテムを情報取得し、それをslotGridへ送る。
    /// その後、アイテムオブジェクトを破壊する。
    /// </summary>
    /// <param name="hit">OnGetItemから送られてきたRaycastHit</param>
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
    /// エネミーに接触したらダメージ処理をする。その後、ノックバック処理をする
    /// </summary>
    /// <param name="enemy">接触したエネミー</param>
    private void TakeDamage(Enemy enemy)
    {
        if(enemy == null) return;

        //プレイヤーの状態が死亡もしくは無敵状態だったらリターン
        if (currentLifeState == lifeState.death || currentInvincible == invincibleState.invincible)
        {
            return;
        }

        //ライフ回復中に攻撃を受けたら回復を中断
        if (currentLifeState == lifeState.healing)
        {
            healingCTS.Cancel();
            healingCTS = new CancellationTokenSource();
            Debug.Log("回復を中断");
        }

        life -= enemy.EnemyDamagePower;

        if (life <= 0)
        {
            ChangeLifeState(lifeState.death);
            Debug.Log("ライフが0になりました");
            return;
        }

        ChangeLifeState(lifeState.healing);
        TakeKnockBack(enemy).Forget();
    }

    /// <summary>
    /// ノックバックする方向を取得し、フィールドに宣言した変数に格納する
    /// 格納後、ステート変更し、指定した時間待機してステートを戻す
    /// </summary>
    /// <param name="enemy">プレイヤーが当たったエネミー</param>>
    private async UniTask TakeKnockBack(Enemy enemy)
    {
        //プレイヤーの状態が死亡もしくは無敵状態だったらリターン
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
    /// 実装されてから指定した時間待ち、ライフが上限値以上まで回復したらステートをaliveに変更
    /// </summary>
    /// <param name="token">このメソッドが実行された時のCancellationToken</param>
    /// <returns>回復中に攻撃されたらunitask終了</returns>
    private async UniTask HealingLife(CancellationToken token)
    {
        await UniTask.WaitForSeconds(startWait, cancellationToken: token);

        while (life <= maxLife)
        {
            if (token.IsCancellationRequested)
            {
                Debug.Log("回復がcancelされました");
                return;
            }

            await UniTask.WaitForSeconds(healingRate);
            life += healingPoint;
            Debug.Log("ライフ回復中");
        }

        life = Mathf.Clamp(life, MINIMUM_LIFE, maxLife);
        ChangeLifeState (lifeState.alive);
        Debug.Log("回復完了");
    }

    private void ChangeLifeState(lifeState state)
    {
        currentLifeState = state;

        switch (state)
        {
            case lifeState.alive:
                //aliveステートに移行した時に実行したい処理を追加
                break;
            case lifeState.healing:
                HealingLife(healingCTS.Token).Forget();
                break;
            case lifeState.death:
                //deathステートに移行した時に実行したい処理を追加
                _gameOverUIManager.CreateGameOverText();
                _gameOverUIManager.CreateContinueButton();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                //今はenabledで操作を無効かしているが、ゲームオーバー時はコンテニューなどの操作を出来るようにするため変更予定
                input.enabled = false;
                break;
            default:
                break;
        }
    }

    //無敵時間をスタート
    private async UniTask StartInvincible()
    {
        currentInvincible = invincibleState.invincible;
        await UniTask.WaitForSeconds(invincibleTime);
        currentInvincible = invincibleState.normal;
    }
}
