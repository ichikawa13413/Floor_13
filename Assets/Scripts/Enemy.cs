using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer;

public class Enemy : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private float damagePower;
    public float EnemyDamagePower { get => damagePower; }

    //--ノックバック系--
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
    /// ノックバックする方向を取得し、フィールドに宣言した変数に格納する
    /// 格納後、ステート変更し、指定した時間待機してステートを戻す
    /// </summary>
    /// <param name="player">エネミーに当たったプレイヤー</param>>
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

        //ノックバックを処理先にやってその後ダメージ処理をする
        knockbackSubject.OnNext(this);
    }*/
}