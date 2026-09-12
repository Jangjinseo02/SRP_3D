using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging; // 네임스페이스 추가

public class PlayerController : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    InputAction moveAction;
    InputAction jumpAction;

    Rigidbody rigid;
    Animator anim;

    [SerializeField] GameObject spine;

    Vector2 inputVec = Vector2.zero;
    float moveSpeed = 5.0f;
    float jumpPower = 15.0f;
    float walkTime = 0.0f;
    float maxTime = 1.0f;

    enum CharacterStates { Idle, Walk, Jump }
    CharacterStates state = CharacterStates.Idle;


    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        moveAction = inputActions.FindAction("Player/Move");
        jumpAction = inputActions.FindAction("Player/Jump");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();

        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;

        jumpAction.started += OnJump;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;

        jumpAction.started -= OnJump;
    }

    void OnMove(InputAction.CallbackContext context)
    {
        //키 입력이 멈추는 경우 상태 전환, 걷기 시간 초기화, 바라보는 방향 초기화
        if (context.canceled) { state = CharacterStates.Idle; walkTime = 0.0f; inputVec = Vector2.zero; }
        else
        {
            state = CharacterStates.Walk;
            // wasd 입력 벡터
            inputVec = context.ReadValue<Vector2>().normalized;
        }
    }

    void Move()
    {
        // 걷기 시간 최대치 조정
        if (state == CharacterStates.Walk) walkTime = walkTime < maxTime ? walkTime + Time.deltaTime : maxTime;

        // 입력 값과 카메라 위치 벡터 합성 >> 방향 벡터 생성(실제 바라보는 위치 * (1),(0),(-1) = 앞 뒤, 좌 우)
        Vector3 dirVec = (Camera.main.transform.forward * inputVec.y) + (Camera.main.transform.right * inputVec.x);
        dirVec.y = 0; // 카메라 벡터의 높이 초기화
        dirVec = dirVec.normalized; // 벡터 정규화

        RaycastHit hit = DetectSlope(dirVec);

        //float angle = Vector3.Angle(hit.normal, Vector3.up);
        //바라보는 방향 회전
        LookAt(dirVec);

        if (hit.normal != Vector3.zero) { dirVec = Vector3.ProjectOnPlane(dirVec, hit.normal); }

        // 걷기 시간에 따른 ANIMATION 변화, 가속도 적용 
        rigid.linearVelocity = dirVec * moveSpeed * walkTime;
        anim.SetFloat("Blend", walkTime, 0.15f, Time.deltaTime);

    }

    public void LookAt(Vector3 pos)
    {
        if (pos == Vector3.zero) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(pos), 0.1f);

        spine.transform.rotation = Quaternion.identity;
    }

    RaycastHit DetectSlope(Vector3 dirVec)
    {
        float sphereRadius = 0.15f;
        Vector3 originRay = transform.position + Vector3.up * 0.3f + dirVec * 0.065f;
        Physics.SphereCast(originRay, sphereRadius, Vector3.down, out RaycastHit hit, 0.6f, LayerMask.GetMask("Ground"));

        return hit;
    }

    void OnJump(InputAction.CallbackContext context)
    {
        rigid.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        Move();
    }
}
