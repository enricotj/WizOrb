using System.Collections.Generic;
using UnityEngine;

public class OrbController : MonoBehaviour
{
    private const float MAX_SYSTEM_ENERGY = 120;
    private const float MIN_SYSTEM_ENERGY = 5f;
    private const float HOLD_RANGE = 0.5f;
    private const float ORB_STICK = 1000f;
    private const float DISTANCE = 4.5f;

    public enum OrbState
    {
        Hold,
        Pull,
        Push
    }
    public OrbState state = OrbState.Hold;

    // components and other objects
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private Camera cam;
    private GameObject player;
    private Rigidbody2D playerRB;
    private TrailRenderer trail;

    private bool focusMode = false;
    private bool oldAutoHold = true;
    private bool autoHold = true;
    private bool autoHoldToggle = true;
    private bool stick = false;
    private bool stickToggle = false;
    public bool ignorePlatforms;
    private float maxSpeed = 4f;
    private float normalDotForce;
    private float springForce = 0;
    private Vector2 inputDir;
    private Vector2 targetPosition;
    private Vector2 targetPositionDir;
    private Vector2 force;
    private Vector2 contactNormal;
    private Vector2 holdOffset;
    private Vector2 playerVector;
    private Vector2 prevPosition;

    private float trailDisableTimer = 0;
    private float maxTrailTime;
    private float trailScale = 1;

    public float measurement = 0;

    [Header("Prefabs")]
    public GameObject visibleOrb;

    void Start()
    {
        player = GameObject.Find("Player");
        playerRB = player.GetComponent<Rigidbody2D>();
        rb = GetComponent<Rigidbody2D>();
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        circleCollider = GetComponent<CircleCollider2D>();
        trail = GetComponent<TrailRenderer>();
        maxTrailTime = trail.time;

        DistanceJoint2D distJoint = GetComponent<DistanceJoint2D>();
        distJoint.connectedBody = playerRB;
        distJoint.distance = DISTANCE;
        distJoint.maxDistanceOnly = true;

        GameObject visOrb = Instantiate(visibleOrb, transform.position, transform.rotation);
        visOrb.GetComponent<Follow>().target = gameObject;
        visOrb.GetComponent<Follow>().speed = 1;

        holdOffset = Vector2.zero;
    }

    void Update()
    {
        GetInputs();

        HandleOrbState();

        springForce = 0;
        float distToPlayer = (playerRB.position - rb.position).magnitude;
        if (distToPlayer > DISTANCE)
        {
            springForce = (distToPlayer - DISTANCE) * MAX_SYSTEM_ENERGY * 10;
        }

        prevPosition = rb.position;
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case OrbState.Hold:
                break;
            default:
                rb.AddForce(force);
                playerRB.AddForce(-force * 0.86f);
                break;
        }
    }

    private void GetInputs()
    {
        oldAutoHold = autoHold;
        if (Input.GetButtonDown("DragButton"))
        {
            autoHoldToggle = !autoHoldToggle;
        }
        autoHold = (Input.GetAxis("Drag") > 0 || Input.GetButton("DragAlt")) ?
            !autoHoldToggle : autoHoldToggle;

        if (Input.GetButtonDown("StickButton"))
        {
            stickToggle = !stickToggle;
        }
        stick = (Input.GetAxis("Stick") > 0 || Input.GetButton("StickShift")) ? 
            !stickToggle : stickToggle;
        
        inputDir = (new Vector2(Input.GetAxis("Horizontal2"), Input.GetAxis("Vertical2"))).normalized;

        if (autoHold && !oldAutoHold)
        {
            state = OrbState.Push;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 pos = focusMode ? rb.position : playerRB.position;
            Vector2 mouseInWorld = cam.GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition);
            inputDir = (mouseInWorld - pos).normalized;
        }
    }

    private void HandleOrbState()
    {
        if (inputDir != Vector2.zero || focusMode || !autoHold)
        {
            state = OrbState.Push;
            targetPosition = rb.position + (inputDir * 5);
        }
        else if ((rb.position - playerRB.position).magnitude > HOLD_RANGE && state != OrbState.Hold && state != OrbState.Pull)
        {
            state = OrbState.Pull;
            targetPosition = playerRB.position;
        }
        else if (state == OrbState.Pull)
        {
            targetPosition = playerRB.position;
            float dist = (targetPosition - rb.position).magnitude;
            if (dist <= (HOLD_RANGE * 3))
            {
                if (dist <= HOLD_RANGE || CheckLineSegmentAgainstCircle(prevPosition, rb.position, playerRB.position, HOLD_RANGE/2))
                {
                    state = OrbState.Hold;
                    rb.velocity = Vector2.zero;
                    SetFriction(0);
                }
                else
                {
                    rb.velocity = (targetPosition - rb.position).normalized * (rb.velocity.magnitude * 0.5f);
                }
            }
        }
        else if (state == OrbState.Hold)
        {
            targetPosition = playerRB.position;
            rb.velocity = Vector2.zero;
        }
        targetPositionDir = (targetPosition - rb.position).normalized;

        if (targetPositionDir != Vector2.zero)
        {
            force = BlackMagic();
        }
        else
        {
            force = Vector2.zero;
        }

        // clamp focus mode velocity
        if (focusMode && rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        if (state == OrbState.Hold)
        {
            holdOffset.x = Mathf.Sin(Time.time * 4f) * 0.15f;
            holdOffset.y = Mathf.Cos(Time.time * 2f) * 0.15f;
            // lerp the orb's position to be held by the player
            float playerVel = Mathf.Clamp(playerRB.velocity.magnitude, 0, player.GetComponent<PlayerController>().maxSpeed);
            rb.MovePosition(Vector2.Lerp(rb.position, playerRB.position + holdOffset + playerRB.velocity.normalized * 0.065f * playerVel, 0.4f));

            // ignore platforms when the player does
            ignorePlatforms = player.GetComponent<PlayerController>().ignoringPlatforms;
        }
        else
        {
            // detect platforms, ignore them if the orb is moving upwards
            float gcf = Mathf.Clamp(Mathf.Abs(rb.velocity.y) * Time.deltaTime, 1f, 50f);
            Vector2 gcSize = Vector2.right * 0.15f + Vector2.up * gcf;
            Vector2 gcOrigin = rb.position + Vector2.down * 0.05f + Vector2.down * gcf * 0.5f;
            int gcLayerMask = LayerMask.GetMask("Platform");
            RaycastHit2D groundCheck = Physics2D.BoxCast(gcOrigin, gcSize, 0, Vector2.zero, 0, gcLayerMask, 0);
            ignorePlatforms = rb.velocity.y > 0f && !groundCheck;
        }

        if (stick && state != OrbState.Hold)
        {
            SetFriction(ORB_STICK);
        }
        else
        {
            SetFriction(0);
        }

        trail.material.color = stick ? Color.magenta : Color.cyan;

        Physics2D.IgnoreLayerCollision(10, 11, ignorePlatforms);

        switch(state)
        {
            case OrbState.Hold:
                Timer.Increment(ref trailScale);
                trail.time = trail.time * trailScale / 2;
                break;
            default:
                trailScale = 2;
                trail.time = maxTrailTime;
                break;
        }
    }

    private void SetFriction(float f)
    {
        if (circleCollider.sharedMaterial.friction != f)
        {
            circleCollider.sharedMaterial.friction = f;
            circleCollider.enabled = false;
            circleCollider.enabled = true;
        }
    }

    private Vector2 BlackMagic()
    {
        float nextVelocity = 0;             // h
        float playerMass = playerRB.mass;   // a
        float orbMass = rb.mass;            // f
        float playerVelocity = Mathf.Abs(Vector2.Dot(playerRB.velocity, targetPositionDir.normalized)); // b
        float orbVelocity = Mathf.Abs(Vector2.Dot(rb.velocity, targetPositionDir.normalized));          // g

        float energy = focusMode ? MIN_SYSTEM_ENERGY : MAX_SYSTEM_ENERGY;

        // a*b^2*f - 2*a*b*f*g
        nextVelocity = playerMass*Mathf.Pow(playerVelocity, 2)*orbMass - 2*playerMass*playerVelocity*orbMass*orbVelocity;
        // ... + a*f*g^2 + 2*k*(a + f)
        nextVelocity += playerMass*orbMass*Mathf.Pow(orbVelocity, 2) + 2*energy*(playerMass + orbMass);
        // sqrt(a*f*...)
        nextVelocity = (Mathf.Sqrt(playerMass*orbMass*nextVelocity));
        // ... + a*b*f + f^2*g
        nextVelocity += playerMass*playerVelocity*orbMass + Mathf.Pow(orbMass, 2)*orbVelocity;
        // ... / (f * (a + f))
        nextVelocity /= orbMass*(playerMass + orbMass);

        Vector2 finalOrbForce = targetPositionDir * (nextVelocity - orbVelocity) * orbMass / Time.fixedDeltaTime;
        return finalOrbForce;
    }

    private void OnApplicationQuit()
    {
        circleCollider.sharedMaterial.friction = ORB_STICK;
    }

    private bool CheckLineSegmentAgainstCircle(Vector2 p1, Vector2 p2, Vector2 cen, float r)
    {
        float x1 = p1.x;
        float y1 = p1.y;
        float x2 = p2.x;
        float y2 = p2.y;
        float cx = cen.x;
        float cy = cen.y;
        x1 -= cx;
        y1 -= cy;
        x2 -= cx;
        y2 -= cy;
        float a = Mathf.Pow(x1, 2) + Mathf.Pow(x2, 2) - Mathf.Pow(r, 2);
        float b = 2*(x1*(x2 - x1) + y1*(y2 - y1));
        float c = Mathf.Pow((x2 - x1), 2) + Mathf.Pow((y2 - y1), 2);
        float disc = Mathf.Pow(b, 2) - 4*a*c;
        if(disc <= 0)
        {
            return false;
        }
        float sqrtdisc = Mathf.Sqrt(disc);
        float t1 = (-b + sqrtdisc)/(2*a);
        float t2 = (-b - sqrtdisc)/(2*a);
        return ((0 < t1 && t1 < 1) || (0 < t2 && t2 < 1));
    }
}
