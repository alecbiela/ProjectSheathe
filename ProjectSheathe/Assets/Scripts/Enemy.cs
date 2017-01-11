using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Enemy : MonoBehaviour {

    /* Other Object Variables */
    protected GameObject Player;
    private GameObject deflectHitBox;
    protected EncounterManager Handler;
    protected GameObject hitSpark;
    protected System.Random rand = new System.Random(); // Random for random things

    /* Movement and Health Variables */
    protected Vector3 vecToPlayer = new Vector3(0, 0, 0);
    [HideInInspector] public int health;
    [SerializeField] protected int MAX_HEALTH;
    [HideInInspector] public int stunState; // Stun state enum: 0=unstunned, 1=secondwinded, 2=stunned
    protected bool trackPlayer; // Whether the enemy should be rotating to face the player
    protected float rotSpeed = 1f;
    public bool hitRecently; // Prevents multiple hits from player in the same attack
    [HideInInspector] public float slowMod = 0; // Slow factor for things like the SLOB AOE slow
    protected List<Explosion> slowFields = new List<Explosion>(); // SLOB grenades currently inside of
    protected List<BigShield> bigShields = new List<BigShield>(); // Guardian shields this enemy is inside
    public bool guarded; // Is it covered by a shield without a player in it?
    //public Vector3 force = new Vector3(0, 0, 0); // Not sure what these are for, they are unused. If you know they are archaic, please CTRL+F and remove -- ATTN: ALEC/MJ/SIMON
    //public bool attacking { get; private set; }

    /* Bullets and Firing Variables */
    public GameObject BulletPrefab;
    protected float timer = 0; // Timer for next fire
    protected float FLASH_TIME = 1.1666f; // How long the enemy flashes for before they shoot
    protected float currFlashTime;
    protected float redFlashTime; // Time at which flash goes red

    /* Other */
    [HideInInspector] public string type;
    [HideInInspector] public string rank;

    /* Line Rendering Variables */
    [HideInInspector] public LineRenderer lineRendererComponent;
    protected float counter; // Timer to be able to do things as the line is kept on longer
    protected float dist; // Distance of the line
    protected Vector3 origin; // Start point of the line
    protected Vector3 destination; // End of the line
    protected float lineDrawSpeed = 600f; // Time it takes the line to go from origin to destination

    protected virtual void Start()
    {
        /* Other Objects */
        Player = GameObject.FindGameObjectWithTag("Player");
        Handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        hitSpark = GameObject.FindGameObjectWithTag("HitSpark"); // Particle system

        /* Movement and Health */
        health = MAX_HEALTH;
        stunState = 0; // Unstunned
        hitRecently = false;
        trackPlayer = true;

        /* Bullets and Firing */

        /* Line Renderer */
        currFlashTime = FLASH_TIME;
        redFlashTime = .2f;
        origin = transform.position;
        destination = Player.transform.position;
        lineRendererComponent = GetComponent<LineRenderer>();
        lineRendererComponent.SetPosition(0, origin);
        //lineRendererComponent.SetColors(Color.red, Color.red);
        lineRendererComponent.SetWidth(.15f, .15f);
        lineRendererComponent.enabled = false;
        dist = Vector3.Distance(origin, destination);
    }

    protected virtual void Stun() // Base stun logic
    {
        stunState = 2; // Stun
        //timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
        currFlashTime = FLASH_TIME;
        timer = 0;
        counter = 0;
        GetComponent<SpriteRenderer>().color = Color.black; // once you go black...
    }

    public virtual void SecondWind() // Call this where enemy takes secondwind
    {
        stunState = 1; // Secondwinded
        GetComponent<SpriteRenderer>().color = Color.white;
        // Override to give the enemy class its special attributes if they have been lost.
    }
    
    public virtual void Fire() // Fires a bullet or flashes the warning
    {   // It's more code to do it this way, but it's slightly faster and is 5x more readable
        timer++; // Alternatively we could repeat these two lines in every inherited Fire() and put the basic fire here to be completely overridden by all but 3 enemies
        currFlashTime = currFlashTime - Time.deltaTime;
    }

    public virtual void Destructor() // Clears/Deletes the special ability (or any other delete logic that may need to be added)
    {

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (hitRecently && Handler.PlayerScript.BAState < 2 && Handler.PlayerScript.SliceState < 2) // Let enemy get hit again if original attack is over
        {
            //Debug.Log("Unhit");
            hitRecently = false;
        }
        lineRendererComponent.enabled = false;
        //Debug.Log(health);
        if (stunState == 0 && health <= 1) // Stun enemies once their health reaches a certain value if they have never been stunned
        {
            Stun();
        }

        if (stunState != 2)
        {
            if (trackPlayer)
            {
                vecToPlayer = (Player.transform.position - transform.position);    //this is correct - the bullet fires on this path and it's directly into the character
                float angle = Mathf.Atan2(vecToPlayer.y, vecToPlayer.x) * Mathf.Rad2Deg;
                Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
                if (type == "Guardian") transform.rotation = Quaternion.Slerp(transform.rotation, q, (Handler.speedMod - slowMod) * Time.deltaTime * 0.1f * rotSpeed); // It's only one, so this is fine
                else transform.rotation = Quaternion.Slerp(transform.rotation, q, (Handler.speedMod-slowMod) * Time.deltaTime * 0.9f * rotSpeed); //Quaternion.LookRotation(transform.position - Player.transform.position);
                // line render
                origin = transform.position;
                destination = GetComponentInParent<Enemy>().Player.transform.position; // don't update this here if the enemy ever needs to draw a line to somewhere else and the player
                dist = Vector3.Distance(origin, destination);
            }

            if (timer != 0 && type !="HotBox") // fire first at 600 frames// What? Do we still do this?
            {
                Fire();
            }
        }

        for (int i = 0; i < slowFields.Count; i++) // Check the slowing fields
        {
            if (!slowFields[i].isTrigger)
            {
                slowFields.RemoveAt(i);
                i--;
            }
        }
        if (slowMod > 0 && slowFields.Count == 0)
        {
            slowMod = 0;
        }

        if (bigShields.Count > 0) guarded = false; // Check guardian protection status
        for (int i = 0; i < bigShields.Count; i++)
        {
            if (bigShields[i] == null) // A shield went away
            {
                bigShields.RemoveAt(i);
                i--;
            }
            else if (!bigShields[i].playerInside) // Player is inside this shield
            {
                guarded = true; // I inverted this: before if the p was in one of the shields, it could hit through all the ones the enemy was in
            }
        }
        if (guarded && bigShields.Count == 0) // No shields left
        {
            guarded = false;
        }
    }

    // Handles collision of enemy with other objects
    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        //Debug.Log("Trigger");
        if (col.gameObject.layer == 13) // Slow grenade
        {
            if (col.gameObject.GetComponent<Explosion>().canHurtEnemies) // Explodes on contact and has field
            {
                if (slowMod <= 0) slowMod = col.gameObject.GetComponent<Explosion>().slowFactor;
                slowFields.Add(col.gameObject.GetComponent<Explosion>());
            }
            return;
        }

        if (col.gameObject.tag == "BigShield") // Add a shield if it is entered
        {
            bigShields.Add(col.gameObject.GetComponent<BigShield>());
            return;
        }
        
        if(col.gameObject.tag == "MedicBullet" && stunState == 2) // Medic bullets heal and apply second wind // used to be stunState != 2
        {
            // Call secondwind here as desired
            health = (health >= MAX_HEALTH) ? MAX_HEALTH : (health + 1); // If topped off, don't add any
            //stunState = 1; // Secondwinded
            SecondWind();
            Destroy(col.gameObject);
            //Debug.Log("Medic bullet received by enemy");
            return;
        }

        if (!guarded) // Take the hit if unguarded
        {
            //Debug.Log("Hit"); // FOR SOME REASON THIS METHOD IS NOT BEING TRIGGERED ON BA WHEN ENEMIES ARE STUNNED-- PLZ INVESTIGATE
            if (col.tag.Contains("SliceHitbox")) // Slice
            {
                if (!hitRecently)
                {
                    health--;
                    hitRecently = true;
                }
                hitSpark.GetComponent<ParticleSystem>().Emit(2);
                return;
            }

            if (col.tag.Contains("BAHitbox")) // Basic Attack
            {
                //Debug.Log("BA Hit");
                if (!hitRecently)
                {
                    //Debug.Log("Enemy Took Damage");
                    if (Player.GetComponent<Character>().OverclockState == 4) health -= 2; // If OC active
                    else health--;
                    hitRecently = true;
                }
                hitSpark.GetComponent<ParticleSystem>().Emit(2);
                return;
            }

            if (col.tag == "Bullet" || col.tag == "SlowBullet") // If a bullet is hitting it ( ͡° ͜ʖ ͡°)
            {
                if (col.gameObject.GetComponent<Bullet>().CanHurtEnemies)
                {
                    // Destroy the bullet and subtract enemy health or do whatever you gotta do
                    health--;
                    Destroy(col.gameObject);
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "BigShield") // Leave the shield
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
                guarded = false;
            }
        }
    }
}
