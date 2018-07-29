using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Constants
    private const float PLATFORM_DROP_DURATION = 1f;

    [Header("Prefabs")]
    public GameObject orbPrefab;

    [Header("Movement")]
    public float maxSpeed;
    public float fallSpeed = 30;
    public float fastFallSpeed = 40;
    public float accel;
    public float airAccel;
    public float ffAccel;
    public float initJumpForce;
    public float sustainedJumpForce;
    public float sustainedJumpWindow = 0.1f;
    public int maxJumps;

    [Header("Input")]
    public float deadZone = 0.2f;

    [Header("Public Info (Don't Change)")]
    public Vector2 input;
    public bool ignoringPlatforms;

    private Rigidbody2D rb;
    private Vector2 prevInput;
    private bool tryJump = false;
    private bool jumpAxisDown = false;
    private bool fastFall = false;
    private bool grounded = false;
    private int jumps = 1;
    private float sustainedJumpTimer;
    private float platformDropTimer = 0;
    private float jumpForce = 0;
    private float sustainedJumpSpeed;

    void Start()
    {
        // init private variables with public properties
        jumps = maxJumps;
        sustainedJumpTimer = sustainedJumpWindow;

        // instantiate/get components
        rb = this.GetComponent<Rigidbody2D>();
        Instantiate(orbPrefab, transform.position + Vector3.back, transform.rotation);
    }

    void FixedUpdate()
    {
        ApplyJumpingForce();
        ApplyHorizontalMovementForce();
    }

    void Update()
    {
        PollInput();

        // Ignore platforms unless otherwise indicated
        ignoringPlatforms = true;

        HandleGrounding();

        HandleJumping();
    }

    private void ApplyHorizontalMovementForce()
    {
        if (Mathf.Abs(input.x) > 0)// && (Mathf.Sign(input.x) == -Mathf.Sign(rb.velocity.x)))
        {
            if (grounded)
            {
                rb.AddForce(Vector2.right * input.x * accel);
            }
            else
            {
                rb.AddForce(Vector2.right * input.x * airAccel);
            }
        }

        SetVX(Mathf.Clamp(rb.velocity.x, -maxSpeed, maxSpeed));
    }

    private void ApplyJumpingForce()
    {
        if (tryJump)
        {
            if (jumps > 0 && !jumpAxisDown)
            {
                SetVY(0);
                //float mod = 0.2f * (jumps / maxJumps) + 0.8f;
                Debug.Log("JUMP");
                rb.AddForce(Vector2.up * initJumpForce);
                jumps--;
            }
            else if (jumpAxisDown)
            {
                // if sustaining first jump
                if (jumps == (maxJumps - 1) && sustainedJumpTimer > 0)
                {
                    int susFrames = Mathf.RoundToInt(sustainedJumpTimer * 60);
                    if (susFrames == 1)
                    {
                        rb.AddForce(Vector2.up * sustainedJumpForce);
                    }
                    else
                    {
                        rb.AddForce(Vector2.up * 10);
                    }
                    Timer.Increment(ref sustainedJumpTimer);
                }
            }
            jumpAxisDown = true;
        }
        else if (jumpAxisDown)
        {
            jumpAxisDown = false;
            sustainedJumpTimer = 0;
        }

        // Fast Fall Velocity
        if (fastFall && rb.velocity.y > -fastFallSpeed)
        {
            rb.AddForce(Vector2.down * ffAccel);
        }

        // Limit fall-speed
        float fallLimit = fastFall ? -fastFallSpeed : -fallSpeed;
        if (rb.velocity.y < fallLimit)
        {
            SetVY(fallLimit);
        }
    }

    private void HandleJumping()
    {
        jumpForce = 0;
        if (rb.velocity.y <= 0)
        {
            if (grounded && (jumps < maxJumps || sustainedJumpTimer < sustainedJumpWindow))
            {
                jumps = maxJumps;
                sustainedJumpTimer = sustainedJumpWindow;
            }
        }
        if (rb.velocity.y >= 0)
        {
            fastFall = false;
        }
    }

    private void HandleGrounding()
    {
        HandleFastFallAndPlatformDrop();

        // Ground Check
        bool pground = grounded;
        float gcf = Mathf.Clamp(Mathf.Abs(rb.velocity.y) * 2f * Time.deltaTime, 0.3f, 50f);
        Vector2 gcSize = Vector2.right * 0.4f + Vector2.up * gcf;
        Vector2 gcOrigin = rb.position + Vector2.down * 0.50f + Vector2.down * gcf * 0.5f;
        int gcLayerMask = LayerMask.GetMask("Obstacles", "Platform");
        RaycastHit2D groundCheck = Physics2D.BoxCast(gcOrigin, gcSize, 0, Vector2.zero, 0, gcLayerMask, 0);
        if (groundCheck)
        {
            bool isPlatform = groundCheck.transform.tag == "Platform";
            if (!ignoringPlatforms && !isPlatform)
            {
                // IMPLICITLY ignore platforms
                ignoringPlatforms = true;
            }
            if (groundCheck.transform.tag == "Wall" || (!ignoringPlatforms && isPlatform))
            {
                grounded = true;
                fastFall = false;
            }
        }
        else
        {
            if (pground && jumps == maxJumps)
            {
                jumps--;
                sustainedJumpTimer = 0;
            }
            grounded = false;
        }

        Physics2D.IgnoreLayerCollision(9, 11, ignoringPlatforms);
    }

    private void HandleFastFallAndPlatformDrop()
    {
        // Check for fast-fall and platform drop
        if (input.y < -0.5f)
        {
            if (grounded || Mathf.Abs(rb.velocity.y) < 4f)
            {
                platformDropTimer = PLATFORM_DROP_DURATION;
            }
            else
            {
                platformDropTimer = PLATFORM_DROP_DURATION / 10;
                if (prevInput.y >= -0.5F)
                {
                    fastFall = true;
                }
            }
        }

        // EXPLICITLY ignore platforms (or not)
        Vector2 pcSize = Vector2.right * 0.25f + Vector2.up * 0.5f;
        Timer.Increment(ref platformDropTimer);
        ignoringPlatforms = 
            platformDropTimer > 0 || 
            input.y <= -0.5f || 
            rb.velocity.y > 0 || 
            Physics2D.BoxCast(rb.position, pcSize, 0, Vector2.zero, 0.5f, LayerMask.GetMask("Platform"), 0);
    }

    private void PollInput()
    {
        // Misc. Inputs
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        prevInput = input;

        // Get Main Inputs
        //tryJump = Input.GetAxis("Jump") > 0 || input.y >= deadZone;
        input = Vector2.right * Input.GetAxis("Horizontal") + Vector2.up * Input.GetAxis("Vertical");
    }

    private void SetVX(float vx)
    {
        rb.velocity = Vector2.right * vx + Vector2.up * rb.velocity.y;
    }

    private void SetVY(float vy)
    {
        rb.velocity = Vector2.right * rb.velocity.x + Vector2.up * vy;
    }

}
