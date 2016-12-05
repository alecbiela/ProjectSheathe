using UnityEngine;
using System.Collections;

public class Explosion : MonoBehaviour {

    public float explodeTime;
    private float timer;
    public float slowFactor;
    public int id;
    public bool isTrigger;
    public bool canHurtEnemies = false;

	// Use this for initialization
	void Start () {
        timer = explodeTime;
        id = this.gameObject.GetInstanceID();
        isTrigger = true;
	}
	
	// Update is called once per frame
	void Update () {
	    if (this.gameObject.activeSelf)
        {
            if (timer <= 0 && isTrigger)
            {
                isTrigger = false;
            }
            else if (timer <= 0)
            {
                Destroy(this.gameObject);
            }
            else
            {
                timer -= Time.deltaTime;
            }
        }
	}
}
