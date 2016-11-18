using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour {

    private GameObject[] sliceHitBoxes;
    private GameObject[] baHitBoxes;
    private GameObject deflectHitBox;

    Rigidbody2D rigidBody;
    [SerializeField] private float maxSpeed = 8f; // The fastest the player can travel in any direction
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

    private float sliceTimer;
    private float baTimer;
    private float deflectTimer;

    private int sliceBoxes;
    public bool chargingSlice;
    public bool slowMovement;

    //can look at these elsewhere (perhaps to disregard input?) but can only set in here
    public bool Slicing { get; private set; }
    public bool Deflecting { get; private set; }
    public bool Attacking { get; private set; }

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        sliceTimer = 0;
        baTimer = 0;
        deflectTimer = 0;
        Slicing = false;
        chargingSlice = false;
        Attacking = false;
        Deflecting = false;
        sliceBoxes = 0;
        slowMovement = false;

        //populates various arrays of gameobjects with their hitboxes
        //adds by name, so we know which is which
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

    //your friendly neighborhood update method
    private void Update()
    {
        ExecuteTimedActions();
    }


    public void controllerMove(float hMove, float vMove, float hLook, float vLook) // Movement and rotation with controller
    {
        if(!Attacking && !Deflecting && !Slicing)   //only move when they are not doing these actions
            rigidBody.velocity = new Vector2(hMove * maxSpeed, vMove * maxSpeed);
        if (hLook != 0 && vLook != 0 && !Attacking && !Slicing)
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

    //handles Slice mechanic, takes DURATION OF KEYPRESS as an argument
    public void Slice(float duration)
    {
        if (Deflecting || Attacking || Slicing) return;

        sliceBoxes = Mathf.FloorToInt(duration / SLICE_TIMESTEP);
        if(sliceBoxes > 5)
        {
            sliceBoxes = 5;
        }

        //Debug.Log("Accepted slice input, " + duration + " seconds.");
        //start timer for slice mechanic
        sliceTimer = SLICE_PRELOAD + SLICE_ACTIVE + SLICE_AFTER;
        chargingSlice = true;
        // maxSpeed = 4f;
        // Debug.Log("Changed speed to 4f");
    }

    //handles basic attack mechanic, takes nothing
    public void BasicAttack()
    {
        if (chargingSlice || Deflecting || Attacking) return;

        //Debug.Log("Accepted basic attack input.");
        baTimer = BASIC_PRELOAD + BASIC_ACTIVE + BASIC_AFTER;
    }

    //handles deflect mechanic, takes nothing
    public void Deflect()
    {
        if (chargingSlice || Attacking || Deflecting) return;

        //Debug.Log("Accepted deflect input.");
        deflectTimer = DEFLECT_PRELOAD + DEFLECT_ACTIVE + DEFLECT_AFTER;
    }

    //executes any time-based actions (slicing, dashing, majongling, basic attacking, deflecting)
    private void ExecuteTimedActions()
    {
        //work the timers
        sliceTimer = sliceTimer <= 0 ? 0 : sliceTimer - Time.deltaTime;
        deflectTimer = deflectTimer <= 0 ? 0 : deflectTimer - Time.deltaTime;
        baTimer = baTimer <= 0 ? 0 : baTimer - Time.deltaTime;

        //attack is over, hitboxes go away
        if (baTimer <= BASIC_AFTER)
        {
            baHitBoxes[2].gameObject.SetActive(false);
        }
        //it's time to attack (in a basic fashion)
        else if (baTimer <= (BASIC_ACTIVE + BASIC_AFTER))
        {
            //activating hitboxes based on where we are in the anim
            if(baTimer <= (BASIC_ACTIVE/3) + BASIC_AFTER)
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

        //slice is over, hitboxes go away
        if (sliceTimer <= SLICE_AFTER)
        {
            for (int i = 0; i < 6; i++)
            {
                sliceHitBoxes[i].gameObject.SetActive(false);
            }
        }
        //it's time to slice
        else if (sliceTimer <= (SLICE_ACTIVE + SLICE_AFTER))
        {
            //always activate the first box
            sliceHitBoxes[0].gameObject.SetActive(true);

            //additional hitboxes
            for (int i = 0; i < sliceBoxes; i++)
            {
                sliceHitBoxes[i + 1].gameObject.SetActive(true);
            }

            chargingSlice = false;
            Slicing = true;
            maxSpeed = 8f;
            slowMovement = false;
            Debug.Log("Reset speed");
        }


        //deflect is over, hitbox goes away
        if (deflectTimer <= DEFLECT_AFTER)
        {
            deflectHitBox.gameObject.SetActive(false);
        }
        //it's time to deflect
        else if (deflectTimer <= (DEFLECT_ACTIVE + DEFLECT_AFTER))
        {
            deflectHitBox.gameObject.SetActive(true);
            Deflecting = true;
        }


        if (chargingSlice == true) slowMovement = true;

        //un-flags any abilities that are not on a timer
        //this will allow the player to perform other actions
        if (deflectTimer == 0) Deflecting = false;
        if (sliceTimer == 0)
        {
            if (slowMovement == true)
            {
                maxSpeed = 4f;
                Debug.Log("Lowered speed");
            }

            Slicing = false;
        }
        if (baTimer == 0) Attacking = false;
    }
}
