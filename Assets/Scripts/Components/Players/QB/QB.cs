using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FootballGame.Common;
using System;

public class QB : MonoBehaviour
{
    [Header("Set In Inspector")]
    public GameObject wrX;
    public Transform wrXHead;
    public Transform wrXFoot;
    public GameObject wrB;
    public GameObject football;
    public float throwSpeed;
    [Header("Lead Throw (ReleaseFootball)")]
    [Tooltip("Horizontal ball speed used to pick the flight time, as a multiple of the WR's top speed.")]
    public float leadThrowSpeedMultiplier = 1.5f;
    [Tooltip("Shortest flight time for a lead throw, in seconds.")]
    public float leadThrowMinFlightTime = 1.5f;
    [Tooltip("Longest flight time for a lead throw, in seconds (the old fixed value was 4).")]
    public float leadThrowMaxFlightTime = 4f;
    public GameObject parentFootballPositionPrefab;
    public Transform handPosition;
    public Transform wrTargetPosition;
    public LineRenderer lineRenderer;
    public Transform wr1Hand;


    [Header("Set Dynamically")]
    Vector3 expectedWRPosition = Vector3.zero;
    Vector3 wrXPositionWhenBallThrown = Vector3.zero;
    private float requiredFOV = 0f;
    private Vector3 throwForce;
    public bool throwBPerformed = false;
    private Vector3 throwingTargetPosition;
    private GameObject mainCamera;
    public float lineCount = 20;
    private List<Vector3> linePoints = new List<Vector3>();
    KeyValuePair<float, float> footballProjectileRotationTime = new KeyValuePair<float, float>();
    public GameObject parentFootballPosition;
    public float yForce;
    float rotateValue = 0.0f;
    private PlayerInput playerInput;
    private InputAction throwAction;
    private bool move = false;
    private Animator playerAnimator;
    Vector3 wrPosition = new Vector3(0, 0, 0);
    float launchAngle = 45f;
    float launchDirection = 0f;
    float launchVelX = 0f;
    float launchVelY = 0f;
    float launchVelZ = 0f;
    float startTime = 0.0f;
    float timeInAir = 0f;
    float timeToCatch = 0f;
    float timeThrown = 0f;
    float throwTime = 0f;
    float differenceTime = 0f;
    float delT = 0f;
    public bool thrown = false;
    bool setPositionForFirstTime = false;
    KeyValuePair<float, float> footballProjectileRotationTimeDraw = new KeyValuePair<float, float>();
    private int currCameraIndex;
    private float cameraOffset;
    private Vector3 footballPositionLastFrame = new Vector3(0, 0, 0);
    float timeThrowStarted = 0f;
    float timeOfPreviousThrowFrame = 0f;
    Vector3 positionOfCameraWhenStartingThrow = new Vector3(0, 0, 0);

    float totalTimeMovingCameraToSpotForThrow = 0f;
    bool cameraSetForLaunch = false;
    bool cameraAlignedAfterCatch = false;
    float timeStartedCameraAlignmentAfterCatch = 0f;
    bool startedCameraAlignmentAfterCatch = false;
    Vector3 cameraDestinationDirection = new Vector3(0, 0, 0);
    Vector3 cameraPositionWhenStartingAlignmentAfterCatch = new Vector3(0, 0, 0);
    bool addedForceToCamera = false;
    bool canStartFollowing = false;


    public string PlayBookName = "Playbook_Canvas";
    // Start is called before the first frame update
    void Awake()
    {
        wrX.GetComponent<WR>().SetFootballInstance(football);
        wrB.GetComponent<WR>().SetFootballInstance(football);
        wrX.GetComponent<WR>().SetCanSetThrowTarget(true);
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
        //throwAction = playerInput.actions["Throw"];
        throwAction = playerInput.actions["ThrowXbox360"];
        playerInput.actions["ThrowXbox360"].performed += OnThrowPerformed;
        playerInput.actions["ThrowBXbox360"].performed += OnThrowBPerformed;
        playerInput.actions["PressedPlay1"].performed += OnPlay1Selected;
        //playerInput.actions["ThrowXbox360"].performed += OnThrowBPerformed;
        throwAction.ReadValue<float>();
        yForce = 20f;
        mainCamera = GameObject.Find("Camera");
        cameraOffset = -3f;
        footballPositionLastFrame = football.transform.position;
        //parentFootballPositionGetComponent<Rigidbody>().isKinematic = true;
    }



    private void OnPlay1Selected(InputAction.CallbackContext context)
    {
        if (GameObject.Find(PlayBookName) != null && GameObject.Find(PlayBookName).activeSelf)
        {
            GameObject.Find(PlayBookName).SetActive(false);
            if (gameObject.GetComponent<QB>() != null && gameObject.GetComponent<QB>().wrB != null && gameObject.GetComponent<QB>().wrB.GetComponent<WR>() != null && gameObject.GetComponent<QB>().wrB.GetComponent<WR>().GetRouteSegments() != null
                && gameObject.GetComponent<QB>().wrB.GetComponent<WR>().GetRouteSegments().Count <= 0)
            {
                //GetComponent<QB>().wrB.GetComponent<WR>().GetRoute().routeParts
                RouteSegment s1 = new RouteSegment
                {
                    startPosition = gameObject.GetComponent<QB>().wrB.transform.position,
                    direction = new Vector3(1, 0, 1).normalized,
                    runDistance = 3f * (2.6f)
                };

                RouteSegment s2 = new RouteSegment
                {
                    startPosition = gameObject.GetComponent<QB>().wrB.transform.position + (new Vector3(1, 0, 1).normalized * (3f * 2.6f)),
                    direction = new Vector3(0, 0, 1).normalized,
                    runDistance = 8f * (2.6f)
                };

                RouteSegment s3 = new RouteSegment
                {
                    startPosition = gameObject.GetComponent<QB>().wrB.transform.position + (new Vector3(1, 0, 1).normalized * (8f * 2.6f)),
                    direction = new Vector3(1, 0, 0.01f).normalized,
                    runDistance = 12f * (2.6f)
                };

                gameObject.GetComponent<QB>().wrB.GetComponent<WR>().SetRouteSegments(new List<RouteSegment>()
                {
                    s1,
                    s2,
                    s3
                });

                /*gameObject.GetComponent<QB>().wrB.GetComponent<WR>().SetRouteSegments(new List<RouteSegment>()
                {
                    s1
                });*/
            }
        }

    }

    public void SetThrowingTargetPosition(Vector3 tPosition) {
        throwingTargetPosition = tPosition;
    }

    public GameObject GetFootball() {
        return football;
    }
    public bool GetIsThrown() {
        return thrown;
    }

    public float GetTimeToCatch()
    {
        return timeToCatch;
    }
    public float GetTimeThrown()
    {
        return timeThrown;
    }


    public Animator GetAnimator() {
        return GetComponent<Animator>();
    }
    private void LateUpdate()
    {
        float timeToCatchCamera = 0f;
        /*if (!setPositionForFirstTime && thrown) {
            timeThrown = Time.time;
            setPositionForFirstTime = true;
            currCameraIndex = 0;
            Vector3 tDest = new Vector3(0, 0, 0);
            if (transform.forward.z >= 0)
            {
                tDest = wrX.transform.position + -(wrX.transform.forward * 6f);
                tDest = tDest + new Vector3(0, 15f, 0);
                //tDest = tDest + new Vector3(0, 42f, 0);
            }
            else
            {
                tDest = wrX.transform.position + (wrX.transform.forward * 6f);
                tDest = tDest + new Vector3(0, 15f, 0);
            }
            Vector3 forceVector = wrX.transform.position - transform.position;
            Vector3 cameraForce = tDest - mainCamera.transform.position;

            Vector3 forceVectorFootball = new Vector3(0, 0, 0);
            if (throwingTargetPosition.magnitude > 0)
            {
                forceVectorFootball = throwingTargetPosition - football.transform.position;
            }
            else {
                forceVectorFootball = wrTargetPosition.position - football.transform.position;
            }
            //Vector3 forceVectorFootball = wrTargetPosition.position - football.transform.position;
            //forceVectorFootball = throwingTargetPosition - football.transform.position;
            Vector3 forceVectorCamera = tDest - mainCamera.transform.position;
            Vector3 forceVectorFinal = new Vector3(forceVectorFootball.x, 0f, forceVectorFootball.z);
            Vector3 forceVectorFinalCamera = new Vector3(forceVectorCamera.x, forceVector.y, forceVectorCamera.z);


            float fvX = forceVectorFinal.x > 0 ? forceVectorFinal.x : 1;
            float fvZ = forceVectorFinal.z > 0 ? forceVectorFinal.z : 1;
            float fvXCamera = forceVectorFinalCamera.x > 0 ? forceVectorFinalCamera.x : 1;
            float fvZCamera = forceVectorFinalCamera.z > 0 ? forceVectorFinalCamera.z : 1;
            timeToCatch = Mathf.Sqrt((forceVectorFinal.x * forceVectorFinal.x) + (forceVectorFinal.z * forceVectorFinal.z)) / (throwSpeed * Mathf.Abs(fvZ / fvX));

            float speedDifference = (tDest - mainCamera.transform.position).magnitude / (wrTargetPosition.position - football.transform.position).magnitude;
            //float timeToCatchCamera = Mathf.Sqrt((forceVectorFinalCamera.x * forceVectorFinalCamera.x) + (forceVectorFinalCamera.z * forceVectorFinalCamera.z)) / ((throwSpeed) * Mathf.Abs(fvZCamera / fvXCamera));
            timeToCatchCamera = timeToCatch;
            float velX = forceVectorFinal.x / timeToCatch;
            float velXCamera = forceVectorFinalCamera.x / timeToCatch;
            float velZ = forceVectorFinal.z / timeToCatch;
            float velZCamera = forceVectorFinalCamera.z / timeToCatch;
            float velXZ = Mathf.Sqrt((velX * velX) + (velZ * velZ));
            float velY = 0f;
            
            if (throwingTargetPosition.magnitude > 0)
            {
                velY = ((0.5f * Mathf.Abs(Physics.gravity.y) * (timeToCatch * timeToCatch)) + (throwingTargetPosition.y - football.transform.position.y)) / (timeToCatch);
            }
            else {
                velY = ((0.5f * Mathf.Abs(Physics.gravity.y) * (timeToCatch * timeToCatch)) + (wrTargetPosition.position.y - football.transform.position.y)) / (timeToCatch);
            }
            
            float velYCamera = ((0.5f * Mathf.Abs(Physics.gravity.y) * (timeToCatch * timeToCatch)) + ((tDest.y - mainCamera.transform.position.y))) / (timeToCatch);
            launchVelY = velY;
            float threeDimVelMagnitude = Mathf.Sqrt((velXZ * velXZ) + (velY * velY));
            launchAngle = Mathf.Asin(velY / threeDimVelMagnitude) * Mathf.Rad2Deg;



            //NEEDED FOR ENLARGED GRAVITY SOLUTION
            //Tuple<float, float> roots = SolveQuadratic(4.9f * (22200 / 5f), -velY, -football.transform.position.y);
            Tuple<float, float> roots = SolveQuadratic(4.9f, -velY, -football.transform.position.y);





            timeInAir = roots.Item1;
            //Tuple<float, float> roots = 
            //timeOfFlight = velY / ((0.5f) * Mathf.Abs(Physics.gravity.y));
            float direction = -Mathf.Atan(velZ / velX) * Mathf.Rad2Deg;
            setPositionForFirstTime = true;
            parentFootballPosition = Instantiate<GameObject>(parentFootballPositionPrefab);
            parentFootballPosition.GetComponent<TestCollision>().wr1Hand = wr1Hand;
            parentFootballPosition.GetComponent<TestCollision>().wrBodyPosition = wrX.transform;
            parentFootballPosition.GetComponent<TestCollision>().leftArm = wrX.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(5).GetChild(2);
            parentFootballPosition.GetComponent<TestCollision>().rightArm = wrX.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetChild(1);


            //parentFootballPosition.transform.position = football.transform.position;
            parentFootballPosition.transform.position = football.transform.position;
            football.transform.parent = null;
            football.transform.localEulerAngles = new Vector3(0, direction, launchAngle);
            parentFootballPosition.transform.localEulerAngles = football.transform.localEulerAngles;
            football.transform.parent = parentFootballPosition.transform;
            //football.AddComponent<Rigidbody>();
            //parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            //parentFootballPosition.GetComponent<Rigidbody>().angularDrag = 0.75f;




            parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            //parentFootballPosition.GetComponent<Rigidbody>().angularDrag = 0.75f;





            //Vector3 impulseForce = new Vector3(vx * parentFootballPositionGetComponent<Rigidbody>().mass, vy * parentFootballPositionGetComponent<Rigidbody>().mass, vz * parentFootballPositionGetComponent<Rigidbody>().mass);
            Vector3 impulseForceOfficial = new Vector3(velX * parentFootballPosition.GetComponent<Rigidbody>().mass, velY * parentFootballPosition.GetComponent<Rigidbody>().mass, velZ * parentFootballPosition.GetComponent<Rigidbody>().mass);

            //GameObject parentFootballPosition = Instantiate<GameObject>()


            parentFootballPosition.GetComponent<Rigidbody>().AddForce(impulseForceOfficial, ForceMode.Impulse);

            float stepTime = timeInAir / lineCount;
            float timeVal = Mathf.Abs(timeInAir);
            linePoints.Clear();
            //projectileAngleTimePair = new KeyValuePair<float, float>(yAngle, timeVal);
            lineRenderer.positionCount = linePoints.Count;
            lineRenderer.SetPositions(linePoints.ToArray());

            //projectileAngleTimePair = new KeyValuePair<float, float>(yAngle, timeVal);
            //return projectileAngleTimePair;
            for (int i = 0; i < lineCount; i++)
            {
                if (i == lineCount - 1)
                {
                    int y = 0;
                }
                float stepTimePassed = stepTime * i;
                Vector3 movementVector = new Vector3(
                    velX * stepTimePassed,
                    velY * stepTimePassed - 0.5f * Mathf.Abs(Physics.gravity.y) * stepTimePassed * stepTimePassed,
                    velZ * stepTimePassed);
                if (i == ((lineCount / 2) - 1))
                {
                    float val = stepTimePassed;
                }
                else if (i == (lineCount / 2)) {
                    float g = stepTimePassed;
                }
                linePoints.Add(movementVector + parentFootballPosition.transform.position);
            }
            lineRenderer.positionCount = linePoints.Count;
            lineRenderer.SetPositions(linePoints.ToArray());
            //lineRenderer.
        }*/

        if (thrown)
        {

            float rotValue = Time.time - throwTime;
            float t = Time.deltaTime * 360;
            delT += Time.deltaTime;
            //football.transform.localEulerAngles += new Vector3(Time.deltaTime * -360, 0, 0);
            float rotationAmount = -900 * Time.deltaTime;
            float rotationValue = launchAngle - launchAngle * ((Time.time - throwTime) / (timeInAir / 2f));
            float difference = Time.time - differenceTime;
            //float rValue = (-1f) * (Time.fixedDeltaTime / (timeInAir / 2f)) * launchAngle;
            float rotateAmountTest = (launchAngle + ((Physics.gravity.y / 4400f) * (Time.deltaTime))) - launchAngle;
            float rotateAmount = (launchVelY * Time.time - 0.5f * Mathf.Abs(Physics.gravity.y) * Time.time * Time.time) - (launchVelY * differenceTime - 0.5f * Mathf.Abs(Physics.gravity.y) * differenceTime * differenceTime);
            float rValue = (-1f) * (difference / (timeToCatch / 2f)) * launchAngle;
            differenceTime = Time.time;
            if ((parentFootballPosition != null) && (parentFootballPosition.GetComponent<TestCollision>() != null && !parentFootballPosition.GetComponent<TestCollision>().stopFootball /* && !parentFootballPosition.GetComponent<TestCollision>().addForce*/))
            {
                football.transform.Rotate(Vector3.right, rotationAmount, Space.Self);
                football.transform.parent.Rotate(Vector3.forward, rValue, Space.Self);
            }
            Vector3 footballNormalizedAngles = GetNormalizedEulerAngles(football.transform.localEulerAngles);
            int d = 0;
        }
            
    }

    private bool FitsInVerticalView(
    Camera cam,
    Vector3 footballPosition,
    Vector3 wrTargetPosition)
    {
        Vector3 footballViewport =
            cam.WorldToViewportPoint(footballPosition);

        Vector3 wrViewport =
            cam.WorldToViewportPoint(wrTargetPosition);

        return
            footballViewport.z > 0f &&
            wrViewport.z > 0f &&
            footballViewport.y <= 1f &&
            wrViewport.y >= 0f;
    }

    private float CalculateRequiredFOV(
        Camera cam,
        Vector3 targetCameraPosition,
        Vector3 footballPosition,
        Vector3 wrTargetPosition)
    {
        Vector3 originalPosition = cam.transform.position;
        float originalFOV = cam.fieldOfView;

        // Temporarily use the camera's future position
        cam.transform.position = targetCameraPosition;

        float minFOV = originalFOV;
        float maxFOV = 179f;

        for (int i = 0; i < 15; i++)
        {
            float testFOV = (minFOV + maxFOV) * 0.5f;

            cam.fieldOfView = testFOV;

            if (FitsInVerticalView(
                cam,
                footballPosition,
                wrTargetPosition))
            {
                maxFOV = testFOV;
            }
            else
            {
                minFOV = testFOV;
            }
        }

        float requiredFOV = maxFOV;

        // Restore camera
        cam.transform.position = originalPosition;
        cam.fieldOfView = originalFOV;

        return requiredFOV;
    }

    Vector3 GetPositionBetween(Camera cam, GameObject firstObject, GameObject secondObject, Vector3 targetCamPosition, Vector3 targetFirstObject, Vector3 targetSecondObject) {
        Vector3 originalCameraPosition = cam.gameObject.transform.position;
        Vector3 originalFirstObject = firstObject.transform.position;
        Vector3 originalSecondObject = secondObject.transform.position;
        cam.transform.position = targetCamPosition;
        Vector3 viewportPos = cam.WorldToViewportPoint(targetFirstObject);
        Vector3 topEdgePosition = cam.ViewportToWorldPoint(new Vector3(viewportPos.x, 1f, viewportPos.z));
        Vector3 firstExpected = new Vector3(0f, 0f, 0f);
        Vector3 secondExpected = new Vector3(0f, 0f, 0f);

        bool inView = viewportPos.z > 0 &&
            viewportPos.x >= 0f && viewportPos.x <= 1f &&
            viewportPos.y >= 0f && viewportPos.y <= 1f;
        float deltaFromTop = 0f;
        float deltaFromBottom = 0f;
        if (inView)
        {
            int k = 0;
            firstExpected = cam.gameObject.transform.position;
        }
        else
        {
            int h = 0;
            deltaFromTop = Vector3.Dot(firstObject.transform.position - topEdgePosition, cam.gameObject.transform.up);
            //mainCamera.transform.position += mainCamera.transform.up * deltaFromTop;
            firstExpected = cam.gameObject.transform.position + mainCamera.transform.up * deltaFromTop;
            //deltaFromTop = viewportPos.y - 1f;
        }

        viewportPos = cam.WorldToViewportPoint(targetSecondObject);
        Vector3 bottomEdgePosition = cam.ViewportToWorldPoint(new Vector3(viewportPos.x, 0f, viewportPos.z));
        inView = viewportPos.z > 0 &&
            viewportPos.x >= 0f && viewportPos.x <= 1f &&
            viewportPos.y >= 0f && viewportPos.y <= 1f;
        if (inView)
        {
            int k = 0;
            secondExpected = cam.gameObject.transform.position;
        }
        else
        {
            int h = 0;
            //deltaFromBottom = Vector3.Dot(bottomEdgePosition - wrX.transform.position, mainCamera.transform.up);
            deltaFromBottom = Vector3.Dot(secondObject.transform.position - bottomEdgePosition, cam.gameObject.transform.up);
            //mainCamera.transform.position += mainCamera.transform.up * deltaFromBottom;
            secondExpected = cam.gameObject.transform.position + cam.gameObject.transform.up * deltaFromBottom;
            //deltaFromTop = viewportPos.y - 1f;
        }
        cam.gameObject.transform.position = originalCameraPosition;
        firstObject.transform.position = originalFirstObject;
        secondObject.transform.position = originalSecondObject;
        Vector3 horFirst = new Vector3(firstExpected.x, 0f, firstExpected.z);
        Vector3 horSecond = new Vector3(secondExpected.x, 0f, secondExpected.z);
        return (horFirst + horSecond) / 2;
    }

    /*if (!setPositionForFirstTime && thrown)
    {
        setPositionForFirstTime = true;
        parentFootballPosition.transform.position = handPosition.transform.position;
    }*/

    // Update is called once per frame
    Vector3 GetPositionToKeepInView(GameObject objectToKeepInView, List<Axis> axes, Camera camera) {
        Vector3 targetPosition = camera.transform.position;
        for (int i = 0; i < axes.Count; i++) {
            Vector3 movementDir = axes[i] == Axis.right ? camera.transform.right : axes[i] == Axis.up ? camera.transform.up : axes[i] == Axis.forward ? camera.transform.forward : Vector3.zero;
            Vector3 viewportPos = camera.GetComponent<Camera>().WorldToViewportPoint(objectToKeepInView.transform.position);
            Vector3 edgePos = axes[i] == Axis.right ? new Vector3(1f, viewportPos.y, viewportPos.z) : axes[i] == Axis.up ? new Vector3(viewportPos.x, 1f, viewportPos.z) : axes[i] == Axis.forward ? new Vector3(viewportPos.x, viewportPos.y, 1f) : Vector3.zero;
            Vector3 expectedPosition = new Vector3(0f, 0f, 0f);
            bool inView = axes[i] == Axis.right ? viewportPos.x >= 0f && viewportPos.x <= 1f : axes[i] == Axis.up ? viewportPos.y >= 0f && viewportPos.y <= 1f : axes[i] == Axis.forward ? viewportPos.z >= 0f && viewportPos.z <= 1f : false;
            float deltaFromEdge = 0f;
            if (!inView)
            {
                deltaFromEdge = Vector3.Dot(football.transform.position - edgePos, movementDir);
                expectedPosition = targetPosition + movementDir * deltaFromEdge;
                targetPosition += (expectedPosition - targetPosition);
            }
        }
        return targetPosition;
    }
    void Update()
    {
        if (expectedWRPosition.magnitude > 0) {
            Debug.DrawRay(wrX.transform.position, (expectedWRPosition - wrX.transform.position) + new Vector3(0, 10f, 0f), Color.blue);
        }
        Transform wrToUse;
        if (throwBPerformed)
        {
            wrToUse = wrB.transform;
        }
        else
        {
            wrToUse = wrX.transform;
        }

        int x = 0;
        if (playerAnimator.GetBool("isThrowing") && !thrown) {
            if ((Time.time - timeThrowStarted) <= (13f / 60f))
            {
                Vector3 targetCamPosition;
                Vector3 diff;
                Vector3 sourceVec;
                Vector3 targetVec;
                float horAngle;
                float twistAngle = 0f;
                if (throwingTargetPosition.magnitude > 0)
                {
                    Vector3 horDiff = new Vector3(throwingTargetPosition.x - football.transform.position.x, 0f, throwingTargetPosition.z - football.transform.position.z);
                    //targetCamPosition = new Vector3(transform.position.x, 0f, transform.position.z) + (new Vector3(0, 300f, -300f));
                    //targetCamPosition = new Vector3(football.transform.position.x, 0f, football.transform.position.z) + (new Vector3(0, 250f, -250f));
                    targetCamPosition = football.transform.position + (new Vector3(0, 10f, -75f));
                    //targetCamPosition += new Vector3(0f, football.transform.position.y - 1.7f, 0f);
                    diff = targetCamPosition - mainCamera.transform.position;
                    //mainCamera.transform.position += (diff * (Time.time - timeThrowStarted) / (13f / 60f));
                    Vector2 angleCoordinates = new Vector2(Mathf.Sqrt(3f) / (2f), -1f / 2f);
                    sourceVec = mainCamera.transform.forward;
                    //targetVec = new Vector3((football.transform.position - mainCamera.transform.position).normalized.x, 0f, (football.transform.position - mainCamera.transform.position).normalized.z);
                    targetVec = new Vector3(0f, angleCoordinates.y, angleCoordinates.x);
                    twistAngle = Vector3.SignedAngle(sourceVec, targetVec, mainCamera.transform.right);
                }
                else
                {
                    Vector3 horDiff = new Vector3(throwingTargetPosition.x - football.transform.position.x, 0f, throwingTargetPosition.z - football.transform.position.z);
                    //targetCamPosition = football.transform.position + (-horDiff.normalized * 5f);
                    //targetCamPosition = new Vector3(football.transform.position.x, 0f, football.transform.position.z) + (new Vector3(0, 100f, -100f));
                    targetCamPosition = football.transform.position + (new Vector3(0, 10f, -75f));
                    diff = targetCamPosition - mainCamera.transform.position;
                    //mainCamera.transform.position += (diff * (Time.time - timeThrowStarted) / (13f / 60f));
                    sourceVec = new Vector3(mainCamera.transform.forward.x, 0f, mainCamera.transform.forward.z);
                    targetVec = new Vector3((throwingTargetPosition - football.transform.position).normalized.x, 0f, (throwingTargetPosition - football.transform.position).normalized.z);
                    horAngle = Vector3.SignedAngle(sourceVec, targetVec, Vector3.up);
                }

                float currentFOV = mainCamera.GetComponent<Camera>().fieldOfView;
                //mainCamera.GetComponent<Camera>().fieldOfView += ((50f - currentFOV) * displacementValue);
                //mainCamera.GetComponent<Camera>().fieldOfView += ((50f - currentFOV)  * displacementValue);
                float displacementValue = (Time.time - timeThrowStarted) < (13f / 60f) ? (Time.time - timeThrowStarted) / timeThrowStarted : 1f;
                //mainCamera.GetComponent<Camera>().fieldOfView += ((75f - currentFOV) * displacementValue);
                Vector3 forceVectorFootball = wrTargetPosition.position - football.transform.position;
                totalTimeMovingCameraToSpotForThrow = (Time.time - timeThrowStarted);
            }
            else if (!cameraSetForLaunch) {
                Vector3 targetCamPosition;
                Vector3 diff;
                Vector3 sourceVec;
                Vector3 targetVec;
                float horAngle;
                float twistAngle = 0f;
                if (throwingTargetPosition.magnitude > 0)
                {
                    Vector3 horDiff = new Vector3(throwingTargetPosition.x - football.transform.position.x, 0f, throwingTargetPosition.z - football.transform.position.z);
                    targetCamPosition = football.transform.position + (new Vector3(0, 10f, -75f));
                    diff = targetCamPosition - mainCamera.transform.position;
                    //mainCamera.transform.position += (diff);
                    Vector2 angleCoordinates = new Vector2(Mathf.Sqrt(3f) / (2f), -1f / 2f);
                    sourceVec = mainCamera.transform.forward;
                    //targetVec = new Vector3((football.transform.position - mainCamera.transform.position).normalized.x, 0f, (football.transform.position - mainCamera.transform.position).normalized.z);
                    targetVec = new Vector3(0f, angleCoordinates.y, angleCoordinates.x);
                    twistAngle = Vector3.SignedAngle(sourceVec, targetVec, mainCamera.transform.right);
                }
                else
                {
                    Vector3 horDiff = new Vector3(throwingTargetPosition.x - football.transform.position.x, 0f, throwingTargetPosition.z - football.transform.position.z);
                    targetCamPosition = football.transform.position + (new Vector3(0, 10f, -75f));
                    diff = targetCamPosition - mainCamera.transform.position;
                    //mainCamera.transform.position += (diff);
                    sourceVec = new Vector3(mainCamera.transform.forward.x, 0f, mainCamera.transform.forward.z);
                    targetVec = new Vector3((throwingTargetPosition - football.transform.position).normalized.x, 0f, (throwingTargetPosition - football.transform.position).normalized.z);
                    horAngle = Vector3.SignedAngle(sourceVec, targetVec, Vector3.up);
                }


                float currentFOV = mainCamera.GetComponent<Camera>().fieldOfView;
                //mainCamera.GetComponent<Camera>().fieldOfView += (75f - currentFOV);
                totalTimeMovingCameraToSpotForThrow = (13f / 60f);
                cameraSetForLaunch = true;
            }
            else
            {
                int j = 0;
            }
        }
        if (setPositionForFirstTime) {
            canStartFollowing = true;
        }
        if (thrown && !wrX.GetComponent<WR>().CaughtFootball())
        {
            if ((Time.time - timeThrown) <= timeToCatch && cameraSetForLaunch)
            {
                Vector2 horVec = new Vector2(football.transform.parent.gameObject.GetComponent<Rigidbody>().velocity.x, football.transform.parent.gameObject.GetComponent<Rigidbody>().velocity.z);
                float velMag = Mathf.Sqrt((horVec.x * horVec.x) + (horVec.y * horVec.y));
                Debug.DrawRay(throwingTargetPosition, Vector3.up * 1000f, Color.green);
                //Debug.DrawRay(wrToUse.GetComponent<WR>().GetLeftHand().position, Vector3.up * 1000f, Color.red);
                Vector3 tPos = wrToUse.GetComponent<WR>().GetLeftHand().position + (-wrToUse.GetComponent<WR>().GetLeftHand().forward * (wrToUse.GetComponent<WR>().GetDistanceBetweenHands() / 2f));
                //Debug.DrawRay(tPos, Vector3.up * 1000f, Color.red);
                //throwingTargetPosition = throwingTargetPosition + (-wrToUse.GetComponent<WR>().GetLeftHand().forward * (wrToUse.GetComponent<WR>().GetDistanceBetweenHands() / 2f));
                //mainCamera.transform.position = football.transform.position + new Vector3(-1f, 2.5f, cameraOffset);
                float value = Mathf.Abs((Time.time - timeThrown) - timeToCatch) > 1 ? -1f : (Time.time - timeThrown) - timeToCatch;
                //mainCamera.transform.position += ((wrX.transform.position - mainCamera.transform.position) * ((Time.time - timeThrown) / timeToCatch)) + new Vector3(0, 2.5f, cameraOffset);
                //+ new Vector3(0, 3.5f, cameraOffset)
                Vector3 targetDir = wrToUse.transform.position - transform.position;
                //Vector3 targetCameraSpot = new Vector3(wrToUse.transform.position.x, 0f, wrToUse.transform.position.z) + new Vector3(-targetDir.normalized.x * 6f, 8f, -targetDir.normalized.z * 6f);
                //mainCamera.transform.position += ((targetCameraDirection.normalized) * (football.transform.position - footballPositionLastFrame).magnitude);

                //mainCamera.transform.position += (football.transform.position - footballPositionLastFrame) + (horDiff * (football.transform.position - footballPositionLastFrame).magnitude);
                //mainCamera.transform.position += (football.transform.position - footballPositionLastFrame);
                Vector3 forceDir = new Vector3((throwingTargetPosition - football.transform.position).normalized.x, 0f, (throwingTargetPosition - football.transform.position).normalized.z).normalized;
                //Vector3 targetCamPosition = football.transform.position + (-forceDir * 20f);

                //targetCamPosition = new Vector3(targetCamPosition.x, football.transform.position.y + 15f, targetCamPosition.z);
                float displacementValue = (Time.time - timeThrown) < timeToCatch ? (Time.time - timeThrown) / timeToCatch : 1f;
                float displacementValueOffset = (Time.time - timeThrown) < timeToCatch / 2 ? (Time.time - timeThrown) / (timeToCatch / 2) : 1f;
                //float displacementValue = (Time.time - timeThrown) / timeToCatch;
                //Vector3 targetCamPosition = new Vector3(football.transform.position.x, 0f, football.transform.position.z) + (new Vector3(0, mainCamera.transform.position.y, -225f));
                float deltaX = wrX.transform.position.x - football.transform.position.x;
                Vector3 targetCamPosition = football.transform.position + new Vector3(deltaX / 2f, 5f, -50f);
                List<Axis> axes = new List<Axis>() { Axis.right, Axis.up };
                targetCamPosition = football.transform.position + new Vector3(0f, 5f, -50f);
                
                //Vector3 targetCamPosition = new Vector3(football.transform.position.x, 0f, football.transform.position.z) + (new Vector3(0f, throwingTargetPosition.y, throwingTargetPosition.z - 175f));
                //Vector3 targetCamPosition = new Vector3(throwingTargetPosition.x, throwingTargetPosition.y + 128.1f, throwingTargetPosition.z - 175f);
                Vector3 diff = Vector3.zero;
                float deltaMagnitude = (new Vector3(football.transform.position.x, 0f, football.transform.position.z) - new Vector3(footballPositionLastFrame.x, 0f, footballPositionLastFrame.z)).magnitude;
                float deltaScaler = deltaMagnitude / (targetCamPosition - mainCamera.transform.position).magnitude;

                //if ((Time.time - timeThrown) > timeToCatch / 2f)
                if ((Time.time - timeThrown) <= timeToCatch)
                {
                    diff = (targetCamPosition - mainCamera.transform.position) * displacementValue;
                    mainCamera.transform.position += diff;
                }
                else {
                    diff = (targetCamPosition - mainCamera.transform.position) * displacementValue;
                    mainCamera.transform.position += diff;
                }
                targetCamPosition = GetPositionToKeepInView(football, axes, mainCamera.GetComponent<Camera>());
                float fieldOfView = 50f;
                float targetCameraAngle = 10f;
                float currentCameraAngle = mainCamera.transform.eulerAngles.x;
                float currentFOV = mainCamera.GetComponent<Camera>().fieldOfView;
                float fieldOfViewNeeded = Mathf.Min(75f, CalculateRequiredFOV(mainCamera.GetComponent<Camera>(), targetCamPosition, football.transform.position, wrX.transform.position));
                if (fieldOfViewNeeded > 50f)
                {
                    //mainCamera.GetComponent<Camera>().fieldOfView += ((50f - currentFOV) * displacementValue);
                    //mainCamera.GetComponent<Camera>().fieldOfView += ((fieldOfViewNeeded - currentFOV) * displacementValue);
                    mainCamera.GetComponent<Camera>().fieldOfView += (fieldOfViewNeeded - currentFOV);
                }
                else {
                    //mainCamera.GetComponent<Camera>().fieldOfView += ((50f - currentFOV) * displacementValue);
                    mainCamera.GetComponent<Camera>().fieldOfView += (50f - currentFOV);
                }
                footballPositionLastFrame = football.transform.position;

                currCameraIndex++;
            }
            else if ((Time.time - timeThrown) >= timeToCatch) {
                //Debug.DrawRay(wrX.GetComponent<WR>().leftHand.Find("LeftPalmCatchArea").GetChild(0).position, Vector3.up * 50f, Color.red);
                Rigidbody rb = wrX.GetComponent<Rigidbody>();
                //rb.AddForce(-rb.velocity * rb.mass, ForceMode.Impulse);
            }
            else {
                bool zForwardDirectionIsPositive = true;
                if ((parentFootballPosition != null) && (parentFootballPosition.GetComponent<TestCollision>() != null && parentFootballPosition.GetComponent<TestCollision>().stopFootball))
                {
                    bool xxx = true;
                    if (!startedCameraAlignmentAfterCatch)
                    {
                        Vector3 tDest = new Vector3(0, 0, 0);
                        if (zForwardDirectionIsPositive)
                        {
                            tDest = (new Vector3(wrToUse.transform.position.x, 0, wrToUse.transform.position.z) + new Vector3(0, 8f, 0f)) + -(new Vector3(wrToUse.transform.forward.x, 0, wrToUse.transform.forward.z).normalized * 15f);
                        }
                        else
                        {
                            tDest = (new Vector3(wrToUse.transform.position.x, 0, wrToUse.transform.position.z) + new Vector3(0, 8f, 0f)) + (new Vector3(wrToUse.transform.forward.x, 0, wrToUse.transform.forward.z).normalized * 15f);
                        }
                        startedCameraAlignmentAfterCatch = true;
                        timeStartedCameraAlignmentAfterCatch = Time.time;
                        cameraPositionWhenStartingAlignmentAfterCatch = mainCamera.transform.position;
                        Vector3 targetCameraSpot = wrToUse.transform.position + new Vector3(tDest.x, 8f, tDest.z);
                        cameraDestinationDirection = tDest - cameraPositionWhenStartingAlignmentAfterCatch;
                    }
                    
                    if ((Time.time - timeThrown) > timeToCatch && (Time.time - timeStartedCameraAlignmentAfterCatch) <= (1f / 2f))
                    {
                        float horAngle = 0f;
                        Vector3 targetDirection = new Vector3(0, 0, 0);
                        
                        if (zForwardDirectionIsPositive)
                        {
                            horAngle = Vector3.SignedAngle(mainCamera.transform.forward, wrToUse.transform.forward, Vector3.up);
                            targetDirection = wrToUse.transform.forward;
                        }
                        else {
                            horAngle = Vector3.SignedAngle(mainCamera.transform.forward, -wrToUse.transform.forward, Vector3.up);
                            targetDirection = -wrToUse.transform.forward;
                        }
                    }
                    else
                    {
                        //CalculateCameraDirection(Vector3 tDest, bool zForwardDirectionIsPositive, GameObject wrToUse)
                        float horAngle = 0f;
                        Vector3 targetDirection = new Vector3(0, 0, 0);
                        targetDirection = wrToUse.transform.forward;
                        if (zForwardDirectionIsPositive)
                        {
                            horAngle = Vector3.SignedAngle(mainCamera.transform.forward, wrToUse.transform.forward, Vector3.up);
                            targetDirection = wrToUse.transform.forward;
                        }
                        else
                        {
                            horAngle = Vector3.SignedAngle(mainCamera.transform.forward, -wrToUse.transform.forward, Vector3.up);
                            targetDirection = -wrToUse.transform.forward;
                        }
                    }
                }

                Vector3 forceDir = new Vector3((throwingTargetPosition - transform.position).normalized.x, 0f, (throwingTargetPosition - transform.position).normalized.z).normalized;
                Vector3 targetCamPosition = throwingTargetPosition + (-throwForce.normalized * 15f);
                targetCamPosition = new Vector3(targetCamPosition.x, 10f, targetCamPosition.z);
                currCameraIndex++;
            }
            
            float rotValue = Time.time - throwTime;
            float t = Time.deltaTime * 360;
            delT += Time.deltaTime;
            //football.transform.localEulerAngles += new Vector3(Time.deltaTime * -360, 0, 0);
            float rotationAmount = -900 * Time.deltaTime;
            float rotationValue = launchAngle - launchAngle * ((Time.time - throwTime) / (timeInAir / 2f));
            float difference = Time.time - differenceTime;
            //float rValue = (-1f) * (Time.fixedDeltaTime / (timeInAir / 2f)) * launchAngle;
            float rotateAmountTest = (launchAngle + ((Physics.gravity.y / 4400f) * (Time.deltaTime))) - launchAngle;
            float rotateAmount = (launchVelY * Time.time - 0.5f * Mathf.Abs(Physics.gravity.y) * Time.time * Time.time) - (launchVelY * differenceTime - 0.5f * Mathf.Abs(Physics.gravity.y) * differenceTime * differenceTime);
            float rValue = (-1f) * (difference / (timeToCatch / 2f)) * launchAngle;
            differenceTime = Time.time;
            // Rotate the child object around its local X-axis
            //print("Physics.Gravity: " + Physics.gravity.y);
            
            if ((parentFootballPosition != null) && (parentFootballPosition.GetComponent<TestCollision>() != null && !parentFootballPosition.GetComponent<TestCollision>().stopFootball /* && !parentFootballPosition.GetComponent<TestCollision>().addForce*/)) {
                football.transform.Rotate(Vector3.right, rotationAmount, Space.Self);
                football.transform.parent.Rotate(Vector3.forward, rValue, Space.Self);
            }
            Vector3 footballNormalizedAngles = GetNormalizedEulerAngles(football.transform.localEulerAngles);
            int d = 0;
        }
    }


    Vector3 CalculateCameraDirection(Vector3 tDest, bool zForwardDirectionIsPositive, GameObject wrToUse) {
        Vector3 cameraDestDirection = new Vector3(0, 0, 0);
        if (zForwardDirectionIsPositive)
        {
            tDest = (new Vector3(wrToUse.transform.position.x, 0, wrToUse.transform.position.z) + new Vector3(0, 8f, 0f)) + -(new Vector3(wrToUse.transform.forward.x, 0, wrToUse.transform.forward.z).normalized * 15f);
        }
        else
        {
            tDest = (new Vector3(wrToUse.transform.position.x, 0, wrToUse.transform.position.z) + new Vector3(0, 8f, 0f)) + (new Vector3(wrToUse.transform.forward.x, 0, wrToUse.transform.forward.z).normalized * 15f);
        }
        startedCameraAlignmentAfterCatch = true;
        timeStartedCameraAlignmentAfterCatch = Time.time;
        cameraPositionWhenStartingAlignmentAfterCatch = mainCamera.transform.position;
        Vector3 targetCameraSpot = wrToUse.transform.position + new Vector3(tDest.x, 8f, tDest.z);
        cameraDestinationDirection = tDest - cameraPositionWhenStartingAlignmentAfterCatch;
        return cameraDestDirection;
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
        else
        {
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

    private void OnThrowPerformed(InputAction.CallbackContext context) {
        wrPosition = wrX.transform.position;
        wrX.GetComponent<WR>().SetCanSetThrowTarget(true);
        playerAnimator.SetBool("isThrowing", true);
        timeThrowStarted = Time.time;
        timeOfPreviousThrowFrame = Time.time;
        positionOfCameraWhenStartingThrow = mainCamera.transform.position;
        //cameraDestinationDirection = wrX.transform.position + new Vector3(0, -7f, 9f);

    }

    private void OnThrowBPerformed(InputAction.CallbackContext context)
    {
        throwBPerformed = true;
        wrPosition = wrB.transform.position;
        wrB.GetComponent<WR>().SetCanSetThrowTarget(true);
        playerAnimator.SetBool("isThrowing", true);
        timeThrowStarted = Time.time;
        timeOfPreviousThrowFrame = Time.time;
        positionOfCameraWhenStartingThrow = mainCamera.transform.position;
        //cameraDestinationDirection = wrX.transform.position + new Vector3(0, -7f, 9f);

    }


    /*public void MoveFootball(GameObject f)
    {
        Vector3 forceInit = wrPosition - f.transform.position;

        Vector2 coordinates = Gamepad.all[0].leftStick.ReadValue();

        Vector3 coordinates2 = new Vector3(coordinates.x, 0, coordinates.y);
        Vector3 forceNorm = coordinates2 - forceInit.normalized;
        float dif = forceInit.magnitude / coordinates2.magnitude;
        Vector3 analogForce = coordinates2 * dif;
        float targetRatio = forceInit.z / forceInit.x;
        float analogRatio = coordinates.y / coordinates.x;
        float amountToLeadWR = analogRatio / targetRatio;
        //Vector2 forceAverage = new Vector3((forceInit.x + analogForce.x) / 2, (forceInit.z + analogForce.z) / 2);
        Vector2 forceAverage = new Vector3((forceInit.x * (2f / 3f)) + (analogForce.x * (1f / 3f)), (forceInit.z * (2f / 3f)) + analogForce.z * (1f / 3f));
        Vector3 forceVTest = new Vector3(forceInit.x, 10f, forceInit.z) * 35f;
        //Vector3 forceVTest2 = new Vector3(forceAverage.x, 10f, forceAverage.y) * 35f;
        //Vector3 forceVTest2 = new Vector3(forceAverage.x * (10f/yForce), yForce, forceAverage.y * (10f / yForce)) * 35f;
        float multiplier = 1f;
        Vector3 leadVelocity = wrX.transform.forward + new Vector3(forceNorm.x, 0f, forceNorm.z);
        if (forceInit.z > 10f && multiplier < 1.5f)
        {
            multiplier += forceInit.z - 10f;
        }
        //Vector3 forceVTest2 = new Vector3(forceAverage.x * (10f / yForce), yForce, forceAverage.y * multiplier) * 35f;
        //Vector3 forceVTest2 = new Vector3(forceInit.x + leadVelocity.x, yForce, forceInit.z + leadVelocity.z) * 35f;
        Vector3 forceVTest2 = new Vector3(forceInit.x, yForce, forceInit.z) * 35f;
        //Vector3 analogForce = coordinates2 * dif;
        //Original code needs to be kept
        f.AddComponent<Rigidbody>();
        Rigidbody r = f.GetComponent<Rigidbody>();
        f.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
        footballProjectileRotationTime = DrawTrajectory.instance.UpdateTrajectory(forceVTest2, f.GetComponent<Rigidbody>(), f.transform.position);
        //DrawTrajectory.instance.UpdateTrajectory(forceVTest2, null, f.transform.position);



        //DrawTrajectory.instance.UpdateTrajectory
    }*/

    private Vector3 GetNormalizedEulerAngles(Vector3 pivotRotation)
    {
        Vector3 standardizedAngles = new Vector3(0, 0, 0);
        float rotX = pivotRotation.x > 180 ? pivotRotation.x - 360f : pivotRotation.x;
        float rotY = pivotRotation.y > 180 ? pivotRotation.y - 360f : pivotRotation.y;
        float rotZ = pivotRotation.z > 180 ? pivotRotation.z - 360f : pivotRotation.z;
        standardizedAngles = new Vector3(rotX, rotY, rotZ);
        return standardizedAngles;
    }

    static Tuple<float, float> SolveQuadratic(float a, float b, float c)
    {
        // Calculate the discriminant
        float discriminant = b * b - 4 * a * c;

        // Check if the discriminant is negative (no real roots)
        if (discriminant < 0)
        {
            Console.WriteLine("No real roots");
            return null;
        }

        // Calculate the roots using the quadratic formula
        float root1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float root2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        // Return the roots as a Tuple
        return Tuple.Create(root1, root2);
    }

    public Vector3 PredictFuturePosition(float predictionTime, Rigidbody rb)
    {
        Vector3 simulatedPosition = rb.gameObject.transform.position;
        Vector3 simulatedVelocity = rb.velocity;

        float acceleration = wrX.GetComponent<WR>().GetRunAcceleration() / rb.mass;

        float dt = Time.fixedDeltaTime;

        for (float t = 0; t < predictionTime; t += dt)
        {
            Vector3 desiredDirection = rb.gameObject.transform.forward;

            if (simulatedVelocity.magnitude < wrX.GetComponent<WR>().GetTargetSpeed())
            {
                simulatedVelocity += desiredDirection * acceleration * dt;

                simulatedVelocity = Vector3.ClampMagnitude(
                    simulatedVelocity,
                    wrX.GetComponent<WR>().GetTargetSpeed());
            }

            simulatedPosition += simulatedVelocity * dt;
        }

        return simulatedPosition;
    }

    public void ReleaseFootball()
    {
        thrown = true;
        throwTime = Time.time;
        differenceTime = Time.time;

        if (!setPositionForFirstTime && thrown)
        {
            timeThrown = Time.time;
            setPositionForFirstTime = true;
            currCameraIndex = 0;
            Vector3 tDest = new Vector3(0, 0, 0);
            Transform wrToUse;
            if (throwBPerformed)
            {
                wrToUse = wrB.transform;
            }
            else {
                wrToUse = wrX.transform;
            }


            if (transform.forward.z >= 0)
            {
                tDest = wrToUse.transform.position + -(wrToUse.transform.forward * 6f);
                tDest = tDest + new Vector3(0, 15f, 0);
                //tDest = tDest + new Vector3(0, 42f, 0);
            }
            else
            {
                tDest = wrToUse.transform.position + (wrToUse.transform.forward * 6f);
                tDest = tDest + new Vector3(0, 15f, 0);
            }
            Vector3 forceVector = wrToUse.transform.position - transform.position;
            Vector3 cameraForce = tDest - mainCamera.transform.position;

            Vector3 forceVectorFootball = new Vector3(0, 0, 0);
            throwingTargetPosition = wrToUse.GetComponent<WR>().GetLeftHandChildPosition("LeftPalmCatchArea");
            //throwingTargetPosition = wrToUse.GetComponent<WR>().GetLeftHand().position;
            float dist = wrToUse.GetComponent<WR>().GetDistanceBetweenHands();
            throwingTargetPosition = throwingTargetPosition + (-wrToUse.GetComponent<WR>().GetLeftHand().forward * (wrToUse.GetComponent<WR>().GetDistanceBetweenHands() / 2f));
            if (throwingTargetPosition.magnitude > 0)
            {
                forceVectorFootball = throwingTargetPosition - football.transform.position;
                Debug.DrawRay(wrX.transform.position, throwingTargetPosition - wrX.transform.position, Color.green);
            }
            else
            {
                forceVectorFootball = wrToUse.position - football.transform.position;
            }
            //Vector3 forceVectorFootball = wrTargetPosition.position - football.transform.position;
            //forceVectorFootball = throwingTargetPosition - football.transform.position;
            Vector3 forceVectorCamera = tDest - mainCamera.transform.position;
            Vector3 forceVectorFinal = new Vector3(forceVectorFootball.x, 0f, forceVectorFootball.z);
            throwForce = forceVectorFinal;
            Vector3 forceVectorFinalCamera = new Vector3(forceVectorCamera.x, forceVector.y, forceVectorCamera.z);

            Transform handForCatch = wrToUse.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(5).GetChild(2).GetChild(0).GetChild(0);
            //Transform handForCatch = wrToUse.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(5).GetChild(2).GetChild(0);
            float fvX = forceVectorFinal.x > 0 ? forceVectorFinal.x : 1;
            float fvZ = forceVectorFinal.z > 0 ? forceVectorFinal.z : 1;
            float fvXCamera = forceVectorFinalCamera.x > 0 ? forceVectorFinalCamera.x : 1;
            float fvZCamera = forceVectorFinalCamera.z > 0 ? forceVectorFinalCamera.z : 1;
            //timeToCatch = Mathf.Sqrt((forceVectorFinal.x * forceVectorFinal.x) + (forceVectorFinal.z * forceVectorFinal.z)) / (throwSpeed);
            // LEAD THROW: aim where the WR's left palm will be if they keep running the way they face at top
            // speed, at the palm's current height. The flight time comes from the distance to that point at
            // leadThrowSpeedMultiplier x the WR's top speed (clamped), and the launch velocity below makes the
            // ball arrive there in exactly that time. The WR's movement / aim assist covers the difference
            // when the WR doesn't run straight.
            WR receiver = wrToUse.GetComponent<WR>();
            Vector3 receiverPalm = receiver.GetLeftHandChildPosition("LeftPalmCatchArea");
            Vector3 receiverRunDirection = Vector3.ProjectOnPlane(wrToUse.forward, Vector3.up);
            receiverRunDirection = receiverRunDirection.sqrMagnitude > 1e-6f ? receiverRunDirection.normalized : Vector3.forward;
            float receiverTopSpeed = receiver.GetTargetSpeed();
            float leadBallSpeed = Mathf.Max(0.01f, receiverTopSpeed * leadThrowSpeedMultiplier);
            Vector3 handOffset = -handForCatch.forward * (receiver.GetDistanceBetweenHands() / 2f);
            timeToCatch = leadThrowMinFlightTime;
            for (int leadIteration = 0; leadIteration < 5; leadIteration++)
            {
                Vector3 leadPoint = receiverPalm + receiverRunDirection * receiverTopSpeed * timeToCatch + handOffset;
                float horizontalDistance = Vector3.ProjectOnPlane(leadPoint - football.transform.position, Vector3.up).magnitude;
                timeToCatch = Mathf.Clamp(horizontalDistance / leadBallSpeed, leadThrowMinFlightTime, leadThrowMaxFlightTime);
            }
            throwingTargetPosition = receiverPalm + receiverRunDirection * receiverTopSpeed * timeToCatch + handOffset;
            throwingTargetPosition = new Vector3(throwingTargetPosition.x, receiverPalm.y, throwingTargetPosition.z);
            /* Previous target: assumed the WR keeps its current velocity for exactly 4 seconds.
            timeToCatch = 4f;
            Vector3 testVec = wrX.GetComponent<WR>().GetExpectedTargetUsingLeftHandPosition(timeToCatch) + (-handForCatch.forward * (wrX.GetComponent<WR>().GetDistanceBetweenHands() / 2));
            //Vector3 adjustedVelocity = new Vector3(wrX.GetComponent<Rigidbody>().velocity.x, 0f, wrX.GetComponent<Rigidbody>().velocity.z);
            //Vector3 adjustedVelocity = wrX.GetComponent<Rigidbody>().transform.forward * wrX.GetComponent<WR>().GetTargetSpeed();
            throwingTargetPosition = wrToUse.GetComponent<WR>().GetLeftHandChildPosition("LeftPalmCatchArea") + (wrX.GetComponent<Rigidbody>().velocity * timeToCatch);
            //throwingTargetPosition = wrToUse.GetComponent<WR>().GetLeftHandChildPosition("LeftPalmCatchArea") + (adjustedVelocity * timeToCatch);
            //throwingTargetPosition = throwingTargetPosition + (new Vector3(0, 0, 1) * wrX.GetComponent<WR>().GetTargetSpeed() * timeToCatch);
            throwingTargetPosition = throwingTargetPosition + (-handForCatch.forward * (wrX.GetComponent<WR>().GetDistanceBetweenHands() / 2));
            throwingTargetPosition = new Vector3(throwingTargetPosition.x, wrToUse.GetComponent<WR>().GetLeftHandChildPosition("LeftPalmCatchArea").y, throwingTargetPosition.z);
            */
            wrToUse.GetComponent<WR>().SetTargetPosition(throwingTargetPosition);
            if (throwingTargetPosition.magnitude > 0) {
                forceVectorFootball = throwingTargetPosition - football.transform.position;
                //forceVectorFinal = new Vector3(forceVectorFootball.x, handForCatch.position.y, forceVectorFootball.z);
                forceVectorFinal = new Vector3(forceVectorFootball.x, wrToUse.GetComponent<WR>().GetLeftHandChildPosition("LeftPalmCatchArea").y, forceVectorFootball.z);
                
                //forceVectorFinal = forceVectorFootball;
            }


            float speedDifference = (tDest - mainCamera.transform.position).magnitude / (wrToUse.transform.position - football.transform.position).magnitude;
            float velX = forceVectorFinal.x / timeToCatch;
            float velXCamera = forceVectorFinalCamera.x / timeToCatch;
            float velZ = forceVectorFinal.z / timeToCatch;
            float velZCamera = forceVectorFinalCamera.z / timeToCatch;
            float velXZ = Mathf.Sqrt((velX * velX) + (velZ * velZ));
            float velY = 0f;
            float posXForView = 0f;
            float posYForView = 0f;
            float posZForView = 0f;

            if (throwingTargetPosition.magnitude > 0)
            {
                velY = ((0.5f * Mathf.Abs(Physics.gravity.y) * (timeToCatch * timeToCatch)) + (throwingTargetPosition.y - football.transform.position.y)) / (timeToCatch);
                

            }
            else
            {
                
                velY = ((0.5f * Mathf.Abs(Physics.gravity.y) * (timeToCatch * timeToCatch)) + (handForCatch.position.y - football.transform.position.y)) / (timeToCatch);
            }

            //expectedWRPosition = PredictFuturePosition(timeToCatch, wrX.GetComponent<Rigidbody>());
            float velYCamera = ((0.5f * Mathf.Abs(Physics.gravity.y) * (timeToCatch * timeToCatch)) + ((tDest.y - mainCamera.transform.position.y))) / (timeToCatch);
            launchVelY = velY;
            float threeDimVelMagnitude = Mathf.Sqrt((velXZ * velXZ) + (velY * velY));
            launchAngle = Mathf.Asin(velY / threeDimVelMagnitude) * Mathf.Rad2Deg;



            //NEEDED FOR ENLARGED GRAVITY SOLUTION
            //Tuple<float, float> roots = SolveQuadratic(4.9f * (22200 / 5f), -velY, -football.transform.position.y);
            Tuple<float, float> roots = SolveQuadratic(4.9f, -velY, -football.transform.position.y);





            timeInAir = roots.Item1;
            //Tuple<float, float> roots = 
            //timeOfFlight = velY / ((0.5f) * Mathf.Abs(Physics.gravity.y));
            float direction = -Mathf.Atan(velZ / velX) * Mathf.Rad2Deg;
            setPositionForFirstTime = true;
            parentFootballPosition = Instantiate<GameObject>(parentFootballPositionPrefab);
            if (throwBPerformed)
            {
                parentFootballPosition.GetComponent<TestCollision>().wr1Hand = parentFootballPosition.GetComponent<TestCollision>().leftArm = wrB.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(5).GetChild(2).GetChild(0).GetChild(0);
                parentFootballPosition.GetComponent<TestCollision>().wrBodyPosition = wrB.transform;
                parentFootballPosition.GetComponent<TestCollision>().leftArm = wrB.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(5).GetChild(2);
                parentFootballPosition.GetComponent<TestCollision>().rightArm = wrB.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetChild(1);
            }
            else {
                parentFootballPosition.GetComponent<TestCollision>().wr1Hand = wr1Hand;
                parentFootballPosition.GetComponent<TestCollision>().wrBodyPosition = wrX.transform;
                parentFootballPosition.GetComponent<TestCollision>().leftArm = wrX.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(5).GetChild(2);
                parentFootballPosition.GetComponent<TestCollision>().rightArm = wrX.transform.Find("Root").GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetChild(1);
            }




            //parentFootballPosition.transform.position = football.transform.position;
            parentFootballPosition.transform.position = football.transform.position;
            football.transform.parent = null;
            football.transform.localEulerAngles = new Vector3(0, direction, launchAngle);
            parentFootballPosition.transform.localEulerAngles = football.transform.localEulerAngles;
            football.transform.parent = parentFootballPosition.transform;
            wrToUse.GetComponent<WR>().SetThrowPoint(parentFootballPosition.transform.position);
            wrToUse.GetComponent<WR>().SetThrowDirection(throwingTargetPosition - parentFootballPosition.transform.position);
            //football.AddComponent<Rigidbody>();
            //parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            //parentFootballPosition.GetComponent<Rigidbody>().angularDrag = 0.75f;
            Vector3 targetCamPosition;
            Vector3 diff;
            Vector3 sourceVec;
            Vector3 targetVec;
            Vector3 initialPosition;
            float horAngle;
            if (throwingTargetPosition.magnitude > 0)
            {
                targetCamPosition = football.transform.position + (-forceVectorFinal.normalized * 20f);
                diff = targetCamPosition - mainCamera.transform.position;
                //diff = football.transform.position - mainCamera.transform.position;
                //initialPosition = mainCamera.transform.position + diff;
                //targetCamPosition = initialPosition + (-(throwingTargetPosition - initialPosition).normalized * 15f);
                //diff = targetCamPosition - mainCamera.transform.position;
                //mainCamera.transform.position += new Vector3(diff.x, 15f - mainCamera.transform.position.y, diff.z);
                sourceVec = new Vector3(mainCamera.transform.forward.x, 0f, mainCamera.transform.forward.z);
                targetVec = new Vector3(forceVectorFinal.normalized.x, 0f, forceVectorFinal.normalized.z);
                //targetVec = new Vector3((throwingTargetPosition - football.transform.position).normalized.x, 0f, (throwingTargetPosition - football.transform.position).normalized.z);
                horAngle = Vector3.SignedAngle(sourceVec, targetVec, Vector3.up);
            }
            else
            {
                targetCamPosition = transform.position + (-(wrToUse.transform.position - transform.position).normalized * 20f);
                diff = targetCamPosition - mainCamera.transform.position;
                //mainCamera.transform.position += new Vector3(diff.x, 15f - mainCamera.transform.position.y, diff.z);
                sourceVec = new Vector3(mainCamera.transform.forward.x, 0f, mainCamera.transform.forward.z);
                targetVec = new Vector3((throwingTargetPosition - football.transform.position).normalized.x, 0f, (throwingTargetPosition - football.transform.position).normalized.z);
                horAngle = Vector3.SignedAngle(sourceVec, targetVec, Vector3.up);
            }


            //horAngle = Vector3.SignedAngle(mainCamera.transform.forward, (wrToUse.transform.position - football.transform.position), Vector3.up);
            //mainCamera.transform.Rotate(Vector3.up, horAngle, Space.World);












            parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            //parentFootballPosition.GetComponent<Rigidbody>().angularDrag = 0.75f;





            //Vector3 impulseForce = new Vector3(vx * parentFootballPositionGetComponent<Rigidbody>().mass, vy * parentFootballPositionGetComponent<Rigidbody>().mass, vz * parentFootballPositionGetComponent<Rigidbody>().mass);
            Vector3 impulseForceOfficial = new Vector3(velX * parentFootballPosition.GetComponent<Rigidbody>().mass, velY * parentFootballPosition.GetComponent<Rigidbody>().mass, velZ * parentFootballPosition.GetComponent<Rigidbody>().mass);

            //GameObject parentFootballPosition = Instantiate<GameObject>()


            parentFootballPosition.GetComponent<Rigidbody>().AddForce(impulseForceOfficial, ForceMode.Impulse);

            float stepTime = timeInAir / lineCount;
            float timeVal = Mathf.Abs(timeInAir);
            linePoints.Clear();
            //projectileAngleTimePair = new KeyValuePair<float, float>(yAngle, timeVal);
            lineRenderer.positionCount = linePoints.Count;
            lineRenderer.SetPositions(linePoints.ToArray());

            //projectileAngleTimePair = new KeyValuePair<float, float>(yAngle, timeVal);
            //return projectileAngleTimePair;
            for (int i = 0; i < lineCount; i++)
            {
                if (i == lineCount - 1)
                {
                    int y = 0;
                }
                float stepTimePassed = stepTime * i;
                Vector3 movementVector = new Vector3(
                    velX * stepTimePassed,
                    velY * stepTimePassed - 0.5f * Mathf.Abs(Physics.gravity.y) * stepTimePassed * stepTimePassed,
                    velZ * stepTimePassed);
                if (i == ((lineCount / 2) - 1))
                {
                    float val = stepTimePassed;
                }
                else if (i == (lineCount / 2))
                {
                    float g = stepTimePassed;
                }
                linePoints.Add(movementVector + parentFootballPosition.transform.position);
            }
            lineRenderer.positionCount = linePoints.Count;
            lineRenderer.SetPositions(linePoints.ToArray());
            wrToUse.GetComponent<WR>().SetLinePoints(linePoints);
            //lineRenderer.SetWidth()
            Vector3 t = linePoints[0];
            Vector3 posAtTop = lineCount > 0 ? linePoints[(Convert.ToInt32(lineCount) / 2) - 1] : Vector3.zero;
            Camera c = mainCamera.GetComponent<Camera>();

            Vector3 expectedCamPosition = posAtTop + (new Vector3(0, 5f, -50f));
            wrXPositionWhenBallThrown = wrXFoot.transform.position;
            expectedCamPosition = GetPositionBetween(c, football, wrXFoot.gameObject, expectedCamPosition, posAtTop, wrXFoot.transform.position);
            expectedCamPosition = new Vector3(expectedCamPosition.x, posAtTop.y, expectedCamPosition.z);
            requiredFOV = CalculateRequiredFOV(c, expectedCamPosition, posAtTop, wrXFoot.transform.position);
            //lineRenderer.



            
        }

        
    }

}
