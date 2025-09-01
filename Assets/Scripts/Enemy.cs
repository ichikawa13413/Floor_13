using Cysharp.Threading.Tasks;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Transform _transform;

    [SerializeField] private float damagePower;
    public float EnemyDamagePower { get => damagePower; }

    //--ノックバック系--
    [SerializeField] private float knockbackForce;
    [SerializeField] private Vector3 knockbackUpDirection;
    [SerializeField] private float knockbackDuration;
    private Rigidbody playerRB;
    private Vector3 knockbackDirection;
    private const int RESET_Y_DIRECTION = 0;
    public enum knockbackState
    {
        knockbackActive,   //ノックバック中
        knockbackFinish    //ノックバック終了
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
    /// ノックバックする方向を取得し、フィールドに宣言した変数に格納する
    /// 格納後、ステート変更し、指定した時間待機してステートを戻す
    /// </summary>
    /// <param name="player">エネミーに当たったプレイヤー</param>>
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