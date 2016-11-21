using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InputManager : MonoBehaviour {

    private Character character;
    [SerializeField] private string controllerType = "Keyboard";
    private char osType;
    private bool[] inputFlags;
    private bool initialRTrigger = true; // True until trigger has been pressed, expressly for 360 on mac
    private bool initialLTrigger = true;
    private char beforePriorPreviousLatestKey; // Nice
    private char priorPreviousLatestKey;
    private char previousLatestKey;
    private char latestKey;

    private Dictionary<string, string> inputs = new Dictionary<string, string>();

    private void Awake()
    {
        // INPUT FLAGS, IN ORDER: SLICE[0], ATTACK[1], DEFLECT[2], DASH[3], OVERCLOCK[4], FIRE[5], INTERACT[6]
        inputFlags = new bool[] { false, false, false, false, false, false, false };

        character = GetComponent<Character>();
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) // Set OS type
        {
            osType = 'w';
            initialRTrigger = false;
            initialLTrigger = false;
        }
        else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXDashboardPlayer)
        {
            osType = 'm';
        }
        else if (Application.platform == RuntimePlatform.LinuxPlayer)
        {
            osType = 'l';
            controllerType = "Keyboard";
            initialRTrigger = false;
            initialLTrigger = false;
        }
        else
        {
            throw new System.ArgumentException("This OS is not supported", "e");
        }

        setControlScheme(); // Set initial scheme
    }


    private void Update()
    {


        if (!inputFlags[0]) // Slice
        {
            if (controllerType != "Keyboard" && (osType == 'w' || initialRTrigger))
            {
                inputFlags[0] = Input.GetButton(inputs["Slice"]) || Input.GetAxis(inputs["Slice2"]) > 0;
                if (initialRTrigger) initialRTrigger = false;
            }
            else if (controllerType != "Keyboard" && !initialRTrigger) inputFlags[0] = Input.GetButton(inputs["Slice"]) || Input.GetAxis(inputs["Slice2"]) > -1;
            else inputFlags[0] = Input.GetButton(inputs["Slice"]);
        }

        if (!inputFlags[1]) // Attack
        {
            inputFlags[1] = Input.GetButtonDown(inputs["Attack"]);
        }

        if (!inputFlags[2]) // Deflect
        {
            inputFlags[2] = Input.GetButtonDown(inputs["Deflect"]);
        }

        if (!inputFlags[3]) // Interact
        {
            if ((controllerType == "Xbox360A" || controllerType == "PS4A") && osType == 'w') inputFlags[3] = Input.GetAxis(inputs["Interact"]) < 0;
            else inputFlags[3] = Input.GetButton(inputs["Interact"]);
        }

        if (!inputFlags[4]) // Overclock
        {
            if ((controllerType == "Xbox360B" || controllerType == "PS4B") && osType == 'w') inputFlags[4] = Input.GetAxis(inputs["Overclock"]) < 0;
            else inputFlags[4] = Input.GetButton(inputs["Overclock"]);
        }

        if (!inputFlags[5]) // Fire
        {
            inputFlags[5] = Input.GetButton(inputs["Fire"]);
        }

        if (!inputFlags[6]) // Dash
        {
            if (controllerType != "Keyboard" && (osType == 'w' || initialLTrigger)) // Mac triggers for 360 start at 0 then range from -1 to 1, Triggers are treated as axes for all controllers
            {
                inputFlags[6] = Input.GetButton(inputs["Dash"]) || Input.GetAxis(inputs["Dash2"]) > 0; // Change to get down if on press is desired rather than continuous
                if (initialLTrigger) initialLTrigger = false;
            }
            else if (controllerType != "Keyboard" && !initialLTrigger) inputFlags[6] = Input.GetButton(inputs["Dash"]) || Input.GetAxis(inputs["Dash2"]) > -1;
            else inputFlags[6] = Input.GetButton(inputs["Dash"]);
        }

        if (controllerType == "Keyboard")
        {
            if (Input.GetButtonDown(inputs["HorizontalMove"]) && Input.GetAxisRaw(inputs["HorizontalMove"]) > 0) // D
            {
                beforePriorPreviousLatestKey = priorPreviousLatestKey;
                priorPreviousLatestKey = previousLatestKey;
                previousLatestKey = latestKey;
                latestKey = 'd';
            }
            if (Input.GetButtonDown(inputs["HorizontalMove"]) && Input.GetAxisRaw(inputs["HorizontalMove"]) < 0) // A, set the latest key
            {
                beforePriorPreviousLatestKey = priorPreviousLatestKey;
                priorPreviousLatestKey = previousLatestKey;
                previousLatestKey = latestKey;
                latestKey = 'a';
            }
            if (Input.GetButtonDown(inputs["VerticalMove"]) && Input.GetAxisRaw(inputs["VerticalMove"]) > 0) // W
            {
                beforePriorPreviousLatestKey = priorPreviousLatestKey;
                priorPreviousLatestKey = previousLatestKey;
                previousLatestKey = latestKey;
                latestKey = 'w';
            }
            if (Input.GetButtonDown(inputs["VerticalMove"]) && Input.GetAxisRaw(inputs["VerticalMove"]) < 0) // S
            {
                beforePriorPreviousLatestKey = priorPreviousLatestKey;
                priorPreviousLatestKey = previousLatestKey;
                previousLatestKey = latestKey;
                latestKey = 's';
            }

            checkIfLatest(); // Check if latestKey is valid
                             //Debug.Log(latestKey.ToString());
        }
    }


    private void FixedUpdate()
    {
        // Read the inputs.
        float hMove = Input.GetAxis(inputs["HorizontalMove"]); // Invert stuff here
        float vMove = Input.GetAxis(inputs["VerticalMove"]);

        if (controllerType != "Keyboard")
        {
            float hLook = Input.GetAxis(inputs["HorizontalLook"]); // Invert stuff here
            float vLook = Input.GetAxis(inputs["VerticalLook"]);
            if (osType == 'w') vLook = -vLook;
            character.controllerMove(hMove, vMove, hLook, vLook);
        }
        else
        {
            Vector3 mousePos = Input.mousePosition;
            character.keyboardMove(hMove, vMove, mousePos);
        }
        
        character.InputFlags = inputFlags; // Send input information, then reset it
        for (int i=0;i<inputFlags.Length; i++) inputFlags[i] = false;
    }

    private void setControlScheme() // Sets the current control scheme; http://wiki.unity3d.com/index.php?title=Xbox360Controller, https://www.reddit.com/r/Unity3D/comments/1syswe/ps4_controller_map_for_unity/ (slightly wrong)
    {
        inputs.Clear(); // Empty current scheme

        if (controllerType == "Keyboard" || osType == 'l') // Keyboard
        {
            inputs.Add("HorizontalMove", "KeyHorizontalMove");
            inputs.Add("VerticalMove", "KeyVerticalMove");
            inputs.Add("Slice", "Mouse2");
            inputs.Add("Attack", "Mouse1");
            inputs.Add("Fire", "F");
            inputs.Add("Dash", "LSHIFT");
            inputs.Add("Deflect", "R");
            inputs.Add("Overclock", "SPACE");
            inputs.Add("Interact", "E");
        }
        else // Controller (Win and Mac only)
        {
            inputs.Add("HorizontalMove", "Axis1");
            inputs.Add("VerticalMove", "Axis2");
            if (osType == 'w')
            {
                if (controllerType == "Xbox360A")
                {
                    inputs.Add("HorizontalLook", "Axis4");
                    inputs.Add("VerticalLook", "Axis5");
                    inputs.Add("Slice", "Button2");
                    inputs.Add("Attack", "Button3");
                    inputs.Add("Fire", "Button1");
                    inputs.Add("Dash", "Button0");
                    inputs.Add("Deflect", "Button5");
                    inputs.Add("Overclock", "Button4");
                    inputs.Add("Slice2", "Axis10");
                    inputs.Add("Dash2", "Axis9");
                    inputs.Add("Interact", "Axis7");
                }
                else if (controllerType == "Xbox360B") 
                {
                    inputs.Add("HorizontalLook", "Axis4");
                    inputs.Add("VerticalLook", "Axis5");
                    inputs.Add("Slice", "Button2");
                    inputs.Add("Attack", "Button3");
                    inputs.Add("Fire", "Button4");
                    inputs.Add("Dash", "Button0");
                    inputs.Add("Deflect", "Button5");
                    inputs.Add("Overclock", "Axis7");
                    inputs.Add("Slice2", "Axis10");
                    inputs.Add("Dash2", "Axis9");
                    inputs.Add("Interact", "Button1");
                }
                else if (controllerType == "PS4A")
                {
                    inputs.Add("HorizontalLook", "Axis3");
                    inputs.Add("VerticalLook", "Axis6");
                    inputs.Add("Slice", "Button0");
                    inputs.Add("Attack", "Button3");
                    inputs.Add("Fire", "Button2");
                    inputs.Add("Dash", "Button1");
                    inputs.Add("Deflect", "Button5");
                    inputs.Add("Overclock", "Button4");
                    inputs.Add("Slice2", "Axis5");
                    inputs.Add("Dash2", "Axis4");
                    inputs.Add("Interact", "Axis8");
                }
                else // PS4 B
                {
                    inputs.Add("HorizontalLook", "Axis3");
                    inputs.Add("VerticalLook", "Axis6");
                    inputs.Add("Slice", "Button0");
                    inputs.Add("Attack", "Button3");
                    inputs.Add("Fire", "Button4");
                    inputs.Add("Dash", "Button1");
                    inputs.Add("Deflect", "Button5");
                    inputs.Add("Overclock", "Axis8");
                    inputs.Add("Slice2", "Axis5");
                    inputs.Add("Dash2", "Axis4");
                    inputs.Add("Interact", "Button2");
                }
            }
            else // Mac, can add linux controller support later if desired
            {
                if (controllerType == "Xbox360A")
                {
                    inputs.Add("HorizontalLook", "Axis3");
                    inputs.Add("VerticalLook", "Axis4");
                    inputs.Add("Slice", "Button18");
                    inputs.Add("Attack", "Button19");
                    inputs.Add("Fire", "Button17");
                    inputs.Add("Dash", "Button16");
                    inputs.Add("Deflect", "Button14");
                    inputs.Add("Overclock", "Button13");
                    inputs.Add("Slice2", "Axis6");
                    inputs.Add("Dash2", "Axis5");
                    inputs.Add("Interact", "Button6");
                }
                else // Xbox360 B
                {
                    inputs.Add("HorizontalLook", "Axis3");
                    inputs.Add("VerticalLook", "Axis4");
                    inputs.Add("Slice", "Button18");
                    inputs.Add("Attack", "Button19");
                    inputs.Add("Fire", "Button13");
                    inputs.Add("Dash", "Button16");
                    inputs.Add("Deflect", "Button14");
                    inputs.Add("Overclock", "Buton6");
                    inputs.Add("Slice2", "Axis6");
                    inputs.Add("Dash2", "Axis5");
                    inputs.Add("Interact", "Button17");
                }
            }
        }
    }

    private void checkIfLatest() // Runs until the latest key is either nothing or is being pressed
    {
        if (latestKey == 'd' && !(Input.GetButton(inputs["HorizontalMove"]) && Input.GetAxisRaw(inputs["HorizontalMove"]) > 0)) // D, this is still cross platform, just using key chars for clarity
        {
            latestKey = previousLatestKey;
            previousLatestKey = priorPreviousLatestKey;
            priorPreviousLatestKey = beforePriorPreviousLatestKey;
            beforePriorPreviousLatestKey = '0';
            checkIfLatest();
        }
        if (latestKey == 'a' && !(Input.GetButton(inputs["HorizontalMove"]) && Input.GetAxisRaw(inputs["HorizontalMove"]) < 0)) // A, set the latest key
        {
            latestKey = previousLatestKey;
            previousLatestKey = priorPreviousLatestKey;
            priorPreviousLatestKey = beforePriorPreviousLatestKey;
            beforePriorPreviousLatestKey = '0';
            checkIfLatest();
        }
        if (latestKey == 'w' && !(Input.GetButton(inputs["VerticalMove"]) && Input.GetAxisRaw(inputs["VerticalMove"]) > 0)) // W
        {
            latestKey = previousLatestKey;
            previousLatestKey = priorPreviousLatestKey;
            priorPreviousLatestKey = beforePriorPreviousLatestKey;
            beforePriorPreviousLatestKey = '0';
            checkIfLatest();
        }
        if (latestKey == 's' && !(Input.GetButton(inputs["VerticalMove"]) && Input.GetAxisRaw(inputs["VerticalMove"]) < 0)) // S
        {
            latestKey = previousLatestKey;
            previousLatestKey = priorPreviousLatestKey;
            priorPreviousLatestKey = beforePriorPreviousLatestKey;
            beforePriorPreviousLatestKey = '0';
            checkIfLatest();
        }
    }
}
