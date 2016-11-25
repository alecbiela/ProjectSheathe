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
    private bool active = false;
    private System.Random rand = new System.Random();
    private GameObject deflectHitBox;
    public int health;
    public bool stunned; // if enemy is stunned
    public bool secondWind; // if enemy has received a second wind by having any enemy hit a player
    public GameObject Bullet;
    public Vector3 force = new Vector3(0, 0, 0);
    public bool deflected = false;
    public bool hitRecently;
    private bool trackPlayer;


    void Start() {
        Bullet = new GameObject();
        Player = GameObject.FindGameObjectWithTag("Player");
        Handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        Bullet.AddComponent<SpriteRenderer>();
        Bullet.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Bullet");
        Bullet.AddComponent<BoxCollider2D>();
        Bullet.GetComponent<BoxCollider2D>().isTrigger = true;
        Bullet.AddComponent<Rigidbody2D>();
        Bullet.GetComponent<Rigidbody2D>().gravityScale = 0;
        Bullet.tag = "Bullet";
        Bullet.layer = 11; // Bullets
        Bullet.transform.SetParent(this.transform);
        Bullet.SetActive(false);
        this.gameObject.tag = "Enemy";
        this.gameObject.layer = 9; // Enemies
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
        
        if (active)
        {
            Bullet.GetComponent<Rigidbody2D>().AddForce(force);
            if (Bullet.GetComponent<BoxCollider2D>().IsTouching(Player.GetComponent<BoxCollider2D>()))
            {
                Bullet.SetActive(false);
                active = false;
                //Bullet.GetComponent<SpriteRenderer>
            }            
        }/**/

        if (!stunned)
        {
            //++timer;
            if (trackPlayer)
            {//tracks the player
                vecToPlayer = (Player.transform.position - this.transform.position);
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

        //if(timer >=100)
        if (currFlashTime <= 0)
        {
            //Debug.Log("Bullet fired");
            Bullet.SetActive(true);
            active = true;
            this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
            Bullet.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            Vector3 temp = (Player.transform.position - this.transform.position).normalized * 4000 * Time.deltaTime * Handler.speedMod;
            force = temp;
            Bullet.transform.position = this.transform.position;
            attacking = false;
            trackPlayer = true;
            timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            deflected = false;
            timer = 0;
        }
    }
    public bool Deflected()
    {
        if (deflected && Bullet.GetComponent<BoxCollider2D>().IsTouching(this.GetComponent<BoxCollider2D>()))
        {
            return true;
        }
        return false;
    }
}
