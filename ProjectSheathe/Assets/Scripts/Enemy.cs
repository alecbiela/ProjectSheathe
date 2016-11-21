using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour {
    // Use this for initialization
    GameObject Player;
    GameObject Bullet;
    Vector3 velocity = new Vector3(0, 0, 0);
    Vector3 vecToPlayer = new Vector3(0, 0, 0);
    bool firing = false;
    int timer = 0;
    bool active = false;
    System.Random rand = new System.Random();
	void Start () {
        Bullet = new GameObject();
        Player = GameObject.FindGameObjectWithTag("Player");
        Bullet.AddComponent<SpriteRenderer>();
        Bullet.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Bullet");
        Bullet.AddComponent<BoxCollider2D>();
        Bullet.transform.SetParent(this.transform);
        Bullet.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (active)
        {
            Bullet.transform.position += velocity;
            if (Bullet.GetComponent<BoxCollider2D>().IsTouching(Player.GetComponent<BoxCollider2D>()))
            {
                Bullet.SetActive(false);
                active = false;
                //Bullet.GetComponent<SpriteRenderer>
            }
        }/**/
        ++timer;
        if (!firing)
        {//tracks the player
            vecToPlayer = (Player.transform.position - this.transform.position);
            float angle = Mathf.Atan2(vecToPlayer.y, vecToPlayer.x) * Mathf.Rad2Deg;
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
            this.transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * 0.9f); //Quaternion.LookRotation(this.transform.position - Player.transform.position);
            if(timer>600)
            {
                firing = true;
                timer = rand.Next(0,300);
            }
        }
        else
        {
            if (timer % 3 == 0)
            {
                this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 0);
            }
            else
            {
                this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
            }
            if(timer >=100)
            {
                Bullet.SetActive(true);
                active = true;
                this.GetComponent<SpriteRenderer>().color = new Color(255, 255, 255);
                velocity = (Player.transform.position - this.transform.position).normalized;
                Bullet.transform.position = this.transform.position;
                firing = false;
            }
        }
    }
}
