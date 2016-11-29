using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {
    // Use this for initialization
    private GameObject Player;
    private EncounterManager Handler;
    private Vector3 vecToPlayer = new Vector3(0, 0, 0);
    public bool attacking { get; private set; }
    private int timer = 0;
    private const float FLASH_TIME = 1.1666f; // how long the enemy flashes for before they shoot
    private float currFlashTime = FLASH_TIME;
    private System.Random rand = new System.Random();
    private GameObject deflectHitBox;
    public int health;
    public bool stunned; // if enemy is stunned
    public bool secondWind; // if enemy has received a second wind by having any enemy hit a player
    public GameObject BulletPrefab;
    public Vector3 force = new Vector3(0, 0, 0);
    public bool hitRecently;
    private bool trackPlayer;


    void Start() {
        Player = GameObject.FindGameObjectWithTag("Player");
        Handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        health = 3;
        stunned = false;
        secondWind = false;
        hitRecently = false;
        attacking = false;
        trackPlayer = true;
        //Debug.Log("Enemy start called");
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(health);
        if(secondWind == false && health <= 1) // stun enemies once their health reaches a certain value
        {
            stunned = true;
            this.GetComponent<SpriteRenderer>().color = Color.black; // once you go black...
        }

        //...you may eventually go back
        if(secondWind == true)
        {
            stunned = false;
            this.GetComponent<SpriteRenderer>().color = Color.white;
            // add code here to give the enemy class it's special attributes if they have been lost. Like if the shield enemy has lost their shield, give it back.
        }

        //Looking for bullet movement?  It's in the bullet script now!

        if (!stunned)
        {
            //++timer;
            //if the enemy is currently monitoring the player
            if (trackPlayer)
            {
                vecToPlayer = (Player.transform.position - this.transform.position);    //this is correct - the bullet fires on this path and it's directly into the character
                float angle = Mathf.Atan2(vecToPlayer.y, vecToPlayer.x) * Mathf.Rad2Deg;
                Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
                this.transform.rotation = Quaternion.Slerp(transform.rotation, q, Handler.speedMod * Time.deltaTime * 0.9f); //Quaternion.LookRotation(this.transform.position - Player.transform.position);
            }

            if (timer != 0) // fire first at 600 frames
            {
                Fire(); //firing = true; // for purposes of this build, call Shoot here. After this build, when an enemy shoots will be determined by EncounterManager.
                //Debug.Log("Fire call");
            }
        }
    }

    //Called when the enemy is going to fire a bullet
    public void Fire()
    {
        timer++;
        //Debug.Log(timer);
        attacking = true;
        currFlashTime = currFlashTime - Time.deltaTime;
        if (timer % 3 == 0) // flash
        {
            if (currFlashTime > .20)
            {
                this.GetComponent<SpriteRenderer>().color = new Color(255, 200, 0); // yellow/gold
            }
            else // flash red just before firing
            {
                this.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0); // red
                trackPlayer = false;
            }
        }
        else
        {
            this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // white
        }

        //we fire the actual bullet here
        if (currFlashTime <= 0)
        {
            //Debug.Log("Bullet fired");

            //instantiate a new bullet prefab at this location
            GameObject newBullet = (GameObject)Instantiate(BulletPrefab);
            newBullet.GetComponent<Bullet>().Initialize(this.transform.position, vecToPlayer);  //uses the enemy's last known location of player

            //after attacking, reset color and flags
            this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
            attacking = false;
            trackPlayer = true;

            timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            timer = 0;
        }
    }

    //Handles collision of enemy with other objects
    void OnTriggerEnter2D(Collider2D col)
    {
        //if a bullet is hitting it
        if(col.tag == "Bullet")
        {
            //Debug.Log("Bullet is hitting enemy now");
            if(col.gameObject.GetComponent<Bullet>().CanHurtEnemies)
            {
                //destroy the bullet and subtract enemy health or do whatever you gotta do
                health--;
                Destroy(col.gameObject);
            }
        }

        //if it's a slice hitbox
        if (col.tag.Contains("SliceHitbox"))
        {
            Debug.Log("Hit by slice");

            if (!this.hitRecently)
            {
                health--;
                this.hitRecently = true;
            }

            return;
        }

        //if it's a basic attack hitbox
        if(col.tag.Contains("BAHitbox"))
        {
            if(!this.hitRecently)
            {
                if (Player.GetComponent<Character>().Overclocking) health -= 2;
                else health--;
                this.hitRecently = true;
            }

            return;
        }
    }
}
