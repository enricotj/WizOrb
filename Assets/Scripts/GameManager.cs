using UnityEngine;

public class GameManager: MonoBehaviour {

    public GameObject player;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
    }

    // Use this for initialization
    void Start ()
    {
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.position = Vector3.Lerp(transform.position, player.transform.position + (new Vector3(0, 0, -10)), 0.2f);
	}
}
