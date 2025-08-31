using TMPro;
using UnityEngine;

public class GameOverUIManager : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private TextMeshProUGUI gameOverText;

    //このクラスでゲームオーバー時に表示されるUIの管理を行う
    private void Awake()
    {
        _transform = transform;
    }

    public void CreateGameOverText()
    {
        var text = Instantiate(gameOverText, _transform);
    }
}
