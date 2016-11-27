using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EncounterManager : MonoBehaviour
{

    List<GameObject> Enemies = new List<GameObject>();
    // Use this for initialization
    System.Random rand = new System.Random();
    private GameObject Player;
    private Character PlayerScript;
    [HideInInspector] public float speedMod; // Enemy and bullet speed modifier
    public float baseSpeed { get; private set; }
    public float slowSpeed { get; private set; }
    private int maxEnemyNumber;
    private bool hitRecently;
    private const int BASE_ENEMY_COUNT = 5;
    private int extraEnemies;
    private float time = 0;
    private int randomEnemy;
    System.Random rand2 = new System.Random();

    void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerScript = Player.GetComponent<Character>();
        speedMod = 1f;
        baseSpeed = speedMod;
        //slowSpeed = baseSpeed - PlayerScript.overclockMod;
        slowSpeed = .3f;
        Debug.Log("done");
        maxEnemyNumber = BASE_ENEMY_COUNT;
        extraEnemies = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Spawn();
        DynamicDifficulty();
        ManageAttacks();

        if (PlayerScript.Slicing)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                for (int j = 0; j < PlayerScript.sliceHitBoxes.Length; j++)
                {
                    if (Enemies[i].GetComponent<BoxCollider2D>().IsTouching(
                        PlayerScript.sliceHitBoxes[j].GetComponent<BoxCollider2D>()))
                    {
                        if (Enemies[i].GetComponent<Enemy>().hitRecently == false)
                        {
                            Enemies[i].GetComponent<Enemy>().health--;
                        }
                        //--i;
                        Enemies[i].GetComponent<Enemy>().hitRecently = true;
                        break;
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
                        if (Enemies[i].GetComponent<Enemy>().hitRecently == false)
                        {
                            //Debug.Log("Hit");
                            if (PlayerScript.Overclocking == false)
                            {
                                Enemies[i].GetComponent<Enemy>().health--;
                                //Debug.Log("BA: 1 HP lost");
                            }
                            else
                            {
                                Enemies[i].GetComponent<Enemy>().health = Enemies[i].GetComponent<Enemy>().health - 2;
                                //Debug.Log("BA: 2 HP lost");
                            }
                            Enemies[i].GetComponent<Enemy>().hitRecently = true;
                            //--i;//decrement to avoid skipping an enemy
                        }
                        break;
                    }
                }
            }
        }
        else if (PlayerScript.Deflecting)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                if (Enemies[i].GetComponent<Enemy>().Bullet.GetComponent<BoxCollider2D>().IsTouching
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
            if (Enemies[i].GetComponent<Enemy>().Deflected())
            {
                Enemies[i].GetComponent<Enemy>().health--;
                //--i;
                break;
            }
        }

        // update enemies
        for (int i = 0; i < Enemies.Count; i++)
        {
            // enable enemies to be hit again once the player isn't using basic attack or actively slicing
            if (PlayerScript.Attacking == false && PlayerScript.Slicing == false)
            {
                Enemies[i].GetComponent<Enemy>().hitRecently = false;
            }

            // give enemies their second wind if the player has been hit
            if (PlayerScript.playerHit == true)
            {
                if (Enemies[i].GetComponent<Enemy>().stunned == true)
                {
                    Enemies[i].GetComponent<Enemy>().secondWind = true;
                }
                if (i == Enemies.Count - 1)
                {
                    PlayerScript.playerHit = false;
                }
            }

            // kill enemies if at 0 health or lower or if they are stunned when the player enters Overclock
            if (Enemies[i].GetComponent<Enemy>().health <= 0)
            {
                //Debug.Log("RIP");
                GameObject.DestroyObject(Enemies[i]);
                Enemies.RemoveAt(i);
                PlayerScript.score += 25;
                //--i;
                break;
            }

            if (PlayerScript.Overclocking == true && Enemies[i].GetComponent<Enemy>().stunned == true)
            {
                if (PlayerScript.killStunnedEnemies == true)
                {

                    //Debug.Log("OC RIP");
                    GameObject.DestroyObject(Enemies[i]);
                    Enemies.RemoveAt(i);
                    PlayerScript.score += 100;
                    if (i == Enemies.Count - 1)
                    {
                        PlayerScript.killStunnedEnemies = false;
                    }
                }
            }
        }

    }

    void DynamicDifficulty()
    {
        // more enemies
        extraEnemies = (int)Mathf.Floor(PlayerScript.score / 1000);

        if (extraEnemies > 5)
        {
            extraEnemies = 5;
        }

        maxEnemyNumber = BASE_ENEMY_COUNT + extraEnemies;

        // faster enemies
        if (PlayerScript.Overclocking == false && speedMod < 2)
        {
            speedMod = speedMod + .1f * Mathf.Floor(PlayerScript.score / 800);
            baseSpeed = speedMod;
            slowSpeed = baseSpeed - PlayerScript.overclockMod;
        }
        if (speedMod > 1.8)
        {
            speedMod = 1.8f;
        }
        //Debug.Log(baseSpeed);
        //Debug.Log(slowSpeed);
        // other enemy types
    }

    void Spawn()
    {
        int notStunnedEnemyCount = 0;
        if (Enemies.Count != 0)
        {
            for (int x = 0; x < Enemies.Count; x++)
            {
                if (Enemies[x].GetComponent<Enemy>().stunned == false)
                {
                    notStunnedEnemyCount++;
                }
            }
        }

        if (notStunnedEnemyCount < maxEnemyNumber)
        {
            //Debug.Log("In here fam");
            CreateEnemy();
        }
    }

    void CreateEnemy()
    {
        GameObject E = new GameObject();
        E.AddComponent<SpriteRenderer>();
        E.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Enemy");
        E.transform.SetParent(this.transform);
        //E.transform.Translate(new Vector2(rand.Next(-23, 0), 2+rand.Next(-5, 6)));
        E.transform.position = new Vector2(rand.Next(-11, 11), rand2.Next(-4, 6));
        E.AddComponent<Enemy>();
        E.AddComponent<BoxCollider2D>();
        E.GetComponent<Enemy>().health = 3;
        E.GetComponent<Enemy>().secondWind = false;
        E.GetComponent<Enemy>().stunned = false;
        Enemies.Add(E);
    }

    void ManageAttacks()
    {
        if (time != 0) // decrement time for anything that may need it
        {
            time -= Time.deltaTime;
        }

        // check if any enemy is currently attacking. If so, exit the method
        for (int x = 0; x < Enemies.Count; x++)
        {
            if (Enemies[x].GetComponent<Enemy>().stunned == false && Enemies[x].GetComponent<Enemy>().attacking == true)
            {
                return;
            }
        }

        if (time <= 0)
        {
            int attackPattern = rand2.Next(0, 0); // choose attack pattern
            switch (attackPattern)
            {
                case 0: // Single Attack
                    {
                        // keep choosing a random enemy until one that isn't stunned is found
                        randomEnemy = rand.Next(0, Enemies.Count);

                        while (Enemies[randomEnemy].GetComponent<Enemy>().stunned == true)
                        {
                            randomEnemy = rand.Next(0, Enemies.Count);
                        }

                        Enemies[randomEnemy].GetComponent<Enemy>().Fire();
                        //Debug.Log("Fire");
                        break;
                    }
            }
            // wait some time before firing again. This value MUST at least be 1.1666f since that is how long it takes for an enemy to fire.
            time = rand.Next(2, 3) + 1.1666f;
        }
    }
}
