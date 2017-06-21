using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour {

    public GameObject target;
    [Range(0, 1)]
    public float speed;
	
	// Update is called once per frame
	void Update ()
    {
        this.transform.position = Vector3.Lerp(this.transform.position, target.transform.position, 0.5f);
	}
}
