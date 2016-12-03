using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour
{

    public GameObject[] sliceHitBoxes;//setting all hitboxes to public, so enemyhandler can have access to them -Simon
    public GameObject[] baHitBoxes;//why are these called bahitboxes? basic attack?
    public GameObject deflectHitBox;
    private EncounterManager enemyHandler;

    Rigidbody2D rigidBody;
    [SerializeField] private float maxSpeed = 8f; // The fastest the player can travel in any direction
    [SerializeField] private float maxDashDist = 14f; // Uncanceled dash distance
    [SerializeField] private float dashRate = 1f; // Dash movement per frame
    [SerializeField] public float overclockMod { get; private set; } // Speed modifier for overcock
    const float SLICE_TIMESTEP = 0.5f;  //the time needed to activate each "Level" of slice hitbox

    //# of frames in animation / 60
    private const float SLICE_PRELOAD = 0.166f; // start-up frames
    private const float SLICE_ACTIVE = 0.11666f; // active frames // used to be .25
    private const float SLICE_AFTER = 0.166f; // recovery frames
    private const float BASIC_PRELOAD = 0.083f;
    private const float BASIC_ACTIVE = 0.133333f; // used to be .2
    private const float BASIC_AFTER = 0.083f;
    private const float DEFLECT_PRELOAD = 0.066f;
    private const float DEFLECT_ACTIVE = 0.5f;
    private const float DEFLECT_AFTER = 0.25f;
    private const float DASH_CD = .5833f; // Cooldown
    private const float OVERCLOCK_PRELOAD = 0.066f;
    private const float OVERCLOCK_ACTIVE = 3f; // Overclock needs active frames
    private const float OVERCLOCK_AFTER = 0.05f;
    private const float OVERCLOCK_CD = 20f; // cooldown should be around 20 seconds


    private float sliceHoldTime;
    private float sliceTimer; // Timers for frames
    private float baTimer;
    private float deflectTimer;
    private float overclockTimer;
    private float currDashDist; // Currently traveled distance of the dash
    private float dashCooldown; // Cooldown timer
    private float overclockCooldown;
    private float oldSpeed;
    private bool hitByLaser; // Tracks if being actively hit by laser

    private int sliceBoxes;
    public bool slowMovement;
    private bool[] inputFlags;

    //can look at these elsewhere (perhaps to disregard input?) but can only set in here
    public bool Slicing { get; private set; }
    public bool Deflecting { get; private set; }
    public bool Attacking { get; private set; }
    public bool Dashing { get; private set; }
    public bool Overclocking { get; private set; }
    public bool[] InputFlags { get { return inputFlags; } set { inputFlags = value; } }

    //private bools for key pressed, to prevent simultaneous inputs
    private bool sliceState;
    private bool deflectState;
    private bool baState;
    private bool overclockState;
    private bool timeSlow; // A third variable is needed to separate startup from ending in overclock

    [SerializeField] public int health;
    private bool hitRecently; // variable for future use with attacks that persist in the player's hurtbox
    public bool playerHit; // variable that tells the encounter manager the player has been hit
    public bool killStunnedEnemies;
    public int score;

    private void Awake()
    {
        rigidBody = this.GetComponentInParent<Rigidbody2D>();
        enemyHandler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        sliceHoldTime = 0;
        sliceTimer = 0;
        baTimer = 0;
        deflectTimer = 0;
        overclockTimer = 0;
        currDashDist = 0;
        dashCooldown = 0;
        overclockCooldown = -42;
        overclockMod = .7f;
        timeSlow = false;
        Overclocking = false;
        Dashing = false;
        Slicing = false; // Actually actively slicing
        sliceState = false; // Includes startup/charge and recovery frames
        deflectState = false;
        overclockState = false;
        baState = false;
        Attacking = false;
        Deflecting = false;
        sliceBoxes = 0;
        slowMovement = false;
        inputFlags = new bool[] { false, false, false, false, false, false, false }; // INPUT FLAGS, IN ORDER: SLICE[0], ATTACK[1], DEFLECT[2], INTERACT[6], OVERCLOCK[4], FIRE[5], DASH[6]
        health = 9;
        playerHit = false;
        killStunnedEnemies = false;
        score = 0;

        // Populate various arrays of gameobjects with their hitboxes
        // Add by name, so we know which is which
        sliceHitBoxes = new GameObject[6];
        for (int i = 0; i < 6; i++)
        {
            sliceHitBoxes[i] = GameObject.Find("SliceHitbox" + (i + 1));
            sliceHitBoxes[i].gameObject.SetActive(false);
        }

        baHitBoxes = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            baHitBoxes[i] = GameObject.Find("BAHitbox" + (i + 1));
            baHitBoxes[i].gameObject.SetActive(false);
        }

        deflectHitBox = GameObject.Find("DeflectHitbox");
        deflectHitBox.gameObject.SetActive(false);
    }

    public void controllerMove(float hMove, float vMove, float hLook, float vLook) // Movement and rotation with controller
    {
        //only move when they are not doing these actions
        if (!Attacking && !Deflecting && !Slicing)
        {   
            rigidBody.velocity = new Vector2(hMove * maxSpeed, vMove * maxSpeed);
        }

        if ((hLook != 0 || vLook != 0) && !Attacking && !Slicing)
        {
            float angle = Mathf.Atan2(vLook, hLook) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
        else if ((rigidBody.velocity != Vector2.zero) && !Attacking && !Slicing)
        {
            float angle = Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void keyboardMove(float hMove, float vMove, Vector3 mousePos) // Movement and rotation with keyboard and mouse
    {
        if (!Attacking && !Deflecting && !Slicing)   //only move when they are not doing these actions
            rigidBody.velocity = new Vector2(hMove * maxSpeed, vMove * maxSpeed);
        Vector3 playerPos = Camera.main.WorldToScreenPoint(rigidBody.transform.position);
        mousePos.x = mousePos.x - playerPos.x;
        mousePos.y = mousePos.y - playerPos.y;

        if (!Attacking && !Slicing)
        {
            float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }

    private void Update()
    {
        ProcessInput();
        ExecuteTimedActions();
    }

    private void ProcessInput() // Processes the current input that the character has
    {
        for (int i = 0; i < inputFlags.Length; i++)
        {
            if (i == 0 && sliceHoldTime > 0 && !inputFlags[0]) // Checks for slice release
            {
                if (Deflecting || Attacking || Slicing) continue;

                sliceBoxes = Mathf.FloorToInt(sliceHoldTime / SLICE_TIMESTEP);
                if (sliceBoxes > 5) sliceBoxes = 5;

                sliceTimer = SLICE_PRELOAD + SLICE_ACTIVE + SLICE_AFTER; // Start timer for slice mechanic
                sliceHoldTime = 0;
            }
            if (inputFlags[i])
            {
                switch (i)
                {
                    case 0: // Slicing (hasn't released button yet)
                        if (deflectState || baState || sliceState) continue;
                        //Debug.Log("Slice");
                        sliceState = true;
                        sliceHoldTime += Time.deltaTime;
                        break;
                    case 1: // Attacking
                        if (sliceState || deflectState || baState) continue;
                        //Debug.Log("Attack");
                        baState = true;
                        baTimer = BASIC_PRELOAD + BASIC_ACTIVE + BASIC_AFTER;
                        break;
                    case 2: // Deflecting
                        if (sliceState || baState || deflectState) continue;
                        //Debug.Log("deflect");
                        deflectState = true;
                        deflectTimer = DEFLECT_PRELOAD + DEFLECT_ACTIVE + DEFLECT_AFTER;
                        break;
                    case 3: // Interacting
                        break;
                    case 4: // Overclocking
                        if (!overclockState && overclockCooldown <= 0)
                        {
                            //Debug.Log("Press");
                            overclockState = true;
                            overclockTimer = OVERCLOCK_PRELOAD + OVERCLOCK_ACTIVE + OVERCLOCK_AFTER;
                        }
                        else if (overclockState)
                        {
                            //Debug.Log("Unpress");
                            Overclocking = false;
                        }
                        break;
                    case 5: // Firing
                        break;
                    case 6: // Dashing
                        if (Dashing && (Slicing || baState || deflectState)) // Cancel dash on other actions
                        {
                            //Debug.Log("Cancel");
                            Dashing = false; // Note that canceling dash with actions mapped to mouse buttons DOES NOT WORK on most touchpads because of system-wide accidental input suppression
                            currDashDist = 0;
                            dashCooldown = DASH_CD;
                            this.transform.GetComponent<SpriteRenderer>().color = Color.white;
                        }
                        else if (Slicing || baState || deflectState) continue; // Do not perform dash while doing other actions
                        else
                        {
                            if (currDashDist == 0 && dashCooldown <= 0 && rigidBody.velocity != Vector2.zero) // Only dash if moving
                            {
                                slowMovement = false;
                                Dashing = true;
                                //Debug.Log("Dash");
                                this.transform.GetComponent<SpriteRenderer>().color = Color.blue;
                            }
                            if (Dashing && currDashDist < maxDashDist)
                            {
                                currDashDist += dashRate;
                                rigidBody.transform.position += new Vector3(rigidBody.velocity.normalized.x * dashRate, rigidBody.velocity.normalized.y * dashRate, rigidBody.transform.position.z);
                            }
                            else if (currDashDist >= maxDashDist)
                            {
                                Dashing = false;
                                currDashDist = 0;
                                dashCooldown = DASH_CD;
                                //Debug.Log("End");
                                this.transform.GetComponent<SpriteRenderer>().color = Color.white;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            if (!inputFlags[6] && Dashing)
            {
                //Debug.Log("Release");
                Dashing = false;
                currDashDist = 0;
                dashCooldown = DASH_CD;
                this.transform.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }

    private void ExecuteTimedActions() // Executes any time-based actions (slicing, dashing, basic attacking, deflecting)
    {
        // Work the timers
        sliceTimer = sliceTimer <= 0 ? 0 : sliceTimer - Time.deltaTime; // If sT <= 0 then sT = 0, else = sT-dT
        deflectTimer = deflectTimer <= 0 ? 0 : deflectTimer - Time.deltaTime;
        baTimer = baTimer <= 0 ? 0 : baTimer - Time.deltaTime;
        overclockTimer = overclockTimer <= 0 ? 0 : overclockTimer - Time.deltaTime;

        if (baTimer <= BASIC_AFTER) // If attack is over, hitboxes go away
        {
            baHitBoxes[2].gameObject.SetActive(false);
        }
        else if (baTimer <= (BASIC_ACTIVE + BASIC_AFTER)) // Otherwise, attack (in a basic fashion)
        {
            if (baTimer <= (BASIC_ACTIVE / 3) + BASIC_AFTER) // Activating hitboxes based on where we are in the anim
            {
                baHitBoxes[2].gameObject.SetActive(true);
                baHitBoxes[1].gameObject.SetActive(false);
            }
            else if (baTimer <= (2 * BASIC_ACTIVE / 3) + BASIC_AFTER)
            {
                baHitBoxes[1].gameObject.SetActive(true);
                baHitBoxes[0].gameObject.SetActive(false);
            }
            else
            {
                baHitBoxes[0].gameObject.SetActive(true);
            }

            Attacking = true;
        }

        if (sliceTimer <= SLICE_AFTER) // If slice is over, hitboxes go away
        {
            for (int i = 0; i < 6; i++)
            {
                sliceHitBoxes[i].gameObject.SetActive(false);
            }
        }
        else if (sliceTimer <= (SLICE_ACTIVE + SLICE_AFTER)) // Otherwise, slice
        {
            sliceHitBoxes[0].gameObject.SetActive(true); // Always activate the first box

            for (int i = 0; i < sliceBoxes; i++) // Additional hitboxes
            {
                sliceHitBoxes[i + 1].gameObject.SetActive(true);
            }

            Slicing = true;
            maxSpeed = 7f;
            dashRate = .5f;
            maxDashDist = 3;
            slowMovement = false;
            //Debug.Log("Reset speed");
        }

        if (deflectTimer <= DEFLECT_AFTER) // If deflect is over, hitbox goes away
        {
            deflectHitBox.gameObject.SetActive(false);
        }
        else if (deflectTimer <= (DEFLECT_ACTIVE + DEFLECT_AFTER))// Otherwise, deflect
        {
            deflectHitBox.gameObject.SetActive(true);
            Deflecting = true;
        }

        if (sliceState) slowMovement = true;

        if (!Dashing && dashCooldown > 0) // Increment dash cooldown based on delta time
        {
            dashCooldown -= Time.deltaTime;
        }

        if (overclockState && !Overclocking && !timeSlow) // On activation
        {
            Overclocking = true;
            timeSlow = true;
            enemyHandler.speedMod -= overclockMod; // Slow enemies
            killStunnedEnemies = true;
            Debug.Log("ZA WARUDO: " + enemyHandler.speedMod);
        }
        else if ((!Overclocking && timeSlow) || (Overclocking && overclockTimer <= 0 && timeSlow)) // On end trigger or after ending frames
        {
            enemyHandler.speedMod += overclockMod;// Respeed enemies
            //if (enemyHandler.speedMod < 1)
            //{
            //    enemyHandler.speedMod = enemyHandler.baseSpeed;
            //}
            timeSlow = false;
            Overclocking = false;
            overclockState = false;
            overclockTimer = 0;
            overclockCooldown = OVERCLOCK_CD;
            Debug.Log("WRYYYYYY: " + enemyHandler.speedMod);
        }

        if (!Overclocking && overclockCooldown > 0) // Increment oveclock cooldown
        {
            overclockCooldown -= Time.deltaTime;
        }

        // Un-flag any abilities that are not on a timer, allowing the player to perform other actions
        if (deflectTimer == 0)
        {
            Deflecting = false;
            deflectState = false;
        }
        if (sliceTimer == 0)
        {
            if (slowMovement == true)
            {
                maxSpeed = 2f;
                dashRate = .3f;
                maxDashDist = 2;
                //Debug.Log("Lowered speed");
            }
            Slicing = false;
            sliceState = false;
        }
        if (baTimer == 0)
        {
            Attacking = false;
            baState = false;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == 11) // All bullet types, can use tag for specific actions based on bullet type
        {
            //Debug.Log("Player got hit");
            health--;
            Cancel(); // ends active attacks when hit. This may need to be commented out if we can't get the animations to stop too
                      //Debug.Log("Got em. Health: " + health);
                      //this.gameObject.GetComponent<SpriteRenderer>().color = Color.red;

            /*
            // Hitstop
            if (playerHit == false)
            {
                //float pauseDelay = .7f;
                float pauseDelay = 25.0f / 60.0f;
                //Time.timeScale = .0000001f;
                while (pauseDelay > 0)
                {
                    pauseDelay -= Time.deltaTime;
                    Debug.Log("hitstop");
                }
                //Debug.Log("Out");
                Time.timeScale = 1.0f;
            }
            */

            if (health <= 0)
            {
                health = 0;
                //Debug.Log("GAME OVER");
            }

            enemyHandler.SecondWind();

            //get rid of the bullet that was fired
            Destroy(other.gameObject);
            return;
        }
        else if (other.gameObject.layer == 12 && !hitByLaser) // Laser first hit
        {
            //Debug.Log("LASERED");
            health--;
            Cancel();
            if (health <= 0)
            {
                health = 0;
                //Debug.Log("GAME OVER");
            }
            hitByLaser = true;
            enemyHandler.SecondWind();
        }

    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == 12) hitByLaser = false;
    }


    void Cancel()
    {
        baTimer = 0;
        baHitBoxes[0].gameObject.SetActive(false);
        baHitBoxes[1].gameObject.SetActive(false);
        baHitBoxes[2].gameObject.SetActive(false);

        sliceTimer = 0;
    }

    public void Hitstop(float pauseDelay)
    {
        //float pauseDelay = .7f;
        pauseDelay /= 60.0f;
        Time.timeScale = .0000001f;
        while (pauseDelay > 0)
        {
            pauseDelay -= Time.deltaTime;
            //Debug.Log("hitstop");
            //GameObject.FindGameObjectWithTag("Hitspark").SetActive(true);
        }
        //Debug.Log("Out");
        Time.timeScale = 1.0f;
        //GameObject.FindGameObjectWithTag("Hitspark").SetActive(false);
    }
}
