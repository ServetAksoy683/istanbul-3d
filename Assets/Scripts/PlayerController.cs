using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Animator animator;
    public Transform cameraTransform;
    public float walkSpeed = 1.8f, jogSpeed = 3.9f, sprintSpeed = 6.8f, swimSpeed = 2.3f;
    public float jumpSpeed = 7.0f, gravity = -22f;

    // Ekranda gosterilen teshis bilgileri
    [HideInInspector] public float DbgGround;
    [HideInInspector] public bool DbgGrounded, DbgSwimming;
    [HideInInspector] public int DbgFrames;

    CharacterController cc;
    Vector3 velocity;
    float curSpeed, lastGrounded, jumpBuffer;
    bool swimming;
    Vector3 spawn;
    static readonly int pSpeed = Animator.StringToHash("Speed");
    static readonly int pGrounded = Animator.StringToHash("Grounded");
    static readonly int pSwim = Animator.StringToHash("Swim");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (animator != null) animator.SetBool(pGrounded, true);
    }

    public void Teleport(Vector3 p)
    {
        spawn = p;
        cc.enabled = false;
        transform.position = p;
        velocity = Vector3.zero;
        cc.enabled = true;
    }

    // Modelin boyunu 1.8 m yap, ayaklarini yere oturt (calisma aninda olculur)
    public void FitModel()
    {
        if (animator == null) return;
        Renderer[] rs = animator.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Transform m = animator.transform;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        if (b.size.y > 0.1f)
        {
            float s = 1.8f / b.size.y;
            if (Mathf.Abs(s - 1f) > 0.06f) m.localScale = m.localScale * s;
        }
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        float delta = transform.position.y - b.min.y;
        if (Mathf.Abs(delta) > 0.02f && Mathf.Abs(delta) < 5f) m.position += Vector3.up * delta;
    }

    void Update()
    {
        DbgFrames++;
        if (cc == null || !cc.enabled) return;

        Vector2 j = MobileControls.Move;
#if UNITY_EDITOR || UNITY_STANDALONE
        if (j.sqrMagnitude < 0.0001f)
            j = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
        float m = j.magnitude;
        Vector3 camF = cameraTransform != null ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 camR = Vector3.Cross(Vector3.up, camF);
        Vector3 dir = camF * j.y + camR * j.x;

        Vector3 pos = transform.position;
        float groundY = IstanbulGeo.Height(pos.x, pos.z);
        DbgGround = groundY;
        if (groundY < -1.0f) swimming = true;
        else if (groundY > -0.7f) swimming = false;
        DbgSwimming = swimming;

        float target;
        if (m < 0.08f) target = 0f;
        else if (swimming) target = swimSpeed * Mathf.Clamp01(m);
        else if (m < 0.55f) target = Mathf.Lerp(0.9f, walkSpeed, (m - 0.08f) / 0.47f);
        else if (m < 0.93f) target = Mathf.Lerp(walkSpeed, jogSpeed, (m - 0.55f) / 0.38f);
        else target = sprintSpeed;   // joystick'i sonuna kadar itince kosu
        curSpeed = Mathf.MoveTowards(curSpeed, target, 16f * Time.deltaTime);

        if (dir.sqrMagnitude > 0.01f && m >= 0.08f)
        {
            Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-14f * Time.deltaTime));
        }
        Vector3 move = (m >= 0.08f ? dir.normalized : Vector3.zero) * curSpeed;

        bool grounded = cc.isGrounded || (pos.y - groundY) < 0.12f;
        if (grounded) lastGrounded = Time.time;

        if (swimming)
        {
            velocity = Vector3.zero;
            float y = Mathf.Lerp(pos.y, -1.0f, 1f - Mathf.Exp(-6f * Time.deltaTime));
            Vector3 delta = move * Time.deltaTime;
            delta.y = y - pos.y;
            cc.Move(delta);
            MobileControls.ConsumeJump();
        }
        else
        {
            bool kbJump = false;
#if UNITY_EDITOR || UNITY_STANDALONE
            kbJump = Input.GetKeyDown(KeyCode.Space);
#endif
            if (MobileControls.ConsumeJump() || kbJump) jumpBuffer = 0.15f;
            jumpBuffer -= Time.deltaTime;
            if (grounded && velocity.y < 0f) velocity.y = -2f;
            if (jumpBuffer > 0f && Time.time - lastGrounded < 0.1f)
            {
                velocity.y = jumpSpeed;
                jumpBuffer = 0f;
                lastGrounded = -10f;
            }
            velocity.y += gravity * Time.deltaTime;
            cc.Move((move + Vector3.up * velocity.y) * Time.deltaTime);

            // Guvenlik: zemin carpisi yoksa bile arazinin altina dusme
            Vector3 p2 = transform.position;
            float gy = IstanbulGeo.Height(p2.x, p2.z);
            if (p2.y < gy - 0.02f && velocity.y <= 0f)
            {
                cc.Move(new Vector3(0f, gy - p2.y, 0f));
                velocity.y = 0f;
            }
        }
        DbgGrounded = grounded;

        Vector3 p = transform.position;
        Vector3 c = new Vector3(Mathf.Clamp(p.x, IstanbulGeo.Min.x + 30f, IstanbulGeo.Max.x - 30f), p.y, Mathf.Clamp(p.z, IstanbulGeo.Min.y + 30f, IstanbulGeo.Max.y - 30f));
        if (c != p || p.y < -40f)
        {
            if (p.y < -40f) c = spawn;
            Teleport(c);
        }

        if (animator != null)
        {
            animator.SetFloat(pSpeed, curSpeed, 0.08f, Time.deltaTime);
            animator.SetBool(pGrounded, swimming || grounded || (Time.time - lastGrounded) < 0.12f);
            animator.SetBool(pSwim, swimming);
        }
    }
}
