using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Medic : Enemy {

    /* Special Ability Variables */
    private float active; // What is this? --ATTN: ALEC I GUESS
    private bool isActive; // Tracks if the medic is to be healing things

	// Use this for initialization
	protected override void Start () {
        base.Start();
        type = "Medic";
        rank = "Officer";
        timer = 0;
        isActive = true;
	}

    public override void Fire()
    {
        //Debug.Log("Fire called");
        if (!isActive) return; // When no stunned enemies
        base.Fire();
        //Debug.Log("Timer: " + timer); // the timer isn't being incremented properly // it is somehow becoming a decimal
        if ((int)timer % 3 == 0) // Flash on
        {
            counter += 1f;
            float x = Mathf.Lerp(0, dist, counter);
            Vector3 pointA = origin;
            Vector3 pointB = destination;
            Vector3 direction = Vector3.Normalize(pointB - pointA);
            Vector3 pointAlongLine;
            pointAlongLine = x * direction + pointA; // Get the unit vector in the desired direction, multiply by the desired length and add the starting point.
            
            lineRendererComponent.enabled = true;
            //Debug.Log("Flash time: " + currFlashTime); // currFlashTime keeps getting reset
            if (currFlashTime > redFlashTime) // Flash yellow
            {
                lineRendererComponent.SetColors(new Color(0, 155, 0, 172), new Color(0, 155, 0, 172));  //Medics flash green
                lineRendererComponent.SetPosition(1, pointAlongLine);
            }
            else // Flash red just before firing
            {
                //GetComponent<SpriteRenderer>().color = new Color(255, 0, 0); // red
                lineRendererComponent.SetColors(Color.red, Color.black);
                lineRendererComponent.SetPosition(1, pointAlongLine);
                trackPlayer = false;
            }
        }
        else
        {
            GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // white
            lineRendererComponent.enabled = false;
        }

        //Debug.Log(currFlashTime); // currFlashTime keeps getting reset
        if (currFlashTime <= 0) // Fire when ready
        {
            GameObject newBullet = (GameObject)Instantiate(BulletPrefab); // Instantiate a new bullet to fire
            newBullet.GetComponent<Bullet>().Initialize(transform.position, vecToPlayer);  // Uses the location of another enemy to heal
            //Debug.Log("Bullet fired");
            lineRendererComponent.enabled = false;

            //GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // After attacking, reset color and flags
            trackPlayer = true;

            //timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            timer = 0;
        }
    }

    // Update is called once per frame
    protected override void Update() {
        if (hitRecently && Handler.PlayerScript.BAState < 2 && Handler.PlayerScript.SliceState < 2) // Let enemy get hit again if original attack is over
        {
            hitRecently = false;
        }
        timer += Time.deltaTime * Handler.speedMod;

        //fire if longer than some time
        if (timer >= 4) // used to be (timer >= 4)
        {
            //Debug.Log("reset timer");
            Fire();
            //timer = 0;
        }

        if (trackPlayer)
        {
            //get a random stunned enemy
            List<Vector3> positions = Handler.stunnedEnemyPositions;
            if (positions.Count != 0)
            {
                isActive = true;

                Vector3 randomStunnedEnemy = Handler.stunnedEnemyPositions[rand.Next(Handler.stunnedEnemyPositions.Count)]; // not random since the Medics activate on the same frame
                vecToPlayer = (randomStunnedEnemy - transform.position);    //this is correct - the bullet fires on this path and it's directly into the character
                float angle = Mathf.Atan2(vecToPlayer.y, vecToPlayer.x) * Mathf.Rad2Deg;
                Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, q, (Handler.speedMod - slowMod) * Time.deltaTime * 0.9f * rotSpeed);

                // line render
                origin = transform.position;
                destination = randomStunnedEnemy;
                dist = Vector3.Distance(origin, destination);
            }
            else isActive = false;
        }
    }
}
