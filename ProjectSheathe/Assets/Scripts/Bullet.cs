using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {

    public float bulletSpeed = 10.0f;
    public bool CanHurtEnemies { get; private set; }
    private EncounterManager handler;
    private Vector3 desiredVelocity;

    //called by the enemy that is firing to make a new bullet
    public void Initialize(Vector3 pos, Vector3 vectorToPlayer)
    {
        handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        this.transform.position = pos;
        CanHurtEnemies = false;

        //calculate desired path
        desiredVelocity = vectorToPlayer;
        desiredVelocity.Normalize();
        desiredVelocity *= bulletSpeed;
    }

    //Phyxed Update for Physics
    void FixedUpdate()
    {
        this.transform.position += desiredVelocity * handler.speedMod * Time.deltaTime;
    }

    //handle collisions
    void OnTriggerEnter2D(Collider2D col)
    {
        //bullet collides with the deflect hitbox
        if(col.tag == "DeflectHitbox" && !CanHurtEnemies)
        {
            CanHurtEnemies = true;
            desiredVelocity *= -1;
        }
        //collision w/ Player and Enemies handled in their respective classes


        //goes out of bounds
        if (col.tag == "Boundary")
        {
            Destroy(this.gameObject);
        }
    }
}
