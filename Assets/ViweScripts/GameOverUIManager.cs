using TMPro;
using UnityEngine;

public class GameOverUIManager : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private TextMeshProUGUI gameOverText;

    //���̃N���X�ŃQ�[���I�[�o�[���ɕ\�������UI�̊Ǘ����s��
    private void Awake()
    {
        _transform = transform;
    }

    public void CreateGameOverText()
    {
        var text = Instantiate(gameOverText, _transform);
    }
}
