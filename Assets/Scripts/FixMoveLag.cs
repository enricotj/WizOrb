using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixMoveLag : MonoBehaviour
{
    public float scale = 1;
    public float lerp = 0.5f;

    private Vector3 pos;
    private Vector3 oldOrbPos;
    private Vector3 vel;
    private GameObject orb;

    void Start()
    {
        pos = transform.localPosition;
        vel = Vector3.zero;
        orb = GameObject.FindGameObjectWithTag("Orb");
        oldOrbPos = orb.transform.position;
    }

    // Update is called once per frame
    void LateUpdate ()
    {
        vel = (orb.transform.position - oldOrbPos);
        vel.z = 0;
        if (vel.magnitude > 20f)
        {
            transform.localPosition = pos + vel.normalized * scale;
        }
        else
        {
            transform.localPosition = pos;
        }
        
        oldOrbPos = orb.transform.position;
	}
}
