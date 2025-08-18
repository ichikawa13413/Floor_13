using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using R3;
using System.Collections.Generic;
using System.Linq;

public class SlotGrid : MonoBehaviour
{
    private Transform _transform;
    private IObjectResolver _container;

    //--Slotの生成--
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private int maxSlot;//グリット上に表示させる最大スロット数
    [SerializeField] private Item[] allItems;

    //--ボタン系--
    [SerializeField] private Button dropButtonPrefab;
    [SerializeField] private Button useButtonPrefab;
    [SerializeField] private Vector3 dropButtonOffset;
    [SerializeField] private Vector3 useButtonOffset;
    private Button dropButton;
    private Button useButton;
    private Canvas _canvas;
    public Subject<Slot> slotSubject;
    private Player _player;

    //--Slotをコントローラーで操作するための物--
    private List<Slot> slots;
    private GridLayoutGroup _gridLayoutGroup;
    private int constraint;
    private int currentSlectIndex;
    private Slot currentSelectSlot;
    private PlayerInput playerInput;

    [Inject]
    public void Construct(IObjectResolver container,Canvas canvas, Player player)
    {
        _container = container;
        _canvas = canvas;
        _player = player;
    }

    private void Awake()
    {
        _transform = transform;
        slotSubject = new Subject<Slot>();
        _gridLayoutGroup = GetComponent<GridLayoutGroup>();
        currentSlectIndex = 0;
    }

    private void Start()
    {
        playerInput = _player.MyInput;

        CreateSlot();
        CreateButton();

        //押されたスロットの情報を購読して、押されたスロットの横にボタン設置
        slotSubject.Subscribe(slot => SetButton(slot));

        //プレイヤーがインベントリを閉じたらボタンを非表示
        _player.observableUnit.Subscribe(_ => HideButton());

        slots = GetComponentsInChildren<Slot>().ToList();

        CheckConstraint();
    }

    //スロットをmaxSlot分生成
    private void CreateSlot()
    {
        for (int i = 0; i < maxSlot; i++)
        {
            GameObject slotObj = _container.Instantiate(slotPrefab, _transform);

            Slot slot = slotObj.GetComponent<Slot>();

            if (i < allItems.Length)
            {
                slot.SetItem(allItems[i]);
            }
            else
            {
                slot.SetItem(null);
            }
        }

        gameObject.SetActive(false);
    }

    private void CreateButton()
    {
        dropButton = Instantiate(dropButtonPrefab, _canvas.transform);
        useButton = Instantiate(useButtonPrefab, _canvas.transform);

        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 押されたスロット情報に基づいて、ボタンのクリック処理とボタンの位置を設定。
    /// </summary>
    /// <param name="slot">クリックされたスロット</param>
    private void SetButton(Slot slot)
    {
        dropButton.onClick.RemoveAllListeners();
        dropButton.onClick.RemoveAllListeners();

        dropButton.onClick.AddListener(() => slot.DropItem());
        useButton.onClick.AddListener(() => slot.UseItem());

        RectTransform dropButtonRect = dropButton.GetComponent<RectTransform>();
        RectTransform useButtonRect = useButton.GetComponent<RectTransform>();

        dropButtonRect.position = slot.transform.position;
        useButtonRect.position = slot.transform.position;

        dropButtonRect.position += dropButtonOffset;
        useButtonRect.position += useButtonOffset;

        dropButton.gameObject.SetActive(true);
        useButton.gameObject.SetActive(true);
    }

    private void HideButton()
    {
        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        playerInput.actions["ChoiceUp"].started += OnChoiceUp;
        playerInput.actions["ChoiceDown"].started += OnChoiceDown;
        playerInput.actions["ChoiceLeft"].started += OnChoiceLeft;
        playerInput.actions["ChoiceRight"].started += OnChoiceRight;
    }

    private void OnDisable()
    {
        playerInput.actions["ChoiceUp"].started -= OnChoiceUp;
        playerInput.actions["ChoiceDown"].started -= OnChoiceDown;
        playerInput.actions["ChoiceLeft"].started -= OnChoiceLeft;
        playerInput.actions["ChoiceRight"].started -= OnChoiceRight;
    }

    private void OnChoiceUp(InputAction.CallbackContext context)
    {
        SelectSlot(1);
    }
    private void OnChoiceDown(InputAction.CallbackContext context)
    {
        SelectSlot(2);
    }
    private void OnChoiceLeft(InputAction.CallbackContext context)
    {
        SelectSlot(3);
    }
    private void OnChoiceRight(InputAction.CallbackContext context)
    {
        SelectSlot(4);
    }

    private void SelectSlot(int index)
    {
        foreach(Slot slot in slots)
        {
            slot.HideHighLight();
        }
        slots[index].ShowHighLight();
    }

    private void CheckConstraint()
    {
        if (_gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            constraint = _gridLayoutGroup.constraintCount;
        }
        else
        {
            Debug.LogError("GridLayoutGroupのConstraintはFixed Column Countに設定してください。");
            //とりあえず値を設定しておく
            constraint = 4;
        }
    }
}    
