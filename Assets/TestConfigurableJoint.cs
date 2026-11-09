using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConfigurableJoint : MonoBehaviour
{
    [Header("Set In Inspector")]
    public Rigidbody testPivot;
    // Start is called before the first frame update
    void Start()
    {

        SetRigidbodyPivot(testPivot.gameObject.transform, gameObject.GetComponent<Rigidbody>());

        gameObject.GetComponent<Rigidbody>().AddTorque(Vector3.up * 50, ForceMode.Impulse);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SetRigidbodyPivot(Transform pivot, Rigidbody target)
    {
        Vector3 leftArmPivotPosition = pivot.position;

        // Create an empty GameObject at the LeftArm's pivot point to act as the joint's anchor
        GameObject pivotAnchor = new GameObject("PivotAnchor");
        pivotAnchor.transform.position = leftArmPivotPosition;
        pivotAnchor.transform.parent = pivot;
        Rigidbody pivotRb = pivotAnchor.AddComponent<Rigidbody>();
        pivotRb.isKinematic = true; // We don't want the anchor to move

        // Attach a Configurable Joint to the football
        FixedJoint joint = target.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = pivotRb; // Connect the joint to the pivotAnchor
        //joint.anchor = Vector3.zero; // Football's center of mass is the local anchor point

        joint.anchor = Vector3.zero; // Football's center of mass is the local anchor point
        //target.gameObject.transform.InverseTransformPoint(leftArm.position);

        Debug.DrawRay(target.gameObject.transform.TransformPoint(joint.anchor), Vector3.up * 50f, Color.black);
        //Debug.DrawRay(joint.anchor, Vector3.up * 50f, Color.green);
        // Allow rotation around the Y-axis (Vector3.up) and freeze other movements
        /*joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Locked;*/
    }
}
