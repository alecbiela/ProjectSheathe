using UnityEngine;
using System.Collections;

public class Lock : Enemy
{
    /* Special Ability Variables */

    protected override void Start()
    {
        base.Start();
        type = "Lock";
        rank = "Guard";
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
            
            GameObject newBullet = (GameObject)Instantiate(BulletPrefab);
            newBullet.GetComponent<Rocket>().Initialize(transform.position, Player.transform); // Instantiate a new rocket for firing
            
            GetComponent<SpriteRenderer>().color = new Color(255, 255, 255); // After attacking, reset color and flags
            trackPlayer = true;

            //timer = rand.Next(0, 300); //used to stagger each enemy's firing time because they're all spawned at the same time
            currFlashTime = FLASH_TIME;
            timer = 0;
        }
    }

}