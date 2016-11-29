using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EncounterManager : MonoBehaviour
{
    public GameObject enemyPrefab;
    List<GameObject> Enemies = new List<GameObject>();
    // Use this for initialization
    System.Random rand = new System.Random();
    private GameObject Player;
    private Character PlayerScript;
    [SerializeField] public float speedMod; // Enemy and bullet speed modifier
    public float baseSpeed { get; private set; }
    public float slowSpeed { get; private set; }
    private int maxEnemyNumber;
    private bool hitRecently;
    private const int BASE_ENEMY_COUNT = 5;
    private int extraEnemies;
    private float time = 0;
    private int randomEnemy;
    private int notStunnedEnemyCount;
    private int stunnedEnemyCount;
    System.Random rand2 = new System.Random();

    void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerScript = Player.GetComponent<Character>();
        speedMod = 1f;
        baseSpeed = speedMod;
        //slowSpeed = baseSpeed - PlayerScript.overclockMod;
        slowSpeed = .3f;
        //Debug.Log("done");
        maxEnemyNumber = BASE_ENEMY_COUNT;
        extraEnemies = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Spawn();
        DynamicDifficulty();
        ManageAttacks();

        //Slice Box collision moved to Enemy.cs
        //Basic Attack collision moved to Enemy.cs
        //Deflection Handling moved to Bullet.cs
        //Enemy getting hit by own bullet moved to Enemy.cs

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
        if (PlayerScript.Overclocking == false && speedMod < 3.0)
        {
            //float speedBonus = Mathf.Floor(PlayerScript.score / 1200);
            //speedMod = 1 + .1f * Mathf.Floor(PlayerScript.score / 1200); // this should probably scale by stunned enemy count, not score // the second this gets incremented once, it hits max
            speedMod = 1 + .1f * Mathf.Floor(stunnedEnemyCount / 2);
            baseSpeed = speedMod;
            slowSpeed = baseSpeed - PlayerScript.overclockMod;
        }
        if (speedMod > 3.0)
        {
            speedMod = 3.0f;
        }
        //Debug.Log(baseSpeed);
        //Debug.Log(slowSpeed);
        // other enemy types
    }

    void Spawn()
    {
        notStunnedEnemyCount = 0;
        stunnedEnemyCount = 0;

        if (Enemies.Count != 0)
        {
            for (int x = 0; x < Enemies.Count; x++)
            {
                if (Enemies[x].GetComponent<Enemy>().stunned == false)
                {
                    notStunnedEnemyCount++;
                }
                else
                {
                    stunnedEnemyCount++;
                }
            }
        }

        if (notStunnedEnemyCount < maxEnemyNumber)
        {
            //Debug.Log("In here fam");
            CreateEnemy();
        }
    }

    //Instantiates a new Enemy
    void CreateEnemy()
    {
        GameObject E = (GameObject)Instantiate(enemyPrefab);
        E.transform.SetParent(this.transform);  //kind of get why this is here, but can it be avoided?
        E.transform.position = new Vector2(rand.Next(-11, 11), rand2.Next(-4, 6));
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
            int attackPattern = rand2.Next(0, 2); // choose attack pattern
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
                case 1: // Double Attack
                    {
                        //Debug.Log("Double called");
                        // keep choosing a random enemy until one that isn't stunned is found
                        randomEnemy = rand.Next(0, Enemies.Count);
                        int randomEnemy2 = rand2.Next(0, Enemies.Count);

                        while (Enemies[randomEnemy].GetComponent<Enemy>().stunned == true)
                        {
                            randomEnemy = rand.Next(0, Enemies.Count);
                        }

                        if (Enemies.Count > 1)
                        {
                            while (Enemies[randomEnemy].GetComponent<Enemy>().stunned == true || Enemies[randomEnemy] == Enemies[randomEnemy2])
                            {
                                randomEnemy2 = rand2.Next(0, Enemies.Count);
                            }
                            Enemies[randomEnemy2].GetComponent<Enemy>().Fire();
                        }
                        Enemies[randomEnemy].GetComponent<Enemy>().Fire();
                        //Debug.Log("Fire");
                        break;
                    }
            }
            // wait some time before firing again. This value should at least be 1.1666f since that is how long it takes for an enemy to fire.
            time = rand.Next(2, 3) + 1.1666f;
        }
    }
}
