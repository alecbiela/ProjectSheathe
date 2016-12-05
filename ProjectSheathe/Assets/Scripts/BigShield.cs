using UnityEngine;
using System.Collections;

public class BigShield : MonoBehaviour {

    public bool playerInside = false;
    public int id;

	// Use this for initialization
	void Start () {
        id = this.gameObject.GetInstanceID();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
