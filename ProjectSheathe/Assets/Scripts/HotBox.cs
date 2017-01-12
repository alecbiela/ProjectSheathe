using UnityEngine;
using System.Collections;

class HotBox : Enemy
{
    /* Special Ability Variables */
    private GameObject dome;
    private bool enterCD;
    private const float COOLDOWN = 6.0f;
    private const float STARTUP = .3f;
    private const float ACTIVE = 2.0f; // used to be 2
    private float cooldown;
    private float currentFrames;

    // Use this for initialization
    protected override void Start()
    {
        health = MAX_HEALTH;
        stunState = 0; // Unstunned
        hitRecently = false;
        trackPlayer = true;
        type = "HotBox";
        rank = "Officer";
        cooldown = 0;
        enterCD = false;
        currentFrames = 0;
        dome = transform.GetChild(0).gameObject;
        dome.SetActive(false);
        Player = GameObject.FindGameObjectWithTag("Player");
        Handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        hitSpark = GameObject.FindGameObjectWithTag("HitSpark"); // Particle system
    }

    public override void Fire()
    {
        Debug.Log("Fire called");
        if (currentFrames <= ACTIVE)
        {
            Debug.Log("Attack active");
            dome.SetActive(true);
            dome.GetComponent<SpriteRenderer>().color = Color.red;
        }
        enterCD = true;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        Debug.Log(cooldown);
        //Debug.Log("Health: " + health);
        if (stunState == 0 && health <= 1) // Stun enemies once their health reaches a certain value if they have never been stunned
        {
            Stun();
        }
        if (hitRecently && Handler.PlayerScript.BAState < 2 && Handler.PlayerScript.SliceState < 2) // Let enemy get hit again if original attack is over
        {
            hitRecently = false;
        }

        if (cooldown <= 0)
        {
            if (stunState != 2) GetComponentInChildren<SpriteRenderer>().color = Color.white;

            // fire if player is near
            if (vecToPlayer.magnitude <= 3.5f && currentFrames <= 0)
            {
                Debug.Log("Fire once");
                currentFrames = STARTUP + ACTIVE;
                Fire();
                //Debug.Log("You made me ink: " + vecToPlayer.magnitude);
            }
        }
        else
        {
            cooldown -= Time.deltaTime * Handler.speedMod;
            if (stunState != 2) GetComponentInChildren<SpriteRenderer>().color = Color.yellow; // on CD indicator
        }

        // deplete frames
        if (currentFrames > 0)
        {
            currentFrames -= Time.deltaTime * Handler.speedMod;
            Fire();
        }
        else // reset cooldown and sprite
        {
            currentFrames = 0;
            if (enterCD == true)
            {
                Debug.Log("Cooldown has begun");
                cooldown = COOLDOWN;
                enterCD = false;
            }
            //dome.GetComponent<SpriteRenderer>().color = Color.clear;
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
