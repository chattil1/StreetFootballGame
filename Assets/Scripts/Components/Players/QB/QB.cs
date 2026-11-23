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

    [Header("In-Air Camera (Madden-style)")]
    [Tooltip("Use the Madden-style camera while the ball is in the air. Off = the old fixed (0, 5, -50) offset.")]
    public bool useBroadcastInAirCamera = true;
    // Unity keeps a component's saved values over a script's changed defaults, so when these defaults change,
    // InAirCamDefaultsVersion goes up and OnValidate applies the new defaults once (your own tweaks stay after that).
    [SerializeField, HideInInspector] private int inAirCamDefaultsVersion = 0;
    const int InAirCamDefaultsVersion = 6;
    [Tooltip("Degrees the camera looks down at the field. Madden's in-air view is nearly level (~10), so the far field and stands show and a defender can see the ball coming. It always faces downfield (the way the QB faced at release).")]
    public float inAirCamTilt = 10f;
    [Tooltip("Worked out at each release (you can watch it change here in Play mode): the least distance, from Distance At Catch up to Max Distance At Throw, that keeps the top of the ball at the top of the screen at the top of the throw's arc. Held until then, then eases to Distance At Catch.")]
    public float inAirCamDistanceAtThrow = 7.5f;
    [Tooltip("The MOST the camera may back off right after the throw (eases down to Distance At Catch over the flight). It only goes past the normal distance as much as needed to keep the ball in view, after first lifting (up to Max Raise At Throw).")]
    public float inAirCamMaxDistanceAtThrow = 15f;
    [Tooltip("Distance from the camera to the point it aims at as the ball reaches the WR, in receiver heights (smaller = closer / bigger receiver).")]
    public float inAirCamDistanceAtCatch = 4f;
    [Tooltip("Height of the point the camera aims at, in receiver heights (0.55 = about chest height), so getting closer doesn't put the camera below the players' heads.")]
    public float inAirCamAimHeight = 0.55f;
    [Tooltip("Where the camera centres on the field between the ball's spot (0) and the WR (1), right after the throw.")]
    [Range(0f, 1f)] public float inAirCamCenterAtThrow = 0.5f;
    [Tooltip("Where the camera centres between the ball's spot (0) and the WR (1), as the ball reaches the WR.")]
    [Range(0f, 1f)] public float inAirCamCenterAtCatch = 1f;
    [Tooltip("Camera field of view (degrees) while the ball is in the air and after the catch. Smaller = more zoomed in.")]
    public float inAirCamFieldOfView = 42f;
    [Tooltip("The WIDEST the in-air field of view may be. At each release the field of view for the whole flight is worked out once, from the top of the " +
        "throw's arc: In Air Cam Field Of View, widened only as much as needed to keep the ball in view there (after lifting up to Max Raise At Throw), up to this. It doesn't change during the flight.")]
    public float inAirCamMaxFieldOfView = 55f;
    private float throwFieldOfView = 0f;
    private float throwCameraApexProgress = 0f;
    [Tooltip("Worked out at each release (you can watch it change here in Play mode): the least lift, from Raise At Catch up to Max Raise At Throw, that puts the top of the ball at the top of the screen at the top of the throw's arc. Held until then, then eases to Raise At Catch.")]
    public float inAirCamRaiseAtThrow = 1.25f;
    [Tooltip("The MOST the camera may lift right after the throw (eases down to Raise At Catch over the flight). It only goes above the normal lift as much as needed to keep the top of the ball in the framing area.")]
    public float inAirCamMaxRaiseAtThrow = 2f;
    [Tooltip("How much higher the camera sits as the ball reaches the WR (and after the catch), in receiver heights.")]
    [UnityEngine.Serialization.FormerlySerializedAs("inAirCamRaise")]
    public float inAirCamRaiseAtCatch = 0.75f;
    [Tooltip("Fraction of the screen the ball and WR must stay inside; the camera backs off until they do (1 = screen edge).")]
    [Range(0.3f, 1f)] public float inAirCamFramingArea = 0.9f;
    [Tooltip("Seconds the camera takes to catch up to where it should be.")]
    public float inAirCameraPositionSmoothTime = 0.3f;
    [Tooltip("How quickly the camera turns to face downfield (higher = snappier).")]
    public float inAirCameraTurnSpeed = 5f;
    [Tooltip("Not used any more: the camera's throw values are worked out at the release from the top of the arc.")]
    [Range(0f, 1f)] public float inAirCamBallFrameUntil = 0.4f;

    [Tooltip("After the catch, the camera keeps the exact offset it has from the WR at its catch spot (the In Air Cam Distance At Catch view) and follows him with it.")]
    public bool followReceiverAfterCatch = true;
    [Tooltip("Seconds the camera takes to settle into its catch-spot offset from the WR after the catch (from then on it moves exactly with him).")]
    public float afterCatchSettleTime = 0.3f;
    /// <summary>True while the QB's camera is following the WR after the catch (WR.cs then leaves the camera alone).</summary>
    public static bool CameraFollowingReceiver = false;

    [Header("Pass Speed (real-world, scaled to the players' size)")]
    [Tooltip("Air time of a pass = Pass Air Time Base + Pass Air Time Per Yard x the throw's distance in yards (QB to where the receiver will be), " +
        "divided by Pass Speed Multiplier. Defaults: 10 yd ~0.7 s, 20 yd ~1.15 s, 40 yd ~2.05 s, 60 yd ~2.95 s, roughly Madden's.")]
    public float passAirTimeBase = 0.25f;
    [Tooltip("Seconds of air time added per yard of throw distance.")]
    public float passAirTimePerYard = 0.045f;
    [Tooltip("Speeds up (>1) or slows down (<1) every pass (divides the air time). Works live in Play mode.")]
    [Range(0.25f, 2f)] public float passSpeedMultiplier = 1f;
    [Tooltip("Shortest flight time (s), so very short throws aren't instant.")]
    public float minPassFlightTime = 0.3f;
    [Tooltip("Real height (m) the players' models stand for (6'2\" = 1.88). Sets world units per meter for the ball's speed and gravity.")]
    public float realPlayerHeightMeters = 1.88f;
    [Tooltip("Players are ~35x scale, so normal gravity is far too weak for the ball too. On = the thrown ball falls with real gravity for that size.")]
    public bool scaleBallGravityToPlayerSize = true;
    [Tooltip("Off = the old fixed 4-second flight.")]
    public bool useRealWorldPassSpeed = true;
    /// <summary>Gravity multiplier on the ball in flight (world units per meter); WR's ball predictions use it too.</summary>
    public static float BallGravityMultiplier = 1f;
    private Vector3 afterCatchOffsetVelocity = Vector3.zero;
    private Vector3 inAirCameraVelocity = Vector3.zero;
    private Vector3 inAirCameraThrowDirection = Vector3.zero;
    private Vector3 inAirCameraDownfield = Vector3.forward;
    private float inAirCameraUnit = 0f; // the receiver's height in world units, measured at release
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
    void OnValidate()
    {
        // Apply new in-air camera defaults once when they change (see InAirCamDefaultsVersion).
        if (inAirCamDefaultsVersion < 2)
        {
            inAirCamTilt = 10f;
            inAirCamDistanceAtThrow = 7.5f;
            inAirCamDistanceAtCatch = 4f;
            inAirCamAimHeight = 0.55f;
            inAirCamCenterAtThrow = 0.5f;
            inAirCamCenterAtCatch = 1f;
            inAirCamFramingArea = 0.9f;
        }
        if (inAirCamDefaultsVersion < 3)
        {
            // Version 3 only zooms in a little (your other camera settings, e.g. Distance At Catch, are kept).
            inAirCamFieldOfView = 42f;
            inAirCamRaiseAtCatch = 0.75f;
        }
        if (inAirCamDefaultsVersion < 4)
        {
            // Version 4: closer as the ball arrives (one steady move in), more zoom. Your raise / distance at throw stay.
            inAirCamFieldOfView = 34f;
            inAirCamDistanceAtCatch = 2.2f;
            inAirCamBallFrameUntil = 1f;
        }
        if (inAirCamDefaultsVersion < 5)
        {
            // Version 5 splits Raise into At Throw / At Catch: At Catch keeps your old Raise value; At Throw starts a
            // little higher (+0.5 receiver heights).
            inAirCamRaiseAtThrow = inAirCamRaiseAtCatch + 0.5f;
        }
        if (inAirCamDefaultsVersion < 6)
        {
            // Version 6 splits the At Throw values into normal + max: the normal ones keep your values, the maxes
            // start 50% higher.
            inAirCamMaxRaiseAtThrow = inAirCamRaiseAtThrow * 1.5f;
            inAirCamMaxDistanceAtThrow = inAirCamDistanceAtThrow * 1.5f;
        }
        inAirCamDefaultsVersion = InAirCamDefaultsVersion;
    }

    /// <summary>
    /// Unhook the input callbacks (the input actions asset outlives the scene, so after Restart Play the destroyed
    /// QB would otherwise still get them).
    /// </summary>
    void OnDestroy()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }
        playerInput.actions["ThrowXbox360"].performed -= OnThrowPerformed;
        playerInput.actions["ThrowBXbox360"].performed -= OnThrowBPerformed;
        playerInput.actions["PressedPlay1"].performed -= OnPlay1Selected;
    }

    void Awake()
    {
        // The throw animation's ReleaseFootball event runs on EVERY QB component on the animated object, so a second
        // (unfilled) QB component there throws errors like "wrXFoot of QB has not been assigned" at release.
        QB[] qbsHere = GetComponents<QB>();
        if (qbsHere.Length > 1 && qbsHere[0] == this)
        {
            Debug.LogError("QB: '" + name + "' has " + qbsHere.Length + " QB components. Remove the extra one(s) (the one with empty fields); " +
                "the throw animation's ReleaseFootball event calls every one of them.", this);
        }
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
    /// <summary>Set by the CB when he swats ("SWATTED by CB") or intercepts ("INTERCEPTED by CB") the pass.</summary>
    private string passDefendedNote = null;
    /// <summary>The defender who intercepted the pass (the camera follows him), or null.</summary>
    public Transform interceptedBy = null;

    /// <summary>Called by a defender who swatted or intercepted the pass (for the pass check log and the camera).</summary>
    public void NotePassDefended(string what, Transform interceptor)
    {
        passDefendedNote = what;
        if (interceptor != null)
        {
            interceptedBy = interceptor;
        }
    }

    /// <summary>The spot the pass is aimed at (the receiver's catch palm when it arrives).</summary>
    public Vector3 GetThrowTarget()
    {
        return throwingTargetPosition;
    }

    /// <summary>Which receiver the ball was thrown to (or will be, before the throw).</summary>
    public GameObject GetTargetReceiver()
    {
        return throwBPerformed ? wrB : wrX;
    }

    /// <summary>The thrown ball's Rigidbody while it's flying free (not caught / held), else null.</summary>
    public Rigidbody GetBallInFlight()
    {
        if (!thrown || parentFootballPosition == null)
        {
            return null;
        }
        Rigidbody ball = parentFootballPosition.GetComponent<Rigidbody>();
        return ball != null && !ball.isKinematic ? ball : null;
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
        UpdateInAirCamera();
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
            //parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // sweep against moving (Rigidbody) colliders too, e.g. the WR's palms
            //parentFootballPosition.GetComponent<Rigidbody>().angularDrag = 0.75f;




            parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // sweep against moving (Rigidbody) colliders too, e.g. the WR's palms
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

    /// <summary>
    /// Madden-style camera while the ball is in the air. Like Madden's, it doesn't turn with the throw: it always faces
    /// downfield (+Z or -Z, the way the QB faced at release), nearly level (inAirCamTilt, ~10 degrees, so the far field
    /// and stands show and a defender can see the ball coming), from a low, steady height. It centres on a point on the field that starts between the
    /// ball's spot on the ground and the receiver and moves onto the receiver as the ball arrives, so you read depth
    /// from the ball against the yard lines and see the WR and any defender near him for the catch.
    /// Heights are in receiver heights (measured from his character at release), so it frames the play the same at any
    /// scale; it aims at about chest height and closes in from inAirCamDistanceAtThrow to inAirCamDistanceAtCatch, ending up centred on the receiver.
    /// If the ball (up in its arc) or the WR would leave the middle inAirCamFramingArea of the screen, it backs off
    /// along the view until both fit. Position and rotation are smoothed. Hands over to the existing after-catch
    /// camera once the ball is caught / stopped.
    /// </summary>
    void UpdateInAirCamera()
    {
        CameraFollowingReceiver = false;
        if (!useBroadcastInAirCamera || !thrown || mainCamera == null || football == null)
        {
            return;
        }
        if (FollowReceiverAfterCatch())
        {
            return;
        }
        Transform ballParent = football.transform.parent;
        TestCollision ballFlight = ballParent != null ? ballParent.GetComponent<TestCollision>() : null;
        Rigidbody ballBody = ballParent != null ? ballParent.GetComponent<Rigidbody>() : null;
        if (ballFlight == null || ballFlight.stopFootball || ballBody == null || ballBody.isKinematic)
        {
            return; // not in flight (still in hand, caught, or stopped)
        }
        GameObject receiverObject = throwBPerformed ? wrB : wrX;
        if (receiverObject == null)
        {
            return;
        }
        Camera inAirCamera = mainCamera.GetComponent<Camera>();
        Transform cameraTransform = mainCamera.transform;
        float progress = timeToCatch > 0f ? Mathf.Clamp01((Time.time - timeThrown) / timeToCatch) : 1f;
        float unit = inAirCameraUnit > 0f ? inAirCameraUnit : MeasurePlayerHeight(receiverObject);
        float fieldOfView = throwFieldOfView > 0f ? throwFieldOfView : inAirCamFieldOfView;
        ComputeInAirCameraPose(receiverObject, football.transform.position, progress, unit, inAirCamera,
            fieldOfView, false, Vector3.zero, out Vector3 desiredPosition, out Quaternion viewRotation, out Vector3 receiverSpot, out _, out _, out _);

        // Follow relative to the WR: smooth the camera's offset from him, not its world position, so a fast WR
        // (and a ~1 s pass) never leaves the camera trailing behind.
        Vector3 currentOffset = cameraTransform.position - receiverSpot;
        Vector3 offset = Vector3.SmoothDamp(currentOffset, desiredPosition - receiverSpot, ref inAirCameraVelocity, Mathf.Max(0.01f, inAirCameraPositionSmoothTime));
        cameraTransform.position = receiverSpot + offset;
        float turn = 1f - Mathf.Exp(-inAirCameraTurnSpeed * Time.deltaTime);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, viewRotation, turn);
        if (inAirCamera != null)
        {
            inAirCamera.fieldOfView = fieldOfView; // fixed for the whole flight (worked out at the release)
        }
    }

    /// <summary>
    /// Where the in-air camera should be (and how it should face) at this point of the flight (progress 0 = release,
    /// 1 = the ball reaching the WR), for this ball position.
    ///
    /// solve = false (during the flight): no reacting to the ball. The lift and distance are the throw's values
    /// (In Air Cam Raise / Distance At Throw, worked out at the release), held until the top of the arc, then eased
    /// to Raise / Distance At Catch by the catch. It only backs off further if the WR himself wouldn't fit.
    ///
    /// solve = true (at the release, for the top of the arc): works out those throw values and the field of view.
    /// Starting from the catch values, it lifts only as much as needed to put the top of the ball at the top of the
    /// framing area (up to Max Raise At Throw), then widens the field of view only as much as still needed (up to
    /// Max Field Of View), then backs off only as much as still needed (up to Max Distance At Throw).
    /// </summary>
    void ComputeInAirCameraPose(GameObject receiverObject, Vector3 ballPosition, float progress, float unit, Camera inAirCamera,
        float flightFieldOfView, bool solve, Vector3 receiverShift,
        out Vector3 desiredPosition, out Quaternion viewRotation, out Vector3 receiverSpot,
        out float solvedFieldOfView, out float solvedRaise, out float solvedDistance)
    {
        WR receiver = receiverObject.GetComponent<WR>();

        // receiverShift: where he'll have moved by then (used when working out the pose for a later moment).
        Vector3 receiverPosition = receiverObject.transform.position + receiverShift;
        float groundHeight = receiverPosition.y;
        Vector3 receiverPoint = (receiver != null ? receiver.GetLeftShoulderPosition() : receiverObject.transform.position) + receiverShift;
        Vector3 ballSpot = new Vector3(ballPosition.x, groundHeight, ballPosition.z);
        receiverSpot = new Vector3(receiverPosition.x, groundHeight, receiverPosition.z);

        float eased = Mathf.SmoothStep(0f, 1f, progress);
        Vector3 focus = Vector3.Lerp(ballSpot, receiverSpot, Mathf.Lerp(inAirCamCenterAtThrow, inAirCamCenterAtCatch, eased));
        focus += Vector3.up * (inAirCamAimHeight * unit); // about chest height

        // Fixed view: downfield, tilted down.
        Vector3 downfield = inAirCameraDownfield.sqrMagnitude > 0f ? inAirCameraDownfield : Vector3.forward;
        Vector3 cameraRight = Vector3.Cross(Vector3.up, downfield);
        float pitch = Mathf.Clamp(inAirCamTilt, 2f, 85f);
        Vector3 lookDirection = Quaternion.AngleAxis(pitch, cameraRight) * downfield;

        float fieldOfView = flightFieldOfView;
        float aspect = inAirCamera != null ? inAirCamera.aspect : 16f / 9f;
        float halfHeight = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * inAirCamFramingArea;
        float halfWidth = halfHeight * aspect;
        Collider ballCollider = football.GetComponentInChildren<Collider>();
        Vector3 ballTop = ballPosition + Vector3.up * (ballCollider != null ? ballCollider.bounds.extents.y : 0f);

        float raiseAmount;
        float distance;
        solvedFieldOfView = fieldOfView;
        if (!solve)
        {
            // Hold the throw values until the top of the arc, then ease to the catch values.
            float settle = progress <= throwCameraApexProgress ? 0f
                : Mathf.SmoothStep(0f, 1f, (progress - throwCameraApexProgress) / Mathf.Max(0.01f, 1f - throwCameraApexProgress));
            raiseAmount = Mathf.Lerp(inAirCamRaiseAtThrow, inAirCamRaiseAtCatch, settle);
            distance = Mathf.Lerp(inAirCamDistanceAtThrow, inAirCamDistanceAtCatch, settle) * unit;
            Vector3 lift = Vector3.up * (raiseAmount * unit);
            distance = InAirCameraRange(focus, lookDirection, lift, distance, false, ballTop, receiverPoint, receiverSpot, halfWidth, halfHeight);
        }
        else
        {
            float baseRange = inAirCamDistanceAtCatch * unit;
            float minRaise = inAirCamRaiseAtCatch;
            float maxRaise = Mathf.Max(minRaise, inAirCamMaxRaiseAtThrow);
            Vector3 lift = Vector3.up * (minRaise * unit);
            float receiverRange = InAirCameraRange(focus, lookDirection, lift, baseRange, false, ballTop, receiverPoint, receiverSpot, halfWidth, halfHeight);

            // 1. Lift, as little as needed for the top of the ball to be at the top of the framing area.
            raiseAmount = minRaise;
            if (BallScreenHeight(focus, lookDirection, minRaise * unit, receiverRange, ballTop, halfHeight) > 1f)
            {
                if (BallScreenHeight(focus, lookDirection, maxRaise * unit, receiverRange, ballTop, halfHeight) > 1f)
                {
                    raiseAmount = maxRaise;
                }
                else
                {
                    float low = minRaise, high = maxRaise;
                    for (int i = 0; i < 24; i++)
                    {
                        float middle = 0.5f * (low + high);
                        if (BallScreenHeight(focus, lookDirection, middle * unit, receiverRange, ballTop, halfHeight) > 1f)
                        {
                            low = middle;
                        }
                        else
                        {
                            high = middle;
                        }
                    }
                    raiseAmount = high;
                }
            }
            lift = Vector3.up * (raiseAmount * unit);
            receiverRange = InAirCameraRange(focus, lookDirection, lift, baseRange, false, ballTop, receiverPoint, receiverSpot, halfWidth, halfHeight);

            // 2. Widen the field of view, as little as still needed.
            if (inAirCamMaxFieldOfView > fieldOfView)
            {
                Vector3 cameraAt = focus - lookDirection * receiverRange + lift;
                Vector3 local = Quaternion.Inverse(SafeLookRotation(focus - cameraAt, Vector3.up)) * (ballTop - cameraAt);
                if (local.z > 0.1f * receiverRange)
                {
                    float framing = Mathf.Max(0.05f, inAirCamFramingArea);
                    float neededTan = Mathf.Max(Mathf.Abs(local.y / local.z), Mathf.Abs(local.x / local.z) / Mathf.Max(0.1f, aspect)) / framing;
                    solvedFieldOfView = Mathf.Clamp(2f * Mathf.Atan(neededTan) * Mathf.Rad2Deg, fieldOfView, inAirCamMaxFieldOfView);
                }
                halfHeight = Mathf.Tan(solvedFieldOfView * 0.5f * Mathf.Deg2Rad) * inAirCamFramingArea;
                halfWidth = halfHeight * aspect;
            }

            // 3. Back off, as little as still needed.
            float maxRange = Mathf.Max(receiverRange, inAirCamMaxDistanceAtThrow * unit);
            distance = Mathf.Max(receiverRange, Mathf.Min(maxRange,
                InAirCameraRange(focus, lookDirection, lift, receiverRange, true, ballTop, receiverPoint, receiverSpot, halfWidth, halfHeight)));
        }
        solvedRaise = raiseAmount;
        solvedDistance = distance / Mathf.Max(1e-4f, unit);
        desiredPosition = focus - lookDirection * distance + Vector3.up * (raiseAmount * unit);
        viewRotation = SafeLookRotation(focus - desiredPosition, Vector3.up);
    }

    /// <summary>
    /// The thrown-ball prefab (ParentPosition, with the Rigidbody and TestCollision). If the Inspector field is empty,
    /// was destroyed, or points at an object in the scene instead of the prefab asset (a scene copy is destroyed /
    /// reused, which gives "parentFootballPositionPrefab doesn't exist anymore"), it uses
    /// Assets/Resources/Prefabs/ParentPosition.prefab instead and says so once in the Console.
    /// </summary>
    GameObject GetFootballPrefab()
    {
        bool usable = parentFootballPositionPrefab != null && !parentFootballPositionPrefab.scene.IsValid()
            && parentFootballPositionPrefab.GetComponent<Rigidbody>() != null && parentFootballPositionPrefab.GetComponent<TestCollision>() != null;
        if (!usable)
        {
            GameObject fallback = Resources.Load<GameObject>("Prefabs/ParentPosition");
            if (fallback != null)
            {
                Debug.LogWarning("QB: Parent Football Position Prefab is missing or isn't the prefab asset (it must be Assets/Resources/Prefabs/ParentPosition, " +
                    "not an object in the scene). Using Resources/Prefabs/ParentPosition for throws. Drag that prefab from the Project window into the field to fix it.");
                parentFootballPositionPrefab = fallback;
            }
        }
        return parentFootballPositionPrefab;
    }

    /// <summary>
    /// Worked out once at the release, before the ball flies, from the top of the throw's arc (its biggest delta Y):
    /// where the ball and the receiver will be then. Sets In Air Cam Raise At Throw and Distance At Throw (the
    /// least lift, then the least backing off, that keeps the top of the ball at the top of the screen there) and the
    /// field of view for the whole flight (widened only as much as needed), so the camera doesn't have to chase
    /// the ball mid-flight. Returns the field of view (also set on the camera right away).
    /// </summary>
    float WorkOutThrowCamera(GameObject receiverObject, WR receiver, Vector3 releasePosition, Vector3 launchVelocity, float gravity, float releaseDelay)
    {
        float fieldOfView = inAirCamFieldOfView;
        throwCameraApexProgress = 0f;
        if (!useBroadcastInAirCamera || receiverObject == null || mainCamera == null || timeToCatch <= 0f || gravity <= 0f)
        {
            return fieldOfView;
        }
        float apexTime = Mathf.Clamp(launchVelocity.y / gravity, 0f, timeToCatch);
        Vector3 apex = releasePosition + new Vector3(launchVelocity.x * apexTime, launchVelocity.y * apexTime - 0.5f * gravity * apexTime * apexTime, launchVelocity.z * apexTime);
        Vector3 receiverShift = receiver != null ? receiver.PredictRunDisplacement(releaseDelay + apexTime) : Vector3.zero;
        Camera cam = mainCamera.GetComponent<Camera>();
        float unit = inAirCameraUnit > 0f ? inAirCameraUnit : MeasurePlayerHeight(receiverObject);
        throwCameraApexProgress = apexTime / timeToCatch;
        ComputeInAirCameraPose(receiverObject, apex, throwCameraApexProgress, unit, cam, inAirCamFieldOfView, true, receiverShift,
            out _, out _, out _, out fieldOfView, out float raiseAtThrow, out float distanceAtThrow);
        inAirCamRaiseAtThrow = raiseAtThrow;
        inAirCamDistanceAtThrow = distanceAtThrow;
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }
        return fieldOfView;
    }

    /// <summary>
    /// Where the top of the ball is on screen, vertically, for a camera lifted by raiseHeight at this distance and
    /// aiming at the focus: 0 = middle, 1 = top of the framing area, above 1 = off the top. A ball behind the camera
    /// or nearly overhead counts as 0 (lifting can't bring it in; it comes into view by itself).
    /// </summary>
    static float BallScreenHeight(Vector3 focus, Vector3 lookDirection, float raiseHeight, float range, Vector3 ballTop, float halfHeight)
    {
        Vector3 position = focus - lookDirection * range + Vector3.up * raiseHeight;
        Vector3 local = Quaternion.Inverse(SafeLookRotation(focus - position, Vector3.up)) * (ballTop - position);
        if (local.z <= 0.1f * range)
        {
            return 0f;
        }
        return (local.y / local.z) / Mathf.Max(1e-4f, halfHeight);
    }

    /// <summary>LookRotation that returns the current/identity rotation for a zero vector instead of logging an error every frame.</summary>
    static Quaternion SafeLookRotation(Vector3 forward, Vector3 up)
    {
        return forward.sqrMagnitude > 1e-10f ? Quaternion.LookRotation(forward, up) : Quaternion.identity;
    }

    static Quaternion SafeLookRotation(Vector3 forward)
    {
        return SafeLookRotation(forward, Vector3.up);
    }

    /// <summary>
    /// Shortest distance (from startRange, growing 3% at a time) at which the camera, raised and aiming at the focus,
    /// has the WR (shoulder and feet), and the ball if includeBall, inside the framing area.
    /// </summary>
    float InAirCameraRange(Vector3 focus, Vector3 lookDirection, Vector3 raise, float startRange, bool includeBall,
        Vector3 ballPosition, Vector3 receiverPoint, Vector3 receiverSpot, float halfWidth, float halfHeight)
    {
        float range = startRange;
        for (int i = 0; i < 120; i++)
        {
            Vector3 position = focus - lookDirection * range + raise;
            Quaternion toView = Quaternion.Inverse(SafeLookRotation(focus - position, Vector3.up));
            if ((!includeBall || InAirCameraSees(toView, position, ballPosition, halfWidth, halfHeight)) &&
                InAirCameraSees(toView, position, receiverPoint, halfWidth, halfHeight) &&
                InAirCameraSees(toView, position, receiverSpot, halfWidth, halfHeight))
            {
                break;
            }
            range *= 1.03f;
        }
        return range;
    }

    /// <summary>
    /// After the catch: the camera keeps the same view (downfield, inAirCamTilt, inAirCamFieldOfView) and the exact
    /// offset from the WR that it has at the catch spot (aiming at inAirCamAimHeight on him from inAirCamDistanceAtCatch),
    /// and moves with him. The offset (not the world position) is what eases in, over afterCatchSettleTime, so the camera
    /// never lags behind a running WR; once settled the delta is exact. Returns false if there's no caught ball.
    /// </summary>
    bool FollowReceiverAfterCatch()
    {
        if (!followReceiverAfterCatch)
        {
            return false;
        }
        GameObject receiverObject = throwBPerformed ? wrB : wrX;
        WR receiver = receiverObject != null ? receiverObject.GetComponent<WR>() : null;
        bool intercepted = interceptedBy != null;
        if (!intercepted && (receiver == null || receiver.caughtFootball == null))
        {
            afterCatchOffsetVelocity = Vector3.zero;
            return false;
        }
        if (intercepted)
        {
            receiverObject = interceptedBy.gameObject; // follow the defender who picked it off, the same way
        }
        Transform cameraTransform = mainCamera.transform;
        Camera followCamera = mainCamera.GetComponent<Camera>();

        // The same view as the in-air camera at the catch.
        Vector3 downfield = inAirCameraDownfield.sqrMagnitude > 0f ? inAirCameraDownfield : Vector3.forward;
        Vector3 cameraRight = Vector3.Cross(Vector3.up, downfield);
        float pitch = Mathf.Clamp(inAirCamTilt, 2f, 85f);
        Vector3 lookDirection = Quaternion.AngleAxis(pitch, cameraRight) * downfield;
        Quaternion viewRotation = SafeLookRotation(lookDirection, Vector3.up);
        float unit = inAirCameraUnit > 0f ? inAirCameraUnit : MeasurePlayerHeight(receiverObject);
        Vector3 catchSpotOffset = Vector3.up * (inAirCamAimHeight * unit) - lookDirection * (inAirCamDistanceAtCatch * unit) + Vector3.up * (inAirCamRaiseAtCatch * unit);
        // Same aim as the in-air camera at the catch spot: at inAirCamAimHeight on him, from the raised spot.
        viewRotation = SafeLookRotation(Vector3.up * (inAirCamAimHeight * unit) - catchSpotOffset, Vector3.up);

        Vector3 receiverPosition = receiverObject.transform.position;
        Vector3 currentOffset = cameraTransform.position - receiverPosition;
        Vector3 offset = Vector3.SmoothDamp(currentOffset, catchSpotOffset, ref afterCatchOffsetVelocity, Mathf.Max(0.01f, afterCatchSettleTime));
        cameraTransform.position = receiverPosition + offset;
        float turn = 1f - Mathf.Exp(-inAirCameraTurnSpeed * Time.deltaTime);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, viewRotation, turn);
        if (followCamera != null)
        {
            followCamera.fieldOfView = Mathf.Lerp(followCamera.fieldOfView, inAirCamFieldOfView, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }
        CameraFollowingReceiver = true;
        return true;
    }

    /// <summary>
    /// Air time for a pass of this many yards: passAirTimeBase + passAirTimePerYard x yards, / passSpeedMultiplier.
    /// </summary>
    float PassAirTime(float yards)
    {
        float seconds = (passAirTimeBase + passAirTimePerYard * Mathf.Max(0f, yards)) / Mathf.Max(0.05f, passSpeedMultiplier);
        return Mathf.Max(minPassFlightTime, seconds);
    }

    /// <summary>
    /// The thrown ball's extra gravity (BallGravityMultiplier), so it falls like a real football at this scale.
    /// The throw's velocities are worked out with the same gravity. Stops once it's caught (kinematic).
    /// </summary>
    private void FixedUpdate()
    {
        if (!thrown || parentFootballPosition == null)
        {
            return;
        }
        Rigidbody ballBody = parentFootballPosition.GetComponent<Rigidbody>();
        if (BallGravityMultiplier > 1f && ballBody != null && !ballBody.isKinematic && ballBody.useGravity)
        {
            ballBody.AddForce(Physics.gravity * (BallGravityMultiplier - 1f), ForceMode.Acceleration);
        }
        UpdatePassCheck(ballBody);
    }

    [Tooltip("Logs each pass to the Console: how far the ball was from the aimed spot when due, how close it came to the receiver's left palm, and his speed at the throw vs when it was due.")]
    public bool logPassAccuracy = true;
    WR passCheckReceiver;
    Vector3 passCheckTarget;
    Vector3 passCheckLaunchVelocity;
    Vector3 passCheckReceiverVelocity;
    float passCheckStartTime;
    float passCheckClosest;
    float passCheckClosestTime;
    float passCheckReceiverSpeedAtArrival;
    float passCheckTargetMiss = -1f;
    Vector3 passCheckTargetOffset;
    bool passCheckActive;
    int passCheckStartFrame;
    float passCheckArmError = -1f;
    float passCheckPlannedPalmMiss = -1f;
    bool passCheckHoldingCatch;

    void StartPassCheck(WR receiver, Vector3 target, float releaseDelay, Vector3 launchVelocity, Vector3 receiverVelocity)
    {
        passCheckActive = logPassAccuracy && receiver != null;
        passCheckReceiver = receiver;
        passCheckTarget = target;
        passCheckLaunchVelocity = launchVelocity;
        passCheckReceiverVelocity = receiverVelocity;
        passCheckStartTime = Time.time + releaseDelay;
        passCheckStartFrame = Time.frameCount;
        passCheckArmError = -1f;
        passCheckPlannedPalmMiss = -1f;
        passCheckClosest = float.MaxValue;
        passCheckClosestTime = 0f;
        passCheckReceiverSpeedAtArrival = 0f;
        passCheckTargetMiss = -1f;
        passCheckTargetOffset = Vector3.zero;
    }

    /// <summary>
    /// Follows the pass and logs it once (when caught, or half a second after it was due): where the ball was
    /// relative to the aimed spot when it was due (is the throw itself right?), and how close it came to the
    /// receiver's actual left palm (did the receiver get there?).
    /// </summary>
    void UpdatePassCheck(Rigidbody ballBody)
    {
        if (!passCheckActive || ballBody == null || passCheckReceiver == null)
        {
            return;
        }
        float flightTime = Time.fixedTime - passCheckStartTime;
        Vector3 palm = passCheckReceiver.GetLeftHandChildPosition("LeftPalmCatchArea");
        float toPalm = Vector3.Distance(ballBody.position, palm);
        if (toPalm < passCheckClosest)
        {
            passCheckClosest = toPalm;
            passCheckClosestTime = flightTime;
        }
        if (passCheckTargetMiss < 0f && flightTime >= timeToCatch - 1e-4f)
        {
            passCheckTargetOffset = ballBody.position - passCheckTarget;
            passCheckTargetMiss = passCheckTargetOffset.magnitude;
            Rigidbody receiverBody = passCheckReceiver.GetComponent<Rigidbody>();
            passCheckReceiverSpeedAtArrival = receiverBody != null ? Vector3.ProjectOnPlane(receiverBody.velocity, Vector3.up).magnitude : 0f;
            passCheckArmError = passCheckReceiver.GetLeftArmAimError(out bool holdingCatch);
            passCheckHoldingCatch = holdingCatch;
            passCheckPlannedPalmMiss = Vector3.Distance(passCheckReceiver.GetLeftPalmCatchPosition(), ballBody.position);
        }
        bool caught = passCheckReceiver.caughtFootball != null;
        if (caught || ballBody.isKinematic || passDefendedNote != null || flightTime > timeToCatch + 0.5f)
        {
            passCheckActive = false;
            float unit = Mathf.Max(0.01f, passCheckReceiver.GetUnitsPerMeter());
            float mph = unit * 0.44704f;
            Debug.Log(string.Format(
                "Pass check ({0}): flight {1:0.00}s planned. When due, the ball was {2:0.0} units ({3:0.00} m) from the aimed spot (offset {4}). " +
                "Closest to his left palm: {5:0.0} units ({6:0.00} m) at {7:0.00}s. Launch {8:0} units/s ({9:0} mph). " +
                "Receiver speed at throw {10:0} units/s ({11:0.0} mph), when due {12:0} units/s ({13:0.0} mph).",
                passDefendedNote ?? (caught ? "CAUGHT" : "not caught"), timeToCatch, passCheckTargetMiss, passCheckTargetMiss / unit, passCheckTargetOffset,
                passCheckClosest, passCheckClosest / unit, passCheckClosestTime,
                passCheckLaunchVelocity.magnitude, passCheckLaunchVelocity.magnitude / mph,
                passCheckReceiverVelocity.magnitude, passCheckReceiverVelocity.magnitude / mph,
                passCheckReceiverSpeedAtArrival, passCheckReceiverSpeedAtArrival / mph)
                + string.Format(" When due: holding Y (catch) = {0}, left arm {1:0} deg from its stick aim, the stick-aimed (straight-arm) palm was {2:0.00} m from the ball. " +
                    "Frame rate during the flight: {3:0} fps.",
                    passCheckHoldingCatch, passCheckArmError, passCheckPlannedPalmMiss / unit,
                    (Time.frameCount - passCheckStartFrame) / Mathf.Max(0.01f, Time.fixedTime - passCheckStartTime)));
        }
    }

    /// <summary>
    /// A player's height in world units, from his character's meshes (falls back to 1.8 x his scale). Used to size
    /// the in-air camera, so it frames the play the same whatever the players / field are scaled to.
    /// </summary>
    public static float MeasurePlayerHeight(GameObject player)
    {
        if (player == null)
        {
            return 1.8f;
        }
        bool hasBounds = false;
        Bounds bounds = new Bounds();
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
        {
            if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
            {
                continue;
            }
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        float fromScale = 1.8f * Mathf.Abs(player.transform.lossyScale.y);
        if (!hasBounds || bounds.size.y < 1e-3f)
        {
            return Mathf.Max(0.01f, fromScale);
        }
        // Raised arms or a carried ball can stretch the bounds; don't let it run away from the scale estimate.
        return Mathf.Clamp(bounds.size.y, fromScale * 0.5f, fromScale * 1.6f);
    }

    // Whether point is inside the framing area of a camera at cameraPosition with the given (inverse) rotation.
    static bool InAirCameraSees(Quaternion inverseViewRotation, Vector3 cameraPosition, Vector3 point, float halfWidth, float halfHeight)
    {
        Vector3 local = inverseViewRotation * (point - cameraPosition);
        if (local.z <= 0.01f)
        {
            return false;
        }
        return Mathf.Abs(local.x / local.z) <= halfWidth && Mathf.Abs(local.y / local.z) <= halfHeight;
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
                Rigidbody flightBody = football.transform.parent != null ? football.transform.parent.GetComponent<Rigidbody>() : null;
                Vector3 flightVelocity = flightBody != null ? flightBody.velocity : Vector3.zero;
                Vector2 horVec = new Vector2(flightVelocity.x, flightVelocity.z);
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
                // Old in-air camera (fixed offset from the ball); UpdateInAirCamera replaces it when
                // useBroadcastInAirCamera is on.
                if (useBroadcastInAirCamera)
                {
                }
                else if ((Time.time - timeThrown) <= timeToCatch)
                {
                    diff = (targetCamPosition - mainCamera.transform.position) * displacementValue;
                    mainCamera.transform.position += diff;
                }
                else {
                    diff = (targetCamPosition - mainCamera.transform.position) * displacementValue;
                    mainCamera.transform.position += diff;
                }
                // Old camera helpers (they move the camera temporarily to measure, then put it back): only needed for
                // the old in-air camera.
                float fieldOfView = 50f;
                float targetCameraAngle = 10f;
                float currentCameraAngle = mainCamera.transform.eulerAngles.x;
                float currentFOV = mainCamera.GetComponent<Camera>().fieldOfView;
                float fieldOfViewNeeded = 50f;
                if (!useBroadcastInAirCamera)
                {
                    targetCamPosition = GetPositionToKeepInView(football, axes, mainCamera.GetComponent<Camera>());
                    fieldOfViewNeeded = Mathf.Min(75f, CalculateRequiredFOV(mainCamera.GetComponent<Camera>(), targetCamPosition, football.transform.position, wrX.transform.position));
                }
                if (useBroadcastInAirCamera)
                {
                    // field of view handled by UpdateInAirCamera
                }
                else if (fieldOfViewNeeded > 50f)
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
        f.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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
            // THROW TARGET (as before the run arm swing): where the WR's left palm will be after 4 seconds at its
            // current velocity, at the palm's height.
            // The palm is where it will be once the catch aim has the left arm straight, pointing the way the WR's
            // stick (and L2) aim it, from the shoulder's normal position (GetLeftPalmCatchPosition), not wherever the
            // arm is at the moment of the throw (mid run swing, or still on its way into the catch).
            WR receiver = wrToUse.GetComponent<WR>();
            Vector3 receiverPalm = receiver.GetLeftPalmCatchPosition();
            Vector3 receiverVelocity = wrToUse.GetComponent<Rigidbody>().velocity;
            // Madden-style pass: the air time grows with the throw's distance (QB to where the receiver's catch palm
            // will be), passAirTimeBase + passAirTimePerYard x yards, / passSpeedMultiplier, and the ball flies under
            // real gravity for this size, so short throws are quick and flat and deep balls take longer and arc higher.
            // The lead is solved with it: where his palm will be after the air time, including him still speeding up.
            float unitsPerMeter = receiver.GetUnitsPerMeter();
            float unitsPerYard = unitsPerMeter * 0.9144f;
            BallGravityMultiplier = scaleBallGravityToPlayerSize ? Mathf.Max(1f, unitsPerMeter) : 1f;
            float ballGravity = Mathf.Abs(Physics.gravity.y) * BallGravityMultiplier;
            receiverVelocity = Vector3.ProjectOnPlane(receiverVelocity, Vector3.up);
            float fixedStep = Time.fixedDeltaTime;
            // The impulse only takes effect at the next physics step, but the receiver keeps running until then.
            float releaseDelay = Mathf.Clamp(Time.fixedTime + fixedStep - Time.time, 0f, fixedStep);
            Vector3 ledPalm = receiverPalm;
            if (useRealWorldPassSpeed)
            {
                timeToCatch = PassAirTime(Vector3.ProjectOnPlane(receiverPalm - football.transform.position, Vector3.up).magnitude / unitsPerYard);
                for (int i = 0; i < 8; i++)
                {
                    ledPalm = receiverPalm + receiver.PredictRunDisplacement(releaseDelay + timeToCatch);
                    timeToCatch = PassAirTime(Vector3.ProjectOnPlane(ledPalm - football.transform.position, Vector3.up).magnitude / unitsPerYard);
                }
            }
            else
            {
                timeToCatch = 4f;
            }
            // Whole physics steps, so the ball is exactly at the target on a step (not between two).
            timeToCatch = Mathf.Max(1, Mathf.Round(timeToCatch / fixedStep)) * fixedStep;
            throwingTargetPosition = receiverPalm + receiver.PredictRunDisplacement(releaseDelay + timeToCatch);
            // Aim at the predicted palm itself. (The old half-hand-spacing offset along handForCatch.forward used the
            // hand's direction and spacing at release, which mid run swing point anywhere and pushed the aim off
            // by up to half an arm's length.)
            throwingTargetPosition = new Vector3(throwingTargetPosition.x, receiverPalm.y, throwingTargetPosition.z);
            wrToUse.GetComponent<WR>().SetTargetPosition(throwingTargetPosition);
            inAirCameraThrowDirection = Vector3.ProjectOnPlane(throwingTargetPosition - football.transform.position, Vector3.up);
            inAirCameraDownfield = transform.forward.z >= 0f ? Vector3.forward : Vector3.back;
            inAirCameraUnit = MeasurePlayerHeight(wrToUse.gameObject);
            inAirCameraVelocity = Vector3.zero;
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
                // Physics steps velocity before position (semi-implicit Euler), so after N steps of dt the ball has
                // dropped g*dt²*N(N+1)/2 = g*T*(T+dt)/2, a little more than the exact g*T²/2. Aim for what physics does.
                velY = ((0.5f * ballGravity * timeToCatch * (timeToCatch + fixedStep)) + (throwingTargetPosition.y - football.transform.position.y)) / (timeToCatch);
                

            }
            else
            {
                
                velY = ((0.5f * ballGravity * (timeToCatch * timeToCatch)) + (handForCatch.position.y - football.transform.position.y)) / (timeToCatch);
            }

            //expectedWRPosition = PredictFuturePosition(timeToCatch, wrX.GetComponent<Rigidbody>());
            float velYCamera = ((0.5f * ballGravity * (timeToCatch * timeToCatch)) + ((tDest.y - mainCamera.transform.position.y))) / (timeToCatch);
            launchVelY = velY;
            float threeDimVelMagnitude = Mathf.Sqrt((velXZ * velXZ) + (velY * velY));
            launchAngle = Mathf.Asin(velY / threeDimVelMagnitude) * Mathf.Rad2Deg;



            //NEEDED FOR ENLARGED GRAVITY SOLUTION
            //Tuple<float, float> roots = SolveQuadratic(4.9f * (22200 / 5f), -velY, -football.transform.position.y);
            Tuple<float, float> roots = SolveQuadratic(0.5f * ballGravity, -velY, -football.transform.position.y);





            timeInAir = roots.Item1;
            //Tuple<float, float> roots = 
            //timeOfFlight = velY / ((0.5f) * Mathf.Abs(Physics.gravity.y));
            float direction = -Mathf.Atan(velZ / velX) * Mathf.Rad2Deg;
            setPositionForFirstTime = true;
            parentFootballPosition = Instantiate<GameObject>(GetFootballPrefab());
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
            //parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // sweep against moving (Rigidbody) colliders too, e.g. the WR's palms
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












            parentFootballPosition.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // sweep against moving (Rigidbody) colliders too, e.g. the WR's palms
            //parentFootballPosition.GetComponent<Rigidbody>().angularDrag = 0.75f;





            //Vector3 impulseForce = new Vector3(vx * parentFootballPositionGetComponent<Rigidbody>().mass, vy * parentFootballPositionGetComponent<Rigidbody>().mass, vz * parentFootballPositionGetComponent<Rigidbody>().mass);
            Vector3 impulseForceOfficial = new Vector3(velX * parentFootballPosition.GetComponent<Rigidbody>().mass, velY * parentFootballPosition.GetComponent<Rigidbody>().mass, velZ * parentFootballPosition.GetComponent<Rigidbody>().mass);

            //GameObject parentFootballPosition = Instantiate<GameObject>()


            parentFootballPosition.GetComponent<Rigidbody>().AddForce(impulseForceOfficial, ForceMode.Impulse);
            StartPassCheck(receiver, throwingTargetPosition, releaseDelay, new Vector3(velX, velY, velZ), receiverVelocity);
            throwFieldOfView = WorkOutThrowCamera(wrToUse.gameObject, receiver, football.transform.position,
                new Vector3(velX, velY, velZ), ballGravity, releaseDelay);

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
                    velY * stepTimePassed - 0.5f * ballGravity * stepTimePassed * stepTimePassed,
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
            // Only the old camera uses these; skip them with the Madden-style camera (and don't need wrXFoot then).
            Transform receiverFoot = wrXFoot != null ? wrXFoot : wrToUse.transform;
            wrXPositionWhenBallThrown = receiverFoot.position;
            if (!useBroadcastInAirCamera)
            {
                expectedCamPosition = GetPositionBetween(c, football, receiverFoot.gameObject, expectedCamPosition, posAtTop, receiverFoot.position);
                expectedCamPosition = new Vector3(expectedCamPosition.x, posAtTop.y, expectedCamPosition.z);
                requiredFOV = CalculateRequiredFOV(c, expectedCamPosition, posAtTop, receiverFoot.position);
            }
            //lineRenderer.



            
        }

        
    }

}
