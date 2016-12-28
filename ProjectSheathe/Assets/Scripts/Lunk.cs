using UnityEngine;
using System.Collections;

public class Lunk : Enemy
{
    /* Special Ability Variables */
    [HideInInspector] public GameObject shield;
    private float guardedTime;

    protected override void Start()
    {
        base.Start();
        type = "Lunk";
        rank = "Guard";
        guardedTime = 0.0f;

        shield = transform.GetChild(0).gameObject;
        shield.SetActive(true);
    }

    public override void Destructor() // Clears/Deletes the special ability (or any other delete logic that may need to be added)
    {
        if (shield)
        {
            shield.SetActive(false);
            Destroy(shield.gameObject); // Destroy special
        }
        shield = null;
    }

    protected override void Update()
    {
        base.Update();
        if (hitSpark.GetComponent<Animator>().GetInteger("hitBoxCount") == 7 && guardedTime != 0.0f) // Comment this plz -- ATTN: ANYONE
        {
            if ((Time.time - guardedTime) > (Time.deltaTime))
            {
                hitSpark.GetComponent<Animator>().SetInteger("hitBoxCount", 0);
                guardedTime = 0.0f;
            }
            else
            {
                hitSpark.GetComponent<Animator>().SetInteger("hitBoxCount", 7);

            }
        }
    }

    protected override void Stun()
    {
        base.Stun();
        shield.gameObject.SetActive(false); // End shield if up
    }

    public override void SecondWind()
    {
        base.SecondWind();
        if (!shield.activeSelf) shield.SetActive(true); // If shield is down, put it up
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
            
            if (currFlashTime > redFlashTime) // Flash yellow
            {
                lineRendererComponent.SetColors(new Color(242, 190, 0, 172), new Color(242, 190, 0, 172)); // Yellow (For basic enemies)
                lineRendererComponent.SetPosition(1, pointAlongLine);
            }
            else // Flash red just before firing
            {
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
        
        if (currFlashTime <= 0) // Fire when ready
        {
            GameObject newBullet = (GameObject)Instantiate(BulletPrefab); // Instantiate a new bullet to fire
            newBullet.GetComponent<Bullet>().Initialize(transform.position, vecToPlayer);  // Uses the enemy's last known location of player
            
            GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // After attacking, reset color and flags
            trackPlayer = true;

            //timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            timer = 0;
        }
    }

    protected override void OnTriggerEnter2D(Collider2D col)
    {
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

        if (col.gameObject.tag == "MedicBullet" && stunState != 2) // Medic bullets heal and apply second wind
        {
            health = (health >= MAX_HEALTH) ? MAX_HEALTH : (health + 1); // If topped off, don't add any
            stunState = 1; // Secondwinded
            DestroyImmediate(col.gameObject);
            return;
        }

        if (!guarded) // Take the hit if unguarded
        {
            if (col.tag.Contains("SliceHitbox")) // Slice
            {
                if (shield.activeSelf) // If shield is up
                {
                    shield.SetActive(false);
                    hitRecently = true;
                }
                else if (!hitRecently)
                {
                    health--;
                    hitRecently = true;
                }
                hitSpark.GetComponent<ParticleSystem>().Emit(2);
                return;
            }

            if (col.tag.Contains("BAHitbox")) // Basic Attack
            {
                if (shield.activeSelf) // If shield is up
                {
                    hitSpark.GetComponent<Animator>().SetInteger("hitBoxCount", 7);
                    guardedTime = Time.time;
                }
                else if (!hitRecently)
                {
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
                    if (shield.activeSelf)  // If shield is up
                    {
                        // Do nothing
                    }
                    else
                    {
                        health--;
                    }
                    DestroyImmediate(col.gameObject);
                }
            }
        }
    }

}