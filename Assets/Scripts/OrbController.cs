using System.Collections.Generic;
using UnityEngine;

public class OrbController : MonoBehaviour
{
    private bool colliding = false;
    private float forceScale = 20;
    private float maxSpeed = 50f;
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
        inputDir = (new Vector2(Input.GetAxis("Horizontal2"), Input.GetAxis("Vertical2"))).normalized;
        if (Input.GetMouseButton(0))
        {
            Vector2 positionOnScreen = cam.GetComponent<Camera>().WorldToViewportPoint(transform.position);
            Vector2 mouseOnScreen = cam.GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);
            inputDir = (mouseOnScreen - positionOnScreen).normalized;
        }

        targetPosition = ((Vector2)transform.position) + (inputDir * maxSpeed * Time.deltaTime);
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
        
        if (Input.GetAxis("Pull") > 0)
        {
            //Vector2 newPos = Vector2.MoveTowards(rb.position, playerRB.position, maxSpeed * Time.deltaTime);
            //rb.velocity = Vector2.zero;
            //rb.MovePosition(newPos);
            targetPositionDir = playerRB.position - rb.position;
            if (targetPositionDir.magnitude <= maxSpeed * Time.deltaTime)
            {
                rb.velocity = Vector2.zero;
                rb.MovePosition(playerRB.position);
            }
            else
            {
                rb.velocity = targetPositionDir.normalized * maxSpeed;
            }
            targetPositionDir.Normalize();
        }

        normalDotForce = Vector2.Dot(contactNormal, targetPositionDir);
        force = targetPositionDir * forceScale;

        // Debug lines
        Debug.DrawLine(rb.position, rb.position + contactNormal, Color.red);
        Debug.DrawLine(rb.position, rb.position + targetPositionDir, Color.cyan);
    }

    void FixedUpdate()
    {
        // add force to the orb
        if (inputDir != Vector2.zero)
        {
            rb.AddForce(force);
        }

        // only add force to the player if the orb is colliding with a wall
        if ((inputDir != Vector2.zero || (Input.GetAxis("Pull") > 0)) &&
            colliding &&
            (normalDotForce < 0 || Input.GetAxis("Pull") > 0))
        {
            Vector2 orbDir = (playerRB.position - rb.position).normalized;
            float angularDot = Mathf.Clamp(Mathf.Abs(Vector2.Dot(orbDir, force.normalized)) * 1.5f, 0, 1);
            playerRB.AddForce(force * normalDotForce * 5);
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
}
