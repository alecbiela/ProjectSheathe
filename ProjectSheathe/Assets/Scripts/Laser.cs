using UnityEngine;
using System.Collections;

public class Laser : Enemy
{
    /* Special Ability Variables */
    [HideInInspector] public GameObject laser;
    private const float LASER_TIME = 2.5f; // Time laser is actively dealing damage
    private float fireTime; // Laser timer and is it firing
    private bool firing;


    protected override void Start()
    {
        base.Start();
        type = "Light";
        rank = "Guard";
        firing = false;
        fireTime = 0f;
        FLASH_TIME = 2.322222f; // MODIFY CHARGE FOR LASER HERE
        currFlashTime = FLASH_TIME;
        redFlashTime = .28333f;
        rotSpeed = 100f; // MAKE THIS LOW ENOUGH THAT OVERCLOCK AFFECTS LASER ENEMIES
        laser = Instantiate(BulletPrefab);
        laser.SetActive(false);
    }

    protected override void Stun()
    {
        base.Stun();
        firing = false;
        laser.gameObject.SetActive(false); // End laser
    }

    public override void Fire()
    {
        base.Fire();
        if (timer % 3 == 0) // Flash on
        {
            counter += 1f;
            Vector3 pointA = origin;
            Vector3 pointB = destination;
            Vector3 direction = Vector3.Normalize(pointB - pointA);
            Vector3 pointAlongLine;
            lineRendererComponent.SetWidth(.15f + counter * .005f, .15f + counter * .005f); // Laser gets wider as it charges, and goes off infinitely
            pointAlongLine = direction * 1000;

            lineRendererComponent.enabled = true;
            if (currFlashTime > redFlashTime) // Flash red and get bigger and more opaque
            {
                lineRendererComponent.SetColors(new Color(255, 153, 0, 128 + counter / 2), new Color(255, 1153, 0, 128 + counter / 2));
                lineRendererComponent.SetPosition(1, pointAlongLine);
            }
            else // Flash solid red just before firing
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

        if (currFlashTime <= 0) // FIRE THA LAYZER
        {
            Vector3 pointA = origin;
            lineRendererComponent.enabled = true;
            lineRendererComponent.SetWidth(.43f, .43f);
            lineRendererComponent.SetColors(new Color(255, 0, 0), Color.black);
            lineRendererComponent.SetPosition(1, (Vector3.Normalize(vecToPlayer) * 1000) + pointA); // Fire solidly in the direction of fire

            if (firing && fireTime <= 0) // End laser
            {
                laser.SetActive(false);
                trackPlayer = true;
                firing = false;

                //timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
                currFlashTime = FLASH_TIME;
                timer = 0;
                counter = 0;
            }
            else if (!firing) // Start laser
            {
                firing = true;
                fireTime = LASER_TIME;
                laser.SetActive(true);
                laser.transform.position = transform.position;
                laser.transform.rotation = transform.rotation;
            }
            else
            {
                fireTime -= Time.deltaTime; // Keep firing
            }
        }
    }

    public override void Destructor() // Clears/Deletes the special ability (or any other delete logic that may need to be added)
    {
        if (laser)
        {
            laser.SetActive(false);
            Destroy(laser.gameObject); // Destroy special
        }
        laser = null;
    }
}
