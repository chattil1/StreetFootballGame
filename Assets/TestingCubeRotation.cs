using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestingCubeRotation : MonoBehaviour
{
    [Header("Set Dynamically")]
    float torqueAmountToRotateFootballToCenter = 0;
    float timeRotated = 0f;
    Vector3 originalCenterOfMass = new Vector3(0, 0, 0);
    float currentAngle;
    bool timeSet = false;
    // Start is called before the first frame update
    void Start()
    {
        ///Functionality with a parent pivot
        /*originalCenterOfMass = transform.parent.gameObject.GetComponent<Rigidbody>().centerOfMass;
        transform.parent.gameObject.GetComponent<Rigidbody>().centerOfMass = new Vector3(transform.parent.position.x, transform.parent.position.y + 0.49f, transform.parent.position.z);
        torqueAmountToRotateFootballToCenter = GetTorqueToRotateFootball(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 1);
        transform.parent.gameObject.GetComponent<Rigidbody>().AddTorque(Vector3.up * torqueAmountToRotateFootballToCenter, ForceMode.Impulse);*/
        originalCenterOfMass = gameObject.GetComponent<Rigidbody>().centerOfMass;
        GetComponent<Rigidbody>().centerOfMass = new Vector3(-2.5f, 0f, 0);
        torqueAmountToRotateFootballToCenter = GetTorqueToRotateFootball(new Vector3(0, 0, 0), new Vector3(0, 0, 0), 1);
        timeRotated = Time.time;
        GetComponent<Rigidbody>().AddTorque(Vector3.up * torqueAmountToRotateFootballToCenter, ForceMode.Impulse);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /*if (!timeSet) {
            timeRotated = Time.time;
            timeSet = true;
        }*/
        if ((Time.time - timeRotated) < 1.0f)
        {
            ///Functionality for parent pivot
            /*float torqueThisFrame = torqueAmountToRotateFootballToCenter * Time.fixedDeltaTime;
            //transform.parent.gameObject.GetComponent<Rigidbody>().AddTorque(Vector3.up * torqueThisFrame, ForceMode.Impulse);
            currentAngle += transform.parent.gameObject.GetComponent<Rigidbody>().angularVelocity.y * Time.fixedDeltaTime * Mathf.Rad2Deg;*/

            float torqueThisFrame = torqueAmountToRotateFootballToCenter * Time.fixedDeltaTime;
            //GetComponent<Rigidbody>().AddTorque(Vector3.up * torqueThisFrame, ForceMode.Impulse);
            currentAngle += GetComponent<Rigidbody>().angularVelocity.y * Time.fixedDeltaTime * Mathf.Rad2Deg;
        }
        else {
            int x = 0;
        }

    }
    private float GetTorqueToRotateFootball(Vector3 armDirection, Vector3 bodyDirection, float rotationTime = 1f)
    {
        //float temporaryDisplacementAngleRadians = GetAngleDisplacement(armDirection, bodyDirection) * Mathf.Deg2Rad;

        float temporaryDisplacementAngleRadians = 90f * Mathf.Deg2Rad;
        float targetAngularVelocity = temporaryDisplacementAngleRadians / 1;
        //float angularAcceleration = 2 * temporaryDisplacementAngleRadians / (Mathf.Pow(rotationTime, 2));
        //float footballMass = gameObject.GetComponent<Rigidbody>() != null ? gameObject.GetComponent<Rigidbody>().mass : 0f;
        //float momentOfInertia = footballMass * Mathf.Pow(Vector3.Distance(new Vector3(transform.parent.position.x, transform.parent.position.y + 0.49f, transform.parent.position.z), transform.position), 2);

        //float momentOfInertia = (footballMass * Mathf.Pow(Vector3.Distance(gameObject.GetComponent<Rigidbody>().centerOfMass, originalCenterOfMass), 2)) + (footballMass * Mathf.Pow(Vector3.Distance(gameObject.GetComponent<Rigidbody>().centerOfMass, originalCenterOfMass), 2));
        //float momentOfInertia = ((1f / 12f) * footballMass * (Mathf.Pow(1, 2) + 2 * Mathf.Pow(Vector3.Distance(gameObject.GetComponent<Rigidbody>().centerOfMass, originalCenterOfMass), 2))) + (footballMass * Mathf.Pow(Vector3.Distance(gameObject.GetComponent<Rigidbody>().centerOfMass, originalCenterOfMass), 2));

        //float momentOfInertia = gameObject.GetComponent<Rigidbody>() != null ? gameObject.GetComponent<Rigidbody>().inertiaTensor.z : 0;

        float momentOfInertia = gameObject.GetComponent<Rigidbody>() != null ? gameObject.GetComponent<Rigidbody>().inertiaTensor.z : 0;
        //float momentOfInertia = ((1f / 3f) * footballMass * (Mathf.Pow(2f * Vector3.Distance(gameObject.GetComponent<Rigidbody>().centerOfMass, originalCenterOfMass), 2)));
        //float torque = momentOfInertia * targetAngularVelocity;
        //float torque = momentOfInertia * targetAngularVelocity;

        float torque = momentOfInertia * targetAngularVelocity;
        return torque;
    }
}
