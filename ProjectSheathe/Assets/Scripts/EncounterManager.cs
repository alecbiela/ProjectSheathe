using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class EncounterManager : MonoBehaviour
{
    //PREFABS
    public GameObject b451cPrefab;
    public GameObject lockPrefab;  //after you add it here, make sure to head down to the array to include it
    public GameObject lightPrefab;
    public GameObject lunkPrefab;
    public GameObject slobPrefab;
    public GameObject guardianPrefab;
    public GameObject medicPrefab;
    public GameObject hotBoxPrefab;

    List<GameObject> enemies = new List<GameObject>(); // all enemies
    //List<GameObject> guards = new List<GameObject>(); // only guards
    //List<GameObject> officers = new List<GameObject>(); // only officers
    List<int> activeEnemies = new List<int>();
    Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();
    public List<Vector3> stunnedEnemyPositions { get; private set; }

    // Use this for initialization
    System.Random rand = new System.Random();
    private GameObject Player;
    private Character PlayerScript;
    [SerializeField] public float speedMod; // Enemy and bullet speed modifier
    public float baseSpeed { get; private set; }
    public float slowSpeed { get; private set; }
    private int maxEnemyNumber;
    private int maxOfficerNumber;
    private bool hitRecently;
    private const int BASE_ENEMY_COUNT = 5;
    private const int ABSOLUTE_MAX_GUARD_COUNT = 20;
    private const int ABSOLUTE_MAX_OFFICER_NUMBER = 4;
    private int extraEnemies;
    private float time = 0;
    private int randomEnemy;
    private int notStunnedEnemyCount;
    private int stunnedEnemyCount = 0;
    private Dictionary<int, int> quadrants; // Quadrant id, quadrant count
    private int secondWindCounter = 0;
    public int extraWind = 0; // for use in recovering a stock of the second wind counter
    System.Random rand2 = new System.Random();
    [SerializeField] private GameObject[] officerSpawns;
    private int wave = 0;
    private Text waveUIText;
    private GameObject secondWindUIElement;

    void Awake()
    {       
        //add your prefab to the dictionary here
        // Guards
        enemyPrefabs.Add("b451c", b451cPrefab);
        enemyPrefabs.Add("lock", lockPrefab);
        enemyPrefabs.Add("light", lightPrefab);
        enemyPrefabs.Add("lunk", lunkPrefab);
        // Officers
        enemyPrefabs.Add("slob", slobPrefab);
        enemyPrefabs.Add("guardian", guardianPrefab);
        enemyPrefabs.Add("medic", medicPrefab);
        enemyPrefabs.Add("hotBox", hotBoxPrefab);

        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerScript = Player.GetComponent<Character>();
        waveUIText = GameObject.FindGameObjectWithTag("WaveElement").GetComponent<Text>();
        setWave();
        secondWindUIElement = GameObject.FindGameObjectWithTag("SecondWindElement");
        speedMod = 1f;
        baseSpeed = speedMod;
        //slowSpeed = baseSpeed - PlayerScript.overclockMod;
        slowSpeed = .3f;
        //Debug.Log("done");
        maxEnemyNumber = BASE_ENEMY_COUNT;
        maxOfficerNumber = 0;
        extraEnemies = 0;
        quadrants = new Dictionary<int, int> { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
    }

    // Update is called once per frame
    void Update()
    {
        //secondWindCounter += extraWind;
        //if (secondWindCounter > 3) secondWindCounter = 3;
        //extraWind = 0;

        //update active enemy list as well as stunned enemy position list
        UpdateEnemyLists();
        if(enemies.Count <= 0) Spawn();
        DynamicDifficulty();
        ManageAttacks();

        //Slice Box collision moved to Enemy.cs
        //Basic Attack collision moved to Enemy.cs
        //Deflection Handling moved to Bullet.cs
        //Enemy getting hit by own bullet moved to Enemy.cs

        // update enemies

        stunnedEnemyCount = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            
            // count stunned enemies
            if (enemies[i].GetComponent<Enemy>().stunned == true)
            {
                stunnedEnemyCount++;
            }

            if(stunnedEnemyCount == enemies.Count && PlayerScript.Overclocking == false) // if all enemies are stunned, give the player Overclock
            {
                PlayerScript.overclockCooldown = 0;
            }
            
            // enable enemies to be hit again once the player isn't using basic attack or actively slicing
            if (PlayerScript.Attacking == false && PlayerScript.Slicing == false)
            {
                enemies[i].GetComponent<Enemy>().hitRecently = false;
            }

            // kill enemies if at 0 health or lower
            if (enemies[i].GetComponent<Enemy>().health <= 0)
            {
                //Debug.Log("RIP");
                if (enemies[i].GetComponent<Enemy>().rank == "officer")
                {
                    PlayerScript.score += 75;
                    PlayerScript.setScore();
                }
                else if (enemies[i].GetComponent<Enemy>().rank == "guard")
                {
                    PlayerScript.score += 50;
                    PlayerScript.setScore();
                }
                UpdateQuadrants(enemies[i].transform.position);
                if (enemies[i].GetComponent<Enemy>().type == "Light" || enemies[i].GetComponent<Enemy>().type == "Lunk" || enemies[i].GetComponent<Enemy>().type == "SLOB")
                {
                    Destroy(enemies[i].GetComponent<Enemy>().special.gameObject); // Destroy laser or shield as well
                    enemies[i].GetComponent<Enemy>().special = null;
                }
                Destroy(enemies[i]);
                enemies.RemoveAt(i);
                break;
            }
        }
    }

    //kills all stunned enemies, called when the player overclocks (from character.cs)
    public void KillStunnedEnemies()
    {
        List<int> enemiesToKill = new List<int>();

        //find out which enemies we should kill
        for(int i=0; i<enemies.Count; i++)
        {
            if(enemies[i].GetComponent<Enemy>().stunned)
            {
                enemiesToKill.Add(i);
            }
        }

        //once we have the list of enemies to kill, actually kill them
        for(int i = (enemiesToKill.Count - 1); i >= 0; i--)
        {
            if (enemies[i].GetComponent<Enemy>().rank == "officer")
            {
                PlayerScript.score += 150;
                PlayerScript.setScore();
            }
            else if (enemies[i].GetComponent<Enemy>().rank == "guard")
            {
                PlayerScript.score += 100;
                PlayerScript.setScore();
            }
            UpdateQuadrants(enemies[enemiesToKill[i]].transform.position);
            if (enemies[enemiesToKill[i]].GetComponent<Enemy>().type == "Light" || enemies[enemiesToKill[i]].GetComponent<Enemy>().type == "Lunk" || enemies[enemiesToKill[i]].GetComponent<Enemy>().type == "SLOB")
            {
                Destroy(enemies[enemiesToKill[i]].GetComponent<Enemy>().special.gameObject); // Destroy laser or shield as well
                enemies[enemiesToKill[i]].GetComponent<Enemy>().special = null;
            }
            GameObject.DestroyObject(enemies[enemiesToKill[i]]);
            enemies.RemoveAt(enemiesToKill[i]);
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
            //Debug.Log("SEC: " + stunnedEnemyCount);
            baseSpeed = speedMod;
            slowSpeed = baseSpeed - PlayerScript.overclockMod;
        }
        if (speedMod > 3.0f)
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
        for (int i = 0; i < 4; i++)
        {
            officerSpawns[i].GetComponent<OfficerSpawnPoint>().filled = false;
        }
        // get rid of all lingering lasers upon spawn
        GameObject[] lingeringLasers = GameObject.FindGameObjectsWithTag("Laser");
        if (lingeringLasers.Length != 0)
        {
            foreach (GameObject laser in lingeringLasers)
            {
                GameObject.DestroyObject(laser);
                //Debug.Log("Destroyed lingering laser");
            }
        }

        // reset the chunk or "secondWindCounter"
        secondWindCounter = 0;
        for (int i = 0; i < 3; i++)
        {
            secondWindUIElement.transform.GetChild(i).gameObject.SetActive(true);
        }

        //calculates number of enemies to spawn (only does this when needed now, as opposed to every frame) CHANGE THIS SECTION IF MAXENEMY SHOULD BE MORE THAN GUARDS
        maxEnemyNumber = BASE_ENEMY_COUNT + (int)(PlayerScript.score / 650);
        if (maxEnemyNumber > ABSOLUTE_MAX_GUARD_COUNT) maxEnemyNumber = ABSOLUTE_MAX_GUARD_COUNT;

        // spawn officers first but only when the number of officers that should spawn changes, so every score threshold
        int oldMaxOfficerNumber = maxOfficerNumber;
        maxOfficerNumber = 0 + (int)(PlayerScript.score / 1200); // should really be around 1000
        if (maxOfficerNumber > ABSOLUTE_MAX_OFFICER_NUMBER) maxOfficerNumber = ABSOLUTE_MAX_OFFICER_NUMBER;

        if(maxOfficerNumber >= ABSOLUTE_MAX_OFFICER_NUMBER || oldMaxOfficerNumber != maxOfficerNumber)
        {
            //Debug.Log("spawn:" + maxOfficerNumber);
            int slobs = 0;
            int medics = 0;
            int guardians = 0;
            //int hotboxes = 0;
            for (int i = 0; i < maxOfficerNumber; i++)
            {
                int officerType = rand.Next(0, 3);
                switch (officerType)
                {
                    case 0:
                    case 3: // REMOVE WHEN HOTBOX IS FIXED
                        //Debug.Log("slob");
                        if (slobs >= 4) i--; // CHANGE BACK TO 2 WHEN HOTBOX IS FIXED
                        else CreateEnemy("slob"); slobs++;
                        break;
                    case 1:
                        //Debug.Log("medic");
                        if (medics >= 2) i--;
                        else CreateEnemy("medic"); medics++;
                        break;
                    case 2:
                        //Debug.Log("guardian");
                        if (guardians >= 2) i--;
                        else CreateEnemy("guardian"); guardians++;
                        break;
                    //case 3:
                        //if (hotboxes >= 4) i--;
                        //else CreateEnemy("hotbox"); hotboxes++;
                        //break;
                }
            }
        }

        int b451cs = 0;
        int lights = 0;
        int locks = 0;
        int lunks = 0;

        //spawns guards
        for (int i = 0; i < maxEnemyNumber; i++)
        {
            int enemyType = rand.Next(0, 4);
            switch(enemyType)
            {
                case 0:
                    if (b451cs >= 6) i--;
                    else CreateEnemy("b451c"); b451cs++;
                    break;
                case 1:
                    if (lunks >= 6) i--;
                    else CreateEnemy("lunk"); lunks++;
                    break;
                case 2:
                    if (locks >= 4) i--;
                    else CreateEnemy("lock"); locks++;
                    break;
                case 3:
                    if (lights >= 4) i--;
                    else CreateEnemy("light"); lights++;
                    break;
            }
        }

        //update attack time so that enemies do not attack immediately
        time = rand.Next(2, 4) + 1.1666f;

        //if we ever have a wave variable, increment it here
        wave++;
        setWave();
    }

    private void setWave()
    {
        waveUIText.text = wave.ToString();
    }

    //Instantiates a new Enemy
    //Takes a string for the enemy name (MUST match a Dictionary key for the prefab you want)
    void CreateEnemy(string name)
    {
        int[] orderedQuads = PopulatedQuadrantOrder();
        //Debug.Log("Quad Order: " + orderedQuads[0] + "," + orderedQuads[1] + "," + orderedQuads[2] + "," + orderedQuads[3]);
        GameObject E = (GameObject)Instantiate(enemyPrefabs[name]);
        //E.transform.SetParent(this.transform);  //kind of get why this is here, but can it be avoided?

        int targetQuad;
        Vector2 targetPos = new Vector2();
        if (name == "slob" || name == "medic" || name == "guardian" || name == "hotbox")
        {
            bool positionFilled = false;
            int i = 0;
            do // Spawn officer in the most populated quad that is not filled
            {
                i++;
                targetQuad = orderedQuads[orderedQuads.Length - i];
                positionFilled = officerSpawns[targetQuad].GetComponent<OfficerSpawnPoint>().filled;
                //Debug.Log("Tried Off Quad:" + (orderedQuads.Length - i) + "filled?:" + positionFilled);
            } while (positionFilled);
            targetPos = officerSpawns[targetQuad].transform.position;
            officerSpawns[targetQuad].GetComponent<OfficerSpawnPoint>().filled = true; // Fill the quad on spawn
        }
        else
        {
            targetQuad = orderedQuads[0];
            //Debug.Log("Guard in quad:" + targetQuad);

            //finds a random position for the enemy 
            switch (targetQuad)
            {
                case 0: //Q1, X(0,12) Y(0,8.5)
                    targetPos.x = (float)(12 * rand.NextDouble());
                    targetPos.y = (float)(8.5 * rand.NextDouble());
                    break;
                case 1: //Q2, X(-12,0) Y(0,8.5)
                    targetPos.x = (float)(-12 * rand.NextDouble());
                    targetPos.y = (float)(8.5 * rand.NextDouble());
                    break;
                case 2: //Q3, X(-12,0) Y(-8.5,0)
                    targetPos.x = (float)(-12 * rand.NextDouble());
                    targetPos.y = (float)(-8.5 * rand.NextDouble());
                    break;
                case 3: //Q4, X(0,12) Y(-8.5,0)
                    targetPos.x = (float)(12 * rand.NextDouble());
                    targetPos.y = (float)(-8.5 * rand.NextDouble());
                    break;
                default:
                    //Debug.Log("Target Quadrant index out of bounds.");
                    break;
            }
        }


        quadrants[targetQuad]++;
        E.transform.position = targetPos;
        foreach (GameObject enemy in enemies)
        {
            if (E.GetComponent<Collider2D>().IsTouching(enemy.GetComponent<Collider2D>()))  // This is not working, and I'm not sure why
            {
                //Debug.Log("ECH HALP");
                DestroyImmediate(E);
                Respawn(name);
                return;
            }
        }
        E.GetComponent<Enemy>().lineRendererComponent = E.GetComponent<LineRenderer>();
        //E.GetComponent<Enemy>().origin = E.transform;
        //E.GetComponent<Enemy>().destination = Player.transform;
        enemies.Add(E);
    }


    //returns the quadrants in order of population low -> high
    int[] PopulatedQuadrantOrder()
    {
        //Debug.Log("Quad Pops: " + quadrants[0] + "," + quadrants[1] + "," + quadrants[2] + "," + quadrants[3]);
        IEnumerable<KeyValuePair<int, int>> orderedQuadsEnum = quadrants.OrderBy(pair => pair.Value).Take(4); // Order it based on count
        int[] orderedQuads = { 0, 0, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            orderedQuads[i] = orderedQuadsEnum.ElementAt(i).Key; // Get the actual quadrant index
        }
        return orderedQuads;
    }

    //decrement the quadrant the enemy was in (called when enemy is being destroyed)
    void UpdateQuadrants(Vector2 pos)
    {
        if(pos.x >= 0)
        {
            if(pos.y >= 0) { quadrants[0]--; }
            else { quadrants[3]--; }
        }
        else
        {
            if(pos.y >= 0) { quadrants[1]--; }
            else { quadrants[2]--; }
        }
    }

    //Decides when enemies should attack
    void ManageAttacks()
    {
        time -= Time.deltaTime * speedMod;

        if (time <= 0 && activeEnemies.Count >= 1)
        {
            int attackPattern = rand.Next(0, 2); // make the max a variable that is determined by the dynamic difficulty in the future?
            switch(attackPattern)
            {
                case 0: // Single Attack
                    //choose a random active enemy
                    randomEnemy = activeEnemies[rand.Next(activeEnemies.Count)];
                    enemies[randomEnemy].GetComponent<Enemy>().Fire();
                    break;
                case 1: // Double Attack
                    //Debug.Log("Double");
                    //choose a random active enemy
                    randomEnemy = activeEnemies[rand.Next(activeEnemies.Count)];

                    //if there is more than 1 enemy left, 2 attack at the same time
                    if (activeEnemies.Count > 1)
                    {
                        int randomEnemy2 = activeEnemies[rand2.Next(activeEnemies.Count)];

                        //ensure no duplicates
                        while (randomEnemy == randomEnemy2)
                        {
                            randomEnemy2 = activeEnemies[rand2.Next(activeEnemies.Count)];
                        }

                        enemies[randomEnemy].GetComponent<Enemy>().Fire();
                        enemies[randomEnemy2].GetComponent<Enemy>().Fire();
                    }
                    else
                    {
                        enemies[randomEnemy].GetComponent<Enemy>().Fire();
                    }
                    break;
            }
            

            // wait some time before firing again. This value should at least be 1.1666f since that is how long it takes for an enemy to fire.
            time = rand.Next(2, 4) + 1.1666f;
        }
    }

    //gives stunned enemies second wind when the player is hit (called from Player script)
    public void SecondWind()
    {
        if (secondWindCounter < 2) // 3rd hit second wind
        {
            secondWindUIElement.transform.GetChild(secondWindCounter).gameObject.SetActive(false);
            secondWindCounter++;
            //Debug.Log("SWC: " + secondWindCounter);
        }
        else
        {
            secondWindUIElement.transform.GetChild(secondWindCounter).gameObject.SetActive(false);
            Enemy e;

            for (int i = 0; i < enemies.Count; i++)
            {
                e = enemies[i].GetComponent<Enemy>();
                if (e.stunned)
                {
                    e.secondWind = true;
                }
            }
            secondWindCounter = 0;
            for (int i = 0; i < 3; i++)
            {
                secondWindUIElement.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }

    //update the bins of enemies for handling in other methods and scripts
    public void UpdateEnemyLists()
    {
        //loop through all enemies
        List<int> activeIndicies = new List<int>();
        List<Vector3> positions = new List<Vector3>();

        //if stunned, add positions to stunned enemies list, otherwise they are active
        for (int i = 0; i < enemies.Count; i++)
        {
            //if we wanted to get all damaged enemies, not just stunned (for medics),
            //we check HERE based on health, and not stunned flag
            if (!enemies[i].GetComponent<Enemy>().stunned)
            {
                if(enemies[i].GetComponent<Enemy>().type != "Medic")
                    activeIndicies.Add(i);
            }
            else positions.Add(enemies[i].transform.position);
        }

        //update the actual lists
        activeEnemies = activeIndicies;
        stunnedEnemyPositions = positions;
    }

    //respawns an enemy if it landed on top of another one when it was spawned
    //takes the type of enemy, passed from enemy.cs
    public void Respawn(string type)
    {
        CreateEnemy(type);
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
