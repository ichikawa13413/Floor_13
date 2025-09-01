using Cysharp.Threading.Tasks;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private float damagePower;
    public float EnemyDamagePower { get => damagePower; }

    //--�m�b�N�o�b�N�n--
    [SerializeField] private float knockbackForce;
    [SerializeField] private Vector3 knockbackUpDirection;
    [SerializeField] private float knockbackDuration;
    private Rigidbody playerRB;
    private Vector3 knockbackDirection;
    private const int RESET_Y_DIRECTION = 0;
    public enum knockbackState
    {
        knockbackActive,   //�m�b�N�o�b�N��
        knockbackFinish    //�m�b�N�o�b�N�I��
    }
    public knockbackState currentKnockback { get; private set;}

    private void Awake()
    {
        _transform = transform;
        currentKnockback = knockbackState.knockbackFinish;
    }

    private void FixedUpdate()
    {
        if (currentKnockback == knockbackState.knockbackActive)
        {
            playerRB.AddForce(knockbackDirection, ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            GetKnockBackDirection(player);
        }
    }

    /// <summary>
    /// �m�b�N�o�b�N����������擾���A�t�B�[���h�ɐ錾�����ϐ��Ɋi�[����
    /// �i�[��A�X�e�[�g�ύX���A�w�肵�����ԑҋ@���ăX�e�[�g��߂�
    /// </summary>
    /// <param name="player">�G�l�~�[�ɓ��������v���C���[</param>>
    private async UniTask GetKnockBackDirection(Player player)
    {
        playerRB = player.GetComponent<Rigidbody>();
        playerRB.linearVelocity = Vector3.zero;

        Vector3 direction = player.transform .position - _transform.position;
        direction.y = RESET_Y_DIRECTION;
        knockbackDirection = (direction.normalized * knockbackForce) + knockbackUpDirection;

        currentKnockback = knockbackState.knockbackActive;
        await UniTask.WaitForSeconds(knockbackDuration);
        currentKnockback = knockbackState.knockbackFinish;
    }
}