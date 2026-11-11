using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Linq;
using FootballGame.Common;
using FootballGame.TransformOperations.Rotation.Validators;
using FootballGame.TransformOperations.Controllers;
using FootballGame.TransformOperations.Interfaces;
using FootballGame.TransformOperations.Models;


public class WR : MonoBehaviour
{
    [Header("Set In Inspector")]
    public GameObject QB;
    public GameObject testPivotForAim;
    public Transform rightArm;
    public Transform rightForeArm;
    public Transform leftForeArm;
    public Transform leftArm;
    public Transform leftArmPosition;
    public Transform rightElbowPosition;
    public Transform rightElbowEndPosition;
    public Transform leftElbowPosition;
    public Transform leftElbowEndPosition;
    public Transform leftElbowEndPositionVertical;
    public Transform leftArmStartingPoint;
    public Transform targetPosition;
    public Transform rightTargetPosition;
    public Transform testMoveObject;
    public Transform palmStartSpot;
    public Transform expectedTransform;
    public Transform armEndSpot;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Run Arm Swing (procedural, synced to the run animation)")]
    [Tooltip("Degrees the upper arms swing forward from hanging straight down.")]
    public float armSwingForwardAmplitude = 60f;
    [Tooltip("Degrees the upper arms swing back from hanging straight down.")]
    public float armSwingBackAmplitude = 45f;
    [Tooltip("Degrees the arm angles in towards the chest at the front of the swing.")]
    public float armSwingAcrossBody = 12f;
    [Tooltip("How far each shoulder moves forward / back with its arm, as a fraction of the upper arm's length.")]
    public float shoulderLungeFraction = 0.3f;
    [Tooltip("Extra forward lean of the swing centre, in degrees.")]
    public float armSwingForwardBias = 5f;
    [Tooltip("Elbow bend: forearm angle forward of the upper arm, in degrees.")]
    public float armSwingElbowBend = 85f;
    [Tooltip("How far the upper arms angle out from the body, in degrees.")]
    public float armSwingAbduction = 10f;
    [Tooltip("Shifts the swing against the run cycle (0..1). Change if the arms swing with the same-side leg.")]
    [Range(0f, 1f)] public float armSwingPhaseOffset = 0f;
    [Tooltip("Arm swings per run animation cycle (1 = one forward/back per loop of the run clip).")]
    public float armSwingCyclesPerLoop = 1f;
    [Tooltip("Seconds to blend the arms into / out of the swing (e.g. after letting go of catch).")]
    public float armSwingBlendTime = 0.2f;
    [Tooltip("Seconds per swing when the run state's timing isn't available (e.g. mid-transition).")]
    public float armSwingFallbackCycleTime = 0.7f;
    private float armSwingWeight = 0f;
    private float armSwingPhase = 0f;
    // The run arm swing is drawn on top of the arms' normal pose and never saved into it: the normal pose
    // (shoulder positions, forearm rotations) is recorded before the swing is drawn and restored at the start of
    // the next LateUpdate, so the catch aim, MoveToPoint and MoveToAlignWith always work on the normal pose.
    private bool runArmSwingDrawn = false;
    private Vector3 swingBaseLeftShoulderLocalPosition;
    private Vector3 swingBaseRightShoulderLocalPosition;
    private Vector3 swingBaseLeftUpperArmLocalPosition;
    private Vector3 swingBaseRightUpperArmLocalPosition;
    private Quaternion swingBaseLeftForeArmLocalRotation;
    private Quaternion swingBaseRightForeArmLocalRotation;

    [Header("Catching")]
    [Tooltip("Old behaviour: pressing catch on the ground leaps the WR towards the stick. Off = keep running while catching.")]
    public bool leapWhenCatching = false;

    [Header("Catch Assist (while the ball is in the air)")]
    [Tooltip("Most the left arm's aim is pulled from the stick target towards the ball (0..1).")]
    [Range(0f, 1f)] public float aimAssistMaxWeight = 0.5f;
    [Tooltip("Seconds before the ball reaches the WR when the aim assist starts.")]
    public float aimAssistStartTime = 1.5f;
    [Tooltip("Seconds before the ball reaches the WR when the aim assist is at full weight.")]
    public float aimAssistFullTime = 0.5f;
    [Tooltip("No aim assist if the ball passes further than this many arm lengths from the shoulder.")]
    public float aimAssistMaxMissArmLengths = 4f;
    [Tooltip("Most the run direction is pulled from the stick towards where the ball comes down (0..1).")]
    [Range(0f, 1f)] public float moveAssistMaxWeight = 0.5f;
    [Tooltip("Seconds before the ball comes down when the movement assist starts.")]
    public float moveAssistStartTime = 3f;
    [Tooltip("Seconds before the ball comes down when the movement assist is at full weight.")]
    public float moveAssistFullTime = 1f;
    [Tooltip("No movement assist if the landing spot is more than this many degrees from the stick direction.")]
    public float moveAssistMaxAngle = 100f;
    [Tooltip("While catching (Y), most degrees per second the movement assist turns the WR.")]
    public float catchAssistTurnSpeed = 90f;
    [Tooltip("How far ahead to predict the ball, in seconds.")]
    public float ballPredictionTime = 6f;
    private readonly List<Vector3> predictedBallPath = new List<Vector3>();
    private float predictedBallPathStep = 0f;
    private int predictedBallPathFrame = -1;
    public Vector3 elbowVerticalRotationAxis = Vector3.up;
    public Vector3 elbowForward = new Vector3(0, -1, 0);
    public Vector3 wrForward = new Vector3(1, 0, 1);
    public Vector3 targetForward = new Vector3(-1, 0, -1);

    //public Transform rightArm;
    //public Transform leftArm;
    [Header("Set Dynamically")]
    float aimSpeed = 0f;
    PlaneViewModel currentLeftArmRotation = new PlaneViewModel();
    float maxHorizontalRotationCounterClockwise = 45f;
    float maxHorizontalRotationClockwise = 225f;
    // Vertical limits for the left arm, in degrees from level (raising = counter-clockwise = negative).
    // "Forward" = Vector3.forward plane (arm out to the side), "Right" = Vector3.right plane (arm out in front);
    // the limit at any yaw in between is a blend of the two. 135 up = 45 degrees past straight up,
    // 225 up = over the head and 45 degrees down the other side.
    float maxVerticalRotationCounterClockwise = 225f;   // up, out to the side (continues over the top to the right side)
    float maxVerticalRotationClockwise = 90f;           // down, out to the side (straight down)
    float maxVerticalRotationCounterClockwiseRight = 135f; // up, out in front
    float maxVerticalRotationClockwiseRight = 135f;        // down, out in front (a little behind the leg)
    ArmRotationState leftArmRotationState = ArmRotationState.none;
    KeyValuePair<Axis, float> armMovementInstruction = new KeyValuePair<Axis, float>();
    public Vector3 cursorPoint = Vector3.zero;
    private bool currentlyRotatingAroundAxis = false;
    private Vector3 axisOfRotation = Vector3.zero;
    Vector3 positionTowardsCatchTarget = Vector3.zero;
    List<Vector3> pointsOfTrajectory = new List<Vector3>();
    Vector3 throwPoint = Vector3.zero;
    Vector3 throwDirection = Vector3.zero;
    Vector3 lastPosition = Vector3.zero;
    private bool performedJump = false;
    private bool playerOnGround = false;
    Vector3 throwTargetPosition;
    private Vector3 footballSpot;
    private PlayerState wrState = PlayerState.none;
    private bool cameraSetAfterCatch = false;
    //This is to Make A GameObject Run Using Rigidbody
    private GameObject mainCamera;
    private float runAcceleration = 75f * 6f * (20f / 3f);
    private float targetSpeed = 15f * (20f / 3f);
    private float maintainStrength = 10f;
    private bool wrAlignedForRun = false;

    private bool l2TriggerPressed = false;
    private bool rTriggerPressed = false;
    private bool yButtonPressed = false;
    
    private bool isCatching = false;
    Quaternion previousLeftArmRotation;
    Quaternion previousRightArmRotation;
    Quaternion previousLeftShoulderRotation;
    Quaternion previousRightShoulderRotation;
    //Quaternion previousLeftArmRotation;
    //Quaternion previousRightArmRotation;

    private int catchLayerIndex;
    private float currentLayerWeight = 0f;
    public float blendSpeed = 5f;
    private List<RouteSegment> routeSegments = new List<RouteSegment>();
    public Transform leftHandCatchPosition;
    public Transform rightHandCatchPosition;
    public GameObject caughtFootball = null;
    public GameObject footballInstance = null;
    public Vector3 armDirectionLastFrame = new Vector3(0, 0, 0);
    Vector2 movementInputFromLastFrame;
    public bool onLeftAnalogStickTouched = false;
    float armOffset = 14.494f;
    float shoulderOffset = 17.226f;
    bool resetZAxis = false;
    public float timeRotatingLeftArm = 0f;
    float footballWidth = 0.18f;
    float rotateAcceleration = 1f;
    public bool isLeftStickHeldInPlace = false;
    public bool isRightStickHeldInPlace = false;
    float timeRotated = 0f;
    public Quaternion finalRotationAroundXAxis;
    public bool movedTestObject = false;
    public float rotationToQB = 0f;
    public bool firstCutAccelerate = false;
    public float accelerationStartTime = 0.0f;
    public float startTime = 0.0f;
    public bool rotate = false;
    public float amountRotated = 0f;
    public float rotateAngle = 0.0f;
    public float acceleration = 1.25f;
    public Vector3 speed = new Vector3(0, 0, 300f);
    Animator wrAnim;
    public bool stop = false;
    public bool changedCollider = false;
    public bool thrownAt = true;
    float timer = 0.0f;
    public bool rotated = false;
    public float rotateValue = 0.0f;
    public Tuple<float, float> leftRotateValue = new Tuple<float, float>(0f, 0f);
    public float amountForeArmRotated = 0.0f;
    public float amountLeftForeArmRotated = 0.0f;
    public float amountLeftArmRotatedHorizontal = 0.0f;
    Vector3 leftForeArmRotationDirection = new Vector3(0, 0, 0);
    Vector3 leftDir = new Vector3(0, 0, 0);
    bool wrRotatedFully = false;
    Quaternion var = Quaternion.Euler(0, 0, 0);
    float previousRotationAmount = 0f;
    bool previousRotationSetForFirstTime = false;
    bool calculatedRotateValue = false;
    float angleBetween = 0;
    Vector3 deltaPos = new Vector3(0, 0, 0);
    float r2Value = 0;
    float r3Value = 0;
    float r4Value = 0;
    bool rotateArmValuesSet = false;
    float moveCounter = 0;
    float armRotationOffset = 0f;
    PlayerInput playerInput;
    Animator playerAnimator;
    private InputAction throwAction;
    
    Vector2 previousMovementInputDirection;
    float analogMovementHoldAmount = 0f;
    float timeAnalogStickHeld = 0f;
    Vector2 setHorizontalInputDirection = new Vector2(0, 0);
    bool setHorizontalDirection = false;
    float previousFrameHorizontalRotationValue = 0f;

    public float leftArmRotation = 0f;
    public float leftArmRotationVertical = 0f;
    public float previousLeftArmRotationVertical = 0f;
    public float armSpeed = 0f;
    public float armSpeedVertical = 0f;
    public float actualRotationVertical = 0f;
    public float previousLeftArmAngle = 0f;

    float totalHorizontalRotation = 0f;
    float totalVerticalRotation = 0f;
    float rotationAmount = 0f;
    float verticalAngleOffset = 0;
    string axisToRotateTo = "";



    bool pivotArmAxisOfRotationSet = false;
    Vector2 movementInputArmPivotRotationAxis = new Vector2(0, 0);
    bool useUpAxisWhenPivotSet = false;




    Vector2 movementInputDecidingPivotRotationAxis = new Vector2(0, 0);
    bool rotateToUpAxis = false;
    bool lockFlagForRotation = false;

    bool rotateWithVector3Forward = true;
    bool alignedWithRotationVector = false;

    private Direction leftArmDirection = Direction.unspecfied;
    private bool rotatedAroundPivotAccess = false;
    Vector3 defaultRotationVectorLeftArmRight = new Vector3(0, 0, 0);
    bool defaultRotationSet = false;
    bool rotationPivotInPlace = false;
    float shoulderVerticalAngleOffset = 17.08397f;

    //0 = DEFAULT
    //1 = FORWARD, 2 = UP, 
    int rotationPivotAxis = 0;
    bool leftArmHeldStill = false;
    private bool completedLeftArmRotationTask = false;
    Quaternion currentLeftArmTargetDirection = new Quaternion(0, 0, 0, 0);
    private Coroutine currentRotationCoroutine;
    Vector3 rightShoulderTargetPosition = new Vector3(0, 0, 0);
    private bool canSetThrowTarget = false;

    /*public class AxisValidation
    {
        public Axis sourceAxis { get; set; }
        public bool conditionToUse { get; set; }
        public Vector3 targetVector { get; set; }
    }*/
    public class MovementValidation
    {
        public Vector3 sourcePosition { get; set; }
        public bool conditionToUse { get; set; }
        public Vector3 targetPosition { get; set; }
    }
    /*public class AxisRotationValidation {
        public Transform sourceTransform { get; set; }
        public AxisValidation axisRotationValidator { get; set; }
        public float rotationAmount { get; set; }
        public bool isFullRotation { get; set; }
        public RotationType rotationType { get; set; }
        public Vector3 axisOfRotation { get; set; }
    }*/
    void Awake()
    {
        // Forearms for the run arm swing. If they weren't set in the Inspector, find the UMA bones by name under the shoulders.
        if (leftForeArm == null && leftArm != null)
        {
            leftForeArm = FindChildRecursive(leftArm, "LeftForeArm");
        }
        if (rightForeArm == null && rightArm != null)
        {
            rightForeArm = FindChildRecursive(rightArm, "RightForeArm");
        }
        float forwardSign = transform.forward.z > 0 ? 1f : -1f;
        currentLeftArmRotation = GetPlaneViewModel(Vector3.up, Vector3.forward * forwardSign, -leftArm.Find("LeftArm").right, -1f, 0f, 0f, currentLeftArmRotation.currentLeftRotation, currentLeftArmRotation.currentRightRotation, currentLeftArmRotation.currentUpRotation, currentLeftArmRotation.currentDownRotation, maxHorizontalRotationClockwise, maxHorizontalRotationCounterClockwise, maxVerticalRotationClockwise, maxVerticalRotationCounterClockwise);
        ITransformRotation leftArmDefaultController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), transform, leftHand, Vector2.zero, transform.forward, Vector2.zero, -leftArm.Find("LeftArm").right, Vector3.forward * forwardSign, 30f * aimSpeed * Mathf.Pow(0f, 3f), "LeftArm", currentLeftArmRotation);
        leftArmDefaultController.InitializePlane();
        currentLeftArmRotation = leftArmDefaultController.GetTransformViewModel().GetPlaneDetails();
        if (leftHand != null && leftHand.Find("LeftPalmCatchArea") != null) {
            cursorPoint = leftHand.Find("LeftPalmCatchArea").position;
        }
        mainCamera = GameObject.Find("Camera");
        previousLeftArmRotation = leftArm.Find("LeftArm").rotation;
        previousRightArmRotation = rightArm.Find("RightArm").rotation;
        previousLeftShoulderRotation = leftArm.rotation;
        previousRightShoulderRotation = rightArm.rotation;
        catchLayerIndex = GetComponent<Animator>().GetLayerIndex("ProceduralCatchLayer");
        footballWidth = 0.18f;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
        throwAction = playerInput.actions["Catch"];
        playerInput.actions["Run"].performed += OnRun;
        playerInput.actions["Run"].canceled += OnRunCanceled;
        playerInput.actions["Catch"].performed += OnCatch;
        playerInput.actions["Catch"].canceled += OnCatchCanceled;
        playerInput.actions["YButtonPressed"].performed += OnYButtonPressed;
        playerInput.actions["YButtonPressed"].canceled += OnYButtonCanceled;
        playerInput.actions["L2TriggerPressed"].performed += OnL2TriggerPressed;
        playerInput.actions["L2TriggerPressed"].canceled += OnL2TriggerCanceled;
        throwAction.ReadValue<Vector2>();
        previousRotationAmount = transform.localRotation.y;

        //Remember that this uses COSINE AND NOT TANGENT
        //KEEP IN MIND FOR wrForward AND targetForward
        //--> X COMPONENT IS EQUIVALENT TO Z VALUE IN WORLD SPACE
        //--> Z COMPONENT IS EQUIVALENT TO VECTOR MAGNITUDE IN WORLD SPACE
        //rotationAmount = LookAtQB(QB.transform, wrForward, targetForward);
        timer = Time.time;
        verticalAngleOffset = leftArm.eulerAngles.z - GetLeverArmAngle(new Vector2(Mathf.Sqrt(Mathf.Pow(leftArm.right.z, 2) + Mathf.Pow(leftArm.right.x, 2)), leftArm.right.y));
    }
    public void SetPlaneInitialState(float defaultDirectionSign) {
        Vector2 horDirection = new Vector3(-leftArm.Find("LeftArm").right.x, 0f, -leftArm.Find("LeftArm").right.z).normalized;
        Vector2 originalDirection = new Vector3(0f, 0f, 1f * defaultDirectionSign).normalized;
        float deltaAngle = Vector3.SignedAngle(originalDirection, horDirection, Vector3.up);
    }
    public Vector3 GetExpectedTarget(Transform rootTransform, Transform childTransform, Vector3 expectedRootPosition) {
        Vector3 originalPosition = rootTransform.position;
        rootTransform.position = expectedRootPosition;
        Vector3 expectedTarget = childTransform.position;
        //expectedTarget = expectedTarget + (-leftHand.forward * ((leftHand.position - rightHand.position).magnitude / 2f));
        rootTransform.position = originalPosition;
        return expectedTarget;
    }
    public Vector3 PredictFuturePosition(float predictionTime, Rigidbody rb, Vector3 currTargetPosition, float delayTime = 0f)
    {
        Vector3 simulatedPosition = currTargetPosition;
        //Vector3 simulatedPosition = rb.transform.position;
        float xValue = rb.gameObject.transform.forward.x > 0.01f ? rb.gameObject.transform.forward.x : 0f;
        float zValue = rb.gameObject.transform.forward.z > 0.01f ? rb.gameObject.transform.forward.z : 0f;
        Vector3 simulatedVelocity = new Vector3(rb.velocity.x * xValue,
            rb.velocity.y * rb.gameObject.transform.forward.y,
            rb.velocity.z * zValue);

        float acceleration = GetRunAcceleration() / rb.mass;

        float dt = Time.fixedDeltaTime;
        //float dt = Time.deltaTime;

        for (float t = 0; t < predictionTime; t += dt)
        {
            Vector3 desiredDirection = new Vector3(xValue, rb.gameObject.transform.forward.y, zValue).normalized;

            if (simulatedVelocity.magnitude < GetTargetSpeed())
            {
                simulatedVelocity += desiredDirection * acceleration * dt;

                simulatedVelocity = Vector3.ClampMagnitude(
                    simulatedVelocity,
                    GetTargetSpeed());
                simulatedPosition += simulatedVelocity * dt;
            }
            else {
                simulatedPosition += simulatedVelocity * dt;
                simulatedVelocity = Vector3.ClampMagnitude(
                    simulatedVelocity,
                    GetTargetSpeed());
            }

            //simulatedPosition += simulatedVelocity * dt;
        }

        return simulatedPosition;
    }

    public void SetFootballInstance(GameObject football) {
        footballInstance = football;
    }
    public void SetCanSetThrowTarget(bool canSet) {
        canSetThrowTarget = canSet;
    }
    public void SetRouteSegments(List<RouteSegment> wrRouteSegments)
    {
        routeSegments = wrRouteSegments;
    }
    public List<RouteSegment> GetRouteSegments()
    {
        return routeSegments == null ? new List<RouteSegment>() : routeSegments;
    }
    Vector3 DetermineInputVector3D(Vector2 movementInput, float zSign) {
        float zValue = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y) ? zSign * Mathf.Abs(movementInput.y) : zSign * Mathf.Abs(movementInput.x);
        //float 
        if (zValue <= 0.01f)
        {
            zValue = 0f;
        }
        Vector3 inputVector = new Vector3(movementInput.x, movementInput.y, zValue);
        return inputVector;
    }
    AxisValidation DetermineAxisClosestToTarget(List<AxisValidation> conditionsToUseAxis, Transform sourceTransform) {
        float minAngleOfAllAxes = float.MaxValue;
        AxisValidation axisVectorValidated = new AxisValidation { sourceAxis = Axis.none, conditionToUse = false, targetVector = new Vector3(0, 0, 0) };
        foreach (var axisDetails in conditionsToUseAxis) {
            if (axisDetails.conditionToUse) {
                Vector3 vectorToTest = axisDetails.targetVector;
                float angleTotal = RetrieveMinDistanceToAimVectorAtTarget(sourceTransform, vectorToTest, axisDetails.sourceAxis == Axis.forward ? true : false);
                if (Math.Abs(angleTotal) < Mathf.Abs(minAngleOfAllAxes))
                {
                    minAngleOfAllAxes = Mathf.Abs(angleTotal);
                    axisVectorValidated = new AxisValidation { sourceAxis = axisDetails.sourceAxis, conditionToUse = axisDetails.conditionToUse, targetVector = axisDetails.targetVector };
                }
            }
        }
        return axisVectorValidated;
    }
    bool SnapToDirection(Axis axisToRotate, Transform transformToRotate, Vector3 targetDirection) {
        Quaternion rotationToTarget = Quaternion.FromToRotation(axisToRotate == Axis.right ? transformToRotate.right : axisToRotate == Axis.forward ? transformToRotate.forward : axisToRotate == Axis.up ? transformToRotate.up : Vector3.zero, targetDirection);
        Quaternion rotationPerformed = rotationToTarget * transformToRotate.rotation;
        Vector3 axisRotated = axisToRotate == Axis.right ? rotationPerformed * Vector3.right : axisToRotate == Axis.forward ? rotationPerformed * Vector3.forward : axisToRotate == Axis.up ? rotationPerformed * Vector3.up : Vector3.zero;
        if (Mathf.Abs(axisRotated.normalized.x - targetDirection.normalized.x) <= 0.01f && Mathf.Abs(axisRotated.normalized.y - targetDirection.normalized.y) <= 0.01f && Mathf.Abs(axisRotated.normalized.z - targetDirection.normalized.z) <= 0.01f) {
            transformToRotate.rotation = rotationPerformed * transformToRotate.rotation;
            return true;
        }
        return false;
    }

    Vector3 GetAxisVector(Axis sourceAxis, Transform sourceTransform) {
        return sourceAxis == Axis.right ? sourceTransform.right : sourceAxis == Axis.forward ? sourceTransform.forward : sourceAxis == Axis.up ? sourceTransform.up : sourceAxis == Axis.inverseRight ? -sourceTransform.right : sourceAxis == Axis.inverseForward ? -sourceTransform.forward : sourceAxis == Axis.inverseUp ? -sourceTransform.up : Vector3.zero;
    }
    bool AlignUsingGlobalAndLocalAxes(Axis sourceAxis, Transform armToRotate, Vector3 targetDirection, float rotationQuantity, bool fullRotation) {
        Vector3 sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
        RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.up, rotationQuantity, fullRotation);
        sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
        RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.right, rotationQuantity, fullRotation);
        sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
        RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, Vector3.forward, rotationQuantity, fullRotation);
        sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
        RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, armToRotate.up, rotationQuantity, fullRotation);
        sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
        RotateTransformToDirection(sourceAxisVector, armToRotate, targetDirection, armToRotate.right, rotationQuantity, fullRotation);
        sourceAxisVector = GetAxisVector(sourceAxis, armToRotate);
        if (Mathf.Abs(sourceAxisVector.normalized.x - targetDirection.normalized.x) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.y - targetDirection.normalized.y) <= 0.01f && Mathf.Abs(sourceAxisVector.normalized.z - targetDirection.normalized.z) <= 0.01f) {
            return true;
        }
        return false;
    }
    private bool RotateTransformToDirectionUsingAxis(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false)
    {
        try
        {
            Vector3 projectedSource = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
            Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);
            float angleSource = Vector3.SignedAngle(projectedSource, projectedTarget, rotationAxis);
            float amountToRotatePerFrame = angleMagnitude;
            if (fullRotation)
            {
                armToRotate.Rotate(rotationAxis, angleSource, Space.World);
            }
            else
            {
                if (Mathf.Abs(angleSource) > amountToRotatePerFrame)
                {
                    armToRotate.Rotate(rotationAxis, Mathf.Sign(angleSource) * amountToRotatePerFrame, Space.World);
                }
                else
                {
                    armToRotate.Rotate(rotationAxis, angleSource, Space.World);
                }
            }
        }
        catch (Exception ex) {
            print("Error Trying to Rotate Transform: " + armToRotate.name + ". Error: " + ex.Message);
            return false;
        }
        return false;
    }
    List<KeyValuePair<string, bool>> AlignAxis(List<AxisRotationValidation> rotationValidators) {
        List<KeyValuePair<string, bool>> transformRotationCheck = new List<KeyValuePair<string, bool>>();
        foreach (var axisDetails in rotationValidators) {
            if (ValidationUtility.IsFullyInitialized(axisDetails, int.MaxValue) && axisDetails.axisRotationValidator.conditionToUse) {
                bool properlyRotated = axisDetails.rotationType == RotationType.snap ? SnapToDirection(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector) : axisDetails.rotationType == RotationType.useGlobalAndLocalAxes ? AlignUsingGlobalAndLocalAxes(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.rotationAmount, axisDetails.isFullRotation) :
                                       axisDetails.rotationType == RotationType.useSingleAxis ? RotateTransformToDirectionUsingAxis(GetAxisVector(axisDetails.axisRotationValidator.sourceAxis, axisDetails.sourceTransform), axisDetails.sourceTransform, axisDetails.axisRotationValidator.targetVector, axisDetails.axisOfRotation, 30f, axisDetails.isFullRotation) : false;
                transformRotationCheck.Add(new KeyValuePair<string, bool>(axisDetails.sourceTransform.name, properlyRotated));
            }
        }
        return transformRotationCheck;
    }

    /*List<KeyValuePair<string, bool>> AlignRootAxis(Axis sourceAxis, Transform sourceTransform, Transform handTransform, Vector3 inputVector, Vector3 targetDirection, float rotationQuantity, string childArmName) {
        List<AxisRotationValidation>rootRotationValidators = new List<AxisRotationValidation> {
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = sourceAxis == Axis.forward && (inputVector.y < 0 && inputVector.x <= 0),
                targetVector = new Vector3(inputVector.x, inputVector.y, 0f)
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform.Find(childArmName),
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = sourceTransform.Find(childArmName).right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        }
        };
        return rootRotationValidators;
    }*/
    List<KeyValuePair<string, bool>> AlignShoulderAxis(Axis sourceAxis, Transform sourceTransform, Transform handTransform, Vector3 inputVector, Vector3 targetDirection, float rotationQuantity, string childArmName)
    {
        List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = sourceAxis == Axis.forward,
                targetVector = targetDirection
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useGlobalAndLocalAxes,
            axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = sourceAxis == Axis.forward && (inputVector.y < 0 && inputVector.x <= 0),
                targetVector = new Vector3(inputVector.x, inputVector.y, 0f)
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform.Find(childArmName),
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = sourceTransform.Find(childArmName).right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = sourceAxis == Axis.forward && (!(inputVector.y < 0 && inputVector.x <= 0) && inputVector.y >= 0),
                targetVector = new Vector3(inputVector.x, 0f, inputVector.y)
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform.Find(childArmName),
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = sourceTransform.Find(childArmName).right //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = sourceAxis == Axis.forward && (!(inputVector.y < 0 && inputVector.x <= 0) && inputVector.y < 0),
                targetVector = -new Vector3(inputVector.x, inputVector.y, 0f)
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform.Find(childArmName),
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = sourceTransform.Find(childArmName).right //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.up,
                conditionToUse = sourceAxis == Axis.up,
                targetVector = inputVector
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform.Find(childArmName),
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = sourceTransform.Find(childArmName).right //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.none, //THIS MEANS THIS IS A SNAP ROTATION OR SOME OTHER TYPE OF ROTATION THAT DOESN'T TAKE A VECTOR AXIS AS INPUT
                conditionToUse = sourceAxis == Axis.up,
                targetVector = targetDirection
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            //sourceTransform = leftArm,
            sourceTransform = handTransform,
            rotationType = RotationType.snap,
            axisOfRotation = Vector3.zero //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        }};
        return AlignAxis(rotationValidators);
    }

    List<KeyValuePair<string, bool>> AlignArmAxisNew(Axis sourceAxis, Transform sourceTransform, Transform handTransform, Vector2 movementInput, Vector3 inputVector, Vector3 targetDirection, Vector3 rootDirection, float rotationQuantity)
    {
        Vector3 defaultTargetXAxis = new Vector3(0, 0, 1);
        List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.inverseRight,
                conditionToUse = (sourceAxis == Axis.inverseRight) && (rootDirection.z <= 0.01f && inputVector.y <= 0.01f),
                targetVector = new Vector3(inputVector.x, Mathf.Abs(inputVector.y) <= Mathf.Abs(inputVector.x) ? inputVector.y : Mathf.Sign(inputVector.y) * Mathf.Abs(inputVector.x), inputVector.y)
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useGlobalAndLocalAxes,
            axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.inverseRight,
                conditionToUse = (sourceAxis == Axis.inverseRight) && (rootDirection.z <= 0.01f && inputVector.y > 0.01f),
                targetVector = inputVector
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useGlobalAndLocalAxes,
            axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.inverseRight,
                conditionToUse = (sourceAxis == Axis.inverseRight) && (rootDirection.z > 0.01f && inputVector.y <= 0.01f),
                targetVector = new Vector3(movementInput.x, Mathf.Abs(movementInput.y), Mathf.Abs(movementInput.x) <= 0.98f ? movementInput.y : Mathf.Abs(movementInput.x))
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useGlobalAndLocalAxes,
            axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.inverseRight,
                conditionToUse = (sourceAxis == Axis.inverseRight) && (rootDirection.z > 0.01f && inputVector.y > 0.01f),
                targetVector = inputVector
            },
            isFullRotation = false,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useGlobalAndLocalAxes,
            axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        },
        new AxisRotationValidation()
        { //Rotating X Axis (TWISTING ARM)
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z <= 0.01f && inputVector.y <= 0.01f),
                targetVector = new Vector3(movementInput.x, Mathf.Abs(movementInput.x) >= Mathf.Abs(movementInput.y) ? 0f : Mathf.Abs(movementInput.y), Mathf.Abs(movementInput.x) >= Mathf.Abs(movementInput.y) ? Mathf.Abs(movementInput.x) : 0f)
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z <= 0.01f && inputVector.y > 0.01f),
                targetVector = defaultTargetXAxis
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        },
        new AxisRotationValidation()
        { //Rotating X Axis (TWISTING ARM)
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z > 0.01f && inputVector.y <= 0.01f),
                targetVector = movementInput.y < 0f ? -defaultTargetXAxis : defaultTargetXAxis
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        },
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = Axis.forward,
                conditionToUse = (sourceAxis == Axis.forward) && (rootDirection.z > 0.01f && inputVector.y > 0.01f),
                targetVector = defaultTargetXAxis
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = -sourceTransform.right, //FOR SINGLE AXIS SPECIFY AXIS OF ROTATION
        }};
        return AlignAxis(rotationValidators);
    }
    List<KeyValuePair<string, bool>> MoveToAlignWith(GameObject football, Transform sourceTransform, Transform targetTransform, Vector3 offsetDirection, string childName, float scaler) {
        List<KeyValuePair<string, bool>> rotationOutcomes = new List<KeyValuePair<string, bool>>();
        MoveToPoint(sourceTransform, sourceTransform.position, sourceTransform.Find(childName).position, 1f);
        MoveToPoint(sourceTransform.Find(childName), sourceTransform.Find(childName).position, sourceTransform.position, 1f);
        Vector3 tPosition = targetTransform.position - (offsetDirection * scaler);
        MoveToPoint(sourceTransform, sourceTransform.position, tPosition, 1f);
        MoveToPoint(sourceTransform.Find(childName), sourceTransform.Find(childName).position, sourceTransform.position, 1f);
        return rotationOutcomes;
    }
    List<KeyValuePair<string, bool>> AlignWithOtherAxis(GameObject football, Axis sourceAxis, Axis twistAxis, Transform sourceTransform, Transform targetTransform, Vector3 targetDirection, Vector3 palmCatchArea, float rotationQuantity, string childNameSource, string childNameTarget)
    {
        Vector3 defaultTargetXAxis = new Vector3(0, 0, 1);
        //ROTATION PROCESSING
        List<AxisRotationValidation> rotationValidators = new List<AxisRotationValidation> {
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation {
                sourceAxis = sourceAxis,
                conditionToUse = true,
                targetVector = targetDirection
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform,
            rotationType = RotationType.useGlobalAndLocalAxes,
            axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        }};
        List<KeyValuePair<string, bool>> outcomes = AlignAxis(rotationValidators);
        rotationValidators = new List<AxisRotationValidation> {
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation
            {
                sourceAxis = sourceAxis,
                conditionToUse = true,
                targetVector = targetDirection
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform.Find(childNameSource),
            rotationType = RotationType.useGlobalAndLocalAxes,
            axisOfRotation = Vector3.zero, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        }};
        outcomes = AlignAxis(rotationValidators);
        rotationValidators = new List<AxisRotationValidation> {
        new AxisRotationValidation()
        {
            axisRotationValidator = new AxisValidation
            {
                sourceAxis = twistAxis,
                conditionToUse = true,
                targetVector = targetTransform.Find(childNameTarget).forward
            },
            isFullRotation = true,
            rotationAmount = rotationQuantity,
            sourceTransform = sourceTransform.Find(childNameSource),
            rotationType = RotationType.useSingleAxis,
            axisOfRotation = sourceTransform.Find(childNameSource).right, //ONLY ROTATIONTYPE.SINGLEAXIS WILL SPECIFY AN AXIS OF ROTATION
        }};
        outcomes = AlignAxis(rotationValidators);
        //MOVEMENT PROCESSING
        if (football != null && palmCatchArea != Vector3.zero)
        {
            MoveToPoint(football.transform, football.transform.position, palmCatchArea, 1f);
        }
        return outcomes;
        //return AlignAxis(rotationValidators);
    }

    private List<KeyValuePair<string, bool>> MoveFootballToPoint(Transform fB, Vector3 sourcePosition, Vector3 targetPosition, float magnitude)
    {
        List<KeyValuePair<string, bool>> outcomes = new List<KeyValuePair<string, bool>>();
        try
        {
            fB.position += (targetPosition - sourcePosition) * magnitude;
            outcomes.Add(new KeyValuePair<string, bool>(fB.name, true));
        } catch (Exception ex) {
            outcomes.Add(new KeyValuePair<string, bool>(fB.name, false));
        }
        return outcomes;
    }
    List<KeyValuePair<string, bool>> MoveFootball(GameObject football) {
        List<KeyValuePair<string, bool>> footballMovementOutcomes = new List<KeyValuePair<string, bool>>();
        return footballMovementOutcomes;
    }
    ITransformRotation GetTransformController(BodyPart bodyPart, Axis axis, Axis twistAxisOfRotation, Axis axisToTwist, Transform sourceTransform, Transform rootTransform, Transform handTransform, Vector2 analogStickInput, Vector3 rootDirection, Vector3 targetDirection, Vector3 twistAxisOfRotationVector, Vector3 defaultTargetXAxis, float rotationPerFrame, string twistTransformName, PlaneViewModel planeDetails)
    {
        TransformViewModel viewModel = GenerateTransformViewModel(bodyPart, planeDetails, axis, twistAxisOfRotation, axisToTwist, sourceTransform, rootTransform, handTransform, analogStickInput, rootDirection, targetDirection, twistAxisOfRotationVector, defaultTargetXAxis, rotationPerFrame, twistTransformName);
        switch (bodyPart) {
            case BodyPart.root:
                return new RootController(viewModel);
            case BodyPart.leftShoulder:
                //BODY PART IS NEEDED BECAUSE IT'S CALLING BASE CONSTRUCTOR
                return new LeftShoulderController(viewModel);
            case BodyPart.rightShoulder:
                //BODY PART IS NEEDED BECAUSE IT'S CALLING BASE CONSTRUCTOR
                return new RightShoulderController(viewModel);
            case BodyPart.leftArm:
                //BODY PART IS NEEDED BECAUSE IT'S CALLING BASE CONSTRUCTOR
                return new LeftArmController(viewModel);
            default:
                break;
        }
        /*if (axis == Axis.up)
        {
            return new TransformUpAxisRotator();
        }
        else if (axis == Axis.forward) {
            return new TransformForwardAxisRotator();
        }*/
        return new Controller(new TransformViewModel());
    }

    TransformViewModel GenerateTransformViewModel(BodyPart bodyPart, PlaneViewModel planeDetails, Axis axis, Axis twistAxisOfRotation, Axis axisToTwist, Transform sourceTransform, Transform rootTransform, Transform handTransform, Vector2 analogStickInput, Vector3 rootDirection, Vector3 targetDirection, Vector3 xAxisOfRotation, Vector3 defaultTargetXAxis, float rotationPerFrame, string twistTransformName) {
        return new TransformViewModel(bodyPart, planeDetails, axis, twistAxisOfRotation, axisToTwist, sourceTransform, rootTransform, handTransform, analogStickInput, rootDirection, targetDirection, xAxisOfRotation, defaultTargetXAxis, rotationPerFrame, twistTransformName);
    }

    Vector3 GetThrowPoint(Vector3 rootDirection, Vector3 originalInput, Vector3 editedInput, Vector2 mInput) {
        Vector3 offsetVector = new Vector3(0, 0, 0);
        if (rootDirection.z <= 0.01f)
        {
            if (mInput.y <= 0.01f)
            {
                offsetVector = editedInput;
                if (transform.name.ToLower().Contains("wrb"))
                {
                    int h = 0;
                }
                Vector3 aorRight = new Vector3(mInput.x, Mathf.Abs(mInput.y), Mathf.Abs(mInput.y) > Mathf.Abs(mInput.x) ? Mathf.Abs(mInput.x) : Mathf.Abs(mInput.y));
            }
            else
            {
                offsetVector = originalInput;
            }
        }
        else
        {
            if (originalInput.y <= 0.01f)
            {
                offsetVector = editedInput;
            }
            else
            {
                offsetVector = originalInput;
            }
        }
        return offsetVector;
    }

    public bool CaughtFootball() {
        return caughtFootball != null;
    }
    private void OnCollisionEnter(Collision collision) {
        if (collision.collider.tag.ToLower() == "field") {
            playerOnGround = true;
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.tag.ToLower() == "field")
        {
            playerOnGround = true;
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.tag.ToLower() == "field")
        {
            playerOnGround = false;
        }
    }

    public float GetRunAcceleration() {
        return runAcceleration;
    }
    public float GetTargetSpeed()
    {
        return targetSpeed;
    }
    public Vector3 GetFootballSpot() {
        return footballSpot;
    }

    public Vector3 GetLeftHandChildPosition(string childName)
    {
        Vector3 spot = Vector3.zero;
        if (leftHand.Find(childName) != null) {
            spot = leftHand.Find(childName).position;
        }
        return spot;
    }
    public Transform GetLeftHand() {
        return leftHand;
    }

    public float GetDistanceBetweenHands() {
        return (leftHand.position - rightHand.position).magnitude;
    }

    public void SetThrowDirection(Vector3 throwDir) {
        throwDirection = throwDir;
    }
    public void SetThrowPoint(Vector3 throwPos) {
        throwPoint = throwPos;
    }
    public void SetTargetPosition(Vector3 target)
    {
        throwTargetPosition = target;
    }
    public void SetLinePoints(List<Vector3> linePoints) {
        pointsOfTrajectory = linePoints;
    }
    Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;

        float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    KeyValuePair<int, Vector3> GetClosestPoint(List<Vector3> positions, Transform arm, Transform football, Vector3 throwPoint) {
       // KeyValuePair<int, Vector3> gg = new KeyValuePair<int, Vector3>()
        Vector3 closestPoint = Vector3.zero;
        float bestDistance = float.MaxValue;
        int index = -1;
        for (int i = 0; i < positions.Count() - 1; i++)
        {
            Vector3 p = ClosestPointOnSegment(
                positions[i],
                positions[i + 1],
                arm.position);

            float d = (arm.position - p).sqrMagnitude;

            if (d < bestDistance)
            {
                bestDistance = d;
                closestPoint = p;
                index = i;
            }
        }

        return new KeyValuePair<int, Vector3>(index, closestPoint);
    }

    // Update is called once per frame
    void Update()
    {

        if (leftHand.Find("LeftPalmCatchArea") != null && leftHand.Find("LeftPalmCatchArea").GetChild(0) != null)
        {
            footballSpot = leftHand.Find("LeftPalmCatchArea").GetChild(0).position;
        }
        else {
            footballSpot = Vector3.zero;
        }
        Vector2 movementInput = playerInput.actions["Catch"].ReadValue<Vector2>();
        if ((movementInputFromLastFrame - movementInput).magnitude <= 0.05f && movementInput.magnitude > 0)
        {
            isLeftStickHeldInPlace = true;
        }
        else {
            currentlyRotatingAroundAxis = false;
            isLeftStickHeldInPlace = false;
        }
        Vector3 inputVector = new Vector3(movementInput.x, 0f, movementInput.y);
        Vector3 forceForBallDir = footballInstance.transform.position - footballSpot;

        isCatching = rTriggerPressed && yButtonPressed && movementInput.magnitude > 0f;
        //Vector2 movementInput = playerInput.actions["Catch"].ReadValue<Vector2>();
        // Run with R2 whether or not catch (Y) is held.
        bool canRun = rTriggerPressed && wrAlignedForRun && playerOnGround;
        bool canMoveAround = wrAlignedForRun && movementInput.magnitude > 0 && playerOnGround;
        Vector3 jumpDir = footballInstance.transform.position - GetComponent<Rigidbody>().position;
        if (isCatching && playerOnGround && leapWhenCatching)
        {
            if (movementInput.magnitude > 0)
            {
                jumpDir = movementInput.y < 0.5f ? new Vector3(movementInput.x, 1f, movementInput.y) : new Vector3(movementInput.x, movementInput.y, movementInput.y);
                gameObject.GetComponent<Rigidbody>().AddForce(jumpDir.normalized * 40f, ForceMode.Impulse);
            }
        }
        else if (!canRun && !canMoveAround && playerOnGround)
        {
            Vector3 velocity = gameObject.GetComponent<Rigidbody>().velocity;
            gameObject.GetComponent<Rigidbody>().AddForce(-velocity, ForceMode.Impulse);
        }
        /*if (isCatching)
        {

            Vector3 horizontalVel = Vector3.ProjectOnPlane(gameObject.GetComponent<Rigidbody>().velocity, Vector3.up);
            float speed = horizontalVel.magnitude;
            if (speed < (targetSpeed / 2f))
            {
                //gameObject.GetComponent<Rigidbody>().AddForce((runAcceleration) * inputVector, ForceMode.Force);
                float horizontalDistance = (new Vector3(footballInstance.transform.position.x, 0, footballInstance.transform.position.x) - new Vector3(transform.position.x, 0, transform.position.z)).magnitude;
                float verticalDistance = footballInstance.transform.position.y - transform.position.y;
                bool canJump = (horizontalDistance <= 10f && verticalDistance <= 10f) && movementInput.y > 0f;
                Vector3 diff = footballInstance.transform.position - transform.position;
                if (rTriggerPressed && playerOnGround && movementInput.y > 0)
                {
                    gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(movementInput.x, movementInput.y, 0f) * 25f, ForceMode.Impulse);

                }
                else if (rTriggerPressed && playerOnGround)
                {
                    gameObject.GetComponent<Rigidbody>().AddForce(5f * new Vector3(movementInput.x, 0, movementInput.y), ForceMode.Impulse);
                }

            }
        }*/




        //MAKE SURE THAT THE ANIMATOR KNOWS THAT WE ARE CATCHING
        
        
        GetComponent<Animator>().SetBool("isCatching", isCatching);

        float targetWeight = isCatching ? 1f : 0f;
        currentLayerWeight = Mathf.MoveTowards(currentLayerWeight, targetWeight, 1f);
        GetComponent<Animator>().SetLayerWeight(catchLayerIndex, currentLayerWeight);

        // Follow the arms' current pose when not catching, unless that pose has the run arm swing drawn on it
        // (then the saved normal pose from LateUpdate is kept).
        if (!isCatching && !runArmSwingDrawn)
        {
            previousLeftArmRotation = leftArm.Find("LeftArm").rotation;
            previousRightArmRotation = rightArm.Find("RightArm").rotation;
            previousLeftShoulderRotation = leftArm.rotation;
            previousRightShoulderRotation = rightArm.rotation;
        }

        //Running Routes
        //direction
        if (routeSegments != null && routeSegments.Count > 0) {
            transform.position += ((routeSegments[0].direction * routeSegments[0].runDistance) * Time.deltaTime);
            if (
                //IF WE ARE WITHIN RANGE, MOVE ON TO NEXT RUN SEGMENT IN ROUTE
                (transform.position - (routeSegments[0].startPosition + routeSegments[0].direction * routeSegments[0].runDistance)).magnitude < (0.01f * 2.6f)
                ||
                //IF WE ARE PAST THE FORWARD CHECKPOINT, MOVE ON TO NEXT RUN SEGMENT IN ROUTE
                (transform.position.z >= (routeSegments[0].startPosition + routeSegments[0].direction * routeSegments[0].runDistance).z)) {
                routeSegments.RemoveAt(0);
                if (routeSegments.Count > 0) {
                    routeSegments[0].startPosition = transform.position;
                }
            }
        }
        if (!previousRotationSetForFirstTime)
        {
            previousRotationSetForFirstTime = true;
            previousRotationAmount = transform.localEulerAngles.y;
        }
        float rValue = Time.deltaTime * rotationAmount;
        if ((amountRotated + rValue) < rotationAmount)
        {
            transform.Rotate(Vector3.up, rValue, Space.Self);
        }
        else if (amountRotated < rotationAmount)
        {
            transform.Rotate(Vector3.up, rotationAmount - amountRotated, Space.Self);
        }
        if (wrRotatedFully)
        {
            rotated = true;
            if (!defaultRotationSet) {
                defaultRotationVectorLeftArmRight = new Vector3(leftArm.right.x, 0f, leftArm.right.z);
                defaultRotationSet = true;
            }
            


        }
        if (!changedCollider)
        {
            changedCollider = true;
        }
        movementInputFromLastFrame = movementInput;
    }

    public void CatchFootball(Vector2 input) {

    }

    public Vector3 GetTargetThrowingPosition(Vector2 input, Vector2 wrDir, Vector2 wrForward, Vector3 wrPosition, float angleSign, float armMagnitude) {
        float inputAngle = GetLeverArmAngle(input);
        float wrDirAngle = GetLeverArmAngle(wrDir);
        float wrForwardAngle = GetLeverArmAngle(wrForward);
        //float deltaBetweenInputAndWrDir = (inputAngle - wrDirAngle) * angleSign;
        float deltaBetweenInputAndWrDir = 0f;

        //POINTED TOWARDS QB
        if (angleSign < 0)
        {
            deltaBetweenInputAndWrDir = GetUnitCircleEulerAngle(wrForwardAngle) - 360f;
        } //POINTED AWAY FROM QB
        else {
            deltaBetweenInputAndWrDir = GetUnitCircleEulerAngle(wrForwardAngle) - 180f;
        }
        //float targetAngle = GetUnitCircleEulerAngle(wrForwardAngle + deltaBetweenInputAndWrDir);

        float targetAngle = GetUnitCircleEulerAngle(inputAngle + deltaBetweenInputAndWrDir);
        Vector2 targetCoordinates = GetAngleCoordinates(targetAngle);
        return wrPosition + (new Vector3(targetCoordinates.x, 0, targetCoordinates.y) * armMagnitude);
    }

    public Vector3 GetExpectedPivotAxis(Transform armOfPivot, Vector3 axisToPoint, Vector3 targetAxis, int rotationAxisIndex = -1)
    {
        Vector3 pivotAxis = axisToPoint;
        if (rotationAxisIndex != -1) {
            Quaternion rotation = Quaternion.FromToRotation(pivotAxis, targetAxis);
            Quaternion fQ = rotation * leftArm.rotation;
            Vector3 globalAxis = rotationAxisIndex == 0 ? Vector3.up : rotationAxisIndex == 1 ? Vector3.forward : rotationAxisIndex == 2 ? Vector3.right : Vector3.zero;
            Vector3 expectedAxis = fQ * globalAxis;
            return expectedAxis;
        }
        return pivotAxis;
    }

    private float RetrieveMinDistanceToAimVectorAtTarget(Transform armAxis, Vector3 currentAxisOfRotation, bool rotateShoulderForward)
    {
        float totalRotation = 0f;
        if (rotateShoulderForward)
        {
            //STEP 1  - HORIZONTAL
            Vector3 projectedForward = Vector3.ProjectOnPlane(armAxis.forward, Vector3.up);
            Vector3 projectedTarget = Vector3.ProjectOnPlane(currentAxisOfRotation, Vector3.up);
            float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, Vector3.up);
            Quaternion rotationForward = Quaternion.AngleAxis(angleForward, Vector3.up);
            Quaternion fQForward = rotationForward * armAxis.rotation;
            Vector3 fForward = fQForward * Vector3.forward;
            Vector3 fRight = fQForward * Vector3.right;
            //STEP 2 - RIGHT AXIS 
            Vector3 projectedUpRight = Vector3.ProjectOnPlane(fForward, fRight);
            Vector3 projectedTargetRight = Vector3.ProjectOnPlane(currentAxisOfRotation, fRight);
            float angleRight = Vector3.SignedAngle(projectedUpRight, projectedTargetRight, fRight);
            Quaternion rotationRight = Quaternion.AngleAxis(angleRight, fRight);
            Quaternion fQRight = rotationRight * fQForward;
            Vector3 fUp = fQRight * Vector3.up;
            Vector3 fForwardFinal = fQRight * Vector3.forward;
            //STEP 3 - VERTICAL
            Vector3 projectedForwardVertical = Vector3.ProjectOnPlane(fForwardFinal, fUp);
            Vector3 projectedTargetVertical = Vector3.ProjectOnPlane(currentAxisOfRotation, armAxis.up);
            float angleVertical = Vector3.SignedAngle(projectedForwardVertical, projectedTargetVertical, armAxis.up);
            totalRotation = Mathf.Abs(angleForward) + Mathf.Abs(angleRight) + Mathf.Abs(angleVertical);
        }
        else if (!rotateShoulderForward) {
            //STEP 1  - HORIZONTAL
            Vector3 projectedUp = Vector3.ProjectOnPlane(armAxis.up, Vector3.up).magnitude > 0.01f ? Vector3.ProjectOnPlane(armAxis.up, Vector3.up) : Vector3.right;
            Vector3 projectedTarget = Vector3.ProjectOnPlane(currentAxisOfRotation, Vector3.up).magnitude > 0.01f ? Vector3.ProjectOnPlane(currentAxisOfRotation, Vector3.up) : Vector3.right;
            float angleUp = Vector3.SignedAngle(projectedUp, projectedTarget, Vector3.up);
            Quaternion rotationUp = Quaternion.AngleAxis(angleUp, Vector3.up);
            Quaternion fQUp = rotationUp * armAxis.rotation;
            Vector3 fUp = fQUp * Vector3.up;
            Vector3 fRight = fQUp * Vector3.right;
            //STEP 2 - RIGHT AXIS 
            Vector3 projectedUpRight = Vector3.ProjectOnPlane(fUp, fRight);
            Vector3 projectedTargetRight = Vector3.ProjectOnPlane(currentAxisOfRotation, fRight);
            float angleRight = Vector3.SignedAngle(projectedUpRight, projectedTargetRight, fRight);
            Quaternion rotationRight = Quaternion.AngleAxis(angleRight, fRight);
            Quaternion fQRight = rotationRight * fQUp;
            Vector3 fUpFinal = fQRight * Vector3.up;
            Vector3 fForwardFinal = fQRight * Vector3.forward;
            //STEP 3 - VERTICAL
            Vector3 projectedForwardVertical = Vector3.ProjectOnPlane(fUpFinal, fForwardFinal);
            Vector3 projectedTargetVertical = Vector3.ProjectOnPlane(currentAxisOfRotation, fForwardFinal);
            float angleVertical = Vector3.SignedAngle(projectedForwardVertical, projectedTargetVertical, fForwardFinal);
            totalRotation = Mathf.Abs(angleUp) + Mathf.Abs(angleRight) + Mathf.Abs(angleVertical);
        }
        return totalRotation;
    }


    private bool MoveToPointPerFrame(Transform armToMove, Vector3 sourcePosition, Vector3 targetPosition, float movementPerFrame, bool isFullRotation) {
        if (isFullRotation)
        {
            armToMove.position += (targetPosition - sourcePosition);
            return true;
        }
        else {
            if (movementPerFrame >= (targetPosition - sourcePosition).magnitude)
            {
                armToMove.position += (targetPosition - sourcePosition);
                return true;
            }
            else {
                armToMove.position += ((targetPosition - sourcePosition).normalized * movementPerFrame);
            }
        }

        return false;
    }
    private void MoveToPoint(Transform armToMove, Vector3 sourcePosition, Vector3 targetPosition, float magnitude) {
        armToMove.position += (targetPosition - sourcePosition) * magnitude;
    }
    

    private void AimAtTargetInput(Transform armToAim, Vector3 inputVector, char axis = 'r', bool isNegative = true) {
        int sign = isNegative ? -1 : 1;
        Vector3 sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //if ()
        RotateTransformToDirection(-armToAim.right, armToAim, inputVector, Vector3.up);
        RotateTransformToDirection(-armToAim.right, armToAim, inputVector, Vector3.right);
        RotateTransformToDirection(-armToAim.right, armToAim, inputVector, Vector3.forward);
        RotateTransformToDirection(-armToAim.right, armToAim, inputVector, armToAim.forward);
        RotateTransformToDirection(-armToAim.right, armToAim, inputVector, armToAim.up);
    }

    private void AimAtTargetInput(float angleAmount, bool fullRotation, Transform armToAim, Vector3 inputVector, char axis = 'r', bool isNegative = true)
    {
        float sign = isNegative ? -1f : 1f;
        Vector3 sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //if ()

        RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.up, angleAmount, fullRotation);
        sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.forward, angleAmount, fullRotation);
        sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.right, angleAmount, fullRotation);
        sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.forward, angleAmount, fullRotation);
        sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.up, angleAmount, fullRotation);
        //if (!fullRotation)
        //{
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.up, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.forward, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.right, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.forward, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.up, angleAmount, fullRotation);
        //}
        //else
        //{
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.up, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.forward, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.right, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.forward, angleAmount, fullRotation);
        //    sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
        //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.up, angleAmount, fullRotation);
        //}

    }


    //private void AimAtTargetInput(Transform armToAim, Vector3 inputVector, char axis = 'r', bool isNegative = true)
    //{
    //    int sign = isNegative ? -1 : 1;
    //    Vector3 sourceDirection = axis == 'r' ? armToAim.right : armToAim.forward;
    //    //if ()
    //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.up);
    //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.right);
    //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, Vector3.forward);
    //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.forward);
    //    RotateTransformToDirection(sign * sourceDirection, armToAim, inputVector, armToAim.up);
    //}
    private void AlignArmAxis(Transform armToRotate, bool useUp, Vector3 analogAxisOfRotationInput3DVector, Vector3 inputVector, Transform armHand, bool rotateWithForward, bool isParent = true, bool isRightArm = false)
    {
        string findString = isRightArm ? "RightArm" : "LeftArm";
        if (rotateWithForward)
        {
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.up);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.right);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.forward);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.right);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.up);
            if (isParent)
            {
                Vector3 sourcePivot = armToRotate.Find(findString).forward;
                Vector3 rotationVec = new Vector3(0, 0, 0);
                float tSign = 1f;
                if (analogAxisOfRotationInput3DVector.x >= 0 && analogAxisOfRotationInput3DVector.y < 0)
                {
                    tSign = -1f;
                }
                if (inputVector.y < 0)
                {
                    if (inputVector.x >= 0)
                    {
                        //tSign = -1f;
                        if (Mathf.Abs(analogAxisOfRotationInput3DVector.y) > Mathf.Abs(inputVector.y))
                        {
                            //rotationVec = new Vector3(Mathf.Abs(analogAxisOfRotationInput3DVector.x) > 0.01f ? tSign * analogAxisOfRotationInput3DVector.x : 0f, Mathf.Abs(analogAxisOfRotationInput3DVector.y), tSign * analogAxisOfRotationInput3DVector.z);
                        }
                        else
                        {
                            tSign = -1f;
                            //rotationVec = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? tSign * inputVector.x : 0f, Mathf.Abs(inputVector.y), tSign * inputVector.z);
                        }

                    }
                    else
                    {
                        tSign = 1f;
                        //rotationVec = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? tSign * inputVector.x : 0f, Mathf.Abs(inputVector.y), tSign * inputVector.z);
                    }
                }
                else
                {
                    //rotationVec = Mathf.Abs(analogAxisOfRotationInput3DVector.y) > Mathf.Abs(inputVector.y) ? analogAxisOfRotationInput3DVector : new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? inputVector.x : 0f, 1 - Mathf.Abs(inputVector.normalized.y), inputVector.y);
                    //rotationVec = new Vector3(Mathf.Abs(analogAxisOfRotationInput3DVector.x) > 0.01f ? tSign * analogAxisOfRotationInput3DVector.x : 0f, tSign * analogAxisOfRotationInput3DVector.y, tSign * analogAxisOfRotationInput3DVector.z);
                }
                rotationVec = inputVector; 
                RotateTransformToDirection(sourcePivot, armToRotate.Find(findString), rotationVec, armToRotate.Find(findString).right);
            }
        }
    }

    private void AlignArmAxis(bool fullRotation, float rotationAmount, Transform armToRotate, bool useUp, Vector3 analogAxisOfRotationInput3DVector, Vector3 inputVector, Transform armHand, bool rotateWithForward, bool isParent = true, bool isRightArm = false)
    {
        if (isRightArm)
        {
            int h = 0;
        }
        string findString = isRightArm ? "RightArm" : "LeftArm";
        Vector2 twoDimInput = new Vector2(analogAxisOfRotationInput3DVector.x, analogAxisOfRotationInput3DVector.z).normalized;
        if (rotateWithForward)
        {
            /*RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.up);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.right);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.forward);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.right);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.up);*/


            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.up, rotationAmount, fullRotation);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.right, rotationAmount, fullRotation);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, Vector3.forward, rotationAmount, fullRotation);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.up, rotationAmount, fullRotation);
            RotateTransformToDirection(armToRotate.forward, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.right, rotationAmount, fullRotation);
            if (isParent)
            {
                Vector3 sourcePivot = armToRotate.Find(findString).forward;
                Vector3 rotationVec = new Vector3(0, 0, 0);
                float tSign = 1f;
                if (analogAxisOfRotationInput3DVector.x >= 0 && analogAxisOfRotationInput3DVector.y < 0)
                {
                    tSign = -1f;
                }
                if (inputVector.y < 0)
                {
                    if (inputVector.x >= 0)
                    {
                        //tSign = -1f;
                        if (Mathf.Abs(analogAxisOfRotationInput3DVector.y) > Mathf.Abs(inputVector.y))
                        {
                            //rotationVec = new Vector3(Mathf.Abs(analogAxisOfRotationInput3DVector.x) > 0.01f ? tSign * analogAxisOfRotationInput3DVector.x : 0f, Mathf.Abs(analogAxisOfRotationInput3DVector.y), tSign * analogAxisOfRotationInput3DVector.z);
                        }
                        else
                        {
                            tSign = -1f;
                            //rotationVec = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? tSign * inputVector.x : 0f, Mathf.Abs(inputVector.y), tSign * inputVector.z);
                        }

                    }
                    else
                    {
                        tSign = 1f;
                        //rotationVec = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? tSign * inputVector.x : 0f, Mathf.Abs(inputVector.y), tSign * inputVector.z);


                    }
                }
                rotationVec = new Vector3(0, 0, 0);
                if (inputVector.y < 0 && inputVector.x <= 0)
                {
                    //rotationVec = new Vector3(analogAxisOfRotationInput3DVector.x, analogAxisOfRotationInput3DVector.y, 0f);
                    rotationVec = new Vector3(inputVector.x, inputVector.y, 0f);

                }
                else
                {
                    rotationVec = inputVector.y >= 0 ? new Vector3(inputVector.x, 0f, inputVector.y) : new Vector3(inputVector.x, inputVector.y, 0f);
                }
                if (inputVector.y < 0) {
                    rotationVec *= -1;
                }
                //rotationVec = inputVector.y < 0 && inputVector.x <= 0 ? inputVector : analogAxisOfRotationInput3DVector;
                RotateTransformToDirection(leftArm.Find("LeftArm").forward, leftArm.Find("LeftArm"), rotationVec, leftArm.Find("LeftArm").right, 30f, true);
            }
        }

        else if (!rotateWithForward)
        {
            //Vector3 originalUpPosition = armToRotate.Find("LeftArm").up;
            //FOR THE SHOULDER
            //RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, Vector3.up);
            RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, Vector3.up, rotationAmount, fullRotation);
            //RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, Vector3.right);
            RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, Vector3.right, rotationAmount, fullRotation);
            //RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, Vector3.forward);
            RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, Vector3.forward, rotationAmount, fullRotation);
            //RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.right);
            RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.forward, rotationAmount, fullRotation);
            RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.right, rotationAmount, fullRotation);
            //RotateTransformToDirection(armToRotate.up, armToRotate, analogAxisOfRotationInput3DVector, armToRotate.forward);
            

            if (isParent)
            {

                Vector3 rotationVec = new Vector3(0, 0, 0);
                float tSign = 1f;
                if (analogAxisOfRotationInput3DVector.x >= 0 && analogAxisOfRotationInput3DVector.y < 0)
                {
                    tSign = -1f;
                }
                if (inputVector.y < 0)
                {
                    if (inputVector.x >= 0)
                    {
                        //tSign = -1f;
                        if (Mathf.Abs(analogAxisOfRotationInput3DVector.y) > Mathf.Abs(inputVector.y))
                        {
                            //rotationVec = new Vector3(Mathf.Abs(analogAxisOfRotationInput3DVector.x) > 0.01f ? tSign * analogAxisOfRotationInput3DVector.x : 0f, Mathf.Abs(analogAxisOfRotationInput3DVector.y), tSign * analogAxisOfRotationInput3DVector.z);
                        }
                        else
                        {
                            tSign = -1f;
                            //rotationVec = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? tSign * inputVector.x : 0f, Mathf.Abs(inputVector.y), tSign * inputVector.z);
                        }

                    }
                    else
                    {
                        tSign = 1f;
                        //rotationVec = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? tSign * inputVector.x : 0f, Mathf.Abs(inputVector.y), tSign * inputVector.z);
                    }
                }
                RotateTransformToDirection(leftArm.Find("LeftArm").up, leftArm.Find("LeftArm"), rotationVec, leftArm.Find("LeftArm").right, 30f, false);
                Vector3 currentHandRight = armHand.right;
                Quaternion rotationHandRight = Quaternion.FromToRotation(currentHandRight, armToRotate.Find(findString).right);
                Transform hand = isRightArm ? rightHand : leftHand;
                Quaternion fQHandRight = rotationHandRight * hand.rotation;
                armHand.rotation = rotationHandRight * armHand.rotation;
            }

        }

    }


    private void RotateTransformToDirection(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis) {
        Vector3 projectedForward = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
        Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);

        float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, rotationAxis);

        //ORIGINALLY WAS 30f
        float amountToRotatePerFrame = 30f;
        if (Mathf.Abs(angleForward) > amountToRotatePerFrame)
        {
            armToRotate.Rotate(rotationAxis, Mathf.Sign(angleForward) * amountToRotatePerFrame, Space.World);
        }
        else
        {
            armToRotate.Rotate(rotationAxis, angleForward, Space.World);
        }
    }

    private void RotateTransformToDirection(Vector3 armToRotateDirection, Transform armToRotate, Vector3 analogAxisOfRotationInput3DVector, Vector3 rotationAxis, float angleMagnitude = 30, bool fullRotation = false)
    {
        Vector3 projectedForward = Vector3.ProjectOnPlane(armToRotateDirection, rotationAxis);
        Vector3 projectedTarget = Vector3.ProjectOnPlane(analogAxisOfRotationInput3DVector, rotationAxis);
        float angleForward = Vector3.SignedAngle(projectedForward, projectedTarget, rotationAxis);
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

    private IEnumerator RotateOverTime(Quaternion targetRotation, float duration) {
        if (Mathf.Abs(duration) < 0.01f) {
            duration = Time.deltaTime;
        }
        Quaternion startRotation = leftArm.rotation;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            leftArm.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        //leftArm.rotation = targetRotation;

        completedLeftArmRotationTask = true;
    }



    public float GetAimDirectionSign(float number) {
        float x = 0;
        return x;
    }

    private LeverArm RotateTowardsTarget(Transform arm, Vector2 movementInput, float currentArmAngleVertical, Direction currentAimDirection, Direction defaultDirection, float currentAnalogInput, float maxHorizontalRotationClockWise, float maxHorizontalRotationCounterClockWise, float maxAdditionalVerticalRotation)
    {
        LeverArm armRotationDetails = new LeverArm();
        float xInputOffset = -movementInput.x * 17;
        float currentAnalogInputVertical = GetLeverArmAngle(new Vector2(Mathf.Abs(movementInput.x), movementInput.y));

        //OLD LINE OF CODE
        float yInputOffset = -movementInput.y * 17f;
        bool horizontalRotationActive = true;
        Vector3 armRotationBeforeTempChange = leftArm.eulerAngles;
        leftArm.eulerAngles = new Vector3(leftArm.eulerAngles.x, leftArm.eulerAngles.y, 0);

        float horUnitCircleAngle = GetLeverArmAngle(new Vector2(-leftArm.right.x, -leftArm.right.z));
        leftArm.eulerAngles = armRotationBeforeTempChange;


        Quadrant armQuadrantBeforeRotation = GetQuadrant(GetUnitCircleEulerAngle(horUnitCircleAngle));


        bool aimBehindHeadQ1 = armQuadrantBeforeRotation == Quadrant.quadrant3 && NumbersAreEqual(currentAnalogInput, 90, 35.001f);
        //bool aimBehindHeadQ1 = NumbersAreEqual(currentAnalogInput, 90, 10.001f);
        //bool aimBehindHeadQ2 = arMQuadrantBeforeRotation == Quadrant.quadrant4 && (GetQuadrant(currentAnalogInput) == Quadrant.quadrant2 && ((currentAnalogInput <= 90 + maxAdditionalVerticalRotation) || NumbersAreEqual(currentAnalogInput, 90 + maxAdditionalVerticalRotation, 0.001f)));
        bool aimBehindHeadQ2 = armQuadrantBeforeRotation == Quadrant.quadrant4 && NumbersAreEqual(currentAnalogInput, 90, 35.001f);
        //bool aimBehindHeadQ2 = NumbersAreEqual(currentAnalogInput, 90, 10.001f);
        //bool aimBehindHeadQ2 = arMQuadrantBeforeRotation == Quadrant.quadrant4 && (GetQuadrant(currentAnalogInput) == Quadrant.quadrant2 && (currentAnalogInput <= 90 + maxAdditionalVerticalRotation));
        bool aimBehindHeadQ4 = armQuadrantBeforeRotation == Quadrant.quadrant2 && (GetQuadrant(currentAnalogInput) == Quadrant.quadrant4 && (currentAnalogInput <= 270 + maxAdditionalVerticalRotation));
        bool aimBehindHeadQ3 = armQuadrantBeforeRotation == Quadrant.quadrant1 && (GetQuadrant(currentAnalogInput) == Quadrant.quadrant3 && (currentAnalogInput >= 270 - maxAdditionalVerticalRotation));
        float rotOffsetVal = 1f;

        if (xInputOffset > 0 && horizontalRotationActive)
        {
            armRotationDetails.rotateDirection = Direction.clockwise;
            armRotationDetails.xInput = (leftArmRotation + (xInputOffset * rotOffsetVal)) < maxHorizontalRotationClockWise ? xInputOffset * rotOffsetVal : leftArmRotation < maxHorizontalRotationClockWise ? maxHorizontalRotationClockWise - leftArmRotation : 0f;           
            leftArmRotation += armRotationDetails.xInput;
        }
        else if (xInputOffset < 0 && horizontalRotationActive)
        {
            armRotationDetails.rotateDirection = Direction.counterclockwise;
            armRotationDetails.xInput = (leftArmRotation + (xInputOffset * rotOffsetVal)) > -maxHorizontalRotationCounterClockWise ? xInputOffset * rotOffsetVal : leftArmRotation > -maxHorizontalRotationCounterClockWise ? -maxHorizontalRotationCounterClockWise - leftArmRotation : 0f;
            leftArmRotation += armRotationDetails.xInput;
        }

        armRotationDetails.totalAmountRotatedHorizontal = leftArmRotation;

        if (yInputOffset > 0)
        {
            if (aimBehindHeadQ1 || aimBehindHeadQ2)
            {
                armRotationDetails.yInput = (leftArmRotationVertical + ((-movementInput.y * 10f) * rotOffsetVal)) > -90f - maxAdditionalVerticalRotation ? (-movementInput.y * 10f) * rotOffsetVal: leftArmRotationVertical > -90f - maxAdditionalVerticalRotation ? ((-90f - maxAdditionalVerticalRotation) - leftArmRotationVertical) : 0f;
            }
            else
            {
                armRotationDetails.yInput = (leftArmRotationVertical + (yInputOffset * rotOffsetVal)) < 90f + maxAdditionalVerticalRotation ? yInputOffset * rotOffsetVal : leftArmRotationVertical < 90f + maxAdditionalVerticalRotation ? ((90 + maxAdditionalVerticalRotation) - leftArmRotationVertical) : 0f;
            }
        }
        else if (yInputOffset < 0)
        {
            if ((movementInput.y < 0 && currentArmAngleVertical > 90) || (NumbersAreEqual(currentAnalogInputVertical, 270, 10.001f)))
            {
                armRotationDetails.yInput = (leftArmRotationVertical - ((-movementInput.y * 10f) * rotOffsetVal)) < 90f + maxAdditionalVerticalRotation ? (-movementInput.y * 10f) * rotOffsetVal : leftArmRotationVertical < 90f + maxAdditionalVerticalRotation ? ((90 + maxAdditionalVerticalRotation) - leftArmRotationVertical) : 0f;
            }
            else
            {
                armRotationDetails.yInput = (leftArmRotationVertical + (yInputOffset * rotOffsetVal)) > -90f - maxAdditionalVerticalRotation ? yInputOffset * rotOffsetVal : leftArmRotationVertical > -90f - maxAdditionalVerticalRotation ? ((-90f - maxAdditionalVerticalRotation) - leftArmRotationVertical) : 0f;
            }
        }
        else
        {
            if (leftArmRotationVertical <= 0)
            {
                float extraVerticalArmInput = (aimBehindHeadQ1 || aimBehindHeadQ2) ? -movementInput.y * 10f : 0;
                armRotationDetails.yInput = (leftArmRotationVertical + extraVerticalArmInput) > -90f - maxAdditionalVerticalRotation ? extraVerticalArmInput : leftArmRotationVertical > -90f - maxAdditionalVerticalRotation ? ((-90 - maxAdditionalVerticalRotation) - leftArmRotationVertical) : 0f;
            }
            else
            {
                float extraVerticalInput = NumbersAreEqual(currentAnalogInputVertical, 270, 10.001f) ? -movementInput.y * 18f : 0;
                armRotationDetails.yInput = (leftArmRotationVertical + extraVerticalInput) < 90f + maxAdditionalVerticalRotation ? extraVerticalInput : leftArmRotationVertical < 90f + maxAdditionalVerticalRotation ? ((90f + maxAdditionalVerticalRotation) - leftArmRotationVertical) : 0f;
            }
        }

        armRotationDetails.totalAmountRotatedVertical = leftArmRotationVertical + (armRotationDetails.yInput);


        return armRotationDetails;
    }
    float AimArmAtPivotTarget(Vector2 movementInput, float armRotation, float deltaHorizontalAngleVerticalPivotClockwise, float deltaHorizontalAngleVerticalPivotCounterClockwise, float currentLeftArmForwardHorizontalDirectionAngle, float targetLeftArmForwardHorizontalAngle, float maxRotationAmountLeftArmClockWise, float maxRotationAmountLeftArmCounterClockWise) {

        if ((Mathf.Abs(deltaHorizontalAngleVerticalPivotClockwise) <= Mathf.Abs(deltaHorizontalAngleVerticalPivotCounterClockwise)) && Mathf.Abs(currentLeftArmForwardHorizontalDirectionAngle - targetLeftArmForwardHorizontalAngle) > 0.001f)
        {
            float aimDirectionSign = 1f;
            if (Mathf.Abs(deltaHorizontalAngleVerticalPivotClockwise) > Mathf.Abs(movementInput.x * 15f))
            {
                if ((armRotation + Mathf.Abs(movementInput.x * 15f)) < maxRotationAmountLeftArmClockWise)
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * Mathf.Abs(movementInput.x * 15f), Space.World);
                    armRotation += Mathf.Abs(movementInput.x * 15f);
                }
                else
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * Mathf.Abs(maxRotationAmountLeftArmClockWise - armRotation), Space.World);
                    armRotation += Mathf.Abs(maxRotationAmountLeftArmClockWise - armRotation);
                }

            }
            else
            {


                if ((armRotation + Mathf.Abs(deltaHorizontalAngleVerticalPivotClockwise) < maxRotationAmountLeftArmClockWise))
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * deltaHorizontalAngleVerticalPivotClockwise, Space.World);
                    armRotation += Mathf.Abs(deltaHorizontalAngleVerticalPivotClockwise);
                }
                else
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * Mathf.Abs(maxRotationAmountLeftArmClockWise - armRotation), Space.World);
                    armRotation += Mathf.Abs(maxRotationAmountLeftArmClockWise - armRotation);
                }
            }
        }
        else if ((Mathf.Abs(deltaHorizontalAngleVerticalPivotClockwise) > Mathf.Abs(deltaHorizontalAngleVerticalPivotCounterClockwise)) && Mathf.Abs(currentLeftArmForwardHorizontalDirectionAngle - targetLeftArmForwardHorizontalAngle) > 0.001f)
        {
            float aimDirectionSign = -1f;


            if (Mathf.Abs(deltaHorizontalAngleVerticalPivotCounterClockwise) > Mathf.Abs(movementInput.x * 15f))
            {
                if ((armRotation + -Mathf.Abs(movementInput.x * 15f)) > -maxRotationAmountLeftArmCounterClockWise)
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * Mathf.Abs(movementInput.x * 15f), Space.World);
                    armRotation += -Mathf.Abs(movementInput.x * 15f);
                }
                else
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * Mathf.Abs(-maxRotationAmountLeftArmCounterClockWise - armRotation), Space.World);
                    armRotation += -Mathf.Abs(-maxRotationAmountLeftArmCounterClockWise - armRotation);
                }

            }
            else
            {
                if (armRotation + -Mathf.Abs(deltaHorizontalAngleVerticalPivotCounterClockwise) > -maxRotationAmountLeftArmCounterClockWise)
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * deltaHorizontalAngleVerticalPivotCounterClockwise, Space.World);
                    armRotation += -Mathf.Abs(deltaHorizontalAngleVerticalPivotCounterClockwise);
                }
                else
                {
                    leftArm.Rotate(Vector3.up, aimDirectionSign * Mathf.Abs(-maxRotationAmountLeftArmCounterClockWise - armRotation), Space.World);
                    armRotation += -Mathf.Abs(-maxRotationAmountLeftArmCounterClockWise - armRotation);
                }
            }

        }
        return armRotation;
    }

    private void OnCatch(InputAction.CallbackContext context)
    {
        //isLeftStickHeldInPlace = true;
        Vector2 val = context.ReadValue<Vector2>();
        Vector2 movementInput = context.ReadValue<Vector2>();
        onLeftAnalogStickTouched = true;
        //movementInputFromLastFrame = movementInput;
    }

    private void OnYButtonPressed(InputAction.CallbackContext context)
    {
        yButtonPressed = true;
    }
    private void OnYButtonCanceled(InputAction.CallbackContext context)
    {
        yButtonPressed = false;
    }

    private void OnL2TriggerPressed(InputAction.CallbackContext context)
    {
        l2TriggerPressed = true;
    }
    private void OnL2TriggerCanceled(InputAction.CallbackContext context)
    {
       l2TriggerPressed = false;
    }
    private void OnTurn(InputAction.CallbackContext context)
    {
        rTriggerPressed = context.ReadValueAsButton();
    }
    private void OnTurnCanceled(InputAction.CallbackContext context)
    {
        rTriggerPressed = context.ReadValueAsButton();
    }
    private void OnRun(InputAction.CallbackContext context)
    {
        rTriggerPressed = context.ReadValueAsButton();
    }

    private void OnRunCanceled(InputAction.CallbackContext context)
    {
        rTriggerPressed = false;
        gameObject.GetComponent<Animator>().SetBool("runningForward", false);
        gameObject.GetComponent<Animator>().SetBool("runningLeft", false);
        gameObject.GetComponent<Animator>().SetBool("runningRight", false);
    }
    private void OnCatchCanceled(InputAction.CallbackContext context)
    {
        aimSpeed = 0f;
        //yButtonPressed = false;
        //isCatching = false;
        int x = 0;
        leftArmDirection = Direction.unspecfied;
        isLeftStickHeldInPlace = false;
        isCatching = false;
        timeAnalogStickHeld = 0f;
        setHorizontalDirection = false;
        //previousMovementInput = new Vector2(0, 0);
        previousMovementInputDirection = new Vector2(0, 0);
        armSpeed = 0f;
        armSpeedVertical = 0f;
        timeRotatingLeftArm = 0f;
        axisToRotateTo = "";
        //2/8/2025
        lockFlagForRotation = false;
        rotateToUpAxis = false;

    }

    public Vector3 GetExpectedTargetUsingLeftHandPosition(float timeToCatch) {
        Vector3 expectedTarget = Vector3.zero;
        if (leftHand != null && leftHand.Find("LeftPalmCatchArea") != null && leftHand.Find("LeftPalmCatchArea").GetChild(0) != null)
        {
            if (expectedTarget.magnitude <= 0 && QB.GetComponent<QB>().GetIsThrown())
            {
                Vector3 tPosition = leftHand.Find("LeftPalmCatchArea").position + (-leftHand.forward * GetDistanceBetweenHands() / 2f);
                //expectedTarget = PredictFuturePosition(timeToCatch, GetComponent<Rigidbody>(), leftHand.Find("LeftPalmCatchArea"));
                expectedTarget = PredictFuturePosition(timeToCatch, GetComponent<Rigidbody>(), tPosition);
                //expectedTarget = GetExpectedTarget(transform, leftHand.Find("LeftPalmCatchArea").GetChild(0), expectedTarget);

            }
            //expectedTarget = PredictFuturePosition()
        }
        return expectedTarget;
    }
    private void FixedUpdate()
    {
        GameObject football = QB.GetComponent<QB>().GetFootball();
        Vector3 twistDir = new Vector3(football.transform.position.x, 0f, football.transform.position.z);
        bool timeOver = QB.GetComponent<QB>().GetIsThrown() && ((Time.time - QB.GetComponent<QB>().GetTimeThrown()) >= QB.GetComponent<QB>().GetTimeToCatch()) ? true : false;
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        float xValue = Mathf.Abs(rb.gameObject.transform.forward.x) > 0.01f ? rb.gameObject.transform.forward.x : 0f;
        float zValue = Mathf.Abs(rb.gameObject.transform.forward.z) > 0.01f ? rb.gameObject.transform.forward.z : 0f;
        /*Vector3 editedForward = new Vector3(rb.gameObject.transform.forward.x,
            0f,
            rb.gameObject.transform.forward.z).normalized;*/
        Vector3 editedForward = rb.gameObject.transform.forward;
        //Vector3 editedForward = rb.gameObject.transform.forward;
        Vector2 movementInput = playerInput.actions["Catch"].ReadValue<Vector2>();
        //bool canRun = !timeOver && rTriggerPressed && wrAlignedForRun && !isCatching && playerOnGround;
        // Catching (Y) no longer stops the run.
        bool canRun = !timeOver && wrAlignedForRun && playerOnGround && movementInput.magnitude > 0;
        bool canMoveAround = !timeOver && movementInput.magnitude > 0 && playerOnGround;
        if (canRun)
        {
            Vector3 desiredPosition = Vector3.MoveTowards(transform.position, transform.position + transform.forward * targetSpeed, runAcceleration * Time.fixedDeltaTime);
            Vector3 desiredVelocity = Vector3.MoveTowards(gameObject.GetComponent<Rigidbody>().velocity, editedForward.normalized * targetSpeed, runAcceleration * Time.fixedDeltaTime);
            Vector3 acceleration = (desiredVelocity - gameObject.GetComponent<Rigidbody>().velocity) / Time.fixedDeltaTime;
            gameObject.GetComponent<Rigidbody>().AddForce(acceleration * gameObject.GetComponent<Rigidbody>().mass, ForceMode.Force);
            gameObject.GetComponent<Animator>().SetBool("runningForward", true);
        }
        else if (canMoveAround)
        {
            Vector3 desiredVelocity = Vector3.MoveTowards(gameObject.GetComponent<Rigidbody>().velocity, editedForward.normalized * targetSpeed, runAcceleration * Time.fixedDeltaTime);
            Vector3 acceleration = (desiredVelocity - gameObject.GetComponent<Rigidbody>().velocity) / Time.fixedDeltaTime;
            //float accModifier = wrAlignedForRun ? 1f : 1f / 2f;
            float accModifier = 1f;
            lastPosition = transform.position;
            //gameObject.GetComponent<Rigidbody>().AddForce(acceleration * gameObject.GetComponent<Rigidbody>().mass, ForceMode.Force);
            //gameObject.GetComponent<Animator>().SetBool("runningForward", true);

        }
        else {
            int y = 0;
        }
    }

    void PopulateDirectionInfo(PlaneViewModel planeViewModel, Vector3 sourceDirection, Vector3 targetDirection) {
        planeViewModel.referenceVectorDirection = sourceDirection;
        planeViewModel.targetDirection = targetDirection;
    }
    void PopulateAORInfo(PlaneViewModel planeViewModel, Vector3 aor)
    {
        planeViewModel.plane = aor;
    }
    void PopulateRotationInfo(PlaneViewModel planeViewModel, float horizontalRotation, float verticalRotation)
    {
        planeViewModel.horizontalRotation = horizontalRotation;
        planeViewModel.verticalRotation = verticalRotation;
    }
    static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    /// <summary>The ball's rigidbody while it's in flight (thrown, not caught), otherwise null.</summary>
    Rigidbody GetBallInFlight()
    {
        if (footballInstance == null || caughtFootball != null)
        {
            return null;
        }
        Transform ballParent = footballInstance.transform.parent;
        if (ballParent == null || ballParent.GetComponent<TestCollision>() == null)
        {
            return null; // still in the QB's hand
        }
        Rigidbody ball = ballParent.GetComponent<Rigidbody>();
        return ball != null && !ball.isKinematic ? ball : null;
    }

    /// <summary>
    /// Predicted ball positions from now, one every predictedBallPathStep seconds, stepping the rigidbody the way
    /// physics does (gravity + drag). Cached for the frame. False if the ball isn't in flight.
    /// </summary>
    bool PredictBallPath()
    {
        if (predictedBallPathFrame == Time.frameCount)
        {
            return predictedBallPath.Count > 1;
        }
        predictedBallPathFrame = Time.frameCount;
        predictedBallPath.Clear();
        Rigidbody ball = GetBallInFlight();
        if (ball == null)
        {
            return false;
        }
        float step = Mathf.Max(0.005f, Time.fixedDeltaTime);
        predictedBallPathStep = step;
        Vector3 position = ball.position;
        Vector3 velocity = ball.velocity;
        Vector3 gravity = ball.useGravity ? Physics.gravity : Vector3.zero;
        int steps = Mathf.CeilToInt(ballPredictionTime / step);
        predictedBallPath.Add(position);
        for (int i = 0; i < steps; i++)
        {
            velocity += gravity * step;
            velocity *= 1f / (1f + ball.drag * step);
            position += velocity * step;
            predictedBallPath.Add(position);
        }
        return true;
    }

    /// <summary>
    /// When the ball passes closest to the left shoulder (taking the WR's own movement into account):
    /// seconds from now, how far it misses by, and the direction from the shoulder to the ball at that moment.
    /// </summary>
    bool PredictBallClosestApproach(out float seconds, out float missDistance, out Vector3 directionFromShoulder)
    {
        seconds = 0f;
        missDistance = float.MaxValue;
        directionFromShoulder = Vector3.zero;
        if (!PredictBallPath())
        {
            return false;
        }
        Transform shoulder = leftArm.Find("LeftArm");
        Rigidbody wrBody = GetComponent<Rigidbody>();
        Vector3 wrVelocity = wrBody != null ? Vector3.ProjectOnPlane(wrBody.velocity, Vector3.up) : Vector3.zero;
        for (int i = 0; i < predictedBallPath.Count; i++)
        {
            float t = i * predictedBallPathStep;
            Vector3 shoulderThen = shoulder.position + wrVelocity * t;
            Vector3 offset = predictedBallPath[i] - shoulderThen;
            if (offset.magnitude < missDistance)
            {
                missDistance = offset.magnitude;
                seconds = t;
                directionFromShoulder = offset;
            }
        }
        if (directionFromShoulder.sqrMagnitude < 1e-6f)
        {
            return false;
        }
        directionFromShoulder.Normalize();
        return true;
    }

    /// <summary>Where (and in how many seconds) the ball next comes down through the given height.</summary>
    bool PredictBallLanding(float height, out Vector3 landingSpot, out float seconds)
    {
        landingSpot = Vector3.zero;
        seconds = 0f;
        if (!PredictBallPath())
        {
            return false;
        }
        for (int i = 1; i < predictedBallPath.Count; i++)
        {
            Vector3 previous = predictedBallPath[i - 1];
            Vector3 next = predictedBallPath[i];
            if (previous.y >= height && next.y < height && next.y < previous.y)
            {
                float blend = (previous.y - height) / Mathf.Max(1e-6f, previous.y - next.y);
                landingSpot = Vector3.Lerp(previous, next, blend);
                seconds = (i - 1 + blend) * predictedBallPathStep;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Aim assist: pulls the left arm's stick target towards where the ball will be when it reaches the WR,
    /// starting aimAssistStartTime before it arrives and up to aimAssistMaxWeight by aimAssistFullTime.
    /// No pull if the ball will pass far away from the WR.
    /// </summary>
    Vector3 ApplyAimAssist(Vector3 stickTarget)
    {
        if (stickTarget.sqrMagnitude < 1e-6f || aimAssistMaxWeight <= 0f)
        {
            return stickTarget;
        }
        if (!PredictBallClosestApproach(out float seconds, out float missDistance, out Vector3 ballDirection))
        {
            return stickTarget;
        }
        float armLength = Vector3.Distance(leftArm.Find("LeftArm").position, leftHand.position);
        float maxMiss = Mathf.Max(0.01f, armLength * aimAssistMaxMissArmLengths);
        float timing = Mathf.Clamp01((aimAssistStartTime - seconds) / Mathf.Max(0.01f, aimAssistStartTime - aimAssistFullTime));
        float proximity = Mathf.Clamp01(1f - missDistance / maxMiss);
        float weight = aimAssistMaxWeight * timing * proximity;
        return weight > 0f ? Vector3.Slerp(stickTarget.normalized, ballDirection, weight).normalized : stickTarget;
    }

    /// <summary>
    /// Movement assist: pulls the run direction towards where the ball comes down to shoulder height,
    /// starting moveAssistStartTime before it lands and up to moveAssistMaxWeight by moveAssistFullTime.
    /// Only when the landing spot is within moveAssistMaxAngle of where the stick is steering.
    /// </summary>
    Vector3 ApplyMovementAssist(Vector3 stickDirection)
    {
        Vector3 flatStick = Vector3.ProjectOnPlane(stickDirection, Vector3.up);
        if (flatStick.sqrMagnitude < 1e-6f || moveAssistMaxWeight <= 0f)
        {
            return stickDirection;
        }
        float catchHeight = leftArm.Find("LeftArm").position.y;
        if (!PredictBallLanding(catchHeight, out Vector3 landingSpot, out float seconds))
        {
            return stickDirection;
        }
        Vector3 toLanding = Vector3.ProjectOnPlane(landingSpot - transform.position, Vector3.up);
        if (toLanding.sqrMagnitude < 1e-6f || Vector3.Angle(flatStick, toLanding) > moveAssistMaxAngle)
        {
            return stickDirection;
        }
        float timing = Mathf.Clamp01((moveAssistStartTime - seconds) / Mathf.Max(0.01f, moveAssistStartTime - moveAssistFullTime));
        float weight = moveAssistMaxWeight * timing;
        return weight > 0f ? Vector3.Slerp(flatStick.normalized, toLanding.normalized, weight) : stickDirection;
    }

    static readonly string[] runStateNames = { "WRRunning", "metarig|Running" };

    /// <summary>
    /// Phase (0..1) of the run animation if a run state is playing on any Animator layer (or being blended into).
    /// </summary>
    bool TryGetRunAnimationPhase(out float phase)
    {
        phase = 0f;
        Animator animator = playerAnimator != null ? playerAnimator : GetComponent<Animator>();
        if (animator == null)
        {
            return false;
        }
        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layer);
            foreach (string stateName in runStateNames)
            {
                if (current.IsName(stateName))
                {
                    phase = current.normalizedTime - Mathf.Floor(current.normalizedTime);
                    return true;
                }
                if (animator.IsInTransition(layer) && next.IsName(stateName))
                {
                    phase = next.normalizedTime - Mathf.Floor(next.normalizedTime);
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Puts back the arms' normal pose under last frame's run arm swing: shoulder and upper arm positions
    /// and forearm rotations. (Shoulder and upper arm rotations come back from previous*Rotation.)
    /// Call at the start of LateUpdate, before anything reads or moves the arms.
    /// </summary>
    void RestoreArmsUnderRunSwing()
    {
        if (!runArmSwingDrawn)
        {
            return;
        }
        runArmSwingDrawn = false;
        Transform leftUpperArm = leftArm.Find("LeftArm");
        Transform rightUpperArm = rightArm.Find("RightArm");
        leftArm.localPosition = swingBaseLeftShoulderLocalPosition;
        rightArm.localPosition = swingBaseRightShoulderLocalPosition;
        leftUpperArm.localPosition = swingBaseLeftUpperArmLocalPosition;
        rightUpperArm.localPosition = swingBaseRightUpperArmLocalPosition;
        leftForeArm.localRotation = swingBaseLeftForeArmLocalRotation;
        rightForeArm.localRotation = swingBaseRightForeArmLocalRotation;
    }

    /// <summary>
    /// Draws a runner's arm swing on top of the arms' normal pose while R2 is held and the WR isn't catching (Y)
    /// or carrying the ball. Timed from the run animation so the arms stay in step with the legs:
    ///   - upper arms swing forward (armSwingForwardAmplitude) and back (armSwingBackAmplitude), opposite each
    ///     other, angling in across the chest at the front
    ///   - elbows bent by armSwingElbowBend
    ///   - each shoulder moves forward / back with its arm (shoulderLungeFraction of the upper arm's length)
    /// The swing fades in / out over armSwingBlendTime. It's drawn after LateUpdate saves the normal pose and
    /// removed at the start of the next LateUpdate (RestoreArmsUnderRunSwing), so starting a catch blends from the
    /// swing straight into the normal catch aim, and MoveToPoint / MoveToAlignWith keep their default points.
    /// Works from bone positions (bone -> child joint), so it doesn't depend on each bone's local axes.
    /// </summary>
    void ApplyRunArmSwing()
    {
        if (leftArm == null || rightArm == null || leftForeArm == null || rightForeArm == null || leftHand == null || rightHand == null)
        {
            return;
        }
        Transform leftUpperArm = leftArm.Find("LeftArm");
        Transform rightUpperArm = rightArm.Find("RightArm");
        if (leftUpperArm == null || rightUpperArm == null)
        {
            return;
        }
        bool hasRunPhase = TryGetRunAnimationPhase(out float runPhase);
        if (hasRunPhase)
        {
            armSwingPhase = runPhase * armSwingCyclesPerLoop;
        }
        else
        {
            armSwingPhase += Time.deltaTime / Mathf.Max(0.05f, armSwingFallbackCycleTime);
        }
        bool swing = rTriggerPressed && !yButtonPressed && !isCatching && caughtFootball == null &&
                     (hasRunPhase || armSwingWeight > 0f);
        float blendRate = Time.deltaTime / Mathf.Max(0.01f, armSwingBlendTime);
        armSwingWeight = Mathf.MoveTowards(armSwingWeight, swing ? 1f : 0f, blendRate);
        if (armSwingWeight <= 0f)
        {
            return;
        }

        // Record the normal pose underneath, then draw the swing on top.
        swingBaseLeftShoulderLocalPosition = leftArm.localPosition;
        swingBaseRightShoulderLocalPosition = rightArm.localPosition;
        swingBaseLeftUpperArmLocalPosition = leftUpperArm.localPosition;
        swingBaseRightUpperArmLocalPosition = rightUpperArm.localPosition;
        swingBaseLeftForeArmLocalRotation = leftForeArm.localRotation;
        swingBaseRightForeArmLocalRotation = rightForeArm.localRotation;
        runArmSwingDrawn = true;

        GetWRFrame(out Vector3 wrForward, out Vector3 wrRight);
        // -1 (fully back) .. 1 (fully forward); left arm leads, right arm opposite.
        float swingAmount = Mathf.Sin(2f * Mathf.PI * (armSwingPhase + armSwingPhaseOffset));
        SwingArm(leftArm, leftUpperArm, leftForeArm, leftHand, -wrRight, wrForward, wrRight, swingAmount);
        SwingArm(rightArm, rightUpperArm, rightForeArm, rightHand, wrRight, wrForward, wrRight, -swingAmount);
    }

    void SwingArm(Transform shoulder, Transform upperArm, Transform foreArm, Transform hand, Vector3 outward, Vector3 wrForward, Vector3 wrRight, float swingAmount)
    {
        float upperArmLength = Vector3.Distance(upperArm.position, foreArm.position);

        // Shoulder lunges forward / back with its arm.
        shoulder.position += wrForward * (swingAmount * shoulderLungeFraction * upperArmLength * armSwingWeight);

        // Upper arm: hanging down and a little out, swung forward (and in across the chest) or back.
        float swingAngle = swingAmount >= 0f ? swingAmount * armSwingForwardAmplitude : swingAmount * armSwingBackAmplitude;
        float outAngle = armSwingAbduction - Mathf.Max(0f, swingAmount) * armSwingAcrossBody;
        Vector3 hanging = (Vector3.down * Mathf.Cos(outAngle * Mathf.Deg2Rad) + outward * Mathf.Sin(outAngle * Mathf.Deg2Rad)).normalized;
        // Positive angles around wrRight swing backwards, so forward = negative.
        Vector3 upperDirection = Quaternion.AngleAxis(-(swingAngle + armSwingForwardBias), wrRight) * hanging;
        AimBoneAt(upperArm, foreArm.position, upperDirection, armSwingWeight);

        // Elbow: bend forward from wherever the upper arm now points, blended from the normal forearm pose.
        Vector3 currentUpper = (foreArm.position - upperArm.position).normalized;
        Vector3 foreDirection = Quaternion.AngleAxis(-armSwingElbowBend, wrRight) * currentUpper;
        Quaternion normalLocalRotation = foreArm.localRotation;
        AimBoneAt(foreArm, hand.position, foreDirection, 1f);
        foreArm.localRotation = Quaternion.Slerp(normalLocalRotation, foreArm.localRotation, armSwingWeight);
    }

    // Rotate bone so the line from it to childJoint turns towards direction, by weight (0..1).
    static void AimBoneAt(Transform bone, Vector3 childJoint, Vector3 direction, float weight)
    {
        Vector3 current = childJoint - bone.position;
        if (current.sqrMagnitude < 1e-8f || direction.sqrMagnitude < 1e-8f)
        {
            return;
        }
        Vector3 desired = Vector3.Slerp(current.normalized, direction.normalized, Mathf.Clamp01(weight));
        bone.rotation = Quaternion.FromToRotation(current.normalized, desired) * bone.rotation;
    }

    /// <summary>
    /// Left stick -> direction the left arm (-right) should point, always in the 180 degrees in front of the WR.
    /// Built in the WR's own frame: x = WR right, y = up, z = WR forward projected onto the Vector3.up plane,
    /// then returned in world space.
    ///   Without L2: (x, y, min(|x|,|y|))                 stick up = straight up, stick down = straight down
    ///   With L2:    (x, sign(y) * min(|x|,|y|), |y|)     stick up / down = WR forward (L2 tips the aim forward)
    ///   Stick left / right = straight out to that side either way.
    /// e.g. WR facing Vector3.forward:
    ///   (0,1)        -> (0,1,0)               L2: (0,0,1)
    ///   (0,-1)       -> (0,-1,0)              L2: (0,0,1)
    ///   (.5,.87)     -> (.45,.77,.45)         L2: (.45,.45,.77)
    ///   (.71,-.71)   -> (.58,-.58,.58)        L2: (.58,-.58,.58)
    ///   (1,0)        -> (1,0,0)               L2: (1,0,0)
    /// </summary>
    Vector3 GetLeftArmCatchTarget(Vector2 stick, bool l2Held)
    {
        if (stick.sqrMagnitude < 1e-6f)
        {
            return Vector3.zero;
        }
        float smaller = Mathf.Min(Mathf.Abs(stick.x), Mathf.Abs(stick.y));
        Vector3 local = l2Held
            ? new Vector3(stick.x, stick.y > 0f ? smaller : stick.y < 0f ? -smaller : 0f, Mathf.Abs(stick.y))
            : new Vector3(stick.x, stick.y, smaller);

        GetWRFrame(out Vector3 wrForward, out Vector3 wrRight);
        Vector3 target = wrRight * local.x + Vector3.up * local.y + wrForward * local.z;
        return target.sqrMagnitude > 1e-6f ? target.normalized : Vector3.zero;
    }

    /// <summary>WR forward projected onto the Vector3.up plane, and the matching right.</summary>
    void GetWRFrame(out Vector3 wrForward, out Vector3 wrRight)
    {
        wrForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (wrForward.sqrMagnitude < 1e-6f)
        {
            wrForward = Vector3.forward;
        }
        wrForward.Normalize();
        wrRight = Vector3.Cross(Vector3.up, wrForward);
    }

    /// <summary>
    /// Twist reference for the left arm: the direction arm.forward turns towards (around -arm.right) so the hand
    /// faces the right way for a catch. Built from the arm target in the WR's frame (x right, y up, z forward):
    ///   y &lt; 0:       keepTwist (the existing twist for low targets)
    ///   |z| &gt;= .71:  up, made perpendicular to the target (hand up when reaching out in front)
    ///   otherwise:   (-sign(x) * m, m, M) with m = min(|x|,|y|), M = max(|x|,|y|), made perpendicular
    ///                e.g. (1,0,0) -> (0,0,1), (.87,.5,.5) -> (-.5,.5,.87), (-.87,.5,.5) -> (.5,.5,.87)
    /// </summary>
    Vector3 GetLeftArmTwistTarget(Vector3 armTarget, Vector3 keepTwist)
    {
        if (armTarget.sqrMagnitude < 1e-6f)
        {
            return keepTwist;
        }
        GetWRFrame(out Vector3 wrForward, out Vector3 wrRight);
        Vector3 target = armTarget.normalized;
        float x = Vector3.Dot(target, wrRight);
        float y = Vector3.Dot(target, Vector3.up);
        float z = Vector3.Dot(target, wrForward);
        if (y < 0f)
        {
            return keepTwist;
        }
        Vector3 reference;
        if (Mathf.Abs(z) >= 0.71f)
        {
            reference = Vector3.up;
        }
        else
        {
            float smaller = Mathf.Min(Mathf.Abs(x), Mathf.Abs(y));
            float larger = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            float sideSign = x >= 0f ? 1f : -1f;
            reference = wrRight * (-sideSign * smaller) + Vector3.up * smaller + wrForward * larger;
        }
        Vector3 perpendicular = Vector3.ProjectOnPlane(reference, target);
        if (perpendicular.sqrMagnitude < 1e-6f)
        {
            perpendicular = Vector3.ProjectOnPlane(wrForward, target);
        }
        return perpendicular.sqrMagnitude > 1e-6f ? perpendicular.normalized : keepTwist;
    }

    /// <summary>
    /// Direction to turn the WR to after a catch: the current stick direction (x, 0, y) projected onto the
    /// Vector3.up plane; if the stick is centred, the current route segment's direction; otherwise Vector3.forward.
    /// </summary>
    Vector3 GetPostCatchRouteDirection(Vector2 stick)
    {
        Vector3 direction = Vector3.ProjectOnPlane(new Vector3(stick.x, 0f, stick.y), Vector3.up);
        if (direction.sqrMagnitude < 0.01f && routeSegments != null && routeSegments.Count > 0)
        {
            direction = Vector3.ProjectOnPlane(routeSegments[0].direction, Vector3.up);
        }
        return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
    }

    void PopulateRotationConstraintsInfo(PlaneViewModel planeViewModel, float maxHorizontalRotationClockwise, float maxHorizontalRotationCounterClockwise, float maxVerticalRotationClockwise, float maxVerticalRotationCounterClockwise)
    {
        planeViewModel.maxHorizontalRotationClockwise = maxHorizontalRotationClockwise;
        planeViewModel.maxHorizontalRotationCounterClockwise = maxHorizontalRotationCounterClockwise;
        planeViewModel.maxVerticalRotationClockwise = maxVerticalRotationClockwise;
        planeViewModel.maxVerticalRotationCounterClockwise = maxVerticalRotationCounterClockwise;
    }
    void PopulateForwardOverTop(PlaneViewModel planeViewModel, bool isOverTopForward) {
        planeViewModel.gonePastForwardTopLimit = isOverTopForward;
    }
    void PopulateConstraintsRight(PlaneViewModel planeViewModel, float constraintClockwise, float constraintCounterClockwise)
    {
        planeViewModel.maxVerticalRotationClockwiseRight = constraintClockwise;
        planeViewModel.maxVerticalRotationCounterClockwiseRight = constraintCounterClockwise;
    }
    void PopulateRotationRight(PlaneViewModel planeViewModel, float verticalRotationRight)
    {
        planeViewModel.verticalRotationRight = verticalRotationRight;
    }
    PlaneViewModel GetPlaneViewModel(Vector3 planeToRotateIn, Vector3 axisToRotate, Vector3 desiredDirection, float counterClockwise, float currentHorizontalRotation, float currentVerticalRotation, float currentLeftRotation,
        float currentRightRotation, float currentUpRotation, float currentDownRotation, float maxHorizontalRotationClockwise, float maxHorizontalRotationCounterClockwise, float maxVerticalRotationClockwise, float maxVerticalRotationCounterClockwise) 
    {
        return new PlaneViewModel
        {
            counterClockwiseSign = counterClockwise,
            currentRightRotation = currentRightRotation,
            currentLeftRotation = currentLeftRotation,
            currentUpRotation = currentUpRotation,
            currentDownRotation = currentDownRotation,
            horizontalRotation = currentHorizontalRotation,
            verticalRotation = currentVerticalRotation,
            maxHorizontalRotationClockwise = maxHorizontalRotationClockwise,
            maxHorizontalRotationCounterClockwise = maxHorizontalRotationCounterClockwise,
            maxVerticalRotationClockwise = maxVerticalRotationClockwise,
            maxVerticalRotationCounterClockwise = maxVerticalRotationCounterClockwise,
            plane = planeToRotateIn,
            referenceVectorDirection = axisToRotate,
            targetDirection = desiredDirection
        };
    }

    
    void LateUpdate() {
        if (amountRotated >= rotationAmount)
        {
            wrRotatedFully = true;
        } else {
            amountRotated += transform.eulerAngles.y - previousRotationAmount;
            previousRotationAmount = transform.eulerAngles.y;
        }
        bool f = true;
        Vector3 targetDirection = Vector3.zero;
        Vector3 desiredPosition = Vector3.zero;
        if (f)
        {
            if (gameObject.name.ToLower().Contains("wrx"))
            {
                int k = 0;
            }
            RestoreArmsUnderRunSwing();
            leftArm.rotation = previousLeftShoulderRotation;
            rightArm.rotation = previousRightShoulderRotation;
            leftArm.Find("LeftArm").rotation = previousLeftArmRotation;
            rightArm.Find("RightArm").rotation = previousRightArmRotation;

            bool rotateVertical = Mathf.Abs((Time.deltaTime * leftRotateValue.Item2)) > 0 ? true : false;
            angleBetween = (leftArm.right.x * -1f) > deltaPos.normalized.x ? Vector3.Angle((leftArm.right * -1), deltaPos.normalized) : -Vector3.Angle((leftArm.right * -1), deltaPos.normalized);
            if (caughtFootball != null)
            {
                GameObject pivotAnchor = GameObject.Find("PivotAnchor");
                if (pivotAnchor != null && pivotAnchor.GetComponent<Rigidbody>() != null)
                {
                    Vector3 angularTorque = new Vector3(
            pivotAnchor.GetComponent<Rigidbody>().angularVelocity.x * -1f,
            pivotAnchor.GetComponent<Rigidbody>().angularVelocity.y * -1f,
            pivotAnchor.GetComponent<Rigidbody>().angularVelocity.z * -1f);
                    pivotAnchor.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
                }
                if (gameObject.GetComponent<Rigidbody>() != null)
                {
                    Vector3 angularTorque = new Vector3(
                            caughtFootball.GetComponent<Rigidbody>().angularVelocity.x * -1f,
                            caughtFootball.GetComponent<Rigidbody>().angularVelocity.y * -1f,
                            caughtFootball.GetComponent<Rigidbody>().angularVelocity.z * -1f);
                    caughtFootball.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
                    caughtFootball.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
                    caughtFootball.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
                }

                if (GameObject.Find("PivotAnchor") != null)
                {
                    Vector3 targetPosition = leftHand.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * 0.1705792f);
                    Vector3 targetPosition2 = leftHand.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * (0.1705792f / 2f));
                    targetDirection = new Vector3(0, 0, 0);
                    AimAtTargetInput(90f, true, caughtFootball.transform, leftHand.up, 'r', false);
                    MoveToPoint(caughtFootball.transform, caughtFootball.transform.position, leftHand.Find("LeftPalmCatchArea").GetChild(0).position, 1f);
                    Debug.DrawRay(rightArm.Find("RightArm").position, targetDirection, Color.blue);
                    float scaler = 0.5f;
                    Vector3 tRightPoint = leftHand.position - (leftHand.forward * scaler);
                    AimAtTargetInput(30f, true, rightArm, tRightPoint - rightArm.position, 'r', true);
                    AimAtTargetInput(30f, true, rightArm.Find("RightArm"), tRightPoint - rightArm.Find("RightArm").position, 'r', true);
                    RotateTransformToDirection(rightArm.Find("RightArm").forward, rightArm.Find("RightArm"), leftArm.Find("LeftArm").forward, rightArm.Find("RightArm").right, 30f, true);
                    Vector3 actualTarget = targetPosition + new Vector3(-targetDirection.x, 0, -targetDirection.x);
                    Vector3 val = targetDirection.normalized;
                    if (leftHandCatchPosition != null && rightHandCatchPosition != null)
                    {

                        if (Vector3.Distance(leftHand.Find("LeftPalmCatchArea").GetChild(0).position, caughtFootball.transform.position) > 2 && (Vector3.Distance(rightHand.Find("RightPalmCatchArea").position, caughtFootball.transform.position) > 2))
                        {
                            caughtFootball = null;
                            if (gameObject != null)
                            {
                                Destroy(caughtFootball.GetComponent<FixedJoint>());
                            }
                            if (GameObject.Find("PivotAnchor") != null)
                            {
                                Destroy(GameObject.Find("PivotAnchor"));
                            }
                        }
                    }
                }
            }
            //Whether Football is Caught or Not is a factor in determining PlayerState
            ITransformRotation rootController;
            List<KeyValuePair<string, bool>> rootRotation = new List<KeyValuePair<string, bool>>();
            
            if (caughtFootball == null)
            {
                
                if (isLeftStickHeldInPlace || onLeftAnalogStickTouched)
                {
                    if (rTriggerPressed) {
                        wrState = PlayerState.running;
                    }

                    if (isCatching)
                    {
                        wrState = PlayerState.attemptingCatch;
                    }
                    else {
                        wrState = PlayerState.aimingForwardDirection;
                    }
                    if (gameObject.name.ToLower().Contains("wrxtest")) {
                        int j = 0;
                    }
                    //RETRIEVE 3D VECTOR FOR 2D INPUT
                    Vector2 movementInput = playerInput.actions["Catch"].ReadValue<Vector2>();
                    float analogInputAngle = GetLeverArmAngle(movementInput.normalized);
                    float zAxisOfRotationSign = movementInput.y >= 0 ? 1f : -1f;
                    Vector3 inputVector = DetermineInputVector3D(movementInput, zAxisOfRotationSign);
                    Vector3 tVector = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? inputVector.x : 0f, Mathf.Abs(inputVector.y) <= Mathf.Abs(inputVector.x) ? inputVector.y : Mathf.Sign(inputVector.y) * Mathf.Abs(inputVector.x), Mathf.Abs(inputVector.y) > 0.01f ? inputVector.y : 0f);
                    float tVectorZSign = transform.forward.z >= 0 ? 1f : -1f;
                    Vector3 alteredTVector = new Vector3(tVector.x, tVector.y, tVectorZSign * Mathf.Abs(tVector.z));
                    Vector3 inputVectorRootOffset = movementInput.y >= 0 ? inputVector : alteredTVector;
                    Vector3 rootHorizontal = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                    float defaultZValue = transform.forward.z >= 0 ? 1f : -1f;
                    Vector3 defaultHorizontal = new Vector3(0, 0f, defaultZValue).normalized;
                    //float rootOffset = Vector3.SignedAngle(inputVectorRootOffset, rootHorizontal, Vector3.up);
                    float rootOffset = Vector3.SignedAngle(defaultHorizontal, rootHorizontal, Vector3.up);
                    Quaternion horOffsetLookRotation = Quaternion.LookRotation(inputVectorRootOffset);
                    Quaternion horOffsetRotation = Quaternion.AngleAxis(rootOffset, Vector3.up);
                    Quaternion expectedRotation = horOffsetRotation * horOffsetLookRotation;
                    //WE ALWAYS WANT THE EXPECTED FORWARD OF THE QUATERNION THAT WE CREATE MANUALLY
                    Vector3 expectedForward = expectedRotation * Vector3.forward;
                    Quadrant anQuadrant = GetQuadrant(analogInputAngle);
                    ////RETRIEVE 3D VECTOR FOR AXIS OF ROTATION
                    float analogPivotAxisOfRotation = anQuadrant == Quadrant.quadrant1 ? GetUnitCircleEulerAngle(analogInputAngle + 90) : anQuadrant == Quadrant.quadrant2 ? GetUnitCircleEulerAngle(analogInputAngle - 90) : anQuadrant == Quadrant.quadrant3 ? GetUnitCircleEulerAngle(analogInputAngle + 90) : anQuadrant == Quadrant.quadrant4 ? GetUnitCircleEulerAngle(analogInputAngle - 90) : float.MinValue;
                    Vector2 analogAxisOfRotationInput = GetAngleCoordinates(analogPivotAxisOfRotation);
                    float angleSign = analogAxisOfRotationInput.y >= 0 ? 1f : -1f;
                    Vector3 analogAxisOfRotationInput3DVector = DetermineInputVector3D(analogAxisOfRotationInput, angleSign);
                    //DETERMINE WHICH GAMEOBJECT'S AXIS OF ROTATION TO USE. 1)UP OR 2)FORWARD
                    Vector2 twoDimInput = new Vector2(analogAxisOfRotationInput3DVector.x, analogAxisOfRotationInput3DVector.y).normalized;
                    float zSign = movementInput.y >= 0f ? 1f : -1f;
                    float zSignDefaultTwistAxis = Mathf.Sign(analogAxisOfRotationInput3DVector.z);
                    List<AxisValidation> axisValidators = new List<AxisValidation> {
                        new AxisValidation {
                            sourceAxis = Axis.up,
                            conditionToUse = true/*(twoDimInput.x >= 0f && twoDimInput.y >= -Mathf.Sqrt(2f) / 2f) || (twoDimInput.x >= Mathf.Sqrt(2f) / 2f && twoDimInput.y >= -Mathf.Sqrt(2f) / 2f) || (twoDimInput.x < 0 && twoDimInput.y < 0)*/,
                            targetVector = analogAxisOfRotationInput3DVector
                        }, new AxisValidation {
                            sourceAxis = Axis.up,
                            conditionToUse = movementInput.x < 0f && movementInput.y >= 0,
                            targetVector = -analogAxisOfRotationInput3DVector
                        },
                        new AxisValidation {
                            sourceAxis = Axis.up,
                            conditionToUse = movementInput.y < 0 && movementInput.x < 0,
                            targetVector = analogAxisOfRotationInput3DVector
                        }
                    };
                    AxisValidation axisVectorValidated = DetermineAxisClosestToTarget(axisValidators, leftArm);
                    //ITransformRotation leftShoulderController = GetTransformController(BodyPart.leftShoulder, axisVectorValidated.sourceAxis, Axis.right, Axis.up, leftArm, leftHand, movementInput, transform.forward, axisVectorValidated.targetVector, leftArm.Find("LeftArm").right, Vector3.forward, 90f * movementInput.magnitude, "LeftArm");
                    //rootController = GetTransformController(BodyPart.root, Axis.forward, Axis.up, Axis.forward, transform, leftHand, Vector2.zero, transform.forward, targetDirection, Vector3.up, Vector3.forward, 60f * movementInput.magnitude, "LeftArm");
                    Vector3 defaultAxisOfRotation = new Vector3(0, 0, 0);
                    float yValue = Mathf.Abs(axisVectorValidated.targetVector.x) > Mathf.Abs(axisVectorValidated.targetVector.y) ? Mathf.Abs(axisVectorValidated.targetVector.x) : Mathf.Abs(axisVectorValidated.targetVector.y);
                    float zValue = Mathf.Abs(axisVectorValidated.targetVector.x) < Mathf.Abs(axisVectorValidated.targetVector.y) ? Mathf.Abs(axisVectorValidated.targetVector.x) : Mathf.Abs(axisVectorValidated.targetVector.y);
                    if (axisVectorValidated.sourceAxis == Axis.up)
                    {
                        defaultAxisOfRotation = axisVectorValidated.targetVector.y >= 0f ? -rootHorizontal :
                        new Vector3(0f,
                                    Mathf.Abs(axisVectorValidated.targetVector.x),
                                    Mathf.Abs(axisVectorValidated.targetVector.y));
                    }
                    else if (axisVectorValidated.sourceAxis == Axis.forward) {
                        defaultAxisOfRotation = axisVectorValidated.targetVector;
                    }
                    //ITransformRotation leftArmController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, expectedForward, -leftArm.Find("LeftArm").right, analogAxisOfRotationInput3DVector, 30f * movementInput.magnitude, "LeftArm");
                    targetDirection = new Vector3(movementInput.x, 0f, movementInput.y).normalized;
                    Vector3 tForLeftArm;
                    float cRadius = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude;
                    Vector3 cursorPointThisFrame = cursorPoint;
                    cursorPoint += new Vector3(movementInput.x, movementInput.y  * 2f, movementInput.y).normalized * cRadius;
                    //Vector3 expectedPosition = leftHand.Find("LeftPalmCatchArea").position + new Vector3(movementInput.x, movementInput.y, Mathf.Sign(movementInput.y) * Mathf.Abs(movementInput.x)).normalized * cRadius;
                    //cursorSpeed += Time.deltaTime;
                    //float aimSpeed = Mathf.Min(1f, cursorSpeed);
                    float xVal = Mathf.Abs(movementInput.x) > 0.01f ? Mathf.Pow(movementInput.x, 2) * cRadius : 0f;
                    float yVal = Mathf.Abs(movementInput.y) > 0.01f ? Mathf.Pow(movementInput.y, 2) * cRadius : 0f;
                    float zVal = Mathf.Abs(movementInput.x) > 0.01f ? Mathf.Pow(movementInput.x, 2) * cRadius : 0f;
 
                    //Vector3 expectedPosition = leftHand.Find("LeftPalmCatchArea").position + new Vector3(xVal, yVal, zVal).normalized * cRadius;
                    Vector3 expectedPosition = leftHand.Find("LeftPalmCatchArea").position + new Vector3(xVal, yVal, zVal);
                    Vector3 armPositionHorizontal = new Vector3(leftArm.Find("LeftArm").position.x, leftHand.Find("LeftPalmCatchArea").position.y, leftArm.Find("LeftArm").position.z);
                    //Vector3 expectedPosition = cursorPoint;
                    Vector3 tBefore = leftHand.Find("LeftPalmCatchArea").position + -leftArm.Find("LeftArm").right * cRadius;
                    //tForLeftArm = (expectedPosition - leftArm.Find("LeftArm").position).normalized * cRadius;
                    //tForLeftArm = (expectedPosition - leftArm.Find("LeftArm").position).normalized;


                    //tForLeftArm = inputVector.normalized;


                    //tForLeftArm = (expectedPosition - armPositionHorizontal).normalized * cRadius;
                    Vector3 tDirBefore = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).normalized * cRadius;
                    bool isRotatingUpForward = false;
                    bool isRotatingUpBack = false;
                    //WE ARE ROTATING BACK
                    if (Mathf.Abs(movementInput.y) >= Mathf.Abs(movementInputFromLastFrame.y) && tBefore.z >= expectedPosition.z)
                    {

                    }

                    if (movementInput.y >= 0)
                    {
                        //tForLeftArm = -leftArm.Find("LeftArm").right + new Vector3(movementInput.x, movementInput.y, 0f).normalized;
                        cRadius = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude;
                        cursorPointThisFrame = cursorPoint;
                        cursorPoint += new Vector3(movementInput.x, movementInput.y, movementInput.y).normalized * cRadius;
                        float minForwardValue = Math.Abs(movementInput.x) > Math.Abs(movementInput.y) ? movementInput.y : Mathf.Sign(movementInput.y) * Math.Abs(movementInput.x);
                        expectedPosition = leftHand.Find("LeftPalmCatchArea").position + new Vector3(movementInput.x, movementInput.y, minForwardValue).normalized * cRadius;
                        //tForLeftArm = (expectedPosition - leftArm.Find("LeftArm").position).normalized * cRadius;
                        tForLeftArm = (expectedPosition - leftHand.Find("LeftPalmCatchArea").position).normalized;
                        //tForLeftArm = ((leftHand.Find("LeftPalmCatchArea").position + -leftHand.forward * 60f) - leftHand.Find("LeftPalmCatchArea").position).normalized;
                    }
                    else
                    {
                        expectedPosition = leftHand.Find("LeftPalmCatchArea").position + new Vector3(movementInput.x, 0, movementInput.y).normalized * cRadius;
                        tForLeftArm = (expectedPosition - leftHand.Find("LeftPalmCatchArea").position).normalized;
                        //tForLeftArm = -leftArm.Find("LeftArm").right + new Vector3(movementInput.x, 0f, movementInput.y).normalized;
                    }
                    // Stick -> left arm target, in front of the WR (see GetLeftArmCatchTarget). Overrides the two branches above.
                    tForLeftArm = GetLeftArmCatchTarget(movementInput, l2TriggerPressed && yButtonPressed);
                    tForLeftArm = ApplyAimAssist(tForLeftArm);
                    List<KeyValuePair<Axis, float>> instructions = new List<KeyValuePair<Axis, float>>();


                    /*if (movementInput.y >= 0)
                    {
                        //tForLeftArm = -leftArm.Find("LeftArm").right + new Vector3(movementInput.x, movementInput.y, 0f).normalized;
                        cRadius = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude;
                        cursorPointThisFrame = cursorPoint;
                        cursorPoint += new Vector3(movementInput.x, movementInput.y, movementInput.y).normalized * cRadius;
                        expectedPosition = leftHand.Find("LeftPalmCatchArea").position + new Vector3(movementInput.x, movementInput.y, movementInput.y).normalized * cRadius;
                        tForLeftArm = (expectedPosition - leftArm.Find("LeftArm").position).normalized * cRadius;
                        //tForLeftArm = ((leftHand.Find("LeftPalmCatchArea").position + -leftHand.forward * 60f) - leftHand.Find("LeftPalmCatchArea").position).normalized;
                    }
                    else {
                        tForLeftArm = -leftArm.Find("LeftArm").right + new Vector3(movementInput.x, 0f, movementInput.y).normalized;
                    }*/

                    /*if (movementInput.y >= 0)
                    {
                        tForLeftArm = -leftArm.right + new Vector3(movementInput.x, movementInput.y, 0f).normalized;
                    }
                    else
                    {
                        tForLeftArm = -leftArm.right + new Vector3(movementInput.x, 0f, movementInput.y).normalized;
                    }*/
                    Vector3 diffDirection = footballInstance.transform.position - leftArm.Find("LeftArm").position;
                    KeyValuePair<int, Vector3> closestPoint = GetClosestPoint(pointsOfTrajectory, leftArm, footballInstance.transform, throwPoint);
                    int index = closestPoint.Key;
                    Vector3 trajectoryDirection = index >= 0 ? pointsOfTrajectory[index + 1] - pointsOfTrajectory[index] : Vector3.zero;
                    //Vector3 trajectoryDirectionArm = (closestPoint.Value - leftArm.Find("LeftArm").position).normalized;
                    Vector3 trajectoryDirectionArm = throwTargetPosition.magnitude > 0 ? (throwTargetPosition - leftArm.Find("LeftArm").position).normalized : tForLeftArm;
                    Vector3 trajectoryRight = Vector3.Cross(Vector3.up, trajectoryDirectionArm).normalized;
                    Vector3 trajectoryInverseRight = Vector3.Cross(-Vector3.up, trajectoryDirectionArm).normalized;
                    Vector3 trajectoryUp = Vector3.Cross(trajectoryDirectionArm, trajectoryRight).normalized;
                    Vector3 trajectoryDown = Vector3.Cross(trajectoryDirectionArm, trajectoryInverseRight).normalized;
                    float trajectoryArmAngle = GetLeverArmAngle(new Vector2(trajectoryDirection.x, trajectoryDirection.z).normalized);
                    Quadrant trajQuadrant = GetQuadrant(trajectoryArmAngle);
                    ////RETRIEVE 3D VECTOR FOR trajectory Arm Angle AXIS OF ROTATION
                    float trajectoryArmAngleAxisOfRotation = trajQuadrant == Quadrant.quadrant1 ? GetUnitCircleEulerAngle(trajectoryArmAngle + 90) : trajQuadrant == Quadrant.quadrant2 ? GetUnitCircleEulerAngle(trajectoryArmAngle - 90) : trajQuadrant == Quadrant.quadrant3 ? GetUnitCircleEulerAngle(trajectoryArmAngle + 90) : trajQuadrant == Quadrant.quadrant4 ? GetUnitCircleEulerAngle(trajectoryArmAngle - 90) : float.MinValue;
                    Vector2 trajectoryArmAngleInput = GetAngleCoordinates(trajectoryArmAngleAxisOfRotation);

                    float throwDirectionArmAngle = GetLeverArmAngle(new Vector2(throwDirection.x, throwDirection.z).normalized);
                    float throwDirectionPerp = trajQuadrant == Quadrant.quadrant1 ? GetUnitCircleEulerAngle(throwDirectionArmAngle + 90) : trajQuadrant == Quadrant.quadrant2 ? GetUnitCircleEulerAngle(throwDirectionArmAngle - 90) : trajQuadrant == Quadrant.quadrant3 ? GetUnitCircleEulerAngle(throwDirectionArmAngle + 90) : trajQuadrant == Quadrant.quadrant4 ? GetUnitCircleEulerAngle(throwDirectionArmAngle - 90) : float.MinValue;
                    Vector2 throwDirectionPerpendicularInput = GetAngleCoordinates(throwDirectionPerp);
                    float trajectoryArmAngleSign = trajectoryArmAngleInput.y >= 0 ? 1f : -1f;
                    Vector3 throwDrectionAxisOfRotationInput3DVector = DetermineInputVector3D(throwDirectionPerpendicularInput, 1f);
                    Vector3 trajectoryArmAxisOfRotationInput3DVector = DetermineInputVector3D(trajectoryArmAngleInput, trajectoryArmAngleSign);
                    //Vector3 projectedVec = throwDirection.magnitude > 0 ? Vector3.ProjectOnPlane(tForLeftArm, throwDirection) : tForLeftArm;
                    //Vector3 projectedAnalogInput = trajectoryDirection.magnitude > 0 ? Vector3.ProjectOnPlane(tForLeftArm, trajectoryDirection) : tForLeftArm;
                    Vector3 projectedAnalogInput = throwTargetPosition.magnitude > 0 ? Vector3.ProjectOnPlane(tForLeftArm, trajectoryUp.normalized) : tForLeftArm;
                    Vector3 offset = trajectoryInverseRight * movementInput.x + trajectoryDown * movementInput.y;
                    //float radius = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude;
                    float radius = (throwTargetPosition - leftHand.Find("LeftPalmCatchArea").position).magnitude;
                    Vector3 horizontalInput = new Vector3(projectedAnalogInput.x, 0f, projectedAnalogInput.z);
                    //float radius = (closestPoint.Value - leftHand.Find("LeftPalmCatchArea").position).magnitude;
                    //Vector3 desiredPosition = throwTargetPosition + projectedAnalogInput * radius;
          
                    //desiredPosition = closestPoint.Value + offset * radius;
                    desiredPosition = throwTargetPosition + projectedAnalogInput * radius;
                    positionTowardsCatchTarget = desiredPosition;
                    Vector3 desiredDirection = throwTargetPosition.magnitude > 0 ? desiredPosition - leftArm.Find("LeftArm").position : tForLeftArm;

                    //ITransformRotation leftArmController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, inputVectorRootOffset, -leftArm.Find("LeftArm").right, defaultAxisOfRotation, 90f * movementInput.magnitude, "LeftArm");
                    //ITransformRotation leftArmController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, tForLeftArm, -leftArm.Find("LeftArm").right, defaultAxisOfRotation, 135f * movementInput.magnitude, "LeftArm");
                    //ITransformRotation leftShoulderController = GetTransformController(BodyPart.leftShoulder, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm, leftHand, movementInput, transform.forward, desiredDirection, leftArm.right, trajectoryRight, 60f * movementInput.magnitude, "LeftArm");
                    //ITransformRotation leftShoulderControllerRoot = GetTransformController(BodyPart.root, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm, leftHand, movementInput, transform.forward, trajectoryUp, leftArm.right, trajectoryRight, 60f * movementInput.magnitude, "LeftArm");
                    Vector3 targetTwist = movementInput.x <= 0.01f ? trajectoryRight : trajectoryInverseRight;
                    //float xValue = Mathf.Abs(analogAxisOfRotationInput3DVector.x) >= Mathf.Sqrt(2f) / 2f ? analogAxisOfRotationInput3DVector.y : analogAxisOfRotationInput3DVector.x;
                    float xValue = analogAxisOfRotationInput3DVector.x;
                    float zCompValue = Mathf.Abs(analogAxisOfRotationInput3DVector.x) >= Mathf.Abs(analogAxisOfRotationInput3DVector.y) ? Mathf.Abs(analogAxisOfRotationInput3DVector.x) : Mathf.Abs(analogAxisOfRotationInput3DVector.y);
                    float xValueNeg = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y) ? Mathf.Sign(movementInput.x) * Mathf.Abs(movementInput.y) : movementInput.x;
                    float yValuePos = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y) ? Mathf.Abs(movementInput.y) : Mathf.Sign(movementInput.y) * Mathf.Abs(movementInput.x);
                    float yCompValueNeg = Mathf.Abs(movementInput.y);
                    float zCompValueNeg = Mathf.Abs(movementInput.x);
                    Vector3 horizontalAnalogAxisOfRotationVector = Vector3.forward;
                    bool xMagGreater = Mathf.Abs(movementInput.x) > Mathf.Abs(movementInput.y) ? true : false;
                    bool xMagEnough = Mathf.Abs(movementInput.normalized.x) >= 1f / 2f ? true : false;
                    //Vector3 negativeAnalogAxisYMagGreater = movementInput.x >= 0 ? Vector3.right : -Vector3.right;
                    Vector3 negativeAnalogAxisYMagGreater = Vector3.forward;
                    //Vector3 negativeAnalogAxisOfRotationVector = xMagGreater ? negativeAnalogAxisYMagGreater : new Vector3(xValueNeg, yCompValueNeg, zCompValueNeg);
                    Vector3 negativeAnalogAxisOfRotationVector = xMagEnough ? negativeAnalogAxisYMagGreater : new Vector3(xValueNeg, yCompValueNeg, zCompValueNeg);
                    targetTwist = movementInput.y <= 0 ? negativeAnalogAxisOfRotationVector : horizontalAnalogAxisOfRotationVector;
                    // Hand orientation from the arm target (low targets keep the twist above).
                    targetTwist = GetLeftArmTwistTarget(tForLeftArm, targetTwist);
                    float horizontalSign =  transform.forward.x >= 0f ? 1f : -1f;
                    float counterClockwiseSign = -1f;
                    bool isOverTopForward = currentLeftArmRotation.gonePastForwardTopLimit;
                    float verticalRotationRight = currentLeftArmRotation.verticalRotationRight;
                    currentLeftArmRotation = GetPlaneViewModel(leftArm.Find("LeftArm").up, -leftArm.Find("LeftArm").right, tForLeftArm, counterClockwiseSign, currentLeftArmRotation.horizontalRotation, currentLeftArmRotation.verticalRotation, currentLeftArmRotation.currentLeftRotation, currentLeftArmRotation.currentRightRotation, currentLeftArmRotation.currentUpRotation, currentLeftArmRotation.currentDownRotation, maxHorizontalRotationClockwise, maxHorizontalRotationCounterClockwise, maxVerticalRotationClockwise, maxVerticalRotationCounterClockwise);
                    PopulateForwardOverTop(currentLeftArmRotation, isOverTopForward);
                    PopulateRotationRight(currentLeftArmRotation, verticalRotationRight);
                    PopulateConstraintsRight(currentLeftArmRotation, maxVerticalRotationClockwiseRight, maxVerticalRotationCounterClockwiseRight);
                    aimSpeed += Time.deltaTime;
                    aimSpeed = Mathf.Clamp01(aimSpeed);
                    //ITransformRotation leftArmController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, desiredDirection, -leftArm.Find("LeftArm").right, throwDrectionAxisOfRotationInput3DVector, 60f * movementInput.magnitude, "LeftArm");
                    ITransformRotation leftArmController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), transform, leftHand, movementInput, transform.forward, tForLeftArm, -leftArm.Find("LeftArm").right, targetTwist, 30f * aimSpeed * Mathf.Pow(movementInput.magnitude, 3f) , "LeftArm", currentLeftArmRotation);
                    //ITransformRotation leftArmController = GetTransformController(BodyPart.root, Axis.forward, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, targetTwist, -leftArm.Find("LeftArm").right, targetTwist, 60f * movementInput.magnitude, "LeftArm");
                    //ITransformRotation leftArmControllerRoot = GetTransformController(BodyPart.root, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, desiredDirection, trajectoryUp, desiredDirection, 60f * movementInput.magnitude, "LeftArm");

                    //leftArmController = GetTransformController(BodyPart.root, Axis.up, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, inputVectorRootOffset, -leftArm.Find("LeftArm").right, defaultAxisOfRotation, 90f * movementInput.magnitude, "LeftArm");
                    //ITransformRotation leftArmController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, expectedForward, -leftArm.Find("LeftArm").right, defaultAxisOfRotation, 30f * movementInput.magnitude, "LeftArm");
                    Debug.DrawRay(leftArm.Find("LeftArm").position, trajectoryDown * (throwTargetPosition - leftArm.Find("LeftArm").position).magnitude, Color.white);
                    Debug.DrawRay(leftArm.Find("LeftArm").position, desiredDirection * (throwTargetPosition - leftArm.Find("LeftArm").position).magnitude, Color.blue);
                    Debug.DrawRay(leftArm.Find("LeftArm").position, trajectoryDirectionArm * (throwTargetPosition - leftArm.Find("LeftArm").position).magnitude, Color.yellow);
                    //Debug.DrawRay(leftArm.Find("LeftArm").position, trajectoryDirectionArm * (throwTargetPosition - leftArm.Find("LeftArm").position).magnitude, Color.yellow);
                    Debug.DrawRay(leftArm.Find("LeftArm").position, trajectoryInverseRight * (throwTargetPosition - leftArm.Find("LeftArm").position).magnitude, Color.black);
                    bool aligned = false;
                    bool useUp = axisVectorValidated.sourceAxis == Axis.up ? true : false;
                    Vector3 offsetVector = new Vector3(0, 0, 0);
                    Vector3 palmCatchArea = Vector3.zero;
                    //if (isCatching && movementInput.magnitude > 0)
                    if (yButtonPressed && movementInput.magnitude > 0)
                    {
                        bool useLeftHandVertical = false;
                        Vector3 throwPos = leftArm.Find("LeftArm").position;
                        if (
                            !useUp)
                        {
                            rotationPivotInPlace = false;
                            rotateWithVector3Forward = true;
                            Vector3 currentUp = leftArm.forward;
                            Quaternion rotation = Quaternion.FromToRotation(currentUp, analogAxisOfRotationInput3DVector);
                            Quaternion fQ = rotation * leftArm.rotation;
                            Vector3 fForward2 = fQ * Vector3.forward;
                            if (Mathf.Abs(fForward2.normalized.x - analogAxisOfRotationInput3DVector.normalized.x) <= 0.01f && Mathf.Abs(fForward2.normalized.y - analogAxisOfRotationInput3DVector.normalized.y) <= 0.01f && Mathf.Abs(fForward2.normalized.z - analogAxisOfRotationInput3DVector.normalized.z) <= 0.01f)
                            {
                                //Vector3 tVector = new Vector3(Mathf.Abs(inputVector.x) > 0.01f ? inputVector.x : 0f, Mathf.Abs(inputVector.y) <= Mathf.Abs(inputVector.x) ? inputVector.y : Mathf.Sign(inputVector.y) * Mathf.Abs(inputVector.x), Mathf.Abs(inputVector.y) > 0.01f ? inputVector.y : 0f);
                                //LEFT ARM
                                List<KeyValuePair<string, bool>> leftShoulderRotationOutcomes = AlignShoulderAxis(axisVectorValidated.sourceAxis, leftArm, leftHand, inputVector, analogAxisOfRotationInput3DVector, 30f * movementInput.magnitude, "LeftArm");
                                List<KeyValuePair<string, bool>> leftArmRotationOutcomes = AlignArmAxisNew(Axis.inverseRight, leftArm.Find("LeftArm"), leftHand, movementInput, inputVector, analogAxisOfRotationInput3DVector, transform.forward, 30f * movementInput.magnitude);
                                List<KeyValuePair<string, bool>> leftArmRotationOutcomesXAxis = AlignArmAxisNew(Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, inputVector, analogAxisOfRotationInput3DVector, transform.forward, 30f * movementInput.magnitude);
                                //offsetVector = GetThrowPoint(transform.forward, inputVector, alteredTVector, movementInput);
                                offsetVector = expectedForward;
                                useLeftHandVertical = inputVector.y < 0 ? true : false;
                                MoveToPoint(leftArm.Find("LeftArm"), leftArm.Find("LeftArm").position, leftArm.position, 0.75f);
                                //RIGHT ARM
                                float scaler = 0.5f;
                                Vector3 tRightPoint = leftHand.position - (leftHand.forward * scaler);
                                palmCatchArea = Vector3.zero;
                                if (leftHand != null && leftHand.Find("LeftPalmCatchArea") != null && leftHand.Find("LeftPalmCatchArea").GetChild(0) != null)
                                {
                                    palmCatchArea = leftHand.Find("LeftPalmCatchArea").GetChild(0).position;
                                }
                                List<KeyValuePair<string, bool>> rightShoulderMovementOutcomes = MoveToAlignWith(caughtFootball, rightArm, leftArm, leftHand.forward, "RightArm", scaler);
                                List<KeyValuePair<string, bool>> rightShoulderRotationOutcomes = AlignWithOtherAxis(caughtFootball, Axis.inverseRight, Axis.forward, rightArm, leftArm, tRightPoint - rightArm.Find("RightArm").position, palmCatchArea, 30f, "RightArm", "LeftArm");
                            }
                            else
                            {
                                aligned = false;
                            }
                        }
                        else
                        {
                            rotationPivotInPlace = false;
                            rotateWithVector3Forward = false;
                            Vector3 currentUp = leftArm.up;
                            Quaternion rotation = Quaternion.FromToRotation(currentUp, analogAxisOfRotationInput3DVector);
                            
                            Quaternion fQ = rotation * leftArm.rotation;
                            Vector3 fUp = fQ * Vector3.up;
                            if (Mathf.Abs(fUp.normalized.x - analogAxisOfRotationInput3DVector.normalized.x) <= 0.01f && Mathf.Abs(fUp.normalized.y - analogAxisOfRotationInput3DVector.normalized.y) <= 0.01f && Mathf.Abs(fUp.normalized.z - analogAxisOfRotationInput3DVector.normalized.z) <= 0.01f)
                            {
                                float tSign = 0f;
                                bool useXComponent = Mathf.Abs(movementInput.x) >= Mathf.Abs(movementInput.y);
                                float sign = useXComponent ? Mathf.Sign(leftArm.Find("LeftArm").up.y) : Mathf.Sign(leftArm.Find("LeftArm").up.x);
                                #region DETERMINE SIGN
                                if (useXComponent)
                                {
                                    if (sign >= 0)
                                    {
                                        tSign = movementInput.y >= 0 ? -1f : 1f;
                                    }
                                    else
                                    {
                                        tSign = movementInput.y >= 0 ? 1f : -1f;
                                    }
                                }
                                else
                                {
                                    if (sign >= 0)
                                    {
                                        tSign = movementInput.x >= 0 ? -1f : 1f;
                                    }
                                    else
                                    {
                                        tSign = movementInput.x >= 0 ? 1f : -1f;
                                    }
                                }
                                #endregion


                                //ROTATE THE LEFT ARM
                                List<KeyValuePair<string, bool>> leftArmRotationValidation = new List<KeyValuePair<string, bool>>();
                                if (!currentlyRotatingAroundAxis) {
                                    //leftArmRotationValidation = leftArmController.AlignTransformAxis();
                                    if (transform.name.ToLower().Contains("wrb")) {
                                        int h = 0;
                                    }
                                    Vector3 targetCheck = Vector3.zero;
                                    float zCheckValue = 0f;
                                    if (movementInput.y >= 0)
                                    {
                                        zCheckValue = Mathf.Abs(movementInput.x) >= Mathf.Abs(movementInput.y) ? movementInput.y : Mathf.Sign(movementInput.y) * Mathf.Abs(movementInput.x);
                                        targetCheck = new Vector3(movementInput.x, movementInput.y, zCheckValue);
                                    }
                                    else {
                                        zCheckValue = movementInput.y;
                                        //zCheckValue = Mathf.Abs(movementInput.x) <= Mathf.Abs(movementInput.y) ? movementInput.y : Mathf.Sign(movementInput.y) * Mathf.Abs(movementInput.x);
                                        targetCheck = new Vector3(movementInput.x, 0f, movementInput.y);
                                    }
                                    currentLeftArmRotation = leftArmController.AlignWithCheck(targetCheck);
                                    //leftArmRotationValidation = leftArmController.AlignTransformAxis();
                                    if (transform.name.ToLower().Contains("wrb"))
                                    {
                                        int h = 0;
                                    }
                                    //RotateTowardsTargetWithCheck
                                    float rightValue = currentLeftArmRotation.currentRightRotation;
                                    float leftValue = currentLeftArmRotation.currentLeftRotation;
                                    float upValue = currentLeftArmRotation.currentUpRotation;
                                    float downValue = currentLeftArmRotation.currentDownRotation;
                                    //currentLeftArmRotation = GetPlaneViewModel(leftArm.Find("LeftArm").up, -leftArm.Find("LeftArm").right, tForLeftArm, counterClockwiseSign, rightValue, leftValue, upValue, downValue, maxHorizontalRotationClockwise, maxHorizontalRotationCounterClockwise, maxVerticalRotationClockwise, maxVerticalRotationCounterClockwise);
                                    //tForLeftArm = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).normalized * cRadius;
                                    //tForLeftArm = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).normalized * cRadius;
                                    tForLeftArm = (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).normalized;
                                    Vector3 tDirAfter = tForLeftArm;
                                    if (Vector3.Distance(tDirBefore, tDirAfter) <= 0.1f)
                                    {
                                        //currentlyRotatingAroundAxis = true;
                                    }
                                }

                                /*if (currentlyRotatingAroundAxis) {
                                    Vector3 perpendicularVector = Vector3.Cross(Vector3.up, tForLeftArm).normalized;
                                    float rAmount = 0f;
                                    if (leftArm.Find("LeftArm").up.x < 0 && leftArm.Find("LeftArm").up.z >= 0) {
                                       
                                        if (movementInput.y >= 0f)
                                        {
                                            //rAmount = 45f;
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? 45f : -45f;
                                        }
                                        else
                                        {
                                            //rAmount = -45f;
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? -45f : 45f;
                                        }
                                    }
                                    else if (leftArm.Find("LeftArm").up.x >= 0 && leftArm.Find("LeftArm").up.z >= 0)
                                    {
                                        if (movementInput.y >= 0f)
                                        {
                                            //rAmount = -45f;
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? -45f : 45f;
                                        }
                                        else {
                                            rAmount = 45f;
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? 45f : -45f;
                                        }
                                        
                                    }
                                    else if (leftArm.Find("LeftArm").up.x < 0 && leftArm.Find("LeftArm").up.z < 0)
                                    {
                                        if (movementInput.y >= 0f)
                                        {
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? 45f : -45f;
                                            //rAmount = 45f;
                                        }
                                        else
                                        {
                                            //rAmount = -45f;
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? -45f : 45f;
                                        }
                                    }
                                    else if (leftArm.Find("LeftArm").up.x >= 0 && leftArm.Find("LeftArm").up.z < 0)
                                    {
                                        if (movementInput.y >= 0f)
                                        {
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? -45f : 45f;
                                        }
                                        else
                                        {
                                            rAmount = -leftArm.Find("LeftArm").right.z < 0f ? 45f : -45f;
                                        }
                                    }
                                    Vector3 armDirectionBefore = -leftArm.Find("LeftArm").right;
                                    leftArm.Find("LeftArm").Rotate(leftArm.Find("LeftArm").up, rAmount, Space.World);
                                    Vector3 diffInDir = (armDirectionBefore - -leftArm.Find("LeftArm").right).normalized;
                                    cursorPoint += new Vector3(diffInDir.x, diffInDir.y * 2f, diffInDir.z).normalized * cRadius;
                                }*/


                                offsetVector = inputVectorRootOffset;
                                useLeftHandVertical = inputVector.y < 0 ? true : false;
                                MoveToPoint(leftArm.Find("LeftArm"), leftArm.Find("LeftArm").position, leftArm.position, 1f);

                                /*Vector3 targetCatchDirection = Vector3.zero;
                                if (footballSpot.magnitude > 0)
                                {
                                    targetCatchDirection = throwTargetPosition - leftArm.Find("LeftArm").position;
                                    if (targetCatchDirection.magnitude < 20f) {
                                        rootController = GetTransformController(BodyPart.root, Axis.forward, Axis.up, Axis.forward, transform, leftHand, Vector2.zero, transform.forward, targetCatchDirection, Vector3.up, Vector3.forward, 60f * movementInput.magnitude, "LeftArm");
                                        rootRotation = rootController.AlignTransformAxis();

                                        leftArmController = GetTransformController(BodyPart.leftArm, Axis.inverseRight, Axis.inverseRight, Axis.forward, leftArm.Find("LeftArm"), leftHand, movementInput, transform.forward, targetCatchDirection, -leftArm.Find("LeftArm").right, defaultAxisOfRotation, 90f * movementInput.magnitude, "LeftArm");
                                        leftArmRotation = leftArmController.AlignTransformAxis();
                                    }

                                }*/
                                //RIGHT ARM
                                float scaler = 0.5f * (20f / 3f); ;
                                Vector3 tRightPoint = leftHand.position - (leftHand.forward * scaler);
                                palmCatchArea = Vector3.zero;
                                if (leftHand != null && leftHand.Find("LeftPalmCatchArea") != null && leftHand.Find("LeftPalmCatchArea").GetChild(0) != null)
                                {
                                    palmCatchArea = leftHand.Find("LeftPalmCatchArea").GetChild(0).position;
                                }
                                List<KeyValuePair<string, bool>> rightShoulderMovementOutcomes = MoveToAlignWith(caughtFootball, rightArm, leftArm, leftHand.forward, "RightArm", scaler);
                                List<KeyValuePair<string, bool>> rightShoulderRotationOutcomes = AlignWithOtherAxis(caughtFootball, Axis.inverseRight, Axis.forward, rightArm, leftArm, tRightPoint - rightArm.Find("RightArm").position, palmCatchArea, 10f, "RightArm", "LeftArm");


                            }
                            else
                            {
                                int h = 0;
                                aligned = false;
                            }
                        }
                        //OUR ROTATION AXIS
                        //Debug.DrawRay(leftArm.position, analogAxisOfRotationInput3DVector * 50f, Color.green);
                        Vector3 armTarget2 = new Vector3(0, 0, 0);
                        Vector3 expectedTarget = Vector3.zero;
                        if (leftHand != null && leftHand.Find("LeftPalmCatchArea") != null && leftHand.Find("LeftPalmCatchArea").GetChild(0) != null)
                        {
                            palmCatchArea = leftHand.Find("LeftPalmCatchArea").GetChild(0).position;
                            if (expectedTarget.magnitude <= 0 && QB.GetComponent<QB>().GetIsThrown()) {
                                //expectedTarget = PredictFuturePosition(QB.GetComponent<QB>().GetTimeToCatch(), GetComponent<Rigidbody>(), leftHand.Find("LeftPalmCatchArea").GetChild(0));
                                //expectedTarget = GetExpectedTarget(transform, leftHand.Find("LeftPalmCatchArea").GetChild(0), expectedTarget);
                            }
                            //expectedTarget = PredictFuturePosition()
                        }

                        if (expectedTarget.magnitude > 0)
                        {
                            armTarget2 = expectedTarget;
                            
                        }
                        else {
                            if (useLeftHandVertical)
                            {
                                //armTarget2 = new Vector3(throwPos.x, leftHand.Find("LeftPalmCatchArea").GetChild(0).position.y, throwPos.z) + (new Vector3(offsetVector.x, 0f, offsetVector.z).normalized * (leftHand.Find("LeftPalmCatchArea").GetChild(0).position - leftArm.Find("LeftArm").position).magnitude);
                                armTarget2 = leftHand.Find("LeftPalmCatchArea").position;
                            }
                            else
                            {
                                armTarget2 = leftHand.Find("LeftPalmCatchArea").position;
                                //armTarget2 = throwPos + (offsetVector.normalized * (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude);
                            }
                        }

                        
                        //armTarget2 = armTarget2 + (-leftHand.forward * ((leftHand.position - rightHand.position).magnitude / 2f));

                        /*Debug.DrawRay(leftArm.position, analogAxisOfRotationInput3DVector * 50f, Color.green);
                        Vector3 armTarget2 = new Vector3(0, 0, 0);

                        if (useLeftHandVertical)
                        {
                            armTarget2 = new Vector3(throwPos.x, leftHand.Find("LeftPalmCatchArea").position.y, throwPos.z) + (new Vector3(offsetVector.x, 0f, offsetVector.z).normalized * (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude);
                        }
                        else
                        {
                            armTarget2 = throwPos + (offsetVector.normalized * (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude);
                        }
                        Debug.DrawRay(throwPos, armTarget2 - throwPos, Color.black);
                        armTarget2 = armTarget2 + (-leftHand.forward * ((leftHand.position - rightHand.position).magnitude / 2f));*/
                        //Debug.DrawRay(throwPos, armTarget2 - throwPos, Color.red);
                        if (canSetThrowTarget)
                        {
                            //throwTargetPosition = armTarget2;
                        }
                        if (rotateWithVector3Forward && aligned)
                        {
                            rotationPivotInPlace = true;
                            float signOfAngle = 0f;
                            Vector3 flatStartDir = new Vector3(analogAxisOfRotationInput3DVector.x, 0, analogAxisOfRotationInput3DVector.z).normalized;
                            Quaternion rotationForwardToPivot = Quaternion.AngleAxis(Mathf.Sign(signOfAngle) * movementInput.magnitude * 10f, leftArm.forward);
                            Vector3 rotationDirection = new Vector3(-inputVector.x, 0, -inputVector.z).normalized;
                            Quaternion armRotation = Quaternion.AngleAxis(-movementInput.magnitude * 10f, leftArm.forward);
                            Vector3 rotatedDir = armRotation * rotationDirection;
                            print("Before Aligned Rotation: " + flatStartDir);
                            if (Mathf.Abs(movementInput.y) > Mathf.Abs(movementInput.x))
                            {
                                if (rotatedDir.y > flatStartDir.y)
                                {
                                    signOfAngle = -1f;
                                }
                                else
                                {
                                    signOfAngle = 1f;
                                }
                            }
                            else
                            {
                                if (movementInput.x >= 0)
                                {
                                    if (leftArm.forward.y >= 0)
                                    {
                                        signOfAngle = -1f;
                                    }
                                    else
                                    {
                                        signOfAngle = 1f;
                                    }
                                }
                                else
                                {
                                    if (leftArm.forward.y >= 0)
                                    {
                                        signOfAngle = 1f;
                                    }
                                    else
                                    {
                                        signOfAngle = -1f;
                                    }

                                }

                            }

                            leftArm.Rotate(leftArm.forward, Mathf.Sign(signOfAngle) * movementInput.magnitude * 50f, Space.World);
                            rotationPivotInPlace = true;


                            //WR TO THE RIGHT OF QB, AND QB IS POINTED IN NEGATIVE Z DIRECTION WORLD SPACE
                            //IF LEFT ARM HAS A POSITIVE X VALUE THEN ITS SIGN DOESN'T ALIGN WITH LEFT ARMS NATURAL SIGN OF NEGATIVE (POINTING INWARD), SO WE NEED TO FLIP
                            if ((leftArm.Find("LeftArm").forward.y < 0 || Mathf.Abs(leftArm.forward.y) < 0.1f) &&
                                (Mathf.Abs(leftArm.forward.x) < 0.1f || (Mathf.Sign(leftArm.forward.x) >= 0 && Mathf.Sign(leftArm.forward.x) == Mathf.Sign(leftArm.Find("LeftArm").forward.x)) ||
                                    (Mathf.Abs(leftArm.forward.x) < 0.1f || (Mathf.Sign(leftArm.forward.x) < 0 && Mathf.Sign(leftArm.forward.x) != Mathf.Sign(leftArm.Find("LeftArm").forward.x)))))

                            {
                                leftArm.Find("LeftArm").Rotate(Vector3.right, 180, Space.Self);
                            }
                        }
                        else if (!rotateWithVector3Forward && aligned)
                        {
                            float signOfAngle = 0f;
                            rotationPivotInPlace = true;
                            Vector3 flatStartDir = new Vector3(analogAxisOfRotationInput3DVector.x, 0, analogAxisOfRotationInput3DVector.z).normalized;
                            Vector3 rotationDirection = new Vector3(-inputVector.x, 0, -inputVector.z).normalized;
                            Quaternion armRotation = Quaternion.AngleAxis(-movementInput.magnitude * 10f, leftArm.up);
                            Vector3 rotatedDir = armRotation * rotationDirection;
                            if (rotatedDir.y >= flatStartDir.y)
                            {
                                signOfAngle = -1f;
                            }
                            else
                            {
                                signOfAngle = 1f;
                            }
                            leftArm.Rotate(leftArm.up, Mathf.Sign(signOfAngle) * movementInput.magnitude * 50f, Space.World);
                            Vector3 armAxis = leftArm.Find("LeftArm").right;
                            Vector3 currentDir = leftArm.Find("LeftArm").forward;
                            Vector3 currentDown = -leftArm.Find("LeftArm").up;
                            Vector3 planeNormal = armAxis;
                            Vector3 aimArmDirection;

                            //TEST
                            bool case1ToInverse = false;
                            if (useUp)
                            {
                                case1ToInverse = (analogAxisOfRotationInput3DVector.x > 0 && analogAxisOfRotationInput3DVector.y < 0);

                                if (case1ToInverse)
                                {
                                    aimArmDirection = new Vector3(-analogAxisOfRotationInput3DVector.x, -analogAxisOfRotationInput3DVector.y, -analogAxisOfRotationInput3DVector.z).normalized;
                                }
                                else
                                {
                                    aimArmDirection = new Vector3(analogAxisOfRotationInput3DVector.x, analogAxisOfRotationInput3DVector.y, analogAxisOfRotationInput3DVector.z).normalized;
                                }
                            }
                            else
                            {
                                case1ToInverse = (analogAxisOfRotationInput3DVector.x > 0 && analogAxisOfRotationInput3DVector.y < 0);
                                if (case1ToInverse)
                                {
                                    aimArmDirection = new Vector3(-analogAxisOfRotationInput3DVector.x, -analogAxisOfRotationInput3DVector.y, -analogAxisOfRotationInput3DVector.z).normalized;
                                }
                                else
                                {
                                    aimArmDirection = new Vector3(analogAxisOfRotationInput3DVector.x, analogAxisOfRotationInput3DVector.y, analogAxisOfRotationInput3DVector.z).normalized;
                                }
                            }

                            float pAngle = Vector3.SignedAngle(currentDown, aimArmDirection, planeNormal);
                            float forwAngle = Vector3.SignedAngle(currentDir, aimArmDirection, planeNormal);

                            if (useUp)
                            {
                                leftArm.Find("LeftArm").Rotate(Vector3.right, pAngle, Space.Self);
                            }
                            else
                            {
                                leftArm.Find("LeftArm").Rotate(Vector3.right, forwAngle, Space.Self);
                            }


                            if (rotateWithVector3Forward)
                            {

                                Vector3 projectedInput = Vector3.ProjectOnPlane(leftArm.Find("LeftArm").right, Vector3.up).normalized;
                                Vector3 projectCurrentDirection = Vector3.ProjectOnPlane(leftArm.forward, Vector3.up);
                                float offsetAngle = Vector3.SignedAngle(-leftArm.right, -leftArm.Find("LeftArm").right, Vector3.up);
                                Quaternion rotationForwardToPivot = Quaternion.FromToRotation(leftArm.Find("LeftArm").forward, analogAxisOfRotationInput3DVector);
                            }
                            else
                            {
                                Quaternion rotationForwardToPivot = Quaternion.FromToRotation(leftArm.Find("LeftArm").up, analogAxisOfRotationInput3DVector.y < 0 ? -analogAxisOfRotationInput3DVector : analogAxisOfRotationInput3DVector);

                                bool enoughMagnitude = false;
                                if (Mathf.Abs(leftArm.Find("LeftArm").up.x) > 0.1f)
                                {
                                    enoughMagnitude = true;
                                }
                                if (enoughMagnitude && leftArm.Find("LeftArm").up.x < 0 && analogAxisOfRotationInput3DVector.x >= 0 && analogAxisOfRotationInput3DVector.y >= 0)
                                {
                                    int c = 0;
                                    leftArm.Find("LeftArm").Rotate(Vector3.right, 180, Space.Self);
                                }
                                else if (enoughMagnitude && leftArm.Find("LeftArm").up.x < 0 && analogAxisOfRotationInput3DVector.x < 0 && analogAxisOfRotationInput3DVector.y < 0)
                                {
                                    leftArm.Find("LeftArm").Rotate(Vector3.right, 180, Space.Self);
                                }
                            }
                            Vector3 currentHandRight = leftHand.right;
                            Quaternion rotationHandRight = Quaternion.FromToRotation(currentHandRight, leftArm.Find("LeftArm").right);
                            Quaternion fQHandRight = rotationHandRight * leftHand.rotation;
                            Vector3 fHandRight = fQHandRight * Vector3.right;
                            leftHand.rotation = rotationHandRight * leftHand.rotation;
                        }
                    }
                    else if (movementInput.magnitude > 0) {
                        //expectedForward = -leftArm.Find("LeftArm").right;
                        Vector3 armTarget2 = new Vector3(0, 0, 0);
                        bool useLeftHandVertical = movementInput.y < 0f ? true : false;
                        Vector3 expectedTarget = Vector3.zero;
                        if (leftHand != null && leftHand.Find("LeftPalmCatchArea") != null && leftHand.Find("LeftPalmCatchArea").GetChild(0) != null)
                        {
                            palmCatchArea = leftHand.Find("LeftPalmCatchArea").GetChild(0).position;
                            if (expectedTarget.magnitude <= 0 && QB.GetComponent<QB>().GetIsThrown())
                            {
                                //expectedTarget = PredictFuturePosition(QB.GetComponent<QB>().GetTimeToCatch(), GetComponent<Rigidbody>(), leftHand.Find("LeftPalmCatchArea").GetChild(0));
                                //expectedTarget = GetExpectedTarget(transform, leftHand.Find("LeftPalmCatchArea").GetChild(0), expectedTarget);
                                //expectedTarget = expectedTarget + (-leftHand.forward * ((leftHand.position - rightHand.position).magnitude / 2f));
                            }
                            //expectedTarget = PredictFuturePosition()
                        }

                        if (expectedTarget.magnitude > 0)
                        {
                            armTarget2 = expectedTarget;
                            Debug.DrawRay(leftHand.Find("LeftPalmCatchArea").GetChild(0).position, armTarget2 - leftHand.Find("LeftPalmCatchArea").GetChild(0).position, Color.green);
                        }
                        else
                        {
                            if (useLeftHandVertical)
                            {
                                //armTarget2 = new Vector3(throwPos.x, leftHand.Find("LeftPalmCatchArea").GetChild(0).position.y, throwPos.z) + (new Vector3(offsetVector.x, 0f, offsetVector.z).normalized * (leftHand.Find("LeftPalmCatchArea").GetChild(0).position - leftArm.Find("LeftArm").position).magnitude);
                                armTarget2 = leftHand.Find("LeftPalmCatchArea").position;
                            }
                            else
                            {
                                armTarget2 = leftHand.Find("LeftPalmCatchArea").position;
                                //armTarget2 = throwPos + (offsetVector.normalized * (leftHand.Find("LeftPalmCatchArea").position - leftArm.Find("LeftArm").position).magnitude);
                            }

                            //Debug.DrawRay(leftArm.Find("LeftArm").position, armTarget2 - leftArm.Find("LeftArm").position, Color.black);
                            //armTarget2 = armTarget2 + (-leftHand.forward * ((leftHand.position - rightHand.position).magnitude / 2f));
                            //Debug.DrawRay(leftArm.Find("LeftArm").position, armTarget2 - leftArm.Find("LeftArm").position, Color.red);
                        }

                        if (canSetThrowTarget)
                        {
                            //QB.GetComponent<QB>().SetThrowingTargetPosition(armTarget2);
                            //throwTargetPosition = armTarget2;
                        }
                    }
                }
            }
            if (wrState == PlayerState.attemptingCatch && caughtFootball != null)
            {
                wrState = PlayerState.catchAndRun;
            }
            targetDirection = Vector3.forward;
            float rotationAmount = 0f;
            switch (wrState)
            {
                case PlayerState.catchAndRun:
                    // After the ball hits the hand colliders, turn back to the route direction (flattened onto the ground).
                    Vector3 postCatchRouteDirection = GetPostCatchRouteDirection(playerInput.actions["Catch"].ReadValue<Vector2>());
                    rootController = GetTransformController(BodyPart.root, Axis.forward, Axis.up, Axis.forward, transform, transform, leftHand, Vector2.zero, transform.forward, postCatchRouteDirection, Vector3.up, Vector3.forward, 30f, "LeftArm", new PlaneViewModel() { });
                    rootRotation = rootController.AlignTransformAxis();
                    if (rootRotation[0].Value)
                    {
                        wrState = PlayerState.running;
                    }
                    break;
                case PlayerState.attemptingCatch:
                    // Keep running while catching. The stick is aiming the arm, so only the movement assist steers,
                    // turning at most catchAssistTurnSpeed degrees per second towards where the ball comes down.
                    if (playerOnGround && rTriggerPressed)
                    {
                        Vector3 catchRunDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                        Vector3 assistedCatchRunDirection = ApplyMovementAssist(catchRunDirection);
                        if (Vector3.Angle(catchRunDirection, assistedCatchRunDirection) > 0.5f)
                        {
                            rootController = GetTransformController(BodyPart.root, Axis.forward, Axis.up, Axis.forward, transform, transform, leftHand, Vector2.zero, transform.forward, assistedCatchRunDirection, Vector3.up, Vector3.forward, catchAssistTurnSpeed * Time.deltaTime, "LeftArm", new PlaneViewModel() { });
                            rootRotation = rootController.AlignTransformAxis();
                        }
                    }
                    /*if ((isLeftStickHeldInPlace || onLeftAnalogStickTouched) && playerOnGround) {
                        Vector2 movementInput = playerInput.actions["Catch"].ReadValue<Vector2>();
                        rotationAmount = 60f * movementInput.magnitude;
                        Vector3 rootDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                        Vector3 moveDir = throwTargetPosition - transform.position;
                        targetDirection = throwDirection.magnitude > 0 ? new Vector3(moveDir.x, 0f, moveDir.z).normalized : new Vector3(movementInput.x, 0f, movementInput.y).normalized;
                        rootController = GetTransformController(BodyPart.root, Axis.forward, Axis.up, Axis.forward, transform, leftHand, Vector2.zero, transform.forward, targetDirection, Vector3.up, Vector3.forward, rotationAmount, "LeftArm");
                        rootRotation = rootController.AlignTransformAxis();
                        if (rootRotation.Count > 0 && rootRotation[0].Value)
                        {
                            wrAlignedForRun = true;
                        }
                        else
                        {
                            wrAlignedForRun = false;
                        }
                    }*/
                    break;
                case PlayerState.running:
                case PlayerState.aimingForwardDirection:
                    //ROTATE THE PLAYER
                    if ((isLeftStickHeldInPlace || onLeftAnalogStickTouched) && playerOnGround)
                    {
                        //ALIGN ROOT TO ANALOG INPUT
                        Vector2 movementInput = playerInput.actions["Catch"].ReadValue<Vector2>();
                        rotationAmount = 60f * movementInput.magnitude;
                        float originalRootDirectionAngle = GetLeverArmAngle(new Vector2(transform.forward.x, transform.forward.z).normalized);
                        float targetAngle = movementInput.x >= 0 ? originalRootDirectionAngle + -Mathf.Abs(rotationAmount) : originalRootDirectionAngle + Mathf.Abs(rotationAmount);
                        Vector2 targetCoordinates = GetAngleCoordinates(targetAngle);
                        
                        Vector3 rootDirection = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
                        Vector3 moveDir = throwTargetPosition - transform.position;
                        //targetDirection = throwTargetPosition.magnitude > 0 ? new Vector3(moveDir.x, 0f, moveDir.z).normalized : rootDirection + new Vector3(movementInput.x, 0f, movementInput.y).normalized;
                        targetDirection = rootDirection + new Vector3(movementInput.x, 0f, movementInput.y).normalized;
                        targetDirection = ApplyMovementAssist(targetDirection);
                        bool useInverse = Vector3.SignedAngle(rootDirection, targetDirection, Vector3.up) > Vector3.SignedAngle(rootDirection, targetDirection, -Vector3.up);
                        Vector3 aOR = useInverse ? -Vector3.up : Vector3.up;
                        rootController = GetTransformController(BodyPart.root, Axis.forward, Axis.up, Axis.forward, transform, transform, leftHand, Vector2.zero, transform.forward, targetDirection, Vector3.up, Vector3.forward, rotationAmount, "LeftArm", new PlaneViewModel() { });
                        rootRotation = rootController.AlignTransformAxis();

                        if (rootRotation.Count > 0 && rootRotation[0].Value)
                        {
                            wrAlignedForRun = true;
                        }
                        else
                        {
                            wrAlignedForRun = false;
                            //Vector3 velocity = gameObject.GetComponent<Rigidbody>().velocity;
                            //gameObject.GetComponent<Rigidbody>().AddForce(-velocity, ForceMode.Impulse);
                        }
                    }
                    break;
            }

            if (caughtFootball != null) {
                //bool movedCamera = MoveToPointPerFrame(mainCamera.transform, mainCamera.transform.position, new Vector3(transform.position.x, mainCamera.transform.position.y, transform.position.z) - new Vector3(0f, 0f, 17f), 3f, cameraSetAfterCatch ? true : false);
                /*if (movedCamera)
                {
                    cameraSetAfterCatch = true;
                }*/
                ITransformRotation mainCameraController = GetTransformController(BodyPart.root, Axis.forward, Axis.up, Axis.forward, mainCamera.transform, mainCamera.transform, leftHand, Vector2.zero, mainCamera.transform.forward, Vector3.forward, Vector3.up, Vector3.forward, 15f, "LeftArm", new PlaneViewModel() { });
                List<KeyValuePair<string, bool>> mainCameraRotation = mainCameraController.AlignTransformAxis();
                MoveToPoint(caughtFootball.transform, caughtFootball.transform.position, leftHand.Find("LeftPalmCatchArea").GetChild(0).position, 1f);
            }
            previousLeftArmRotation = leftArm.Find("LeftArm").rotation;
            previousRightArmRotation = rightArm.Find("RightArm").rotation;
            previousLeftShoulderRotation = leftArm.rotation;
            previousRightShoulderRotation = rightArm.rotation;
            // Drawn after the normal pose is saved, so it's never part of it.
            ApplyRunArmSwing();

            if (gameObject.name.ToLower().Contains("wrx"))
            {
                int u = 0;
            }
        }
        else {
            if (caughtFootball != null)
            {
                GameObject pivotAnchor = GameObject.Find("PivotAnchor");
                if (pivotAnchor != null && pivotAnchor.GetComponent<Rigidbody>() != null)
                {
                    Vector3 angularTorque = new Vector3(
                        pivotAnchor.GetComponent<Rigidbody>().angularVelocity.x * -1f,
                        pivotAnchor.GetComponent<Rigidbody>().angularVelocity.y * -1f,
                        pivotAnchor.GetComponent<Rigidbody>().angularVelocity.z * -1f);
                    pivotAnchor.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
                    pivotAnchor.GetComponent<Rigidbody>().AddForce((-1f) * pivotAnchor.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
                }
                if (gameObject.GetComponent<Rigidbody>() != null)
                {
                    Vector3 angularTorque = new Vector3(
                            caughtFootball.GetComponent<Rigidbody>().angularVelocity.x * -1f,
                            caughtFootball.GetComponent<Rigidbody>().angularVelocity.y * -1f,
                            caughtFootball.GetComponent<Rigidbody>().angularVelocity.z * -1f);
                    caughtFootball.GetComponent<Rigidbody>().AddTorque(angularTorque, ForceMode.Impulse);
                    caughtFootball.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().velocity, ForceMode.Impulse);
                    caughtFootball.GetComponent<Rigidbody>().AddForce((-1f) * gameObject.GetComponent<Rigidbody>().mass * Physics.gravity, ForceMode.Force);
                }

                if (GameObject.Find("PivotAnchor") != null)
                {
                    Vector3 targetPosition = leftHand.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * 0.1705792f);
                    Vector3 targetPosition2 = leftHand.position + (new Vector3(leftArm.forward.x, 0, leftArm.forward.z) * (0.1705792f / 2f));
                    targetDirection = new Vector3(0, 0, 0);
                    AimAtTargetInput(90f, true, caughtFootball.transform, leftHand.up, 'r', false);
                    MoveToPoint(caughtFootball.transform, caughtFootball.transform.position, leftHand.Find("PalmCatchArea").GetChild(0).position, 1f);
                    Debug.DrawRay(rightArm.Find("RightArm").position, targetDirection, Color.blue);
                    float scaler = 0.5f;
                    Vector3 tRightPoint = leftHand.position - (leftHand.forward * scaler);
                    AimAtTargetInput(30f, true, rightArm, tRightPoint - rightArm.position, 'r', true);
                    AimAtTargetInput(30f, true, rightArm.Find("RightArm"), tRightPoint - rightArm.Find("RightArm").position, 'r', true);
                    RotateTransformToDirection(rightArm.Find("RightArm").forward, rightArm.Find("RightArm"), leftArm.Find("LeftArm").forward, rightArm.Find("RightArm").right, 30f, true);
                    Vector3 actualTarget = targetPosition + new Vector3(-targetDirection.x, 0, -targetDirection.x);
                    Vector3 val = targetDirection.normalized;
                    if (leftHandCatchPosition != null && rightHandCatchPosition != null)
                    {

                        if (Vector3.Distance(leftHand.Find("PalmCatchArea").GetChild(0).position, caughtFootball.transform.position) > 2 && (Vector3.Distance(rightHand.Find("PalmCatchArea").position, caughtFootball.transform.position) > 2))
                        {
                            caughtFootball = null;
                            if (gameObject != null)
                            {
                                Destroy(caughtFootball.GetComponent<FixedJoint>());
                            }
                            if (GameObject.Find("PivotAnchor") != null)
                            {
                                Destroy(GameObject.Find("PivotAnchor"));
                            }
                        }
                    }
                }
            }
        }


    }

    PlayerState DeterminePlayerStateTransition(PlayerState currentState, bool rTriggerPressed, Vector2 movementInput) {
        PlayerState updatedState = PlayerState.none;
        switch (currentState) {
            case PlayerState.attemptingCatch:
                if (rTriggerPressed)
                {
                    updatedState = PlayerState.running;
                }
                else {
                    updatedState = PlayerState.attemptingCatch;
                }
                break;
            case PlayerState.catchAndRun:

                break;
        }
        return updatedState;
    }
    float LookAtQB(Transform qb, Vector3 wrForward, Vector3 targetForward)
    {
        float val = 0;
        Vector3 deltaPosition = transform.position - qb.position;
        float targetAngle = Mathf.Acos(targetForward.x / targetForward.z) * Mathf.Rad2Deg;
        float initialAngle = Mathf.Acos(wrForward.x / wrForward.z) * Mathf.Rad2Deg;
        float direction = Mathf.Atan(deltaPosition.x / deltaPosition.z) * Mathf.Rad2Deg;
        float offsetAngle = targetAngle - initialAngle;
        val = direction + offsetAngle;
        return val;
    }

    public Vector2 GetAngleCoordinates(float angle) {
        float horX = Mathf.Cos(angle * Mathf.Deg2Rad);
        float horY = Mathf.Sin(angle * Mathf.Deg2Rad);
        return new Vector2(horX, horY).normalized;
    }
    public float GetLeverArmAngle(Vector2 movementInput) {
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


    public Vector3 DetermineRotateAxis(Vector3 horizontalAxis, Quadrant armQuadrant, Quadrant analogQuadrant, float angleDifference) {
        Vector3 returnedHorizontalAxis = horizontalAxis;
        switch (analogQuadrant)
        {
            case Quadrant.quadrant1:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        break;
                    case Quadrant.quadrant2:
                        returnedHorizontalAxis *= -1;
                        break;
                    case Quadrant.quadrant3:
                        break;
                    case Quadrant.quadrant4:
                        returnedHorizontalAxis *= Mathf.Abs(angleDifference) > 180f ? -1 : 1;
                        break;
                }
                break;
            case Quadrant.quadrant2:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        returnedHorizontalAxis *= -1;
                        break;
                    case Quadrant.quadrant2:
                        break;
                    case Quadrant.quadrant3:
                        break;
                    case Quadrant.quadrant4:
                        returnedHorizontalAxis *= Mathf.Abs(angleDifference) > 180f ? -1 : 1;
                        break;
                }
                break;
            case Quadrant.quadrant3:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        returnedHorizontalAxis *= Mathf.Abs(angleDifference) > 180f ? -1 : 1;
                        break;
                    case Quadrant.quadrant2:
                        break;
                    case Quadrant.quadrant3:
                        break;
                    case Quadrant.quadrant4:
                        break;
                }
                break;
            case Quadrant.quadrant4:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        break;
                    case Quadrant.quadrant2:
                        returnedHorizontalAxis *= Mathf.Abs(angleDifference) > 180f ? -1 : 1;
                        break;
                    case Quadrant.quadrant3:
                        break;
                    case Quadrant.quadrant4:
                        break;
                }
                break;
        }
        return returnedHorizontalAxis;
    }


    public float GetRotationValueInRadiansFromUnitCircle(float armAngle, float analogAngle, bool isClockwise = true) {
        float rotationAmount = 0f;
        float offsetAngle = 0f;
        Quadrant armQuadrant = GetQuadrant(armAngle);
        Quadrant analogQuadrant = GetQuadrant(analogAngle);
        switch (analogQuadrant) {
            case Quadrant.quadrant1:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        float clockwiseValue = isClockwise ? armAngle >= analogAngle ? (armAngle - analogAngle) : (armAngle) + 270 + (90 - analogAngle) : 0;
                        float counterClockwiseValue = !isClockwise ? analogAngle >= armAngle ? (analogAngle - armAngle) : (90 - armAngle) + 270 + (analogAngle) : 0;
                        rotationAmount = isClockwise ? clockwiseValue * Mathf.Deg2Rad : counterClockwiseValue * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant2:
                        offsetAngle = isClockwise ? (armAngle - 90) + (90 - analogAngle) : analogAngle + 180 + (180 - armAngle);
                        rotationAmount = (offsetAngle * Mathf.Deg2Rad);
                        break;
                    case Quadrant.quadrant3:
                        offsetAngle = isClockwise ? (90 - analogAngle) + 90 + (armAngle - 180) : analogAngle + 90 + (270 - armAngle);
                        rotationAmount = (offsetAngle * Mathf.Deg2Rad);
                        break;
                    case Quadrant.quadrant4:
                        offsetAngle = isClockwise ? (90 - analogAngle) + 180 + (armAngle - 270) : analogAngle + (360 - armAngle);
                        rotationAmount = (offsetAngle * Mathf.Deg2Rad);
                        break;
                }
                break;
            case Quadrant.quadrant2:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        offsetAngle = isClockwise ? armAngle + 180 + (180 - analogAngle) : (90 - armAngle) + (analogAngle - 90);
                        rotationAmount = (offsetAngle * Mathf.Deg2Rad);
                        break;
                    case Quadrant.quadrant2:
                        float clockwiseValue = isClockwise ? armAngle >= analogAngle ? (armAngle - analogAngle) : (armAngle - 90) + 270 + (180 - analogAngle) : 0;
                        float counterClockwiseValue = !isClockwise ? analogAngle >= armAngle ? (analogAngle - armAngle) : (180 - armAngle) + 270 + (analogAngle - 90) : 0;
                        rotationAmount = isClockwise ? clockwiseValue * Mathf.Deg2Rad : counterClockwiseValue * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant3:
                        offsetAngle = isClockwise ? (armAngle - 180) + (180 - analogAngle) : (analogAngle - 90) + 180 + (270 - armAngle);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant4:
                        offsetAngle = isClockwise ? (armAngle - 270) + 90 + (180 - analogAngle) : (360 - armAngle) + 90 + (analogAngle - 90);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                }
                break;
            case Quadrant.quadrant3:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        offsetAngle = isClockwise ? armAngle + 90 + (270 - analogAngle) : (90 - armAngle) + 90 + (analogAngle - 180);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant2:
                        offsetAngle = isClockwise ? (armAngle - 90) + 180 + (270 - analogAngle) : (180 - armAngle) + (analogAngle - 180);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant3:
                        float clockwiseValue = isClockwise ? armAngle >= analogAngle ? (armAngle - analogAngle) : (armAngle - 180) + 270 + (270 - analogAngle) : 0;
                        float counterClockwiseValue = !isClockwise ? analogAngle >= armAngle ? (analogAngle - armAngle) : (270 - armAngle) + 270 + (analogAngle - 180) : 0;
                        rotationAmount = isClockwise ? clockwiseValue * Mathf.Deg2Rad : counterClockwiseValue * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant4:
                        offsetAngle = isClockwise ? (armAngle - 270) + (270 - analogAngle) : (360 - armAngle) + 180 + (analogAngle - 180);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                }
                break;
            case Quadrant.quadrant4:
                switch (armQuadrant)
                {
                    case Quadrant.quadrant1:
                        offsetAngle = isClockwise ? (armAngle) + (360 - analogAngle) : (90 - armAngle) + 180 + (analogAngle - 270);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant2:
                        offsetAngle = isClockwise ? (armAngle - 90) + 90 + (360 - analogAngle) : (180 - armAngle) + 90 + (analogAngle - 270);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant3:
                        offsetAngle = isClockwise ? (armAngle - 180) + 180 + (360 - analogAngle) : (270 - armAngle) + (analogAngle - 270);
                        rotationAmount = offsetAngle * Mathf.Deg2Rad;
                        break;
                    case Quadrant.quadrant4:
                        float clockwiseValue = isClockwise ? armAngle >= analogAngle ? (armAngle - analogAngle) : (armAngle - 270) + 270 + (360 - analogAngle) : 0;
                        float counterClockwiseValue = !isClockwise ? analogAngle >= armAngle ? (analogAngle - armAngle) : (360 - armAngle) + 270 + (analogAngle - 270) : 0;
                        rotationAmount = isClockwise ? clockwiseValue * Mathf.Deg2Rad : counterClockwiseValue * Mathf.Deg2Rad;
                        break;
                }
                break;
        }
        return Mathf.Abs(rotationAmount);
    }

    public bool NumbersAreEqual(float num1, float num2, float threshold) {
        return Mathf.Abs(num1 - num2) <= threshold;
    }

    public float GetUnitCircleEulerAngle(float angle)
    {
        float normalizedAngle = angle > 0 ? angle % 360 : (360 + angle) % 360;
        return normalizedAngle;
    }

    public Quadrant GetQuadrant(float angle) {
        //return ((angle >= 0f && angle < 90f) || (Mathf.Abs(angle - 360f) < 0.000001f)) ? Quadrant.quadrant1 : angle >= 90f && angle < 180f ? Quadrant.quadrant2 : angle >= 180f && angle < 270f ? Quadrant.quadrant3 : angle >= 270f && angle < 360f ? Quadrant.quadrant4 : Quadrant.unspecified;
        return ((angle >= 0f && angle < 90f) || (Mathf.Abs(angle - 360f) < 0.000001f)) ? Quadrant.quadrant1 : angle >= 90f && angle < 180f ? Quadrant.quadrant2 : angle >= 180f && angle < 270f ? Quadrant.quadrant3 : angle >= 270f && angle < 360f ? Quadrant.quadrant4 : Quadrant.unspecified;
    }
    public enum Quadrant
    {
        quadrant1 = 0,
        quadrant2 = 1,
        quadrant3 = 2,
        quadrant4 = 3,
        unspecified = 4
    }

    public enum LeverType
    {
        recipient = 1,
        target = 2,
        unspecified = 3
    }
    public enum Direction
    {
        clockwise = 0,
        counterclockwise = 1,
        unspecfied = 2

    }
    public class LeverArm {
        public float xInput;
        public float yInput;
        public float angle;
        public LeverType leverType;
        public Direction rotateDirection;
        public float totalAmountRotatedHorizontal;
        public float totalAmountRotatedVertical;
    }
}
