using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class GameOverUIManager : MonoBehaviour
{
    private Transform _transform;
    private Canvas _canvas;

    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Vector3 gameOverTextPos;

    [SerializeField] private Image ContinueButton;
    [SerializeField] private Vector3 ContinueButtonPos;
    [SerializeField] private Image QuitButton;
    [SerializeField] private Vector3 QuitButtonPos;

    [Inject]
    public void Construct(Canvas canvas)
    {
        _canvas = canvas;
    }
    //このクラスでゲームオーバー時に表示されるUIの管理を行う
    private void Awake()
    {
        _transform = transform;
        CreateContinueButton();
    }

    public void CreateGameOverText()
    {
        var text = Instantiate(gameOverText, _transform);
    }

    private void CreateContinueButton()
    {
        var continueB = Instantiate(ContinueButton, ContinueButtonPos, quaternion.identity, _canvas.transform);
        var quitB = Instantiate(QuitButton, QuitButtonPos, quaternion.identity, _canvas.transform);

        RectTransform rectContinue = continueB.GetComponent<RectTransform>();
        RectTransform rectQuit = quitB.GetComponent<RectTransform>();

        //指定差表をrectTransformに変換
        rectContinue.position = ContinueButtonPos;
        rectQuit.position = ContinueButtonPos;
    }
}
