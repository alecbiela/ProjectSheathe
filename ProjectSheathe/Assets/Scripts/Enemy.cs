using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour {
    // Use this for initialization
    public GameObject Player;
    private EncounterManager Handler;
    private Vector3 vecToPlayer = new Vector3(0, 0, 0);
    public bool attacking { get; private set; }
    private int timer = 0;
    private float FLASH_TIME = 1.1666f; // how long the enemy flashes for before they shoot
    private float currFlashTime;
    private System.Random rand = new System.Random();
    private GameObject deflectHitBox;
    public int health;
    public int MAX_HEALTH;
    public bool stunned; // if enemy is stunned
    public bool unstunned; // Has enemy been saved by secondwind
    public bool secondWind; // if enemy has received a second wind by having any enemy hit a player
    public GameObject BulletPrefab;
    public Vector3 force = new Vector3(0, 0, 0);
    public bool hitRecently;
    private bool trackPlayer;
    private float rotSpeed = 1f;
    public GameObject special; // Laser for Light, shield for Lunk
    public float slowMod = 0;
    private List<Explosion> slowFields = new List<Explosion>();
    private List<BigShield> bigShields = new List<BigShield>();
    public bool guarded; // Covered by a shield without a player in it
    private bool active = true; //this enemy is actively doing its thang

    // Line rendering stuff
    public LineRenderer lineRendererComponent;
    private float counter;
    private float dist;
    public Vector3 origin;
    public Vector3 destination;
    public float lineDrawSpeed = 600f;
    //private int timer;
    public bool running;
    public string type;

    // Laser stuff
    private const float LASER_TIME = 2f; // Time laser is actively dealing damage
    private float fireTime; // Laser timer and is it firing
    private bool firing; 

    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        Handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        health = MAX_HEALTH;
        stunned = false;
        secondWind = false;
        hitRecently = false;
        attacking = false;
        trackPlayer = true;
        firing = false;
        unstunned = false;
        fireTime = 0f;
        if (BulletPrefab.tag == "Laser")
        {
            type = "Light";
            FLASH_TIME = 2.322222f; // MODIFY CHARGE FOR LASER HERE
            rotSpeed = 100f; // MAKE THIS LOW ENOUGH THAT OVERCLOCK AFFECTS LASER ENEMIES
            special = (GameObject)Instantiate(BulletPrefab);
            special.SetActive(false);
        }
        else if (this.transform.childCount > 0) // Shield as of now, can also be various other abilities for other types
        {
            special = this.transform.GetChild(0).gameObject;
            if (special.tag == "Shield")
            {
                type = "Lunk";
                special.SetActive(true);
            }
            else if (special.tag == "Grenade")
            {
                type = "SLOB";
                special.SetActive(false);
            }
            else if (special.tag == "BigShield")
            {
                type = "Guardian";
                special.SetActive(true);
                bigShields.Add(special.GetComponent<BigShield>()); // Is behind its own shield
            }
        }
        else if (BulletPrefab.tag == "MedicBullet")
        {
            type = "Medic";
        }
        else if (BulletPrefab.tag == "Rocket") type = "Lock";
        else type = "B451C";
        currFlashTime = FLASH_TIME;
        origin = this.transform.position;
        destination = GetComponentInParent<Enemy>().Player.transform.position;
        lineRendererComponent = GetComponent<LineRenderer>();
        lineRendererComponent.SetPosition(0, origin);
        //lineRendererComponent.SetColors(Color.red, Color.red);
        lineRendererComponent.SetWidth(.15f, .15f);
        lineRendererComponent.enabled = false;

        dist = Vector3.Distance(origin, destination);
        //Debug.Log("Enemy start called");
    }

    // Update is called once per frame
    void Update()
    {
        lineRendererComponent.enabled = false;
        //Debug.Log(health);
        if (!secondWind && !unstunned && health <= 1) // stun enemies once their health reaches a certain value
        {
            stunned = true;
            this.GetComponent<SpriteRenderer>().color = Color.black; // once you go black...
        }

        //...you may eventually go back
        if(secondWind)
        {
            stunned = false;
            this.GetComponent<SpriteRenderer>().color = Color.white;
            if (type == "Lunk" && !special.activeSelf) special.SetActive(true); // If shield is up// add code here to give the enemy class it's special attributes if they have been lost. Like if the shield enemy has lost their shield, give it back.
            secondWind = false; // No perma antistun or perma shield
            unstunned = true;
        }

        //Looking for bullet movement?  It's in the bullet script now!

        if (!stunned)
        {
            //medics will track enemies instead of player
            //variables could use some renaming, we'll talk about that later.
            if(type == "Medic" && trackPlayer)
            {
                //get a random stunned enemy
                List<Vector3> positions = Handler.stunnedEnemyPositions;
                if (positions.Count != 0)
                {
                    active = true;

                    Vector3 randomStunnedEnemy = Handler.stunnedEnemyPositions[rand.Next(Handler.stunnedEnemyPositions.Count)];
                    vecToPlayer = (randomStunnedEnemy - this.transform.position);    //this is correct - the bullet fires on this path and it's directly into the character
                    float angle = Mathf.Atan2(vecToPlayer.y, vecToPlayer.x) * Mathf.Rad2Deg;
                    Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
                    this.transform.rotation = Quaternion.Slerp(transform.rotation, q, (Handler.speedMod - slowMod) * Time.deltaTime * 0.9f * rotSpeed);

                    // line render
                    origin = this.transform.position;
                    destination = randomStunnedEnemy;
                    dist = Vector3.Distance(origin, destination);
                }
                else active = false;
            }
            else if (trackPlayer)
            {
                vecToPlayer = (Player.transform.position - this.transform.position);    //this is correct - the bullet fires on this path and it's directly into the character
                float angle = Mathf.Atan2(vecToPlayer.y, vecToPlayer.x) * Mathf.Rad2Deg;
                Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
                if (type == "Guardian") this.transform.rotation = Quaternion.Slerp(transform.rotation, q, (Handler.speedMod - slowMod) * Time.deltaTime * 0.1f * rotSpeed);
                else this.transform.rotation = Quaternion.Slerp(transform.rotation, q, (Handler.speedMod-slowMod) * Time.deltaTime * 0.9f * rotSpeed); //Quaternion.LookRotation(this.transform.position - Player.transform.position);

                // line render
                origin = this.transform.position;
                destination = GetComponentInParent<Enemy>().Player.transform.position; // don't update this here if the enemy ever needs to draw a line to somewhere else and the player
                dist = Vector3.Distance(origin, destination);
            }

            if (timer != 0) // fire first at 600 frames
            {
                Fire();
                //Debug.Log("Fire call");
            }
        }
        for (int i = 0; i < slowFields.Count; i++)
        {
            if (!slowFields[i].isTrigger)
            {
                slowFields.RemoveAt(i);
                i--;
            }
        }
        if (slowMod > 0 && slowFields.Count == 0)
        {
            //Debug.Log("unslowed");
            slowMod = 0;
        }

        if (bigShields.Count > 0) guarded = true; // Guardian stuff
        for (int i = 0; i < bigShields.Count; i++)
        {
            if (bigShields[i] == null)
            {
                bigShields.RemoveAt(i);
                i--;
            }
            else if (bigShields[i].playerInside)
            {
                Debug.Log("unguardedInside");
                guarded = false;
            }
        }
        if (guarded && bigShields.Count == 0)
        {
            Debug.Log("unguardedNoShields");
            guarded = false;
        }
    }

    //Called when the enemy is going to fire a bullet
    public void Fire()
    {
        if (!active) return;    //used for medics when no stunned enemies, but could apply to other enemy types later

        timer++;
        //Debug.Log(timer);
        attacking = true;
        currFlashTime = currFlashTime - Time.deltaTime;
        if (timer % 3 == 0) // flash
        {
            // render line

            //counter += .3f / lineDrawSpeed;
            counter += 1f;
            float x = Mathf.Lerp(0, dist, counter);
            Vector3 pointA = origin;
            Vector3 pointB = destination;
            Vector3 direction = Vector3.Normalize(pointB - pointA);
            Vector3 pointAlongLine;
            if (type == "Light") // Laser gets wider as it charges, and goes off infinitely
            {
                lineRendererComponent.SetWidth(.15f + counter * .005f, .15f + counter * .005f);
                pointAlongLine = direction * 1000;
            }
            else
            {
                // Get the unit vector in the desired direction, multiply by the desired length and add the starting point.
                pointAlongLine = x * direction + pointA;
            }
            
            lineRendererComponent.enabled = true;
            if (type == "SLOB") // Flash circle
            {
                special.transform.position = pointB;
                special.SetActive(true);
            }
            if (currFlashTime > .20)
            {
                //this.GetComponent<SpriteRenderer>().color = new Color(255, 200, 0); // yellow/gold
                if (type == "Light")
                {
                    lineRendererComponent.SetColors(new Color(255, 153, 0, 128 + counter/2), new Color(255, 1153, 0, 128 + counter/2)); // Red + more opaque as charged
                }
                else if(type == "Medic")
                {
                    lineRendererComponent.SetColors(new Color(0, 155, 0, 172), new Color(0, 155, 0, 172));  //Medics flash green
                }
                else lineRendererComponent.SetColors(new Color(242, 190, 0, 172), new Color(242, 190, 0, 172)); // Yellow (For basic enemies)

                if (type == "SLOB") special.GetComponent<SpriteRenderer>().color = new Color(242, 190, 0, 172);
                lineRendererComponent.SetPosition(1, pointAlongLine);
            }
            else // flash red just before firing
            {
                //this.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0); // red
                lineRendererComponent.SetColors(Color.red, Color.black);
                if (type == "SLOB") special.GetComponent<SpriteRenderer>().color = Color.red;
                lineRendererComponent.SetPosition(1, pointAlongLine);
                trackPlayer = false;
            }
        }
        else
        {
            if (type == "SLOB") special.SetActive(false); // Flash circle as well
            this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // white
            lineRendererComponent.enabled = false;
        }

        //we fire the actual bullet here
        if (currFlashTime <= 0)
        {
            //Debug.Log("Bullet fired");

            if (type == "Light") // Fire laser
            {
                Vector3 pointA = origin;
                lineRendererComponent.enabled = true;
                lineRendererComponent.SetWidth(.43f, .43f);
                lineRendererComponent.SetColors(Color.red, Color.black);
                lineRendererComponent.SetPosition(1, (Vector3.Normalize(vecToPlayer) * 1000) + pointA); // Fire solidly in the direction of fire

                if (firing && fireTime <= 0) // End laser
                {
                    //after attacking, reset color and flags
                    //Debug.Log("End Laser");
                    special.SetActive(false);
                    //this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                    attacking = false;
                    trackPlayer = true;
                    firing = false;
                    //LineRenderers[lrInUse].GetComponent<DrawLine>().running = false;
                    //LineRenderers[lrInUse].GetComponent<DrawLine>().lineRendererComponent.enabled = false;

                    timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
                    currFlashTime = FLASH_TIME;
                    timer = 0;
                    counter = 0;
                }
                else if (!firing) // Start firing
                {
                    //Debug.Log("Fire Laser");
                    firing = true;
                    fireTime = LASER_TIME;
                    special.SetActive(true);
                    special.transform.position = this.transform.position;
                    special.transform.rotation = this.transform.rotation;
                }
                else
                {
                    fireTime -= Time.deltaTime; // Keep firing
                }
            }
            else // Or bullet
            {
                //instantiate a new bullet prefab at this location
                GameObject newBullet = (GameObject)Instantiate(BulletPrefab);
                if (type == "Lock")
                {
                    newBullet.GetComponent<Rocket>().Initialize(this.transform.position, Player.transform);
                }
                else if (type == "SLOB")
                {
                    special.SetActive(false);
                    newBullet.GetComponent<Grenade>().Initialize(this.transform.position, Player.transform);
                }
                else
                {
                    //this case includes medic as well, since we are using vecToPlayer to point to the enemy
                    newBullet.GetComponent<Bullet>().Initialize(this.transform.position, vecToPlayer);  //uses the enemy's last known location of player
                }

                //after attacking, reset color and flags
                this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                attacking = false;
                trackPlayer = true;
                //LineRenderers[lrInUse].GetComponent<DrawLine>().running = false;
                //LineRenderers[lrInUse].GetComponent<DrawLine>().lineRendererComponent.enabled = false;

                timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
                currFlashTime = FLASH_TIME;
                timer = 0;
            }
        }
    }

    //Handles collision of enemy with other objects
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.layer == 13)
        {
            if (col.gameObject.GetComponent<Explosion>().canHurtEnemies)
            {
                //Debug.Log("Slowed");
                if (slowMod <= 0) slowMod = col.gameObject.GetComponent<Explosion>().slowFactor;
                slowFields.Add(col.gameObject.GetComponent<Explosion>());
            }
        }

        if (col.gameObject.tag == "BigShield")
        {
            bigShields.Add(col.gameObject.GetComponent<BigShield>());
        }

        //medic bullets heal and apply second wind
        if(col.gameObject.tag == "MedicBullet" && this.stunned)
        {
            health = (health >= MAX_HEALTH) ? MAX_HEALTH : (health + 1);    //futureproof for later, if we heal unstunned enemies
            secondWind = true;
            Destroy(col.gameObject);
            return;
        }

        if (!guarded)
        {
            //if it's a slice hitbox
            if (col.tag.Contains("SliceHitbox"))
            {
                //Debug.Log("Hit by slice");

                if (type == "Lunk" && special.activeSelf) // If shield is up
                {
                    special.SetActive(false);
                    this.hitRecently = true;
                }
                else if (!this.hitRecently)
                {
                    health--;
                    this.hitRecently = true;
                }

                return;
            }

            //if a bullet is hitting it
            if (col.tag == "Bullet" || col.tag == "SlowBullet")
            {
                //Debug.Log("Bullet is hitting enemy now");
                if (col.gameObject.GetComponent<Bullet>().CanHurtEnemies)
                {
                    //destroy the bullet and subtract enemy health or do whatever you gotta do
                    if (type == "Lunk" && special.activeSelf)  // If shield is up
                    {
                        // /Do nothing
                    }
                    else
                    {
                        health--;
                    }
                    Destroy(col.gameObject);
                }
            }

            //if it's a basic attack hitbox
            if (col.tag.Contains("BAHitbox"))
            {
                if (type == "Lunk" && special.activeSelf) { } // If shield is up
                else if (!this.hitRecently)
                {
                    if (Player.GetComponent<Character>().Overclocking) health -= 2;
                    else health--;
                    this.hitRecently = true;
                }

                return;
            }
        }
    }

    //if by chance we spawn on top of another enemy, die so we can respawn
    void OnCollisionEnter2D(Collision2D other)
    {
        /*if (other.gameObject.tag == "Enemy")
        {
            this.health = -1;
            Handler.Respawn(this.type.ToLower());
        }*/
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "BigShield")
        {
            foreach (BigShield s in bigShields)
            {
                if (other.GetComponent<BigShield>().id == s.id)
                {
                    bigShields.Remove(s);
                    break;
                }
            }
            if (bigShields.Count == 0)
            {
                Debug.Log("unguarded");
                guarded = false;
            }
        }
    }
}
