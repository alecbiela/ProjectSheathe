using UnityEngine;
using System.Collections;

public class DrawLine : MonoBehaviour
{
    public LineRenderer lineRendererComponent;
    private float counter;
    private float dist;

    public Transform origin;
    public Transform destination;

    public float lineDrawSpeed = 600f;
    private int timer;
    public bool running;

	// Use this for initialization
	void Start ()
    {
        
        origin = GameObject.FindGameObjectWithTag("LineStart").transform;
        destination = GameObject.FindGameObjectWithTag("LineEnd").transform;
        lineRendererComponent = GetComponent<LineRenderer>();
        //lineRendererComponent.SetPosition(0, origin.position);
        //lineRendererComponent.SetColors(Color.red, Color.red);
        //lineRendererComponent.SetWidth(.3f, .3f);

        dist = Vector3.Distance(origin.position, destination.position);
        
        timer = 0;
        running = false;
	}
	
	// Update is called once per frame
	void Update()
    {
        if(running == true)
        {
            DrawFlash();
        }
    }

    public void Run(Transform orig, Transform dest)
    {
        origin = orig;
        destination = dest;
        running = true;
    }

    void DrawFlash()
    {
        lineRendererComponent = GetComponent<LineRenderer>();
        lineRendererComponent.SetPosition(0, origin.position);
        //lineRendererComponent.SetColors(Color.red, Color.red);
        lineRendererComponent.SetWidth(.3f, .3f);

        dist = Vector3.Distance(origin.position, destination.position);
        timer++;

        lineRendererComponent.enabled = true;
        //counter += .3f / lineDrawSpeed;
        counter += 1f;
        float x = Mathf.Lerp(0, dist, counter);
        Vector3 pointA = origin.position;
        Vector3 pointB = destination.position;

        // Get the unit vector in the desired direction, multiply by the desired length and add the starting point.
        Vector3 pointAlongLine = x * Vector3.Normalize(pointB - pointA) + pointA;

        lineRendererComponent.SetPosition(1, pointAlongLine);
        if (timer > 400)
        {
            lineRendererComponent.SetColors(Color.red, Color.black);
        }
    }
}
