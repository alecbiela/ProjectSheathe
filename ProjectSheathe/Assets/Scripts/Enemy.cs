using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {
    // Use this for initialization
    private GameObject Player;
    private Vector3 vecToPlayer = new Vector3(0, 0, 0);
    private bool firing = false;
    private int timer = 0;
    private const float FLASH_TIME = 1.1666f; // how long the enemy flashes for before they shoot
    private float currFlashTime = FLASH_TIME;
    private bool active = false;
    private System.Random rand = new System.Random();
    private GameObject deflectHitBox;
    public GameObject Bullet;
    public Vector3 force = new Vector3(0, 0, 0);
    public bool deflected = false;


    void Start () {
        Bullet = new GameObject();
        Player = GameObject.FindGameObjectWithTag("Player");
        Bullet.AddComponent<SpriteRenderer>();
        Bullet.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Bullet");
        Bullet.AddComponent<BoxCollider2D>();
        Bullet.GetComponent<BoxCollider2D>().isTrigger = true;
        Bullet.AddComponent<Rigidbody2D>();
        Bullet.GetComponent<Rigidbody2D>().gravityScale = 0;
        Bullet.transform.SetParent(this.transform);
        Bullet.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
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
        ++timer;
        if (!firing)
        {//tracks the player
            vecToPlayer = (Player.transform.position - this.transform.position);
            float angle = Mathf.Atan2(vecToPlayer.y, vecToPlayer.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            this.transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * 0.9f); //Quaternion.LookRotation(this.transform.position - Player.transform.position);
            if (timer > 400) // fire first at 600 frames
            {
                Fire(); //firing = true; // for purposes of this build, call Shoot here. After this build, when an enemy shoots will be determined by EnemyHandler.
            }
        }
    }
    public void Fire()
    {
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
            }
        }
        else
        {
            this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // white
        }

        //if(timer >=100)
        if (currFlashTime <= 0)
        {
            Bullet.SetActive(true);
            active = true;
            this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
            Bullet.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            Vector3 temp = (Player.transform.position - this.transform.position).normalized * 5000 * Time.deltaTime;
            force = temp;
            Bullet.transform.position = this.transform.position;
            firing = false;
            timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            deflected = false;
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
