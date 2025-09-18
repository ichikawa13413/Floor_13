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
        confirmingQuit,   //警告イメージを表示中
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
        //choicearrowを現在選択中のボタンにフォーカスする
        if (currentGameOverUI == gameOverUIState.mainSelection || currentGameOverUI == gameOverUIState.confirmingQuit)
        {
            GameObject currentGameObject = EventSystem.current.currentSelectedGameObject;
            ChoiceArrow.GetComponent<RectTransform>().anchoredPosition = 
                currentGameObject.GetComponent<RectTransform>().anchoredPosition + choiceArrowOffset;
        }
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
        ContinueButton = Instantiate(ContinueButtonPrefab, _canvas.transform);
        QuitButton = Instantiate(QuitButtonPredab, _canvas.transform);

        ContinueButton.onClick.AddListener(_sceneLoadManager.ContinueFunction);
        QuitButton.onClick.AddListener(() => 
        { 
            ChangeState(gameOverUIState.confirmingQuit);
            CreateCaution(); 
        });

        ContinueButton.GetComponent<RectTransform>().anchoredPosition = ContinueButtonPos;
        QuitButton.GetComponent<RectTransform>().anchoredPosition = QuitButtonPos;

        SetPositionArrow(QuitButton.gameObject);
        Debug.Log("処理を実行しました");
    }

    //警告ポップアップを表示する
    private void CreateCaution()
    {
        cautionImage = Instantiate(cautionPrefab, _canvas.transform);
        yesButton = Instantiate(yesButtonPrefab, cautionImage.transform);
        noButton = Instantiate(noButtonPrefab, cautionImage.transform);

        yesButton.GetComponent<RectTransform>().anchoredPosition = yesButtonPos;
        noButton.GetComponent<RectTransform>().anchoredPosition= noButtonPos;

        yesButton.onClick.AddListener(_sceneLoadManager.QuitFunction);
        noButton.onClick.AddListener(() => 
        { 
            Destroy(cautionImage.gameObject); 
            ChangeState(gameOverUIState.mainSelection);
            EventSystem.current.SetSelectedGameObject(QuitButton.gameObject);
        });

        EventSystem.current.SetSelectedGameObject(yesButton.gameObject);
    }

    private void ChangeState(gameOverUIState wantState)
    {
        currentGameOverUI = wantState;
    }

    /// <summary>
    /// 現在選択中のボタンの横にChoiceArrowを置く(まだ生成していない場合は生成する）
    /// EventSystemでtargetを選択中にする、最後に選択したボタン（lastButton）にtargetを代入
    /// </summary>
    /// <param name="target">現在選択中のボタン</param>
    private void SetPositionArrow(GameObject target)
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
        EventSystem.current.SetSelectedGameObject(target);
        lastButton = target;
    }
}