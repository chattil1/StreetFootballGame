using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCollision : MonoBehaviour
{
    [Header("Set In Inspector")]
    public Transform wr1Hand;
    public Transform wrBodyPosition;
    public Transform leftArm;
    public Transform rightArm;




    [Header("Set Dynamically")]
    //Vector3 targetFootballPosition 
    float defaultDistanceBetweenRightArmShoulderAndArm = 0f;
    bool setDefaultDistance = false;
    public bool stopFootball = false;
    public GameObject handCollisionGameObject;
    public GameObject collisionPositionOnFootballPrefab;
    Vector3 velBeforeCollision = new Vector3(0, 0, 0);
    public bool addForce = false;
    public bool withinRange = false;
    bool collided = false;
    Vector3 rotVelBeforeCollision = new Vector3(0, 0, 0);
    Vector3 initialVelocity = new Vector3(0, 0, 0);
    Vector3 collisionPosition = new Vector3(0, 0, 0);
    public bool caughtFootball = false;

    public float horFootballAngleWhenCaught = 0f;
    public bool setAngle = false;
    Vector3 footballCenterOfMassOnCatch;
    Vector3 collisionPoint = new Vector3(0, 0, 0);
    Vector3 centerOfMassPositionOnCollision = new Vector3(0, 0, 0);
private string collisionGameObjectName = "";
    float maxRightAngleCounterClockwise = 195f;
    float maxRightAngleClockWise = 75f;

    float horizontalRotation = 0f;
    float verticalRotation = 0f;
    float xAxisRotation = 0f;
    float forceVal = 0f;


    float timeWRCaughtPass = 0f;
    float timeVal = 0f;

    float sourceLeftArmYAxisAngle = 0f;
    float sourceXAxisAngle = 0f;
    bool alreadySetXAxisTarget = false;

    bool finalVerticalRotationRemaining = false;
    bool finalXAxisRotationRemaining = false;
    bool rotateRemaining = false;

    float rightArmAngleWhenHorizontallyCentered = 0;
    bool foundPrefab = false;
    bool doVerticalRotationNext = false;
    bool doXAxisRotationNext = false;
    float timeThatWeStartedVerticalRotation = 0.0f;
    // Start is called before the first frame update
    void Awake()
    {

        GameObject emptyPrefab = Resources.Load<GameObject>("Prefabs/EmptyGameObject");
        GameObject instantiatedGameObject = new GameObject();
        
        if (emptyPrefab != null)
        {
            foundPrefab = true;
            instantiatedGameObject = Instantiate<GameObject>(emptyPrefab);
            instantiatedGameObject.name = "EmptyGameObjectTemplate";
            collisionPositionOnFootballPrefab = instantiatedGameObject;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (rightArm != null && !setDefaultDistance) {
            setDefaultDistance = true;
            defaultDistanceBetweenRightArmShoulderAndArm = (rightArm.position - rightArm.parent.position).magnitude;
        }
        int x = 0;
        /*if (gameObject != null && addForce) {
             gameObject.GetComponent<Rigidbody>().AddForce(Vector3.up * forceVal, ForceMode.Force);
        }*/
        //&& this.gameObject.GetComponent<QB>().
        if (!collided && x == 1) {
            velBeforeCollision = GetComponent<Rigidbody>().velocity;
        }
    }

    public float GetUnitCircleEulerAngle(float angle)
    {
        float normalizedAngle = angle > 0 ? angle % 360 : (360 + angle) % 360;
        return normalizedAngle;
    }

    void MeasureLeftArmRightDisplacement(float rotationAngle, Transform armTransform)
    {
        // Step 1: Get the initial horizontal direction of LeftArm.right (before the rotation)
        Vector3 initialRight = armTransform.right;

        // Step 2: Apply the rotation (rotating around the global y-axis by the specified angle)
        armTransform.Rotate(Vector3.up, rotationAngle, Space.World);

        // Step 3: Get the new horizontal direction of LeftArm.right (after the rotation)
        Vector3 newRight = armTransform.right;

        // Step 4: Calculate the angle between the initial and new Right vectors
        // We project both vectors onto the horizontal plane (XZ plane) by ignoring the y-component
        Vector3 initialRightXZ = new Vector3(initialRight.x, 0, initialRight.z).normalized;
        Vector3 newRightXZ = new Vector3(newRight.x, 0, newRight.z).normalized;

        // Step 5: Calculate the angle difference between the initial and new vectors in degrees
        float angleDisplacement = Vector3.SignedAngle(initialRightXZ, newRightXZ, Vector3.up);

        // Output the result
        Debug.Log("Horizontal displacement of LeftArm.right after rotation: " + angleDisplacement + " degrees");
    }

    public Vector3 GetPerpendicularVector3D(Vector3 direction) {
        Vector3 reference = Vector3.up;
        Vector3 perpendicularVector = new Vector3(0, 0, 0);
        //perpendicularVector = Vector3.Cross(reference, direction.normalized).normalized;
        if (direction.z >= 0)
        {
            perpendicularVector = Vector3.Cross(reference, direction.normalized).normalized;
        }
        else {
            Vector3 tempVector = new Vector3(-direction.normalized.x, direction.normalized.y, direction.normalized.z);
            perpendicularVector = Vector3.Cross(reference, tempVector).normalized;
        }
        
        //perpendicularVector = new Vector3(perpendicularVector)
        return perpendicularVector;
    }
    float AimArm(GameObjectRotation rotationInfo, Transform arm, float rotationSpeed, int clockwiseDirectionSign, float totalRotation, bool isHorizontalRotation = true, bool rotateAroundXAxis = false)
    {
        if (rotateAroundXAxis) {
            int c = 0;
        }
        GameObjectRotation angleDifference = GetAngleDifference(rotationInfo);

        if (Mathf.Abs(angleDifference.ClockwiseAngleDistance) < Mathf.Abs(angleDifference.CounterClockwiseAngleDistance))
        {
            if (angleDifference.ClockwiseAngleDistanceFromArmOrigin <= angleDifference.MaxClockwiseRotationUnitCircle)
            {
                angleDifference.Direction = WR.Direction.clockwise;
            }
            else if (angleDifference.CounterClockwiseAngleDistanceFromArmOrigin <= angleDifference.MaxCounterClockwiseRotationUnitCircle)
            {
                angleDifference.Direction = WR.Direction.counterclockwise;
            }
            else {
                angleDifference.Direction = WR.Direction.clockwise;
            }
        }
        else {
            if (angleDifference.CounterClockwiseAngleDistanceFromArmOrigin <= angleDifference.MaxCounterClockwiseRotationUnitCircle)
            {
                angleDifference.Direction = WR.Direction.counterclockwise;
            }
            else if (angleDifference.ClockwiseAngleDistanceFromArmOrigin <= angleDifference.MaxClockwiseRotationUnitCircle)
            {
                angleDifference.Direction = WR.Direction.clockwise;
            }
            else
            {
                angleDifference.Direction = WR.Direction.counterclockwise;
            }
        }


        float rotationAmount = 0f;
        if (angleDifference.Direction == WR.Direction.clockwise) {
            if (rotateAroundXAxis) {
                if (clockwiseDirectionSign < 0) {
                    if (totalRotation + (rotationSpeed * (clockwiseDirectionSign)) >= -angleDifference.MaxClockwiseRotationUnitCircle)
                    {

                    }
                    else {
                        int f = 0;
                    }
                } else
                {
                    if (totalRotation + (rotationSpeed * (clockwiseDirectionSign)) <= angleDifference.MaxClockwiseRotationUnitCircle)
                    {

                    }
                    else {
                        int b = 0;
                    }
                }
            }
            if (clockwiseDirectionSign < 0)
            {
                rotationAmount = totalRotation + (rotationSpeed * (clockwiseDirectionSign)) >= -angleDifference.MaxClockwiseRotationUnitCircle ? (rotationSpeed * (clockwiseDirectionSign)) : clockwiseDirectionSign * Mathf.Abs(angleDifference.MaxClockwiseRotationUnitCircle - totalRotation);
            }
            else {
                rotationAmount = totalRotation + (rotationSpeed * (clockwiseDirectionSign)) <= angleDifference.MaxClockwiseRotationUnitCircle ? (rotationSpeed * (clockwiseDirectionSign)) : clockwiseDirectionSign * Mathf.Abs(angleDifference.MaxClockwiseRotationUnitCircle - totalRotation);
            }
            
        } else if (angleDifference.Direction == WR.Direction.counterclockwise) {
            if (rotateAroundXAxis)
            {
                if (clockwiseDirectionSign < 0)
                {
                    if (totalRotation + (rotationSpeed * (-clockwiseDirectionSign)) >= -angleDifference.MaxCounterClockwiseRotationUnitCircle)
                    {

                    }
                    else {
                        int a = 0;
                    }
                }
                else {
                    if (totalRotation + (rotationSpeed * (-clockwiseDirectionSign)) <= angleDifference.MaxCounterClockwiseRotationUnitCircle)
                    {

                    }
                    else {
                        int k = 0;
                    }
                }

            }

            if (-clockwiseDirectionSign < 0)
            {
                rotationAmount = totalRotation + (rotationSpeed * (-clockwiseDirectionSign)) >= -angleDifference.MaxCounterClockwiseRotationUnitCircle ? (rotationSpeed * (-clockwiseDirectionSign)) : (-clockwiseDirectionSign) * Mathf.Abs(angleDifference.MaxCounterClockwiseRotationUnitCircle - totalRotation);
            }
            else {
                rotationAmount = totalRotation + (rotationSpeed * (-clockwiseDirectionSign)) <= angleDifference.MaxCounterClockwiseRotationUnitCircle ? (rotationSpeed * (-clockwiseDirectionSign)) : (-clockwiseDirectionSign) * Mathf.Abs(angleDifference.MaxCounterClockwiseRotationUnitCircle - totalRotation);
            }
            
        }

        if (isHorizontalRotation)
        {
            arm.eulerAngles = new Vector3(arm.eulerAngles.x, arm.eulerAngles.y + rotationAmount, arm.eulerAngles.z);
        }
        else {
            if (rotateAroundXAxis)
            {
                arm.Rotate(rightArm.right, rotationAmount, Space.World);
            }
            else {
                arm.eulerAngles = new Vector3(arm.eulerAngles.x, arm.eulerAngles.y, arm.eulerAngles.z + rotationAmount);
            }
            
        }
        totalRotation += rotationAmount;


        return totalRotation;
    }


    GameObjectRotation GetAngleDifference(GameObjectRotation rotationInfo)
    {
        float difference = rotationInfo.TargetAngle - rotationInfo.CurrentAngle;
        float differenceFromOrigin = rotationInfo.TargetAngle - rotationInfo.SourceAngle;

        // Normalize the difference to be within -180 to 180 degrees
        if (rotationInfo.CurrentAngle < rotationInfo.TargetAngle)
        {
            rotationInfo.ClockwiseAngleDistance = 360 - Mathf.Abs(difference);
            rotationInfo.CounterClockwiseAngleDistance = Mathf.Abs(difference);
            rotationInfo.ClockwiseAngleDistanceFromArmOrigin = 360 - Mathf.Abs(differenceFromOrigin);
            rotationInfo.CounterClockwiseAngleDistanceFromArmOrigin = Mathf.Abs(differenceFromOrigin);
        }
        else if (rotationInfo.CurrentAngle > rotationInfo.TargetAngle) {
            rotationInfo.ClockwiseAngleDistance = Mathf.Abs(difference);
            rotationInfo.CounterClockwiseAngleDistance = 360 - Mathf.Abs(difference);
            rotationInfo.ClockwiseAngleDistanceFromArmOrigin = Mathf.Abs(differenceFromOrigin);
            rotationInfo.CounterClockwiseAngleDistanceFromArmOrigin = 360 - Mathf.Abs(differenceFromOrigin);
           
        }

        return rotationInfo;
    }


    public bool CaughtFootball() {
        bool caughtFootball = false;
        Vector3 toFootball = transform.position - wr1Hand.position;
        //float distanceAlongForward = Vector3.Dot(toFootball, wr1Hand.forward);


        Vector3 rayDirection = wr1Hand.forward * -1;
        RaycastHit hit;
        Debug.DrawRay(wr1Hand.position, wr1Hand.forward * -1 * 50f, Color.red);
        if (Physics.Raycast(wr1Hand.position, rayDirection, out hit, 0.1f)) {
            int c = 0;

            int h = 9;
        }




        if ((toFootball.y < 0 && Mathf.Abs(toFootball.y) <= 0.05f) && (Mathf.Abs(toFootball.x) < 0.05f) && (Mathf.Abs(toFootball.z) < 0.05f)) {
            caughtFootball = true;
        }
        return caughtFootball;
    }


    private void FixedUpdate()
    {
        //foreach (var collision in GetComponent<BoxCollider>().)
        float range = Mathf.Abs(collisionPosition.z - transform.GetChild(0).gameObject.transform.Find("Front").position.z) < Mathf.Abs(collisionPosition.z - transform.GetChild(0).gameObject.transform.Find("Back").position.z) ? transform.GetChild(0).gameObject.transform.Find("Front").position.z : transform.GetChild(0).gameObject.transform.Find("Back").position.z;
        if (withinRange && (Mathf.Abs(collisionPosition.z - range) > 0.5f))
        {
            //TESTING ON 9/8/2024
            //withinRange = false;
        }

        if (!addForce && !withinRange)
        {
            velBeforeCollision = GetComponent<Rigidbody>().velocity;

        }
        if (handCollisionGameObject != null) {
            Vector3 targetPosition2 = handCollisionGameObject.transform.parent.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * (0.1705792f / 2f));
            //wrBodyPosition.gameObject.GetComponent<WR>().leftHandCatchPosition
            Transform lPos = wrBodyPosition.gameObject.GetComponent<WR>().leftHandCatchPosition;
            Transform rPos = wrBodyPosition.gameObject.GetComponent<WR>().rightHandCatchPosition;
            //Vector3.Distance(transform.position, targetPosition2) > 2;
            //if (Vector3.Distance(handCollisionGameObject.transform.parent.position, lPos.position) > 2 && Vector3.Distance(handCollisionGameObject.transform.parent.position, rPos.position) > 2)

            //if (Vector3.Distance(handCollisionGameObject.transform.position, transform.position) > 2)
            //{
            //    addForce = false;
            //    withinRange = false;
            //    caughtFootball = false;
            //    if (wrBodyPosition != null && wrBodyPosition.gameObject != null && wrBodyPosition.gameObject.GetComponent<WR>() != null) {
            //        wrBodyPosition.gameObject.GetComponent<WR>().caughtFootball = null;
            //    }
            //    if (gameObject != null) {
            //        Destroy(gameObject.GetComponent<FixedJoint>());
            //    } if (GameObject.Find("PivotAnchor") != null) {
            //        Destroy(GameObject.Find("PivotAnchor"));
            //    }              
                
            //}
        }


        if (caughtFootball)
        {

        //    GameObject pivotAnchor = GameObject.Find("PivotAnchor");
        //    if (pivotAnchor != null && pivotAnchor.GetComponent<Rigidbody>() != null)
        //    {
        //        Vector3 angularTorque = new Vector3(
        //pivotAnchor.GetComponent<Rigidbody>().angularVelocity.x * -1f,
        //pivotAnchor.GetComponent<Rigidbody>().angularVelocity.y * -1f,
        //pivotAnchor.GetComponent<Rigidbody>().angularVelocity.z * -1f);
        //        pivotAnchor.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
        //        pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
        //        pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
        //    }
        //    if (gameObject.GetComponent<Rigidbody>() != null)
        //    {
        //        Vector3 angularTorque = new Vector3(
        //                gameObject.GetComponent<Rigidbody>().angularVelocity.x * -1f,
        //                gameObject.GetComponent<Rigidbody>().angularVelocity.y * -1f,
        //                gameObject.GetComponent<Rigidbody>().angularVelocity.z * -1f);
        //        gameObject.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
        //        gameObject.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
        //        gameObject.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
        //    }

        //    Vector3 horTorque = new Vector3(0, 2 * Mathf.Deg2Rad, 0);

        //    if (GameObject.Find("PivotAnchor") != null) {

        //        Vector3 targetPosition = handCollisionGameObject.transform.parent.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * 0.1705792f);
        //        Vector3 targetPosition2 = handCollisionGameObject.transform.parent.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * (0.1705792f / 2f));
        //        Debug.DrawRay(handCollisionGameObject.transform.parent.position, new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * 1705792f, Color.red);
        //        Vector3 targetDirection = targetPosition - rightArm.parent.Find("DefaultSpot").position;
        //        if (targetDirection.magnitude > 0.35f) {
        //            targetDirection *= (0.35f / targetDirection.magnitude);
        //        }
        //        AimAtTargetInput(transform, handCollisionGameObject.transform.parent.up, 'r', false, 90f, true);
        //        MoveToPoint(transform, transform.position, handCollisionGameObject.transform.position, 1f);
        //        Debug.DrawRay(rightArm.position, targetDirection, Color.blue);

        //        Vector3 actualTarget = targetPosition + new Vector3(-targetDirection.x, 0, -targetDirection.x);

        //        Vector3 val = targetDirection.normalized;
        //        MoveToPoint(rightArm, rightArm.position, rightArm.parent.Find("DefaultSpot").position + targetDirection, 0.75f);

        //        targetDirection = targetPosition - rightArm.parent.Find("DefaultSpot").position;
        //        AimAtTargetInput(rightArm, targetDirection, 'r', true, 30f, true);
        //    }

        }
        else {
            int h = 0;
        }
    }




    private void AimAtTargetInput(Transform armToAim, Vector3 inputVector, char axis = 'r', bool isNegative = true, float angleAmount = 30f, bool fullRotation = false)
    {
        float sign = isNegative ? -1f : 1f;
        Vector3 sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //if ()

        if (!fullRotation)
        {
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.up, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.forward, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.right, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.forward, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.up, angleAmount, fullRotation);
        }
        else {
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.up, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.forward, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.right, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.forward, angleAmount, fullRotation);
            sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
            RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.up, angleAmount, fullRotation);
        }

    }

    private void RotateTransformToDirection(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false)
    {
        Vector3 projectedForward = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
        Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);

        float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, rotationAxis);

        //ORIGINALLY WAS 30f
        //float amountToRotatePerFrame = 30f;
        float amountToRotatePerFrame = angleMagnitude;
        if (fullRotation)
        {
            armToRotate.Rotate(rotationAxis, angleForward, Space.World);
        }
        else {
            if (Mathf.Abs(angleForward) > amountToRotatePerFrame)
            {
                armToRotate.Rotate(rotationAxis, Mathf.Sign(angleForward) * amountToRotatePerFrame, Space.World);
            }
            else
            {
                armToRotate.Rotate(rotationAxis, angleForward, Space.World);
            }
        }

    }

    private void MoveToPoint(Transform armToMove, Vector3 sourcePosition, Vector3 targetPosition, float magnitude)
    {
        armToMove.position += (targetPosition - sourcePosition) * magnitude;
    }

    private void SetRigidbodyPivot(Transform pivot, Rigidbody target) {
        if (GameObject.Find("PivotAnchor") == null) {
            Vector3 leftArmPivotPosition = pivot.position;

            // Create an empty GameObject at the LeftArm's pivot point to act as the joint's anchor
            GameObject pivotAnchor = new GameObject("PivotAnchor");
            //pivotAnchor.layer = 10;
            pivotAnchor.transform.position = leftArmPivotPosition;
            Rigidbody pivotRb = pivotAnchor.AddComponent<Rigidbody>();
            //pivotAnchor.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            //pivotAnchor.GetComponent<Rigidbody>().isKinematic = true; // We don't want the anchor to move
            pivotAnchor.GetComponent<Rigidbody>().isKinematic = false; // We don't want the anchor to move
            //target.useGravity = false;
            FixedJoint joint = target.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = pivotRb; // Connect the joint to the pivotAnchor
                                           //joint.anchor = Vector3.zero; // Football's center of mass is the local anchor point

            joint.anchor = Vector3.zero; // Football's center of mass is the local anchor point
                                         //target.gameObject.transform.InverseTransformPoint(leftArm.position);

            Debug.DrawRay(pivotAnchor.transform.position, Vector3.up * 50f, Color.gray);
            Debug.DrawRay(target.gameObject.transform.TransformPoint(joint.anchor), Vector3.up * 50f, Color.black);
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Cube" || collision.collider.gameObject.tag == "Hand") {
            //addForce = true;
            //withinRange = true;
            caughtFootball = true;
            stopFootball = true;
            if (collision.collider.gameObject.tag == "Hand") {
                //if (wrBodyPosition != null && wrBodyPosition.gameObject != null && wrBodyPosition.gameObject.GetComponent<WR>() != null)
                
                wrBodyPosition.gameObject.GetComponent<WR>().caughtFootball = gameObject;
                handCollisionGameObject = collision.collider.gameObject;

                float minDistance = float.MaxValue;
                for (int i = 0; i < transform.GetChild(0).childCount; i++)
                {
                    string name = transform.GetChild(0).GetChild(i).name;
                    bool isNumeric = true;
                    foreach (char chr in name.Substring(1))
                    {
                        if (!char.IsDigit(chr))
                        {
                            isNumeric = false;
                        }
                    }
                    if (name != null && name.Length > 0 && name.ToLower()[0] == 'p' && isNumeric)
                    {
                        int childIndex = int.Parse(name.Substring(1));
                        string rightHandTargetName = "";
                        int righthandIndex = 0;
                        if ((childIndex % 6 <= 3 && childIndex % 6 != 0)/* && i < (transform.GetChild(0).childCount - 1)*/)
                        {
                            righthandIndex = childIndex + 3;
                            rightHandTargetName = "P" + righthandIndex.ToString();
                        }
                        else
                        {
                            righthandIndex = childIndex - 3;
                            rightHandTargetName = "P" + righthandIndex.ToString();
                        }

                        float distanceToChildSpotLeftHand = Vector3.Distance(handCollisionGameObject.transform.parent.position, transform.GetChild(0).GetChild(i).position);
                        if (transform.GetChild(0).Find(rightHandTargetName) == null) {
                            int j = 0;
                        }
                        float distanceToChildSpotRightHand = Vector3.Distance(handCollisionGameObject.transform.parent.position, transform.GetChild(0).Find(rightHandTargetName).position);

                        if (distanceToChildSpotLeftHand < minDistance)
                        {
                            minDistance = distanceToChildSpotLeftHand;
                            //wrBodyPosition.gameObject.GetComponent<WR>().leftHandCatchPosition = handCollisionGameObject.transform.parent.GetChild(i);
                            //wrBodyPosition.gameObject.GetComponent<WR>().rightHandCatchPosition = handCollisionGameObject.transform.parent.Find(rightHandTargetName);

                            wrBodyPosition.gameObject.GetComponent<WR>().leftHandCatchPosition = transform.GetChild(0).GetChild(i);
                            wrBodyPosition.gameObject.GetComponent<WR>().rightHandCatchPosition = transform.GetChild(0).Find(rightHandTargetName);
                        }
                        if (distanceToChildSpotRightHand < minDistance)
                        {
                            minDistance = distanceToChildSpotRightHand;
                            //wrBodyPosition.gameObject.GetComponent<WR>().leftHandCatchPosition = handCollisionGameObject.transform.parent.Find(rightHandTargetName); 
                            //wrBodyPosition.gameObject.GetComponent<WR>().rightHandCatchPosition = handCollisionGameObject.transform.parent.GetChild(i);


                            wrBodyPosition.gameObject.GetComponent<WR>().leftHandCatchPosition = transform.GetChild(0).Find(rightHandTargetName);
                            wrBodyPosition.gameObject.GetComponent<WR>().rightHandCatchPosition = transform.GetChild(0).GetChild(i);
                        }
                        if ((wrBodyPosition.gameObject.GetComponent<WR>().leftHandCatchPosition == null || wrBodyPosition.gameObject.GetComponent<WR>().rightHandCatchPosition == null)
                            )
                        {

                        }
                    }


                }


                //leftArm
                //Vector3 targetPosition2 = handCollisionGameObject.transform.parent.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * (0.1705792f / 2f));
                //MoveToPoint(transform, transform.position, targetPosition2, 1f);
                //AimAtTargetInput(transform, handCollisionGameObject.transform.parent.up, 'r', false, 90f, true);
            }

            if (gameObject != null) {
                handCollisionGameObject.transform.parent.gameObject.layer = 12;
                handCollisionGameObject.layer = 12;
                rightArm.Find("RightForeArm").transform.Find("RightHand").gameObject.layer = 12;
                collisionPosition = collision.contacts[0].point;
                collisionPoint = collisionPosition;
                centerOfMassPositionOnCollision = gameObject.GetComponent<Rigidbody>().worldCenterOfMass;
                GameObject collisionPositionGameObject = Instantiate<GameObject>(collisionPositionOnFootballPrefab);
                collisionPositionGameObject.transform.position = collisionPoint;
                collisionPositionGameObject.transform.parent = this.gameObject.transform;
                collisionGameObjectName = collisionPositionGameObject.name;
                Vector3 deltaVector = collisionPoint - centerOfMassPositionOnCollision;
                Debug.DrawRay(centerOfMassPositionOnCollision - deltaVector, Vector3.right * 50f, Color.blue);
                Debug.DrawRay(centerOfMassPositionOnCollision, Vector3.right * 50f, Color.green);
                Debug.DrawRay(collisionPosition, Vector3.right * 50f, Color.yellow);


                initialVelocity = gameObject.GetComponent<Rigidbody>().mass * gameObject.GetComponent<Rigidbody>().velocity;
                rotVelBeforeCollision = gameObject.GetComponent<Rigidbody>().angularVelocity;
                Vector3 inertiaTensor = Mathf.Abs(collisionPosition.z - transform.GetChild(0).gameObject.transform.Find("Front").position.z) < Mathf.Abs(collisionPosition.z - transform.GetChild(0).gameObject.transform.Find("Back").position.z) ? transform.GetChild(0).gameObject.transform.Find("Front").position : transform.GetChild(0).gameObject.transform.Find("Back").position;


                Vector3 angularTorque = new Vector3(
                    gameObject.GetComponent<Rigidbody>().angularVelocity.x * -1,
                    gameObject.GetComponent<Rigidbody>().angularVelocity.y * -1,
                    gameObject.GetComponent<Rigidbody>().angularVelocity.z * -1);
                if (gameObject.GetComponent<FixedJoint>() != null)
                {
                    gameObject.GetComponent<FixedJoint>().anchor = Vector3.zero;

                }


                /*GameObject pivotAnchor = GameObject.Find("PivotAnchor");
                if (pivotAnchor != null && pivotAnchor.GetComponent<Rigidbody>() != null)
                {

                    pivotAnchor.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
                }*/
                if (gameObject.GetComponent<Rigidbody>() != null)
                {
                    
                    gameObject.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
                    gameObject.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
                    gameObject.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
                    //gameObject.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);

                }

                //Rotate Football to right in front of the WR's face
                timeWRCaughtPass = Time.time;

                footballCenterOfMassOnCatch = gameObject.GetComponent<Rigidbody>().worldCenterOfMass;


                //THIS WAS HERE ON 9/14/2025
                SetRigidbodyPivot(leftArm, gameObject.GetComponent<Rigidbody>());

                /*GameObject pivotAnchor = GameObject.Find("PivotAnchor");
                if (pivotAnchor != null && pivotAnchor.GetComponent<Rigidbody>() != null)
                {

                    pivotAnchor.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
                }*/

                horFootballAngleWhenCaught = transform.localEulerAngles.y;

                setAngle = true;
                collided = true;

                timeVal = Time.time;
                

            }

            //leftArm
            Vector3 targetPosition2 = handCollisionGameObject.transform.parent.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * (0.1705792f / 2f));
            MoveToPoint(transform, transform.position, targetPosition2, 1f);
            AimAtTargetInput(transform, handCollisionGameObject.transform.parent.up, 'r', false, 90f, true);
        }
    }



    /*if (GameObject.Find("PivotAnchor") != null && GameObject.Find("PivotAnchor").GetComponent<Rigidbody>() != null)
    {
        Vector3 pivotAngularTorque = new Vector3(
                                GameObject.Find("PivotAnchor").GetComponent<Rigidbody>().angularVelocity.x * -1,
                                GameObject.Find("PivotAnchor").GetComponent<Rigidbody>().angularVelocity.y * -1,
                                GameObject.Find("PivotAnchor").GetComponent<Rigidbody>().angularVelocity.z * -1);
        GameObject.Find("PivotAnchor").GetComponent<Rigidbody>().AddForce((-1) * (GameObject.Find("PivotAnchor").GetComponent<Rigidbody>().mass * GameObject.Find("PivotAnchor").GetComponent<Rigidbody>().velocity), ForceMode.Impulse);
        GameObject.Find("PivotAnchor").GetComponent<Rigidbody>().AddTorque(pivotAngularTorque, ForceMode.Force);
    }*/
    /*if (gameObject.GetComponent<Rigidbody>() != null)
    {

        gameObject.GetComponent<Rigidbody>().AddForce((-1) * gameObject.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
        gameObject.GetComponent<Rigidbody>().AddTorque((-1) * angularTorque, ForceMode.Force);

        if (handCollisionGameObject != null)
        {
            Vector3 deltaDistanceWithHand = handCollisionGameObject.transform.position - transform.position;
            gameObject.GetComponent<Rigidbody>().AddForce(deltaDistanceWithHand, ForceMode.Impulse);
        }

        gameObject.GetComponent<Rigidbody>().AddForce((-1) * gameObject.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
        gameObject.GetComponent<Rigidbody>().AddTorque((-1) * angularTorque, ForceMode.Force);
    }*/
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Cube" || collision.collider.gameObject.tag == "Hand")
        {
            if (gameObject != null)
            {
                addForce = true;
                //GameObject pivotAnchor = new GameObject("PivotAnchor");
                

                /*if (pivotAnchor != null) {
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1) * pivotAnchor.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Impulse);
                }
                gameObject.GetComponent<Rigidbody>().AddForce((-1) * gameObject.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Impulse);*/

                float forceValue =  gameObject.GetComponent<Rigidbody>().mass * Mathf.Abs(Physics.gravity.y);
                //float horForceX =  gameObject.GetComponent<Rigidbody>().mass * (- gameObject.GetComponent<Rigidbody>().velocity.x / Time.deltaTime);
                //float horForceZ =  gameObject.GetComponent<Rigidbody>().mass * (- gameObject.GetComponent<Rigidbody>().velocity.z / Time.deltaTime);
                //float horizontalForce =  gameObject.GetComponent<Rigidbody>().mass * (- gameObject.GetComponent<Rigidbody>().velocity.x / Time.deltaTime);
                timeVal = Time.time;
                forceVal = forceValue;
            }
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.gameObject.tag == "Cube" || collision.collider.gameObject.tag == "Hand")
        {
            if (gameObject != null)
            {
                addForce = false;
                float forceValue =  gameObject.GetComponent<Rigidbody>().mass * Mathf.Abs(Physics.gravity.y);
                forceVal = forceValue;


                //FixedJoint joint = gameObject.AddComponent<FixedJoint>();

                //Destroy(gameObject.GetComponent<FixedJoint>());
                //joint.connectedBody = null;
                if (Vector3.Distance(gameObject.GetComponent<Rigidbody>().worldCenterOfMass, handCollisionGameObject.transform.position) > 0.3f) {
                    addForce = false;
                    withinRange = false;
                    caughtFootball = false;
                    //Destroy(gameObject.GetComponent<FixedJoint>());
                    //Destroy(GameObject.Find("PivotAnchor"));
                }

                // gameObject.GetComponent<Rigidbody>().AddForce(Vector3.up * forceValue, ForceMode.Force);
            }
        }
    }

    private void ApplyTorqueToBallAtOffset(Rigidbody ballRigidBody, Vector3 torqueAxis, float torqueAmount, Transform leftArmTransform, Transform football) {
        Vector3 force = torqueAxis * torqueAmount;
        ballRigidBody.AddForceAtPosition(force, football.position, ForceMode.VelocityChange);
    }





    public float GetLeverArmAngle(Vector2 movementInput)
    {
        float angle = 0f;
        //Q1
        if (movementInput.x >= 0 && movementInput.y >= 0)
        {
            angle = Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
        }
        //Q3
        else if (movementInput.x < 0 && movementInput.y < 0)
        {
            angle = 180 + Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
        }
        //Q2
        else if (movementInput.x < 0 && movementInput.y >= 0)
        {
            angle = 180 + Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
        }
        //Q4
        else if (movementInput.x > 0 && movementInput.y < 0)
        {
            angle = 360 + Mathf.Atan(movementInput.y / movementInput.x) * Mathf.Rad2Deg;
        }

        return angle;
    }
}

public class GameObjectRotation {
    public float MaxClockwiseRotationUnitCircle { get; set; }
    public float MaxCounterClockwiseRotationUnitCircle { get; set; }
    public float SourceAngle { get; set; }
    public float CurrentAngle { get; set; }
    public float TargetAngle { get; set; }
    public float ClockwiseAngleDistance { get; set; }
    public float CounterClockwiseAngleDistance { get; set; }
    public float ClockwiseAngleDistanceFromArmOrigin { get; set; }
    public float CounterClockwiseAngleDistanceFromArmOrigin { get; set; }
    public WR.Direction Direction { get; set; }

}
