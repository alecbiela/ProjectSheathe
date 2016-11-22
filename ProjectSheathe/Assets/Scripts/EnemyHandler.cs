using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyHandler : MonoBehaviour {

    List<GameObject> Enemies = new List<GameObject>();
    // Use this for initialization
    System.Random rand = new System.Random();
    private GameObject Player;
    private Character PlayerScript;
    void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerScript = Player.GetComponent<Character>();
    }
	
	// Update is called once per frame
	void Update () {
	    if(Enemies.Count<5)
        {
            CreateEnemy();
        }

        if (PlayerScript.Slicing)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                for (int j = 0; j < PlayerScript.sliceHitBoxes.Length; j++)
                {
                    if (Enemies[i].GetComponent<BoxCollider2D>().IsTouching(
                        PlayerScript.sliceHitBoxes[j].GetComponent<BoxCollider2D>()))
                    {
                        GameObject.DestroyObject(Enemies[i]);
                        Enemies.RemoveAt(i);
                        --i;
                    }
                }
            }
        }
        else if (PlayerScript.Attacking)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                for (int j = 0; j < PlayerScript.baHitBoxes.Length; j++)
                {
                    if (Enemies[i].GetComponent<BoxCollider2D>().IsTouching(
                        PlayerScript.baHitBoxes[j].GetComponent<BoxCollider2D>()))
                    {
                        GameObject.DestroyObject(Enemies[i]);
                        Enemies.RemoveAt(i);//if they're colliding remove the enemy
                        --i;//decrement to avoid skipping an enemy
                    }
                }
            }
        }
        else if (PlayerScript.Deflecting)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if(Enemies[i].GetComponent<Enemy>().Bullet.GetComponent<BoxCollider2D>().IsTouching
                    (PlayerScript.deflectHitBox.GetComponent<BoxCollider2D>()))
                {
                    Enemies[i].GetComponent<Enemy>().deflected = true;
                    Enemies[i].GetComponent<Enemy>().Bullet.GetComponent<Rigidbody2D>().velocity =
                        -Enemies[i].GetComponent<Enemy>().Bullet.GetComponent<Rigidbody2D>().velocity;
                    Enemies[i].GetComponent<Enemy>().force = -Enemies[i].GetComponent<Enemy>().force;
                    Enemies[i].GetComponent<Enemy>().Bullet.GetComponent<Rigidbody2D>().AddForce(Enemies[i].GetComponent<Enemy>().force);
                }
            }
        }
        for (int i = 0; i < Enemies.Count; i++)
        {
            if(Enemies[i].GetComponent<Enemy>().Deflected())
            {
                GameObject.DestroyObject(Enemies[i]);
                Enemies.RemoveAt(i);
                --i;
            }
        }


   }

    void CreateEnemy()
    {
        GameObject E = new GameObject();
        E.AddComponent<SpriteRenderer>();
        E.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Enemy");
        E.transform.SetParent(this.transform);
        E.transform.Translate(new Vector2(rand.Next(-3, 3), 2+rand.Next(-3, 3)));
        E.AddComponent<Enemy>();
        E.AddComponent<BoxCollider2D>();
        Enemies.Add(E);
    }
}
