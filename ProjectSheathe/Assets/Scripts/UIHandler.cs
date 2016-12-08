using UnityEngine;
using System.Collections;

public class UIHandler : MonoBehaviour {

    private GameObject[] pauseScreenObjects;
    private GameObject kbdButtons;
    private GameObject xboxButtons;
    private GameObject ps4Buttons;

    public bool IsGamePaused { get; private set; }

	// Use this for initialization
	void Awake () {
        pauseScreenObjects = GameObject.FindGameObjectsWithTag("PauseMenuObject");
        kbdButtons = GameObject.FindGameObjectWithTag("kbdControls");
        xboxButtons = GameObject.FindGameObjectWithTag("x360Controls");
        ps4Buttons = GameObject.FindGameObjectWithTag("ps4Controls");

        //game will always start paused (for now)
        SetPaused(true);
	}

    //pauses or unpauses the game based on state passed in (can be called by other methods)
    public void SetPaused(bool state)
    {
        if (state)
        {
            Time.timeScale = 0;
            IsGamePaused = true;
            foreach(GameObject g in pauseScreenObjects)
            {
                g.SetActive(true);
            }
        }
        else
        {
            Time.timeScale = 1;
            IsGamePaused = false;
            foreach(GameObject g in pauseScreenObjects)
            {
                g.SetActive(false);
            }
        }
    }
	
    //changes which control buttons are being displayed on the screen (called from InputManager)
    public void ChangeControlImages(char layout)
    {
        bool keyboard = false;
        bool xbox = false;
        bool ps4 = false;

        switch(layout)
        {
            case 'k':
                keyboard = true;
                break;
            case 'x':
                xbox = true;
                break;
            case 'p':
                ps4 = true;
                break;
            default:
                keyboard = true;
                break;
        }

        //shows or hides the controls
        kbdButtons.SetActive(keyboard);
        xboxButtons.SetActive(xbox);
        ps4Buttons.SetActive(ps4);
    }
}
