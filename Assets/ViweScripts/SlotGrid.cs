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
    [SerializeField] private Vector3 ChoiceArrowOffset;
    [SerializeField] private GameObject ChoiceArrowPrefab;
    private GameObject ChoiceArrow;
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
    private int slotsIndex;
    private bool isOpenKeyboard;
    private bool isLockChoice;
    private Subject<Unit> decisionSubject;//コントローラーでスロットを選択した時に通知
    private enum State//どのボタンを選択中か管理
    {
        dropState,
        useState,
        nullState
    }
    private State currentState;

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
        slotSubject = new Subject<Slot>();
        _gridLayoutGroup = GetComponent<GridLayoutGroup>();
        currentSlectIndex = 0;
        isOpenKeyboard = false;
        isLockChoice = false;
        decisionSubject = new Subject<Unit>();
        currentState = State.nullState;
    }

    private void Start()
    {
        CreateSlot();
        CreateButton();

        //押されたスロットの情報を購読して、押されたスロットの横にボタン設置
        slotSubject.Subscribe(slot => SetButton(slot));

        //プレイヤーがインベントリを閉じたらボタンを非表示
        _player.closeObservable.Subscribe(_ => HideButton());

        //プレイヤーがキーボードで開いたらtrue
        _player.keyboardObservable.Subscribe(_ => isOpenKeyboard = true);

        slots = GetComponentsInChildren<Slot>().ToList();
        slotsIndex = slots.Count - 1;

        CheckConstraint();

        decisionSubject.Subscribe(_ =>
        {
            isLockChoice = true;
            currentState = State.useState;
        });
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

        SetChoiceArrow(useButton.transform.position);
    }

    private void HideButton()
    {
        dropButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        ChoiceArrow.gameObject.SetActive(false);

        //ついでにインベントリをオフにしたらboolをリセット
        isOpenKeyboard = false;
        isLockChoice = false;
        currentState = State.nullState;
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

    public void OnChoiceUp(InputAction.CallbackContext context)
    {
        //インベントリをキーボードで開いて、コントローラーの操作に切り替わったらindex0からスタートする
        if (isOpenKeyboard)
        {
            ResetIndex();
            return;
        }

        //ドロップボタン選択中に十字キー上を押されたらユーズボタンに移動する
        if (currentState == State.dropState && isLockChoice)
        {
            SetChoiceArrow(useButton.transform.position);
            currentState = State.useState;
            return;
        }
        else if (currentState == State.useState)//ユーズボタン選択中に上を押されたら処理を終了
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
        if (currentState == State.useState && isLockChoice)
        {
            SetChoiceArrow(dropButton.transform.position);
            currentState = State.dropState;
            return;
        }
        else if (currentState == State.dropState)//ドロップボタン選択中に上を押されたら処理を終了
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

    private void ResetIndex()
    {
        if (isOpenKeyboard)
        {
            SelectSlot(0);
            currentSlectIndex = 0;
        }
        isOpenKeyboard = false;
    }

    public void OnDecisionButton(InputAction.CallbackContext context)
    {
        if (!this.gameObject.activeSelf) return;
      
        Slot currentSlot = slots[currentSlectIndex];

        SetButton(currentSlot);

        decisionSubject.OnNext(Unit.Default);
    }

    private void SetChoiceArrow(Vector3 ButtonPos)
    {
        ChoiceArrow.SetActive(true);
        ChoiceArrow.transform.position = ButtonPos;
        ChoiceArrow.transform.position += ChoiceArrowOffset;
        currentState = State.useState;
    }

    public void OnQuitChoice(InputAction.CallbackContext context)
    {
        HideButton();
    }
}    