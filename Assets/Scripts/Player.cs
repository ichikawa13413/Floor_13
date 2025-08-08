using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Transform _transform;
    private Vector2 direction;
    private Rigidbody rb;
    private bool isGround;

    [SerializeField] private PlayerInput input;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private LayerMask groundLayer;

    private void Awake()
    {
        _transform = transform;
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        MoveInput();

    }

    private void OnEnable()
    {
        if(input == null) return;
        input.actions["Move"].performed += ChangeDirection;
        input.actions["Move"].canceled += ChangeDirection;
        input.actions["Jump"].performed += OnJump;
    }

    private void OnDisable()
    {
        if (input == null) return;
        input.actions["Move"].performed -= ChangeDirection;
        input.actions["Move"].canceled -= ChangeDirection;
        input.actions["Jump"].performed -= OnJump;
    }
    private void MoveInput()
    {
        //地面判定を行う
        isGround = Physics.CheckSphere(_transform.position, 0.1f, groundLayer);

        //inputActionのvector2をvector3に変更
        Vector3 movement = new Vector3(direction.x,0, direction.y) * speed;

        //プレイヤーの現在位置
        Vector3 position = _transform.position;

        //移動距離
        //float distance = speed * Time.fixedDeltaTime;
        
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
    }

    private void ChangeDirection(InputAction.CallbackContext context)
    {
        direction = context.ReadValue<Vector2>().normalized;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGround)
        {
            rb.linearVelocity = new Vector3(0, jumpForce, 0);
        }
    }
}
