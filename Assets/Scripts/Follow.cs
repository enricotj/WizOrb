using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour {

    public GameObject target;
    [Range(0, 1)]
    public float speed;
    public bool ignoreZ = false;
	
	// Update is called once per frame
	void LateUpdate ()
    {
        Vector3 pos = target.transform.position;
        if (ignoreZ)
        {
            pos.z = this.transform.position.z;
        }
        this.transform.position = Vector3.Lerp(this.transform.position, pos, speed);
	}
}
