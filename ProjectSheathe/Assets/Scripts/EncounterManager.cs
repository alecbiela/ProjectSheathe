using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EncounterManager : MonoBehaviour
{
    //PREFABS
    public GameObject enemyPrefab;
    public GameObject yourEnemyPrefabHere;  //after you add it here, make sure to head down to the array to include it
    
    List<GameObject> enemies = new List<GameObject>();
    List<int> activeEnemies = new List<int>();
    Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();

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
    private int stunnedEnemyCount = 0;
    private int[] quadrantCounts;
    System.Random rand2 = new System.Random();

    void Awake()
    {       
        //add your prefab to the dictionary here
        enemyPrefabs.Add("basic", enemyPrefab);   


        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerScript = Player.GetComponent<Character>();
        speedMod = 1f;
        baseSpeed = speedMod;
        //slowSpeed = baseSpeed - PlayerScript.overclockMod;
        slowSpeed = .3f;
        //Debug.Log("done");
        maxEnemyNumber = BASE_ENEMY_COUNT;
        extraEnemies = 0;
        quadrantCounts = new int[] { 0, 0, 0, 0 };
    }

    // Update is called once per frame
    void Update()
    {
        if(enemies.Count <= 0) Spawn();
        DynamicDifficulty();
        ManageAttacks();

        //Slice Box collision moved to Enemy.cs
        //Basic Attack collision moved to Enemy.cs
        //Deflection Handling moved to Bullet.cs
        //Enemy getting hit by own bullet moved to Enemy.cs

        // update enemies
        for (int i = 0; i < enemies.Count; i++)
        {
            // enable enemies to be hit again once the player isn't using basic attack or actively slicing
            if (PlayerScript.Attacking == false && PlayerScript.Slicing == false)
            {
                enemies[i].GetComponent<Enemy>().hitRecently = false;
            }

            // kill enemies if at 0 health or lower
            if (enemies[i].GetComponent<Enemy>().health <= 0)
            {
                //Debug.Log("RIP");
                UpdateQuadrants(enemies[i].transform.position);
                GameObject.DestroyObject(enemies[i]);
                enemies.RemoveAt(i);
                PlayerScript.score += 25;
                break;
            }

            // if the player is overclocking, then kill all stunned enemies
            if (PlayerScript.Overclocking == true && enemies[i].GetComponent<Enemy>().stunned == true)
            {
                if (PlayerScript.killStunnedEnemies == true)
                {
                    //Debug.Log("OC RIP");
                    UpdateQuadrants(enemies[i].transform.position);
                    GameObject.DestroyObject(enemies[i]);
                    enemies.RemoveAt(i);
                    PlayerScript.score += 100;
                    if (i == enemies.Count - 1)
                    {
                        PlayerScript.killStunnedEnemies = false;
                    }
                }
            }
        }
    }

    //scales the speed based on how many enemies are alive/stunned
    void DynamicDifficulty()
    {
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

    //spawns a new wave of enemies once all have been defeated
    void Spawn()
    {
        //calculates number of enemies to spawn (only does this when needed now, as opposed to every frame)
        maxEnemyNumber = BASE_ENEMY_COUNT + (int)(PlayerScript.score / 300);

        for(int i = 0; i < maxEnemyNumber; i++)
        {
            CreateEnemy("basic");
        }

        //update attack time so that enemies do not attack immediately
        time = rand.Next(2, 4) + 1.1666f;

        //if we ever have a wave variable, increment it here
    }

    //Instantiates a new Enemy
    //Takes a string for the enemy name (MUST match a Dictionary key for the prefab you want)
    void CreateEnemy(string name)
    {
        GameObject E = (GameObject)Instantiate(enemyPrefabs[name]);
        //E.transform.SetParent(this.transform);  //kind of get why this is here, but can it be avoided?

        //finds the quadrant with the least enemies
        int targetQuadrant = LeastPopulatedQuadrant();
        Vector2 targetPos = new Vector2();

        //finds a random position for the enemy 
        switch(targetQuadrant)
        {
            case 0: //Q1, X(0,10) Y(0,8)
                targetPos.x = (float)(10 * rand.NextDouble());
                targetPos.y = (float)(8 * rand.NextDouble());
                break;
            case 1: //Q2, X(-10,0) Y(0,8)
                targetPos.x = (float)(10 * rand.NextDouble()) - 10;
                targetPos.y = (float)(8 * rand.NextDouble());
                break;
            case 2: //Q3, X(-10,0) Y(-8,0)
                targetPos.x = (float)(10 * rand.NextDouble()) - 10;
                targetPos.y = (float)(8 * rand.NextDouble()) - 8;
                break;
            case 3: //Q4, X(0,10) Y(-8,0)
                targetPos.x = (float)(10 * rand.NextDouble());
                targetPos.y = (float)(8 * rand.NextDouble()) - 8;
                break;
            default:
                Debug.Log("Target Quadrant index out of bounds.");
                break;
        }


        quadrantCounts[targetQuadrant]++;
        E.transform.position = targetPos;
        E.GetComponent<Enemy>().lineRendererComponent = E.GetComponent<LineRenderer>();
        //E.GetComponent<Enemy>().origin = E.transform;
        //E.GetComponent<Enemy>().destination = Player.transform;
        enemies.Add(E);
    }


    //returns the lowest-populated quadrant
    int LeastPopulatedQuadrant()
    {
        int lpq = 0;
        int lpqv = quadrantCounts[0];

        for(int i=1; i<4; i++)
        {
            if (quadrantCounts[i] < lpqv)
            {
                lpqv = quadrantCounts[i];
                lpq = i;
            }
        }

        return lpq;
    }

    //decrement the quadrant the enemy was in (called when enemy is being destroyed)
    void UpdateQuadrants(Vector2 pos)
    {
        if(pos.x >= 0)
        {
            if(pos.y >= 0) { quadrantCounts[0]--; }
            else { quadrantCounts[3]--; }
        }
        else
        {
            if(pos.y >= 0) { quadrantCounts[1]--; }
            else { quadrantCounts[2]--; }
        }
    }

    //Decides when enemies should attack
    void ManageAttacks()
    {
        time -= Time.deltaTime * speedMod;

        if (time <= 0 && (activeEnemies = GetActiveEnemies()).Count >= 1)
        {
                    
            //choose a random active enemy
            randomEnemy = activeEnemies[rand.Next(activeEnemies.Count)];

            //if there is more than 1 enemy left, have a chance of 2 attacking at the same time
            if (activeEnemies.Count > 1 && rand.Next(2) == 1)
            {
                int randomEnemy2 = activeEnemies[rand2.Next(activeEnemies.Count)];

                //ensure no duplicates
                while (randomEnemy == randomEnemy2)
                {
                   randomEnemy2 = activeEnemies[rand2.Next(activeEnemies.Count)];
                }

                enemies[randomEnemy2].GetComponent<Enemy>().Fire();
            }
            enemies[randomEnemy].GetComponent<Enemy>().Fire();

            // wait some time before firing again. This value should at least be 1.1666f since that is how long it takes for an enemy to fire.
            time = rand.Next(2, 4) + 1.1666f;
        }
    }

    //gives stunned enemies second wind when the player is hit (called from Player script)
    public void SecondWind()
    {
        Enemy e;

        for(int i=0; i<enemies.Count; i++)
        {
            e = enemies[i].GetComponent<Enemy>();
            if(e.stunned)
            {
                e.secondWind = true;
            }
        }
    }

    //gets a list of indices of active enemies
    private List<int> GetActiveEnemies()
    {
        List<int> activeIndicies = new List<int>();

        //add the indices of the enemies that aren't stunned
        for(int i=0; i < enemies.Count; i++)
        {
            if (!enemies[i].GetComponent<Enemy>().stunned) activeIndicies.Add(i);
        }

        return activeIndicies;
    }

    
    
    //future method to be used to Introduce new enemies
    //introduces a new enemy and displays a tooltip on how to defeat it
    //takes the name of the enemy to introduce
    private void Introduce(int id)
    {
        switch(id)
        {
            case 0: //your enemy name here
                break;
            case 1:
                break;
            default:
                Debug.Log("Tried to introduce enemy " + id + " but case didn't exist.");
                break;
        }
    }
}
