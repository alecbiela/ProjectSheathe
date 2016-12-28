using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Bullet : MonoBehaviour {

    public float bulletSpeed = 10.0f;
    public bool CanHurtEnemies { get; private set; }
    private EncounterManager handler;
    private Vector3 desiredVelocity;
    public float slowMod = 0;
    private List<Explosion> slowFields = new List<Explosion>();

    //called by the enemy that is firing to make a new bullet
    public void Initialize(Vector3 pos, Vector3 vectorToPlayer)
    {
        handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        transform.position = pos;
        CanHurtEnemies = false;

        //calculate desired path
        desiredVelocity = vectorToPlayer;
        desiredVelocity.Normalize();
        desiredVelocity *= bulletSpeed;
        float angle = Mathf.Atan2(desiredVelocity.y, desiredVelocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    //Phyxed Update for Physics
    void FixedUpdate()
    {
        transform.position += desiredVelocity * (handler.speedMod-slowMod) * Time.deltaTime;
        for (int i = 0; i < slowFields.Count; i++)
        {
            if (!slowFields[i].isTrigger)
            {
                slowFields.RemoveAt(i);
                i--;
            }
        }
        if (slowMod > 0 && slowFields.Count == 0) slowMod = 0;
    }

    //handle collisions
    void OnTriggerEnter2D(Collider2D col)
    {
        //bullet collides with the deflect hitbox
        if(col.tag == "DeflectHitbox" && !CanHurtEnemies)
        {
            //if it's a medic bullet, give the player 2 health (any better way to do this?)
            if(tag == "MedicBullet")
            {
                col.gameObject.GetComponent<Character>().health += 2;
                col.gameObject.GetComponent<Character>().setHealth();
                //handler.extraWind = 1;
                Destroy(gameObject);
            }
            CanHurtEnemies = true;
            desiredVelocity *= -1;
            float angle = Mathf.Atan2(desiredVelocity.y, desiredVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        //collision w/ Player and Enemies handled in their respective classes
        if (col.gameObject.layer == 13)
        {
            if (col.gameObject.GetComponent<Explosion>().canHurtEnemies)
            {
                if (slowMod <= 0) slowMod = col.gameObject.GetComponent<Explosion>().slowFactor;
                slowFields.Add(col.gameObject.GetComponent<Explosion>());
            }
        }
        //goes out of bounds
        if (col.tag == "Boundary")
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == 13)
        {
            foreach (Explosion e in slowFields)
            {
                if (other.GetComponent<Explosion>().id == e.id)
                {
                    slowFields.Remove(e);
                    break;
                }
            }
            if (slowFields.Count == 0) slowMod = 0;
        }
    }
}
