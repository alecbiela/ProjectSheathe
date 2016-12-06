using UnityEngine;
using System.Collections;

namespace Assets.Scripts
{
    class HotBox : MonoBehaviour
    {
        private Enemy thisBox;
        private GameObject Dome;
        private const float COOLDOWN = 30.0f;
        private const float ACTIVE = 2.0f;
        private float cooldown;
        private float active;
        private EncounterManager em;

        // Use this for initialization
        void Start()
        {
            em = GameObject.FindGameObjectWithTag("EncounterManager").GetComponent<EncounterManager>();
            cooldown = COOLDOWN;
            active = 50;
            thisBox = this.GetComponentInParent<Enemy>();
            Dome = this.transform.GetChild(0).gameObject;
        }

        // Update is called once per frame
        void Update()
        {
            cooldown -= Time.deltaTime * em.speedMod;
            if (cooldown <= 0)
            {
                thisBox.Fire();
                //Dome.SetActive(true);
                cooldown = COOLDOWN;
                active = ACTIVE;
                this.GetComponentInChildren<SpriteRenderer>().color = Color.cyan;
                Debug.Log("In here");
            }
                if (active>0)
            {
                active -= Time.deltaTime * em.speedMod;
            }
            else
            {
                Dome.GetComponent<SpriteRenderer>().color = Color.clear;
                Dome.SetActive(false);
            }
        }
        //void OnTriggerEnter2D(Collider2D other) realized this was overriding the Enemy's ontrigger enter
        //{
            
        //    if (cooldown <= 0 && other.tag == "player")
        //    {
        //        thisBox.Fire();
        //        //Dome.SetActive(true);
        //        cooldown = COOLDOWN;
        //        active = ACTIVE;
        //        this.GetComponentInChildren<SpriteRenderer>().color = Color.cyan;
        //    }
        //}
    }
}
