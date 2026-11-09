using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RouteSegment {
    //THIS VARIABLE EXPECTS WORLD SPACE COORDINATES
    public Vector3 startPosition { get; set; }
    public Vector3 direction { get; set; }
    public float runDistance { get; set; }
}

public class Route : MonoBehaviour
{
    public string PlayBookName = "Playbook_Canvas";
    private List<RouteSegment> routeParts = new List<RouteSegment>();
    // Start is called before the first frame update
    void Awake()
    {
        /*PlayerInput playsToChooseFrom = gameObject.GetComponent<PlayerInput>();
        if (playsToChooseFrom.actions["PressedPlay1"] != null)
        {
            InputAction throwAction = playsToChooseFrom.actions["PressedPlay1"];
            playsToChooseFrom.actions["PressedPlay1"].performed += OnPressedPlay1;
            //throwAction.Enable();
        }*/
    }

    public void SetRouteParts(List<RouteSegment> parts) {
        routeParts = parts;
    }

    public void SetRouteSegments(List<RouteSegment> routeSegments)
    {
        routeParts = routeSegments;
    }
    public List<RouteSegment> GetRouteSegments()
    {
        return routeParts;
    }

    // Update is called once per frame
    void Update()
    {
        /*if (GameObject.Find(PlayBookName) != null && GameObject.Find(PlayBookName).activeSelf)
        {
            int k = 0;
            if (gameObject.GetComponent<PlayerInput>() != null)
            {
                PlayerInput playsToChooseFrom = gameObject.GetComponent<PlayerInput>();
                if (playsToChooseFrom.actions["PressedPlay1"] != null)
                {
                    InputAction throwAction = playsToChooseFrom.actions["PressedPlay1"];
                    playsToChooseFrom.actions["PressedPlay1"].performed += OnPressedPlay1;
                    //throwAction.Enable();
                }

                //InputAction throwAction = playsToChooseFrom.actions["PressedPlay1"];
                //Pressing A
                //playsToChooseFrom.actions["PressedPlay1"].performed += OnPressedPlay1;
                //throwAction.ReadValue<float>();
            }
        }
        else {
            if (gameObject.GetComponent<PlayerInput>() != null) {
                PlayerInput playsToChooseFrom = gameObject.GetComponent<PlayerInput>();
                if (playsToChooseFrom.actions["PressedPlay1"] != null)
                {
                    //InputAction pressedPlay1Action = playsToChooseFrom.actions["PressedPlay1"];
                    //playsToChooseFrom.actions["PressedPlay1"].performed -= OnPressedPlay1;
                    //pressedPlay1Action.Disable();
                }
            }

                

        }*/
    }


}
