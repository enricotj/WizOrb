using System.Collections.Generic;
using UnityEngine;

public class OrbController : MonoBehaviour
{
    private const float HOLD_RANGE = 0.5f;
    private enum OrbState
    {
        Hold,
        Pull,
        Push
    }
    private OrbState state = OrbState.Hold;
    private bool colliding = false;
    private float maxSpeed = 4f;
    private float normalDotForce;
    private Rigidbody2D rb;
    private GameObject player;
    private Rigidbody2D playerRB;
    private Camera cam;
    private Vector2 inputDir;
    private Vector2 targetPosition;
    private Vector2 targetPositionDir;
    private Vector2 force;
    private Vector2 contactNormal;
    private Dictionary<int, Vector2> contactNormals = new Dictionary<int, Vector2>();

    private const float MAX_SYSTEM_ENERGY = 60f;
    private const float MIN_SYSTEM_ENERGY = 5f;

    private bool focusMode = false;
    private bool autoHold = false;

    // Use this for initialization
    void Start()
    {
        player = GameObject.Find("Player");
        playerRB = player.GetComponent<Rigidbody2D>();
        rb = this.GetComponent<Rigidbody2D>();
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
    }

    void Update()
    {
        bool oldAutoHold = autoHold;
        if (Input.GetButtonDown("Pull"))
        {
            autoHold = !autoHold;
        }

        focusMode = Input.GetButton("Focus");

        if (!autoHold && oldAutoHold)
        {
            state = OrbState.Push;
        }

        inputDir = (new Vector2(Input.GetAxis("Horizontal2"), Input.GetAxis("Vertical2"))).normalized;
        if (Input.GetMouseButton(0))
        {
            Vector2 positionOnScreen = cam.GetComponent<Camera>().WorldToViewportPoint(player.transform.position);
            if (focusMode)
            {
                positionOnScreen = cam.GetComponent<Camera>().WorldToViewportPoint(this.transform.position);
            }
            Vector2 mouseOnScreen = cam.GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);
            inputDir = (mouseOnScreen - positionOnScreen).normalized;
        }

        if (inputDir != Vector2.zero || !autoHold || focusMode)
        {
            state = OrbState.Push;
            targetPosition = rb.position + (inputDir * 5);
            Debug.DrawLine(rb.position, rb.position + inputDir * 5, Color.white);
        }
        else if (((rb.position - playerRB.position).magnitude > HOLD_RANGE) && (state != OrbState.Hold))
        {
            state = OrbState.Pull;
            targetPosition = playerRB.position;
            Debug.DrawLine(rb.position, targetPosition, Color.green);
        }
        else if (((rb.position - playerRB.position).magnitude <= HOLD_RANGE) && (state == OrbState.Pull))
        {
            state = OrbState.Hold;
            rb.velocity = Vector2.zero;
        }
        else
        {
            targetPosition = rb.position;
        }
        targetPositionDir = (targetPosition - rb.position).normalized;

        // Calculate average normal vector of all contact points
        int numContacts = contactNormals.Keys.Count;
        contactNormal = Vector2.zero;
        if (numContacts > 0)
        {
            colliding = true;
            foreach (int id in contactNormals.Keys)
            {
                contactNormal += contactNormals[id];
            }
            contactNormal /= numContacts;
            contactNormal.Normalize();
        }
        else
        {
            colliding = false;
        }
        
        //normalDotForce = Vector2.Dot(contactNormal, targetPositionDir);

        if (targetPositionDir != Vector2.zero)
        {
            force = BlackMagic();
        }
        else
        {
            force = Vector2.zero;
        }

        // Debug lines
        Debug.DrawLine(rb.position, rb.position + contactNormal, Color.red);
        Debug.DrawLine(rb.position, rb.position + targetPositionDir, Color.cyan);

        if (state == OrbState.Hold)
        {
            rb.MovePosition(Vector2.Lerp(rb.position, playerRB.position, 0.5f));
        }

        if (focusMode && rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    void FixedUpdate()
    {
        //if (Input.GetAxis("Pull") > 0)
        //{
        //    targetPositionDir = playerRB.position - rb.position;
        //    if (targetPositionDir.magnitude <= maxSpeed * Time.deltaTime)
        //    {
        //        rb.velocity = Vector2.zero;
        //        rb.MovePosition(playerRB.position);
        //    }
        //    else
        //    {
        //        rb.velocity = targetPositionDir.normalized * maxSpeed;
        //    }
        //    targetPositionDir.Normalize();
        //}


        switch (state)
        {
            case OrbState.Hold:
                break;
            default:
                rb.AddForce(force);
                playerRB.AddForce(force * -0.8f);
                break;
        }
    }
    void OnCollisionEnter2D(Collision2D coll)
    {
        int id = coll.gameObject.GetInstanceID();
        if (coll.gameObject.tag == "Wall" && !contactNormals.ContainsKey(id))
        {
            Vector2 normal = Vector3.zero;
            foreach (ContactPoint2D cp in coll.contacts)
            {
                normal += cp.normal;
            }
            normal /= coll.contacts.Length;
            normal = normal.normalized;

            contactNormals.Add(id, normal);
        }
    }

    private void OnCollisionExit2D(Collision2D coll)
    {
        int id = coll.gameObject.GetInstanceID();
        if (coll.gameObject.tag == "Wall" && contactNormals.ContainsKey(id))
        {
            contactNormals.Remove(id);
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
}
