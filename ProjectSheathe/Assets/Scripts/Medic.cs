using UnityEngine;
using System.Collections;

public class Medic : MonoBehaviour {

    private Enemy thisMedic;
    private float timer;
    private EncounterManager em;

	// Use this for initialization
	void Start () {
        em = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        timer = 0;
        thisMedic = this.GetComponentInParent<Enemy>();
	}
	
	// Update is called once per frame
	void Update () {
        timer += Time.deltaTime * em.speedMod;

        //fire if longer than 1 sec
        if(timer >= 1)
        {
            thisMedic.Fire();
            timer = 0;
        }
	}
}
