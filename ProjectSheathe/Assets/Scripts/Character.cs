using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
    /* General Variables */
    Rigidbody2D rigidBody;
    private GameObject blueScreenGlow;
    private EncounterManager enemyHandler;
    private bool[] inputFlags;
    public bool[] InputFlags { get { return inputFlags; } set { inputFlags = value; } }
    private Text scoreUIText;

    /* Health Variables */
    private const int SECONDWIND_HITS = 3; // Number of hits taken for second wind to activate
    private bool hitByLaser; // Tracks if being actively hit by laser
    private int hpHit; // Times player has been hit towards second wind activation
    [SerializeField] public int health;
    private bool hitByMedicBullet;
    [HideInInspector] public bool playerHit; // Tells the encounter manager the player has been hit
    [HideInInspector] public int score;
    private GameObject healthUIElement;

    /* Movement and Dash Variables */
    private float maxSpeed = 7f; // The fastest the player can travel in any direction
    private List<Explosion> slowFields = new List<Explosion>(); // SLOB fields the player is inside
    [HideInInspector] public float slowMod = 0; // Movement mod for effects (SLOB, etc)
    [HideInInspector] public bool slowMovement; // ******ALEC: look at using slowmod instead of this ************
    private float maxDashDist = 3f; // Uncanceled dash distance
    private float dashRate = .5f; // Dash movement per frame    
    private float currDashDist; // Currently traveled distance of the dash
    private float dashCooldown; // Cooldown timer
    private int dashState;
    public int DashState { get { return DashState; } } // Enum for state: 0=inactive, 1=cooldown, 2=startup, 3=active, 4=recovery
    // Times- (# of frames/60)
    private const float DASH_CD = .5833f; // Cooldown frames

    /* Basic Attack Variables */
    public GameObject[] baHitBoxes;
    private float baTimer; // Duration timer
    private int baState;
    public int BAState { get { return baState; } } // Enum for state: 0=inactive, 1=cooldown, 2=startup, 3=active, 4=recovery
    // Times- (# of frames/60)
    private const float BA_STARTUP = 0.083f; // Startup frames
    private const float BA_ACTIVE = 0.133333f; // Active frames
    private const float BA_RECOVERY = 0.083f; // Recovery frames

    /* Slice Variables */
    public GameObject[] sliceHitBoxes; // Setting all hitboxes to public, so enemyhandler can have access to them -Simon; Analyze this dependency - Trevor
    private const float SLICE_TIMESTEP = 0.5f;  // The time needed to activate each "Level" of slice hitbox    
    private float sliceHoldTime; // Charge timer
    private float sliceTimer; // Duration timer
    private int sliceBoxes; // Number of boxes activated in current slice
    private int sliceState;
    public int SliceState { get { return sliceState; } } // Enum for state: 0=inactive, 1=charge, 2=startup, 3=active, 4=recovery
    // Times- (# of frames/60)
    private const float SLICE_STARTUP = 0.166f; // Startup frames
    private const float SLICE_ACTIVE = 0.11666f; // Active frames
    private const float SLICE_RECOVERY = 0.166f; // Recovery frames

    /* Deflect Varibales */
    public GameObject deflectHitBox;
    private float deflectTimer; // Duration timer
    private int deflectState;
    public int DeflectState { get { return deflectState; } } // Enum for state: 0=inactive, 1=cooldown, 2=startup, 3=active, 4=recovery
    // Times- (# of frames/60)
    private const float DEFLECT_STARTUP = 0.066f; // Startup frames
    private const float DEFLECT_ACTIVE = 0.5f; // Active frames
    private const float DEFLECT_RECOVERY = 0.25f; // Recovery frames

    /* Overclock Variables */
    [SerializeField] public float overclockMod { get; private set; } // Game speed modifier for overclock
    private float overclockTimer; // Duration timer
    private float overclockCooldown; // Cooldown timer
    private Slider overclockCDUISlider; // UI elements
    private GameObject overclockReadyUIElement;
    [HideInInspector] public bool killStunnedEnemies; // Activates to kill enemies ***CONSIDER MOVING TO ENEMY MANAGER
    private int overclockState;
    public int OverclockState { get { return overclockState; } } // Enum for state: 0=inactive, 1=cooldown, 2=startup frame 1, 3=startup, 4=active, 5=recovery, 6=last recovery frame
    // Times- (# of frames/60)
    private const float OVERCLOCK_STARTUP = 0.066f; // Startup frames
    private const float OVERCLOCK_ACTIVE = 3f; // Active frames
    private const float OVERCLOCK_RECOVERY = 0.05f; // Recovery frames
    private const float OVERCLOCK_CD = 10f; // Cooldown frames

    /* Animation Variables */
    private Animator animator;
    private int redTimer;
    private GameObject hitSpark;
    private int sliceAnimTimer;

    private void Awake()
    {
        /* General Variables */
        rigidBody = GetComponentInParent<Rigidbody2D>();
        enemyHandler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        blueScreenGlow = GameObject.FindGameObjectWithTag("BlueScreenGlow");
        blueScreenGlow.SetActive(false);
        score = 0;
        inputFlags = new bool[] { false, false, false, false, false, false, false }; // INPUT FLAGS, IN ORDER: SLICE[0], ATTACK[1], DEFLECT[2], INTERACT[6], OVERCLOCK[4], FIRE[5], DASH[6]
        scoreUIText = GameObject.FindGameObjectWithTag("ScoreElement").GetComponent<Text>();
        setScore();

        /* Health Variables */
        health = 9;
        hitByMedicBullet = false;
        healthUIElement = GameObject.FindGameObjectWithTag("HealthElement");
        playerHit = false;
        killStunnedEnemies = false;
        hpHit = 0;
        setHealth();

        /* Movoement and Dash Variables */
        currDashDist = 0;
        dashCooldown = 0;
        slowMovement = false;

        /* Basic Attack Varables */
        baTimer = 0;
        baState = 0;
        baHitBoxes = new GameObject[3]; // Populate with hitboxes
        for (int i = 0; i < 3; i++)
        {
            baHitBoxes[i] = GameObject.Find("BAHitbox" + (i + 1));
            baHitBoxes[i].gameObject.SetActive(false);
        }

        /* Slice Variables */
        sliceHoldTime = 0;
        sliceTimer = 0;
        sliceState = 0;
        sliceBoxes = 0;
        sliceHitBoxes = new GameObject[6]; // Populate with hitboxes
        for (int i = 0; i < 6; i++)
        {
            sliceHitBoxes[i] = GameObject.Find("SliceHitbox" + (i + 1));
            sliceHitBoxes[i].gameObject.SetActive(false);
        }

        /* Deflect Variables */
        deflectTimer = 0;
        deflectState = 0;
        deflectHitBox = GameObject.Find("DeflectHitbox");
        deflectHitBox.gameObject.SetActive(false);

        /* Overclock Variables */
        overclockTimer = 0;
        overclockCooldown = 0;
        overclockMod = .7f;
        overclockState = 0;
        overclockCDUISlider = GameObject.FindGameObjectWithTag("OverclockCDElement").GetComponent<Slider>();
        overclockReadyUIElement = GameObject.FindGameObjectWithTag("ReadyElement");
        overclockReadyUIElement.SetActive(true);

        /* Animation Variables */
        animator = gameObject.GetComponent<Animator>();
        hitSpark = transform.GetChild(0).gameObject;
    }

    public void controllerMove(float hMove, float vMove, float hLook, float vLook) // Movement and rotation with controller
    {
        // Only move when they are not doing these actions
        if (baState < 2 && deflectState < 2 && sliceState < 2)
        {   
            rigidBody.velocity = new Vector2(hMove * maxSpeed * (1-slowMod), vMove * maxSpeed * (1-slowMod));
        }

        if ((hLook != 0 || vLook != 0) && baState < 2 && sliceState < 2)
        {
            float angle = Mathf.Atan2(vLook, hLook) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
        else if ((rigidBody.velocity != Vector2.zero) && baState < 2 && sliceState < 2)
        {
            float angle = Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void keyboardMove(float hMove, float vMove, Vector3 mousePos) // Movement and rotation with keyboard and mouse
    {
        if (baState < 2 && deflectState < 2 && sliceState < 2)   // Only move when they are not doing these actions
        {
            rigidBody.velocity = new Vector2(hMove * maxSpeed * (1 - slowMod), vMove * maxSpeed * (1 - slowMod));
        }
        Vector3 playerPos = Camera.main.WorldToScreenPoint(rigidBody.transform.position);
        mousePos.x = mousePos.x - playerPos.x;
        mousePos.y = mousePos.y - playerPos.y;

        if (baState < 2 && sliceState < 2) // Rotate
        {
            float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }

    private void Update()
    {
        ProcessInput();
        ExecuteTimedActions();
        // Enum for BA state: 0=inactive, 1=cooldown, 2=startup, 3=active, 4=recovery
        // Enum for Slice state: 0=inactive, 1=charge, 2=startup, 3=active, 4=recovery
        // Enum for state: 0=inactive, 1=cooldown, 2=startup frame 1, 3=startup, 4=active, 5=recovery, 6=last recovery frame
        //switch (overclockState) 
        //{
        //    case 0:
        //        //Debug.Log("inactive");
        //        break;
        //    case 1:
        //        Debug.Log("cooldown");
        //        break;
        //    case 2:
        //        Debug.Log("startup f1");
        //        break;
        //    case 3:
        //        Debug.Log("startup");
        //        break;
        //    case 4:
        //        Debug.Log("active");
        //        break;
        //    case 5:
        //        Debug.Log("recovery");
        //        break;
        //    case 6:
        //        Debug.Log("last recovery frame");
        //        break;
        //    default:
        //        Debug.Log("Action Error.");
        //        break;
        //}
        if (deflectState == 3)
        {
            if (deflectTimer <= DEFLECT_RECOVERY && deflectTimer!=0) // If deflect is over, exit anim
                animator.SetInteger("transitions", 5); // *** SEE: This check might not be necessary due to the new enums for state **** ALSO: put this where it belongs in the timedactions method
           
            else animator.SetInteger("transitions", 4);

        }
        
        if (slowMovement)
        {
            animator.SetInteger("transitions", 2);
        }
        
        if (sliceState == 3) // Same with the rest of these animations, put it where it goes in timed actions *******************************
        {
            animator.SetInteger("transitions", 3);
            sliceAnimTimer = 10;
            hitSpark.GetComponent<Animator>().SetInteger("hitBoxCount", sliceBoxes+1);
            //Debug.Log(sliceBoxes + 1);
            //Debug.Log("Slicing");
        }
        

        if (baState == 3)
        {
            animator.SetInteger("transitions", 1);
            //Debug.Log("attacking");
            //Debug.Log(animator.GetInteger("transitions"));
        }

        if(deflectState < 2 && baState < 2 && sliceState < 1 && !slowMovement) // If we have a sprite for charging the slice, make this sS<1 (1=charge) and do another if
        {
            animator.SetInteger("transitions", 0);
           // if(hitSpark.GetComponent<Animator>().GetInteger("hitBoxCount")>0)
                
        }
        if(redTimer>0) // What is this ?? ---- ATTN: ANYONE WHO KNOWS THE ANIMATION STUFF
        {
            redTimer--;
            if(redTimer<=0)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
            }
        }
        if(sliceAnimTimer>0)
        {
            sliceAnimTimer--;
            if(sliceAnimTimer<=0)
            {
                hitSpark.GetComponent<Animator>().SetInteger("hitBoxCount", 0);
            }
        }
        //Debug.Log(animator.GetInteger("transitions"));
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
        overclockCDUISlider.value = 20 - (overclockCooldown * 2);
    }

    private void ProcessInput() // Processes the current input that the character has
    {
        for (int i = 0; i < inputFlags.Length; i++)
        {
            if (i == 0 && sliceHoldTime > 0 && !inputFlags[0]) // Checks for slice release: this is what triggers the actual slice
            {
                if (deflectState > 1 || baState > 1 || sliceState > 1) continue;
                //Debug.Log("Slice");
                sliceState = 2;

                sliceBoxes = Mathf.FloorToInt(sliceHoldTime / SLICE_TIMESTEP);
                if (sliceBoxes > 5) sliceBoxes = 5;

                sliceTimer = SLICE_STARTUP + SLICE_ACTIVE + SLICE_RECOVERY; // Start timer for slice mechanic
                sliceHoldTime = 0;
            }
            if (inputFlags[i])
            {
                switch (i)
                {
                    case 0: // Charging Slice (button held)
                        if (deflectState > 1 || baState > 1 || sliceState > 1) continue;
                        //Debug.Log("SliceHold");
                        sliceState = 1;
                        sliceHoldTime += Time.deltaTime;
                        slowMovement = true;
                        //animator.SetInteger("transitions", 2);
                        break;
                    case 1: // Attacking
                        if (sliceState > 0 || deflectState > 1 || baState > 1) continue;
                        //Debug.Log("Attack");
                        baState = 2; // Startup
                        baTimer = BA_STARTUP + BA_ACTIVE + BA_RECOVERY;
                        //animator.SetInteger("transitions", 1);
                        break;
                    case 2: // Deflecting
                        if (sliceState > 0 || baState > 1 || deflectState > 1) continue;
                        //Debug.Log("deflect");
                        deflectState = 2; // Startup
                        deflectTimer = DEFLECT_STARTUP + DEFLECT_ACTIVE + DEFLECT_RECOVERY;
                        //animator.SetInteger("transitions", 4);
                        break;
                    case 3: // Interacting
                        break;
                    case 4: // Overclocking
                        if (overclockState == 0)
                        {
                            overclockReadyUIElement.SetActive(false);
                            //Debug.Log("Press");
                            overclockState = 2; // Startup
                            overclockTimer = OVERCLOCK_STARTUP + OVERCLOCK_ACTIVE + OVERCLOCK_RECOVERY;
                        }
                        else if (overclockState > 1) // Manual cancel, trigger recovery
                        {
                            //Debug.Log("Unpress");
                            overclockState = 5; // Recovery frame 1
                            overclockTimer = OVERCLOCK_RECOVERY;
                        }
                        break;
                    case 5: // Firing
                        break;
                    case 6: // Dashing
                        if (dashState > 1 && (sliceState > 1 || baState > 1 || deflectState > 1)) // Cancel dash on other actions
                        {
                            //Debug.Log("Cancel");
                            dashState = 1; // Note that canceling dash with actions mapped to mouse buttons DOES NOT WORK on most touchpads because of system-wide accidental input suppression
                            currDashDist = 0; // Cooldown
                            dashCooldown = DASH_CD;
                            transform.GetComponent<SpriteRenderer>().color = Color.white;
                        }
                        else if (sliceState > 1 || baState > 1 || deflectState > 1) continue; // Do not perform dash while doing other actions
                        else
                        {
                            if (currDashDist == 0 && dashCooldown <= 0 && rigidBody.velocity != Vector2.zero) // Only dash if moving
                            {
                                slowMovement = false;
                                dashState = 3; // Active
                                //Debug.Log("Dash");
                                transform.GetComponent<SpriteRenderer>().color = Color.blue;
                            }
                            if (dashState > 1 && currDashDist < maxDashDist) // WHile dashing
                            {
                                currDashDist += dashRate;
                                rigidBody.transform.position += new Vector3(rigidBody.velocity.normalized.x * dashRate, rigidBody.velocity.normalized.y * dashRate, rigidBody.transform.position.z);
                            }
                            else if (currDashDist >= maxDashDist) // Dash is complete
                            {
                                dashState = 1; // Cooldown
                                currDashDist = 0;
                                dashCooldown = DASH_CD;
                                //Debug.Log("End");
                                transform.GetComponent<SpriteRenderer>().color = Color.white;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            if (!inputFlags[6] && dashState > 1)
            {
                //Debug.Log("Release");
                dashState = 1; // Cooldown
                currDashDist = 0;
                dashCooldown = DASH_CD;
                transform.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }

    private void ExecuteTimedActions() // Executes any time-based actions (slicing, dashing, basic attacking, deflecting)
    {
        /* Dash */
        if (dashState == 1 && dashCooldown > 0) // Increment dash cooldown based on delta time
        {
            dashCooldown -= Time.deltaTime;
        }
        else if (dashState == 1 && dashCooldown <= 0)
        {
            dashState = 0;
        }

        /* Basic Attack */
        if (baTimer <= 0) // If bT <= 0 then bT = 0, else = bT-dT
        {
            baTimer = 0;
            baState = 0; // No cooldown on basic attack
        }
        else baTimer = baTimer - Time.deltaTime;

        if (baState > 1)
        {
            if (baTimer <= BA_RECOVERY) // Recovery frames
            {
                baState = 4; // Recovery
                baHitBoxes[2].gameObject.SetActive(false); // Remove hitboxes
                                                           //animator.SetInteger("transitions", 0);
            }
            else if (baTimer <= (BA_ACTIVE + BA_RECOVERY)) // Active frames
            {
                baState = 3; // Active
                if (baTimer <= (BA_ACTIVE / 3) + BA_RECOVERY) // Activating hitboxes based on where we are in the anim
                {
                    baHitBoxes[2].gameObject.SetActive(true);
                    baHitBoxes[1].gameObject.SetActive(false);
                }
                else if (baTimer <= (2 * BA_ACTIVE / 3) + BA_RECOVERY)
                {
                    baHitBoxes[1].gameObject.SetActive(true);
                    baHitBoxes[0].gameObject.SetActive(false);
                }
                else
                {
                    baHitBoxes[0].gameObject.SetActive(true);
                }
                //animator.SetInteger("transitions", 1);
            }
        }

        /* Slice */
        if (sliceTimer <= 0) // If sT <= 0 then sT = 0, else = sT-dT
        {
            sliceTimer = 0;
            if (slowMovement == true) // ***** SHOULD NOT BE HARD CODED, TRY TO USE SPEED MOD OR HAVE A SET OF VARS
            {
                maxSpeed = 2f;
                dashRate = .3f;
                maxDashDist = 2;
                //Debug.Log("Lowered speed");
            }
            if (sliceState > 1) sliceState = 0; // No cooldown on slice
        }
        else sliceTimer = sliceTimer - Time.deltaTime;

        if (sliceState > 1)
        {
            if (sliceTimer <= SLICE_RECOVERY) // Recovery frames
            {
                sliceState = 4; // Recovery
                for (int i = 0; i < 6; i++) // End hitboxes
                {
                    sliceHitBoxes[i].gameObject.SetActive(false);
                }

            }
            else if (sliceTimer <= (SLICE_ACTIVE + SLICE_RECOVERY)) // Active frames
            {
                sliceHitBoxes[0].gameObject.SetActive(true); // Always activate the first box
                for (int i = 0; i < sliceBoxes; i++) // Additional hitboxes
                {
                    sliceHitBoxes[i + 1].gameObject.SetActive(true);
                }
                sliceState = 3; // Active
                slowMovement = false;
                maxSpeed = 7f; // ******* THESE VALUES SHOULD NOT BE HARD CODED-- Also, move these to a different statement if movement is supposed to come back after releasing the button (active frames)
                dashRate = .5f;
                maxDashDist = 3;
                //Debug.Log("Reset speed");
                //animator.SetInteger("transitions", 3);
            }

            if (sliceState < 4) // During active and startup
            {
                //Debug.Log(sliceState);
                animator.SetInteger("transitions", 2);
            }
        }

        /* Deflect */
        if (deflectTimer <= 0) // If dfT <= 0 then dfT = 0, else = dfT-dT
        {
            deflectState = 0; // No cooldown on deflect
            deflectTimer = 0;
        }
        else deflectTimer = deflectTimer - Time.deltaTime;

        if (deflectState > 1)
        {
            if (deflectTimer <= DEFLECT_RECOVERY) // Recovery frames
            {
                deflectState = 4; // Recovery
                deflectHitBox.gameObject.SetActive(false); // Remove hitbox
                animator.SetInteger("transitions", 5);
            }
            else if (deflectTimer <= (DEFLECT_ACTIVE + DEFLECT_RECOVERY)) // Active frames
            {
                deflectState = 3; // Active
                deflectHitBox.gameObject.SetActive(true);
                animator.SetInteger("transitions", 5);
            }
        }

        /* Overclock */
        overclockTimer = overclockTimer <= 0 ? 0 : overclockTimer - Time.deltaTime; // If bT <= 0 then bT = 0, else = bT-dT

        switch (overclockState)
        {
            case 1: // Cooldown
                if (overclockCooldown > 0) // Increment cooldown
                {
                    //Debug.Log("Cooling: " + overclockCooldown);
                    overclockCooldown -= Time.deltaTime;
                }
                else
                {
                    overclockState = 0; // Ready!
                    overclockReadyUIElement.SetActive(true);
                    overclockCooldown = 0;
                }
                break;
            case 2: // Startup frame 1
                overclockState = 3; // Move on to startup
                enemyHandler.speedMod -= overclockMod; // Slow enemies
                enemyHandler.KillStunnedEnemies();
                //Debug.Log("ZA WARUDO: " + enemyHandler.speedMod);
                Camera.main.GetComponent<UnityStandardAssets.ImageEffects.NoiseAndScratches>().enabled = true;
                //Camera.main.GetComponent<UnityStandardAssets.ImageEffects.Grayscale>().enabled = true;
                blueScreenGlow.SetActive(true);
                Camera.main.GetComponent<UnityStandardAssets.ImageEffects.VignetteAndChromaticAberration>().enabled = true;
                break;
            case 3: // Startup
                if (overclockTimer <= (OVERCLOCK_ACTIVE + OVERCLOCK_RECOVERY))
                {
                    overclockState = 4; // Active
                }
                break;
            case 4: // Active
                if (overclockTimer <= OVERCLOCK_RECOVERY)
                {
                    overclockState = 5; // Recovery
                }
                break;
            case 5: // Recovery
                if (overclockTimer <= 0)
                {
                    overclockState = 6; // Last frame
                }
                break;
            case 6: // Last recovery frame (techically a +1 frame on the end)
                enemyHandler.speedMod += overclockMod; // Respeed enemies
                overclockState = 1; // Cooldown
                overclockTimer = 0;
                overclockCooldown = OVERCLOCK_CD;
                //Debug.Log("WRYYYYYY: " + enemyHandler.speedMod);
                Camera.main.GetComponent<UnityStandardAssets.ImageEffects.NoiseAndScratches>().enabled = false;
                //Camera.main.GetComponent<UnityStandardAssets.ImageEffects.Grayscale>().enabled = false;
                blueScreenGlow.SetActive(false);
                Camera.main.GetComponent<UnityStandardAssets.ImageEffects.VignetteAndChromaticAberration>().enabled = false;
                break;
        }
             
    }

    public void setHealth() // Update health UI element and check for death
    {
        if(health > 9) // Max health ***** SHOULD NOT BE HARD CODED, ADD CONST
        {
            health = 9;
        }
        healthUIElement.GetComponent<Text>().text = health.ToString();
        if (health > 4) healthUIElement.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 201, 0); // Green
        else if (health > 3) healthUIElement.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = new Color(201, 201, 0); // Yellow
        else healthUIElement.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = new Color(201, 0, 0); // Red
        if (health <= 0)
        {
            health = 0;
            Debug.Log("GAME OVER");
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetSceneAt(0).name);
        }
    }

    public void setScore() // Set UI score element
    {
        scoreUIText.text = "Score  " + score.ToString();
    }

    public void resetOverclock()
    {
        overclockCooldown = 0;
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("Collide");
        if (other.tag != "Grenade" && other.gameObject.layer == 11) // Basic bullets + rocket
        {
            // Prevent collision with deflected bullets
            if ((other.tag == "Bullet" || other.tag == "SlowBullet") && other.GetComponent<Bullet>().CanHurtEnemies) return;
            //Debug.Log("Player got hit");
            redTimer = 10;
            gameObject.GetComponent<SpriteRenderer>().color = new Color(255, 0, 0);
            health--;
            setHealth();
            hpHit++;
            //Debug.Log(hpHit);
            Cancel(); // ends active attacks when hit. This may need to be commented out if we can't get the animations to stop too
                        //Debug.Log("Got em. Health: " + health);
                        //gameObject.GetComponent<SpriteRenderer>().color = Color.red;
                          
            enemyHandler.SecondWind();
                
            Destroy(other.gameObject); // Get rid of the bullet that was fired

            return;
        }
        else if (other.gameObject.layer == 12 && !hitByLaser) // Laser first hit
        {
            //Debug.Log("LASERED");
            health--;
            setHealth();
            hpHit++;
            //Debug.Log(hpHit);
            Cancel();
            hitByLaser = true;

            enemyHandler.SecondWind();
        }
        else if (other.gameObject.layer == 13) // Slow field from grenade
        {
            slowFields.Add(other.gameObject.GetComponent<Explosion>());
            if (!other.gameObject.GetComponent<Explosion>().canHurtEnemies && slowMod <= 0)
            {
                //Debug.Log("Slowed");
                slowMod = other.gameObject.GetComponent<Explosion>().slowFactor;
            }
        }
        else if (other.gameObject.layer == 14 && hitByMedicBullet == false) // Medic bullet
        {
            health++;
            setHealth();
            hpHit++;
            hitByMedicBullet = true;
        }

        if (other.gameObject.tag == "BigShield")
        {
            //Debug.Log("Player in");
            other.GetComponent<BigShield>().playerInside = true;
        }
        if (other.gameObject.tag == "Dome")
        {
            health--;
            setHealth();
            hpHit++;
        }
        
    }

    void OnTriggerExit2D(Collider2D other)
    {
        //if the medic bullet passed through us, give us 1 health
        if(other.tag == "MedicBullet")
        {
            hitByMedicBullet = false;
            return;
        }

        if (other.gameObject.layer == 12) hitByLaser = false; // Laser
        if (other.gameObject.layer == 13) // Grenade based slow fields
        {
            foreach (Explosion e in slowFields)
            {
                if (other.GetComponent<Explosion>().id == e.id)
                {
                    slowFields.Remove(e);
                    break;
                }
            }
            if (slowFields.Count == 0)
            {
                //Debug.Log("unslowed");
                slowMod = 0;
            }
        }
        if (other.gameObject.tag == "BigShield")
        {
            //Debug.Log("Player out");
            other.GetComponent<BigShield>().playerInside = false;
        }
    }


    void Cancel()
    {
        baTimer = 0;
        baHitBoxes[0].gameObject.SetActive(false);
        baHitBoxes[1].gameObject.SetActive(false);
        baHitBoxes[2].gameObject.SetActive(false);
        //animator.SetInteger("transitions", 0);
    }

    public void Hitstop(float pauseDelay) // ********* What is this?
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
