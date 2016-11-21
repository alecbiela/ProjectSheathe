using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour {

    private GameObject[] sliceHitBoxes;
    private GameObject[] baHitBoxes;
    private GameObject deflectHitBox;

    Rigidbody2D rigidBody;
    [SerializeField] private float maxSpeed = 8f; // The fastest the player can travel in any direction
    [SerializeField] private float maxDashDist = 14f; // Uncanceled dash distance
    [SerializeField] private float dashRate = 1f; // Dash movement per frame
    const float SLICE_TIMESTEP = 0.3f;  //the time needed to activate each "Level" of slice hitbox

    //# of frames in animation / 60
    private const float SLICE_PRELOAD = 0.166f; // start-up frames
    private const float SLICE_ACTIVE = 0.25f; // active frames
    private const float SLICE_AFTER = 0.166f; // recovery frames
    private const float BASIC_PRELOAD = 0.083f;
    private const float BASIC_ACTIVE = 0.2f;
    private const float BASIC_AFTER = 0.083f;
    private const float DEFLECT_PRELOAD = 0.066f;
    private const float DEFLECT_ACTIVE = 0.5f;
    private const float DEFLECT_AFTER = 0.25f;
    private const float DASH_CD = 2f; // Cooldown

    private float sliceHoldTime;
    private float sliceTimer;
    private float baTimer;
    private float deflectTimer;
    private float currDashDist; // Currently traveled distance of the dash
    private float dashCooldown; // Cooldown timer

    private int sliceBoxes;
    public bool slowMovement;
    private bool[] inputFlags;

    //can look at these elsewhere (perhaps to disregard input?) but can only set in here
    public bool Slicing { get; private set; }
    public bool Deflecting { get; private set; }
    public bool Attacking { get; private set; }
    public bool Dashing { get; private set; }
    public bool[] InputFlags { get { return inputFlags; } set { inputFlags = value; } }

    //private bools for key pressed, to prevent simultaneous inputs
    private bool sliceState;
    private bool deflectState;
    private bool baState;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        sliceHoldTime = 0;
        sliceTimer = 0;
        baTimer = 0;
        deflectTimer = 0;
        currDashDist = 0;
        dashCooldown = 0;
        Dashing = false;
        Slicing = false; // Actually actively slicing
        sliceState = false; // Includes startup/charge and recovery frames
        deflectState = false;
        baState = false;
        Attacking = false;
        Deflecting = false;
        sliceBoxes = 0;
        slowMovement = false;
        inputFlags = new bool[] { false, false, false, false, false, false, false }; // INPUT FLAGS, IN ORDER: SLICE[0], ATTACK[1], DEFLECT[2], INTERACT[6], OVERCLOCK[4], FIRE[5], DASH[6]

        // Populate various arrays of gameobjects with their hitboxes
        // Add by name, so we know which is which
        sliceHitBoxes = new GameObject[6];
        for(int i=0; i<6; i++)
        {
            sliceHitBoxes[i] = GameObject.Find("SliceHitbox" + (i + 1));
            sliceHitBoxes[i].gameObject.SetActive(false);
        }

        baHitBoxes = new GameObject[3];
        for(int i=0; i<3; i++)
        {
            baHitBoxes[i] = GameObject.Find("BAHitbox" + (i + 1));
            baHitBoxes[i].gameObject.SetActive(false);
        }

        deflectHitBox = GameObject.Find("DeflectHitbox");
        deflectHitBox.gameObject.SetActive(false);
    }

    public void controllerMove(float hMove, float vMove, float hLook, float vLook) // Movement and rotation with controller
    {
        if(!Attacking && !Deflecting && !Slicing)   //only move when they are not doing these actions
            rigidBody.velocity = new Vector2(hMove * maxSpeed, vMove * maxSpeed);
        if ((hLook != 0 || vLook != 0) && !Attacking && !Slicing)
        {
            float angle = Mathf.Atan2(vLook, hLook) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
        else if ((rigidBody.velocity != Vector2.zero) && !Attacking && !Slicing)
        {
            float angle = Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void keyboardMove(float hMove, float vMove, Vector3 mousePos) // Movement and rotation with keyboard and mouse
    {
        if(!Attacking && !Deflecting && !Slicing)   //only move when they are not doing these actions
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
        for(int i=0; i<inputFlags.Length; i++)
        {
            if(i == 0 && sliceHoldTime > 0 && !inputFlags[0]) // Checks for slice release
            {
                if (Deflecting || Attacking || Slicing) continue;

                sliceBoxes = Mathf.FloorToInt(sliceHoldTime / SLICE_TIMESTEP);
                if (sliceBoxes > 5) sliceBoxes = 5;
                    
                sliceTimer = SLICE_PRELOAD + SLICE_ACTIVE + SLICE_AFTER; // Start timer for slice mechanic
                sliceHoldTime = 0;
            }
            if(inputFlags[i])
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
                        break;
                    case 5: // Firing
                        break;
                    case 6: // Dashing
                        if (Dashing && (sliceState || baState || deflectState)) // Cancel dash on other actions
                        {
                            //Debug.Log("Cancel");
                            Dashing = false; // Note that canceling dash with actions mapped to mouse buttons DOES NOT WORK on most touchpads because of system-wide accidental input suppression
                            currDashDist = 0;
                            dashCooldown = DASH_CD;
                            this.transform.GetComponent<SpriteRenderer>().color = Color.white;
                        }
                        else if (sliceState || baState || deflectState) continue; // Do not perform dash while doing other actions
                        else
                        {
                            if (currDashDist == 0 && dashCooldown <= 0 && rigidBody.velocity != Vector2.zero) // Only dash if moving
                            {
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
                Debug.Log("Release");
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
        
        if (baTimer <= BASIC_AFTER) // If attack is over, hitboxes go away
        {
            baHitBoxes[2].gameObject.SetActive(false);
        }
        else if (baTimer <= (BASIC_ACTIVE + BASIC_AFTER)) // Otherwise, attack (in a basic fashion)
        {
            if(baTimer <= (BASIC_ACTIVE/3) + BASIC_AFTER) // Activating hitboxes based on where we are in the anim
            {
                baHitBoxes[2].gameObject.SetActive(true);
                baHitBoxes[1].gameObject.SetActive(false);
            }
            else if(baTimer <= (2*BASIC_ACTIVE/3) + BASIC_AFTER)
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
            maxSpeed = 8f;
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
                maxSpeed = 4f;
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
}
