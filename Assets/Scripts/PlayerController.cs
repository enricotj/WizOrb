using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float jumpForce;
    public int maxJumps;
    public float acceleration;
    public float airAcceleration;
    public float maxSpeed;

    private bool jumpAxisDown = false;
    private bool canJump = false;
    private int jumps = 1;
    private bool grounded = false;
    private Rigidbody2D rb;
    private GameObject orb;
    private Vector2 input;

    // Use this for initialization
    void Start()
    {
        jumps = maxJumps;
        rb = this.GetComponent<Rigidbody2D>();
        orb = GameObject.Find("Orb");
        if (orb == null)
        {
            // TODO: Instantiate Orb from prefab.
        }
    }

    // Physics update
    void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (Input.GetAxis("Jump") > 0)
        {
            if (canJump && !jumpAxisDown)
            {
                canJump = false;
                rb.AddForce(new Vector2(0, jumpForce));
            }

            jumpAxisDown = true;
        }
        else
        {
            jumpAxisDown = false;
        }

        if (Mathf.Abs(input.x) > 0 && 
            ((Mathf.Abs(rb.velocity.x) < maxSpeed) || (Mathf.Sign(input.x) == -Mathf.Sign(rb.velocity.x))))
        {
            if (grounded)
            {
                rb.AddForce(new Vector2(input.x * acceleration, 0));
            }
            else
            {
                rb.AddForce(new Vector2(input.x * airAcceleration, 0));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Misc. Inputs
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        // Ground Check
        Vector2 gcOrigin = rb.position + (new Vector2(0, -0.55f));
        Vector2 gcSize = new Vector2(0.4f, 0.1f);
        int gcLayerMask = LayerMask.GetMask("Obstacles");
        RaycastHit2D groundCheck = Physics2D.BoxCast(gcOrigin, gcSize, 0, Vector2.zero, 0, gcLayerMask, 0);
        if (groundCheck && groundCheck.transform.tag == "Wall")
        {
            grounded = true;
        }
        else
        {
            canJump = false;
            grounded = false;
        }

        // Jump Check
        if (rb.velocity.y <= 0 && grounded)
        {
            canJump = true;
        }
    }

    private void LimitVelocity()
    {
        // Clamp horizontal speed.
        if (Mathf.Abs(rb.velocity.x) > maxSpeed)
        {
            SetVelocityX(maxSpeed * Mathf.Sign(rb.velocity.x));
        }
    }

    private void SetVelocityX(float vx)
    {
        rb.velocity = new Vector2(vx, rb.velocity.y);
    }

}
