using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour {

    Rigidbody2D rigidBody;
    [SerializeField] private float maxSpeed = 10f; // The fastest the player can travel in any direction

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    public void controllerMove(float hMove, float vMove, float hLook, float vLook) // Movement and rotation with controller
    {
        rigidBody.velocity = new Vector2(hMove * maxSpeed, vMove * maxSpeed);
        if (hLook != 0 && vLook != 0)
        {
            float angle = Mathf.Atan2(vLook, hLook) * Mathf.Rad2Deg;
            rigidBody.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
        else if (rigidBody.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void keyboardMove(float hMove, float vMove, Vector3 mousePos) // Movement and rotation with keyboard and mouse
    {
        rigidBody.velocity = new Vector2(hMove * maxSpeed, vMove * maxSpeed);
        Vector3 playerPos = Camera.main.WorldToScreenPoint(rigidBody.transform.position);
        mousePos.x = mousePos.x - playerPos.x;
        mousePos.y = mousePos.y - playerPos.y;

        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
        rigidBody.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
}
