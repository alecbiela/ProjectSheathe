using UnityEngine;
using System.Collections;

public class Rocket : MonoBehaviour
{
    private Transform Player;
    public float bulletSpeed = 5.0f;
    private float health = 1f;
    private EncounterManager handler;
    private Vector3 desiredVelocity;
    private Vector3 vectorToPlayer;
    private bool hitRecently;

    //called by the enemy that is firing to make a new bullet
    public void Initialize(Vector3 pos, Transform pPlayer)
    {
        handler = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
        this.transform.position = pos;
        Player = pPlayer;
        hitRecently = false;
    }

    //Phyxed Update for Physics
    void FixedUpdate()
    {
        if (health <= 0)
        {
            Destroy(this.gameObject);
        }
        vectorToPlayer = (Player.position - this.transform.position);
        desiredVelocity = vectorToPlayer;
        desiredVelocity.Normalize();
        desiredVelocity *= bulletSpeed;
        desiredVelocity = desiredVelocity * handler.speedMod * Time.deltaTime; // I misuse the name in this instance, because I need the variable for rotation
        this.transform.position += desiredVelocity;
        float angle = Mathf.Atan2(desiredVelocity.y, desiredVelocity.x) * Mathf.Rad2Deg;
        this.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    //handle collisions
    void OnTriggerEnter2D(Collider2D col)
    {
        //collision w/ Player handled in player

        //if it's a slice hitbox
        if (col.tag.Contains("SliceHitbox") || col.tag.Contains("BAHitbox"))
        {
            Debug.Log("Rocket hit");

            if (!this.hitRecently)
            {
                health--;
                this.hitRecently = true;
            }
            return;
        }

        //goes out of bounds
        if (col.tag == "Boundary")
        {
            Destroy(this.gameObject);
        }
    }
}
