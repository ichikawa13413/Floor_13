using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using R3;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal.Profiling.Memory.Experimental;
using Cysharp.Threading.Tasks;

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
    [SerializeField] private Vector3 ChoiceArrowOffset;
    [SerializeField] private GameObject ChoiceArrowPrefab;
    private GameObject ChoiceArrow;
    private Button dropButton;
    private Button useButton;
    private Canvas _canvas;
    private Subject<Slot> slotMouseClickSubject;
    public ISubject<Slot> slotOnNext => slotMouseClickSubject;
    private bool isMouseClick;
    private Player _player;

    //--Slotをコントローラーで操作するための物--
    private List<Slot> slots;
    private GridLayoutGroup _gridLayoutGroup;
    private int constraint;
    private int currentSlectIndex;
    private int slotsIndex;
    private bool isOpenKeyboard;

    //--コントローラーでボタンを操作する系--
    private bool isLockChoice;
    private Subject<Unit> decisionSubject;//コントローラーでスロットを選択した時に通知
    private enum arrowState//どのボタンを選択中か管理
    {
        dropState,
        useState,
        nullArrowState
    }
    private arrowState currentArrowState;
    private enum buttonState
    {
        dropDecisionState,
        useDecisionState,
        nullButtonState
    }
    private buttonState currentButtonState;

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

        //--スロット系--
        slotMouseClickSubject = new Subject<Slot>();
        _gridLayoutGroup = GetComponent<GridLayoutGroup>();
        isMouseClick = false;

        //--コントローラーでボタンを操作する系--
        currentSlectIndex = 0;
        isOpenKeyboard = false;

        //--コントローラーでボタンを操作する系--
        isLockChoice = false;
        decisionSubject = new Subject<Unit>();
        currentArrowState = arrowState.nullArrowState;
        currentButtonState = buttonState.nullButtonState;
    }

    private void Start()
    {
        CreateSlot();
        CreateButton();

        //マウスでクリックされたスロットの情報を購読して、押されたスロットの横にボタン設置
        slotMouseClickSubject.Subscribe(slot =>
        {
            isMouseClick = true;
            SetButton(slot);
        });

        //プレイヤーがインベントリを閉じたらボタンを非表示
        _player.closeObservable.Subscribe(_ => HideButton());

        //プレイヤーがキーボードで開いたらtrue
        _player.keyboardObservable.Subscribe(_ => isOpenKeyboard = true);

        slots = GetComponentsInChildren<Slot>().ToList();
        slotsIndex = slots.Count - 1;

        CheckConstraint();

        //決定ボタンが押されたら操作をロックして、アローをuseButtonに設定
        decisionSubject.Subscribe(_ =>
        {
            isLockChoice = true;
        });

        //プレイヤーがアイテムを拾ったらセットアイテムをする
        Slot current = slots[0];
        _player.GetItemObservable.Subscribe(playerItem => current.SetItem(playerItem));
    }

    private void Update()
    {
        Debug.Log(currentButtonState);
        Debug.Log(currentSlectIndex);
    }
    private void OnEnable()
    {
        //最初のスロットを選択状態にしておく
        if (slots != null && slots.Count > 0 && !isOpenKeyboard)
        {
            SelectSlot(0);
            currentSlectIndex = 0;
        }
    }

    ///-----<スロット系メソッド>-----

    //スロットをmaxSlot分生成
    private void CreateSlot()
    {
        for (int i = 0; i < maxSlot; i++)
        {
            GameObject slotObj = _container.Instantiate(slotPrefab, _transform);

            Slot slot = slotObj.GetComponent<Slot>();

            if (i < _player.gettedItem.Length)
            {
                slot.SetItem(_player.gettedItem[i]);
            }
            else
            {
                slot.SetItem(null);
            }
        }

        gameObject.SetActive(false);
    }

    //gridLayoutGroupのConstraintがFixedColumnCountに設定されているか確認、その後constraintの値を代入
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
    ///------------------------------

    ///-----<ボタン系メソッド>-----
    private void CreateButton()
    {
        dropButton = Instantiate(dropButtonPrefab, _canvas.transform);
        useButton = Instantiate(useButtonPrefab, _canvas.transform);
        ChoiceArrow = Instantiate(ChoiceArrowPrefab, _canvas.transform);

        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        ChoiceArrow.gameObject.SetActive(false);
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

        if (!isMouseClick)
        {
            SetChoiceArrow(useButton.transform.position);
        }
    }

    private void HideButton()
    {
        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        ChoiceArrow.gameObject.SetActive(false);

        //ついでにインベントリをオフにしたらboolをリセット
        isOpenKeyboard = false;
        isLockChoice = false;
        isMouseClick = false;
        currentArrowState = arrowState.nullArrowState;
        currentButtonState = buttonState.nullButtonState;
    }


    ///-----<コントローラーでインベントリを操作する系>-----
    public void OnChoiceUp(InputAction.CallbackContext context)
    {
        //インベントリをキーボードで開いて、コントローラーの操作に切り替わったらindex0からスタートする
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        //ドロップボタン選択中に十字キー上を押されたらユーズボタンに移動する
        if (currentArrowState == arrowState.dropState && isLockChoice)
        {
            SetChoiceArrow(useButton.transform.position);
            currentArrowState = arrowState.useState;
            currentButtonState = buttonState.useDecisionState;
            return;
        }
        else if (currentArrowState == arrowState.useState)//ユーズボタン選択中に上を押されたら処理を終了
        {
            return;
        }

        int index = currentSlectIndex;

        //一番上の段のスロットだったら移動させない
        if (index >= constraint)
        {
            index -= constraint;
        }

        //gridLayoutGroupの枠外から出ていないか確認
        index = Mathf.Clamp(index, 0, slotsIndex);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    public void OnChoiceDown(InputAction.CallbackContext context)
    {
        //インベントリをキーボードで開いて、コントローラーの操作に切り替わったらindexをリセットする
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        //ユーズボタン選択中に十字キー上を押されたらドロップボタンに移動する
        if (currentArrowState == arrowState.useState && isLockChoice)
        {
            SetChoiceArrow(dropButton.transform.position);
            currentArrowState = arrowState.dropState;
            currentButtonState = buttonState.dropDecisionState;
            return;
        }
        else if (currentArrowState == arrowState.dropState)//ドロップボタン選択中に上を押されたら処理を終了
        {
            return;
        }

        int index = currentSlectIndex;

        //スロットの一番下の段の移動制限用
        int underLimit = slotsIndex - constraint;

        //一番下の段のスロットだったら移動させない
        if (index <= underLimit)
        {
            index += constraint;
        }

        //gridLayoutGroupの枠外から出ていないか確認
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    public void OnChoiceLeft(InputAction.CallbackContext context)
    {
        if (isLockChoice) return;

        //インベントリをキーボードで開いて、コントローラーの操作に切り替わったらindexをリセットする
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        int index = currentSlectIndex;
        index--;

        //gridLayoutGroupの枠外から出ていないか確認
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    public void OnChoiceRight(InputAction.CallbackContext context)
    {
        if (isLockChoice) return;

        //インベントリをキーボードで開いて、コントローラーの操作に切り替わったらindexをリセットする
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        int index = currentSlectIndex;
        index++;

        //gridLayoutGroupの枠外から出ていないか確認
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (index != currentSlectIndex)
        {
            currentSlectIndex = index;
            SelectSlot(currentSlectIndex);
        }
    }

    private void SelectSlot(int index)
    {
        foreach(Slot slot in slots)
        {
            slot.HideHighLight();
        }
        slots[index].ShowHighLight();
    }

    private void ResetIndex()
    {
        if (isOpenKeyboard)
        {
            SelectSlot(0);
            currentSlectIndex = 0;
        }
        isOpenKeyboard = false;
    }

    ///------------------------------

    ///-----<コントローラーでボタンを操作する系>-----

    public void OnDecisionButton(InputAction.CallbackContext context)
    {
        if (!this.gameObject.activeSelf) return;
      
        Slot currentSlot = slots[currentSlectIndex];
        SetButton(currentSlot);

        //選択したスロットで使うボタンを決定後、ボタンを押したのち
        if (isLockChoice && currentButtonState != buttonState.nullButtonState)
        {
            switch (currentButtonState)
            {
                case buttonState.useDecisionState:
                    currentSlot.UseItem();
                    Debug.Log("UseItem");
                    break;
                case buttonState.dropDecisionState:
                    currentSlot.DropItem();
                    Debug.Log("DropItem");
                    break;
            }
            currentButtonState = buttonState.nullButtonState;
            HideButton();
            return;
        }

        decisionSubject.OnNext(Unit.Default);

        currentArrowState = arrowState.useState;
        currentButtonState = buttonState.useDecisionState;
    }

    /// <summary>
    /// 選択中のボタンの右にアイコンを置く
    /// </summary>
    /// <param name="ButtonPos">選択中のボタンの座標</param>
    private void SetChoiceArrow(Vector3 ButtonPos)
    {
        ChoiceArrow.SetActive(true);
        ChoiceArrow.transform.position = ButtonPos;
        ChoiceArrow.transform.position += ChoiceArrowOffset;
        currentArrowState = arrowState.useState;
    }

    public void OnQuitChoice(InputAction.CallbackContext context)
    {
        HideButton();
    }

    ///------------------------------
}