using UnityEngine;
using System.Collections;

public class SLOB : Enemy
{
    /* Special Ability Variables */
    [HideInInspector] public GameObject grenadeWarning;

    protected override void Start()
    {
        base.Start();
        type = "SLOB";
        rank = "Officer";

        grenadeWarning = transform.GetChild(0).gameObject;
        grenadeWarning.SetActive(false);
    }

    protected override void Stun()
    {
        base.Stun();
        grenadeWarning.gameObject.SetActive(false); // End grenade warning if up
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
            grenadeWarning.transform.position = pointB;
            grenadeWarning.SetActive(true);
            if (currFlashTime > redFlashTime) // Flash yellow
            {
                lineRendererComponent.SetColors(new Color(242, 190, 0, 172), new Color(242, 190, 0, 172)); // Yellow (For basic enemies)
                grenadeWarning.GetComponent<SpriteRenderer>().color = new Color(242, 190, 0, 172);
                lineRendererComponent.SetPosition(1, pointAlongLine);
            }
            else // Flash red just before firing
            {
                lineRendererComponent.SetColors(Color.red, Color.black);
                grenadeWarning.GetComponent<SpriteRenderer>().color = Color.red;
                lineRendererComponent.SetPosition(1, pointAlongLine);
                trackPlayer = false;
            }
        }
        else // Flash off
        {
            grenadeWarning.SetActive(false); // Flash circle as well
            GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // white
            lineRendererComponent.enabled = false;
        }
        
        if (currFlashTime <= 0) // Fire when ready
        {
            GameObject newBullet = (GameObject)Instantiate(BulletPrefab); // Instantiate a new bullet to fire
            grenadeWarning.SetActive(false);
            newBullet.GetComponent<Grenade>().Initialize(transform.position, Player.transform);
            
            GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // After attacking, reset color and flags
            trackPlayer = true;
            //timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            timer = 0;
        }
    }

    public override void Destructor() // Clears/Deletes the special ability (or any other delete logic that may need to be added)
    {
        if (grenadeWarning)
        {
            grenadeWarning.SetActive(false);
            DestroyImmediate(grenadeWarning.gameObject); // Destroy special
        }
        grenadeWarning = null;
    }
}
