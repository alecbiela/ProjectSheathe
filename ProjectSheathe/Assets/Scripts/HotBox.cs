using UnityEngine;
using System.Collections;

class HotBox : Enemy
{
    /* Special Ability Variables */
    private GameObject dome;
    private const float COOLDOWN = 30.0f;
    private const float ACTIVE = 2.0f;
    private float cooldown;
    private float active;

    // Use this for initialization
    protected override void Start()
    {
        type = "HotBox";
        rank = "Officer";
        cooldown = COOLDOWN;
        active = 50;
        dome = transform.GetChild(0).gameObject;
        dome.SetActive(false);
        trackPlayer = false;
    }

    public override void Fire()
    {
        dome.SetActive(true);
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (vecToPlayer.magnitude <= 3.5f)
        {
            Fire();
            Debug.Log("You made me ink");
        }

        cooldown -= Time.deltaTime * Handler.speedMod;
        if (cooldown <= 0)
        {
            Fire();
            //Dome.SetActive(true);
            cooldown = COOLDOWN;
            active = ACTIVE;
            GetComponentInChildren<SpriteRenderer>().color = Color.cyan;
            Debug.Log("In here");
        }
            if (active>0)
        {
            active -= Time.deltaTime * Handler.speedMod;
        }
        else
        {
            dome.GetComponent<SpriteRenderer>().color = Color.clear;
            dome.SetActive(false);
        }
    }
    //void OnTriggerEnter2D(Collider2D other) realized this was overriding the Enemy's ontrigger enter
    //{
            
    //    if (cooldown <= 0 && other.tag == "player")
    //    {
    //        thisBox.Fire();
    //        //Dome.SetActive(true);
    //        cooldown = COOLDOWN;
    //        active = ACTIVE;
    //        GetComponentInChildren<SpriteRenderer>().color = Color.cyan;
    //    }
    //}
}
