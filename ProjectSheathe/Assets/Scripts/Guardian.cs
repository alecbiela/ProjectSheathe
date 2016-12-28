using UnityEngine;
using System.Collections;

public class Guardian: Enemy
{
    /* Special Ability Variables */
    [HideInInspector] public GameObject bigShield;

    protected override void Start()
    {
        base.Start();
        type = "Guardian";
        rank = "Officer";

        bigShield = transform.GetChild(0).gameObject;
        bigShield.SetActive(true);
        bigShields.Add(bigShield.GetComponent<BigShield>()); // Is behind its own shield
    }

    protected override void Stun()
    {
        base.Stun();
        bigShield.gameObject.SetActive(false); // End shield
    }

    public override void SecondWind()
    {
        base.SecondWind();
        if (!bigShield.activeSelf) bigShield.SetActive(true); // If shield is down, put it up
    }

    public override void Fire()
    {
        base.Fire();
        if (timer % 3 == 0) // Flash on
        {
            counter += 1f;
            float x = Mathf.Lerp(0, dist, counter);
            Vector3 pointA = origin;
            Vector3 pointB = destination;
            Vector3 direction = Vector3.Normalize(pointB - pointA);
            Vector3 pointAlongLine;
            pointAlongLine = x * direction + pointA; // Get the unit vector in the desired direction, multiply by the desired length and add the starting point.

            lineRendererComponent.enabled = true;
            if (currFlashTime > redFlashTime) // Flash yellow
            {
                //GetComponent<SpriteRenderer>().color = new Color(255, 200, 0); // yellow/gold
                lineRendererComponent.SetColors(new Color(242, 190, 0, 172), new Color(242, 190, 0, 172)); // Yellow (For basic enemies)
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
        else // Flash off
        {
            GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // white
            lineRendererComponent.enabled = false;
        }

        if (currFlashTime <= 0) // When ready, FIRE AWAY
        {
            GameObject newBullet = (GameObject)Instantiate(BulletPrefab); // Instantiate a new bullet to fire
            newBullet.GetComponent<Bullet>().Initialize(transform.position, vecToPlayer);  // Uses the enemy's last known location of player

            GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);// After attacking, reset color and flags
            trackPlayer = true;

            //timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            timer = 0;
        }
    }

    public override void Destructor() // Clears/Deletes the special ability (or any other delete logic that may need to be added)
    {
        if (bigShield)
        {
            bigShield.SetActive(false);
            Destroy(bigShield.gameObject); // Destroy special
        }
        bigShield = null;
    }

}
