using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class EncounterManager : MonoBehaviour
{
    /* Prefabs */
    public GameObject B451CPrefab;
    public GameObject LockPrefab;
    public GameObject LightPrefab;
    public GameObject LunkPrefab;
    public GameObject SLOBPrefab;
    public GameObject GuardianPrefab;
    public GameObject MedicPrefab;
    public GameObject HotBoxPrefab;

    List<GameObject> enemies = new List<GameObject>(); // All enemies
    //List<GameObject> Guards = new List<GameObject>(); // only Guards
    //List<GameObject> officers = new List<GameObject>(); // only officers
    List<int> activeEnemies = new List<int>();
    Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();
    public List<Vector3> stunnedEnemyPositions { get; private set; }

    // Use this for initialization
    System.Random rand = new System.Random();
    private GameObject Player;
    [HideInInspector] public Character PlayerScript;
    [SerializeField] public float speedMod; // Enemy and bullet speed modifier
    public float baseSpeed { get; private set; }
    public float slowSpeed { get; private set; }
    private int maxEnemyNumber;
    private int basicMax;
    private int lunkMax;
    private int lockMax;
    private int lightMax;
    private int maxOfficerNumber;
    private const int BASE_ENEMY_COUNT = 5;
    private const int ABSOLUTE_MAX_GUARD_COUNT = 20;
    private const int ABSOLUTE_MAX_OFFICER_NUMBER = 4;
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
    private Text hintUIText;
    private GameObject secondWindUIElement;

    void Awake()
    {       
        //add your prefab to the dictionary here
        // Guards
        enemyPrefabs.Add("B451C", B451CPrefab);
        enemyPrefabs.Add("Lock", LockPrefab);
        enemyPrefabs.Add("Light", LightPrefab);
        enemyPrefabs.Add("Lunk", LunkPrefab);
        // Officers
        enemyPrefabs.Add("SLOB", SLOBPrefab);
        enemyPrefabs.Add("Guardian", GuardianPrefab);
        enemyPrefabs.Add("Medic", MedicPrefab);
        enemyPrefabs.Add("HotBox", HotBoxPrefab);

        Player = GameObject.FindGameObjectWithTag("Player");
        PlayerScript = Player.GetComponent<Character>();
        waveUIText = GameObject.FindGameObjectWithTag("WaveElement").GetComponent<Text>();
        hintUIText = GameObject.FindGameObjectWithTag("Hint").GetComponent<Text>();
        setWave();
        secondWindUIElement = GameObject.FindGameObjectWithTag("SecondWindElement");
        speedMod = 1f;
        baseSpeed = speedMod;
        //slowSpeed = baseSpeed - PlayerScript.overclockMod;
        slowSpeed = .3f;
        //Debug.Log("done");
        maxEnemyNumber = BASE_ENEMY_COUNT;
        basicMax = 6;
        lunkMax = 6;
        lockMax = 4;
        lightMax = 4;
        maxOfficerNumber = 0;
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
            if (enemies[i].GetComponent<Enemy>().stunState == 2)
            {
                stunnedEnemyCount++;
            }

            if(stunnedEnemyCount == enemies.Count && PlayerScript.OverclockState < 2) // If all enemies are stunned, give the player Overclock
            {
                PlayerScript.resetOverclock();
            }
            
            // enable enemies to be hit again once the player isn't using basic attack or actively slicing


            // kill enemies if at 0 health or lower
            if (enemies[i].GetComponent<Enemy>().health <= 0)
            {
                //Debug.Log("RIP");
                if (enemies[i].GetComponent<Enemy>().rank == "Officer")
                {
                    PlayerScript.score += 75;
                    PlayerScript.setScore();
                }
                else if (enemies[i].GetComponent<Enemy>().rank == "Guard")
                {
                    PlayerScript.score += 50;
                    PlayerScript.setScore();
                }
                UpdateQuadrants(enemies[i].transform.position);
                enemies[i].GetComponent<Enemy>().Destructor();
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
            if(enemies[i].GetComponent<Enemy>().stunState == 2)
            {
                enemiesToKill.Add(i);
            }
        }

        //once we have the list of enemies to kill, actually kill them
        for(int i = (enemiesToKill.Count - 1); i >= 0; i--)
        {
            if (enemies[i].GetComponent<Enemy>().rank == "Officer")
            {
                PlayerScript.score += 150;
                PlayerScript.setScore();
            }
            else if (enemies[i].GetComponent<Enemy>().rank == "Guard")
            {
                PlayerScript.score += 100;
                PlayerScript.setScore();
            }
            UpdateQuadrants(enemies[enemiesToKill[i]].transform.position);
            enemies[enemiesToKill[i]].GetComponent<Enemy>().Destructor();
            Destroy(enemies[enemiesToKill[i]]);
            enemies.RemoveAt(enemiesToKill[i]);
        }
    }

    //scales the speed based on how many enemies are alive/stunned
    void DynamicDifficulty()
    {
        // faster enemies
        if (PlayerScript.OverclockState < 2 && speedMod < 3.0)
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
        //increment wave
        wave++;
        setWave();

        // reset hints
        hintUIText.text = "";
        
        for (int i = 0; i < 4; i++)
        {
            officerSpawns[i].GetComponent<OfficerSpawnPoint>().filled = false;
        }

        // reset the chunk or "secondWindCounter"
        secondWindCounter = 0;
        for (int i = 0; i < 3; i++)
        {
            secondWindUIElement.transform.GetChild(i).gameObject.SetActive(true);
        }

        //calculates number of enemies to spawn (only does this when needed now, as opposed to every frame) CHANGE THIS SECTION IF MAXENEMY SHOULD BE MORE THAN GUARDS
        //maxEnemyNumber = BASE_ENEMY_COUNT + (PlayerScript.score / 650);
        if(wave > 3) maxEnemyNumber += 2;
        if (maxEnemyNumber > ABSOLUTE_MAX_GUARD_COUNT) maxEnemyNumber = ABSOLUTE_MAX_GUARD_COUNT;

        // spawn officers first but only when the number of officers that should spawn changes, so every score threshold
        int oldMaxOfficerNumber = maxOfficerNumber;
        maxOfficerNumber = 0 + (PlayerScript.score / 1200); // should really be around 1000
        if (maxOfficerNumber > ABSOLUTE_MAX_OFFICER_NUMBER) maxOfficerNumber = ABSOLUTE_MAX_OFFICER_NUMBER;

        if(maxOfficerNumber >= ABSOLUTE_MAX_OFFICER_NUMBER || oldMaxOfficerNumber != maxOfficerNumber)
        {
            //Debug.Log("spawn:" + maxOfficerNumber);
            int SLOBs = 0;
            int Medics = 0;
            int Guardians = 0;
            //int HotBoxes = 0;
            for (int i = 0; i < maxOfficerNumber; i++)
            {
                int officerType = rand.Next(0, 3);
                switch (officerType)
                {
                    case 0:
                    case 3: // REMOVE WHEN HotBox IS FIXED
                        //Debug.Log("SLOB");
                        if (SLOBs >= 4) i--; // CHANGE BACK TO 2 WHEN HotBox IS FIXED
                        else CreateEnemy("SLOB"); SLOBs++;
                        break;
                    case 1:
                        //Debug.Log("Medic");
                        if (Medics >= 2) i--;
                        else CreateEnemy("Medic"); Medics++;
                        break;
                    case 2:
                        //Debug.Log("Guardian");
                        if (Guardians >= 2) i--;
                        else CreateEnemy("Guardian"); Guardians++;
                        break;
                    //case 3:
                        //if (HotBoxes >= 4) i--;
                        //else CreateEnemy("HotBox"); HotBoxes++;
                        //break;
                }
            }
        }

        int B451Cs = 0;
        int Lights = 0;
        int Locks = 0;
        int Lunks = 0;

        //spawns Guards
        if (wave > 4)
        {
            for (int i = 0; i < maxEnemyNumber; i++)
            {
                int enemyType = rand.Next(0, 4);
                switch (enemyType)
                {
                    case 0:
                        if (B451Cs >= basicMax) i--; // 6 max
                        else CreateEnemy("B451C"); B451Cs++;
                        break;
                    case 1:
                        if (Lunks >= lunkMax) i--; // 6 max
                        else CreateEnemy("Lunk"); Lunks++;
                        break;
                    case 2:
                        if (Locks >= lockMax) i--; // 4 max
                        else CreateEnemy("Lock"); Locks++;
                        break;
                    case 3:
                        if (Lights >= lightMax) i--; // 4 max
                        else CreateEnemy("Light"); Lights++;
                        break;
                }
            }
        }
        else
        {
            // should probably make this its own method later
            FirstWaves();
        }
        //update attack time so that enemies do not attack immediately
        time = rand.Next(2, 4) + 1.1666f;
    }

    private void FirstWaves() // sets up the first 4 waves and makes 1 less enemy each time because the game spawns one less than what I want each time and I don't know why
    {
        switch (wave)
        {
            case 1:
                //Debug.Log("Wave spawn 1");
                for (int x = 0; x <= 4; x++)
                {
                    CreateEnemy("B451C");
                }
                hintUIText.text = "Attack, Slice, or Deflect bullets to damage enemies!";
                break;
            case 2:
                //Debug.Log("Wave spawn 2");
                CreateEnemy("B451C");
                CreateEnemy("B451C");
                CreateEnemy("B451C");
                CreateEnemy("Lunk");
                CreateEnemy("Lunk");
                hintUIText.text = "Slice heavy enemies to break their purple shields!";
                break;
            case 3:
                //Debug.Log("Wave spawn 3");
                CreateEnemy("B451C");
                CreateEnemy("B451C");
                CreateEnemy("Lunk"); 
                CreateEnemy("Lock"); 
                CreateEnemy("Lock");   
                hintUIText.text = "Hit enemy undeflectable rockets to destroy them!";
                break;
            case 4:
                //Debug.Log("Wave spawn 4");
                CreateEnemy("B451C");
                CreateEnemy("B451C");
                CreateEnemy("Lunk");
                CreateEnemy("Lunk");
                CreateEnemy("Lock");
                CreateEnemy("Light");
                CreateEnemy("Light");
                hintUIText.text = "Dash to avoid enemy lasers!";
                break;
        }
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
        if (name == "SLOB" || name == "Medic" || name == "Guardian" || name == "HotBox")
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
                Debug.Log("ECH HALP");
                Destroy(E);
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
                if (e.stunState == 2) // Is stunned
                {
                    e.SecondWind();
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
            //if we wanted to get all damaged enemies, not just stunned (for Medics),
            //we check HERE based on health, and not stunned flag
            if (enemies[i].GetComponent<Enemy>().stunState != 2) // Not stunned
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
