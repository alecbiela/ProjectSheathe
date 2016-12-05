using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grenade : MonoBehaviour
{

    public float bulletSpeed = 10.0f;
    public bool CanHurtEnemies { get; private set; }
    private EncounterManager handler;
    private Vector3 desiredVelocity;
    public GameObject ExplosionPrefab;
    private GameObject explosion;
    public float slowMod = 0;
    private List<Explosion> slowFields = new List<Explosion>();

    //called by the enemy that is firing to make a new bullet
    public void Initialize(Vector3 pos, Transform Player)
    {
        handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        this.transform.position = pos;
        CanHurtEnemies = false;


        //calculate desired path
        desiredVelocity = (Player.position - this.transform.position);
        desiredVelocity.Normalize();
        desiredVelocity *= bulletSpeed;
        float angle = Mathf.Atan2(desiredVelocity.y, desiredVelocity.x) * Mathf.Rad2Deg;
        this.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        explosion = (GameObject)Instantiate(ExplosionPrefab);
        explosion.transform.position = Player.position;
        explosion.gameObject.SetActive(true);
        explosion.transform.GetChild(0).gameObject.SetActive(false);
    }

    //Phyxed Update for Physics
    void FixedUpdate()
    {
        this.transform.position += desiredVelocity * (handler.speedMod-slowMod) * Time.deltaTime;
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
        if (col.tag == "DeflectHitbox" && !CanHurtEnemies)
        {
            CanHurtEnemies = true;
            desiredVelocity *= -1;
            // Set grenade destination here, or just have it explode on enemies
        }
        //collision w/ Player and Enemies handled in their respective classes
        if (col.gameObject.GetInstanceID() == explosion.GetInstanceID()) // Explode when the hit box is reached
        {
            //Debug.Log("Boom");
            desiredVelocity *= 0;
            explosion.transform.GetChild(0).gameObject.SetActive(true);
            Destroy(this.gameObject);
        }
        else if (col.gameObject.layer == 13)
        {
            if (slowMod <= 0) slowMod = col.gameObject.GetComponent<Explosion>().slowFactor;
            slowFields.Add(col.gameObject.GetComponent<Explosion>());
        }

        if (col.tag == "Enemy" && CanHurtEnemies)
        {
            explosion.transform.position = this.transform.position;
            desiredVelocity *= 0;
            explosion.transform.GetChild(0).GetComponent<Explosion>().canHurtEnemies = true;
            explosion.transform.GetChild(0).gameObject.SetActive(true);
            Destroy(this.gameObject);
        }

        //goes out of bounds
        if (col.tag == "Boundary")
        {
            Destroy(this.gameObject);
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