using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyHandler : MonoBehaviour {

    List<GameObject> Enemies = new List<GameObject>();
    // Use this for initialization
    System.Random rand = new System.Random();
    void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	    if(Enemies.Count<5)
        {
            CreateEnemy();
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
