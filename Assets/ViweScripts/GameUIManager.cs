using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using VContainer;

public class GameUIManager : MonoBehaviour
{
    private Transform _transform;
    private Canvas _canvas;
    private SceneLoadManager _sceneLoadManager;
    private Player _player;
    private enum gameOverUIState
    {
        mainSelection,    //Continue・QuitButtonを選択中
        Caution,   //警告イメージを表示中
        hideGameOverUI    //ゲームオーバーUIを非表示中（生存中）  
    }
    gameOverUIState currentGameOverUI;

    //--ゲームオーバーテキスト--
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Vector3 gameOverTextPos;

    //--ゲームオーバー時に表示されるボタン--
    [SerializeField] private Button ContinueButtonPrefab;
    [SerializeField] private Vector3 ContinueButtonPos;
    [SerializeField] private Button QuitButtonPredab;
    [SerializeField] private Vector3 QuitButtonPos;
    private Button ContinueButton;
    private Button QuitButton;

    //--ゲーム終了時にでる警告画面--
    [SerializeField] private Image cautionPrefab;
    [SerializeField] private Button yesButtonPrefab;
    [SerializeField] private Button noButtonPrefab;
    [SerializeField] private Vector3 yesButtonPos;
    [SerializeField] private Vector3 noButtonPos;
    private Image cautionImage;
    private Button yesButton;
    private Button noButton;

    //--コントローラーでボタンを操作する時に使う系--
    [SerializeField] private Vector2 choiceArrowOffset;
    [SerializeField] private GameObject ChoiceArrowPrefab;
    private readonly Vector3 CHOICE_ARROW_ROTATION = new Vector3(0, 0, 180);
    private GameObject ChoiceArrow;
    private GameObject lastButton;

    [Inject]
    public void Construct(Canvas canvas, SceneLoadManager sceneLoadManager, Player player)
    {
        _canvas = canvas;
        _sceneLoadManager = sceneLoadManager;
        _player = player;
    }

    //このクラスでゲームオーバー時に表示されるUIの管理を行う
    private void Awake()
    {
        _transform = transform;
        currentGameOverUI = gameOverUIState.hideGameOverUI;
    }

    private void Update()
    {
        Debug.Log("<color=red>" + currentGameOverUI + "</color>");
        Debug.Log(EventSystem.current.currentSelectedGameObject);

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            //todo
            //現状の問題はマウスでボタン以外をクリックした時にlastButtonが一個前のもの記憶している（コンテニューボタンを選択している場合、ボタン以外をクリックするとクイットボタンに戻る
            EventSystem.current.SetSelectedGameObject(lastButton);
        }

        SetChoiceArrow();
    }

    public void CreateGameOverText()
    {
        TextMeshProUGUI text = Instantiate(gameOverText, _canvas.transform);
        text.GetComponent<RectTransform>().anchoredPosition = gameOverTextPos;
        ChangeState(gameOverUIState.mainSelection);
    }

    /// <summary>
    /// コンテニューボタンとクイットボタンを指定座標に生成、各ボタンに機能を追加
    /// その後、ChoiceArrowを生成、初期位置はQuitButton
    /// </summary>
    public void CreateContinueButton()
    {
        ChangeState(gameOverUIState.mainSelection);

        ContinueButton = Instantiate(ContinueButtonPrefab, _canvas.transform);
        QuitButton = Instantiate(QuitButtonPredab, _canvas.transform);

        ContinueButton.onClick.AddListener(_sceneLoadManager.ContinueFunction);
        QuitButton.onClick.AddListener(CreateCaution);

        ContinueButton.GetComponent<RectTransform>().anchoredPosition = ContinueButtonPos;
        QuitButton.GetComponent<RectTransform>().anchoredPosition = QuitButtonPos;

        CreateChoiceArrow(QuitButton.gameObject);
        Debug.Log("コンテニューボタンを表示");
    }

    //警告ポップアップを表示する
    private void CreateCaution()
    {
        ChangeState(gameOverUIState.Caution);

        cautionImage = Instantiate(cautionPrefab, _canvas.transform);
        yesButton = Instantiate(yesButtonPrefab, cautionImage.transform);
        noButton = Instantiate(noButtonPrefab, cautionImage.transform);

        yesButton.GetComponent<RectTransform>().anchoredPosition = yesButtonPos;
        noButton.GetComponent<RectTransform>().anchoredPosition= noButtonPos;

        yesButton.onClick.AddListener(_sceneLoadManager.QuitFunction);
        noButton.onClick.AddListener(() => 
        {
            //警告イメージを破壊してEventSystem.currentをQuitButtonにする
            Destroy(cautionImage.gameObject); 
            ChangeCurrentButton(QuitButton.gameObject);
        });

        //警告画面はyesButtonを初期選択にする
        ChangeCurrentButton(yesButton.gameObject);
    }

    private void ChangeState(gameOverUIState wantState)
    {
        currentGameOverUI = wantState;
    }

    private void ChangeCurrentButton(GameObject wantButton)
    {
        EventSystem.current.SetSelectedGameObject(wantButton);
        lastButton = wantButton;
    }

    /// <summary>
    /// 現在選択中のボタンの横にChoiceArrowを置く(まだ生成していない場合は生成する）
    /// EventSystemでtargetを選択中にする、最後に選択したボタン（lastButton）にtargetを代入
    /// </summary>
    /// <param name="target">現在選択中のボタン</param>
    private void CreateChoiceArrow(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if(ChoiceArrow == null)
        {
            ChoiceArrow = Instantiate(ChoiceArrowPrefab, _canvas.transform);
            ChoiceArrow.GetComponent<RectTransform>().anchoredPosition = target.transform.position;
            ChoiceArrow.GetComponent<RectTransform>().anchoredPosition += choiceArrowOffset;
            ChoiceArrow.transform.localEulerAngles = CHOICE_ARROW_ROTATION;
        }

        ChoiceArrow.GetComponent<RectTransform>().anchoredPosition = target.GetComponent<RectTransform>().anchoredPosition;
        ChoiceArrow.GetComponent<RectTransform>().anchoredPosition += choiceArrowOffset;
        ChangeCurrentButton(target);
    }

    /// <summary>
    /// 現在選択中のボタンにアローをセットする
    /// </summary>
    private void SetChoiceArrow()
    {
        //現在選択中のボタンの横にアローを設置
        if (currentGameOverUI == gameOverUIState.mainSelection || currentGameOverUI == gameOverUIState.Caution)
        {
            GameObject currentGameObject = EventSystem.current.currentSelectedGameObject;
            ChoiceArrow.GetComponent<RectTransform>().anchoredPosition =
                currentGameObject.GetComponent<RectTransform>().anchoredPosition + choiceArrowOffset;
        }
    }
}