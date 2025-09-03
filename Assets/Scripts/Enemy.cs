using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;

public class Enemy : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private float damagePower;
    public float EnemyDamagePower { get => damagePower; }

    //--�m�b�N�o�b�N�n--
    private const int RESET_Y_DIRECTION = 0;
    private Subject<Enemy> knockbackSubject;
    public Observable<Enemy> knockbackObservable => knockbackSubject;

    private void Awake()
    {
        _transform = transform;
        knockbackSubject = new Subject<Enemy>();
    }

    private void FixedUpdate()
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            player.GetKnockBackDirection(_transform.position).Forget();
        }
    }
    /*
    /// <summary>
    /// �m�b�N�o�b�N����������擾���A�t�B�[���h�ɐ錾�����ϐ��Ɋi�[����
    /// �i�[��A�X�e�[�g�ύX���A�w�肵�����ԑҋ@���ăX�e�[�g��߂�
    /// </summary>
    /// <param name="player">�G�l�~�[�ɓ��������v���C���[</param>>
    private async UniTask GetKnockBackDirection(Player player)
    {
        if(player.currentInvincible == Player.invincibleState.invincible) return;

        playerRB = player.GetComponent<Rigidbody>();
        playerRB.linearVelocity = Vector3.zero;

        Vector3 direction = player.transform .position - _transform.position;
        direction.y = RESET_Y_DIRECTION;
        knockbackDirection = (direction.normalized * knockbackForce) + knockbackUpDirection;

        currentKnockback = knockbackState.knockbackActive;
        await UniTask.WaitForSeconds(knockbackDuration);
        currentKnockback = knockbackState.knockbackFinish;

        //�m�b�N�o�b�N��������ɂ���Ă��̌�_���[�W����������
        knockbackSubject.OnNext(this);
    }*/
}