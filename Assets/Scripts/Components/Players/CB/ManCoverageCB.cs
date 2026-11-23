using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// (Named ManCoverageCB so it doesn't clash with your own CB script.)
/// Cornerback in man-to-man coverage on one receiver (WRX by default).
///
/// Setup: none needed. When the scene loads, if there's no ManCoverageCB yet, one is made from a copy of WRX (same UMA body,
/// animator and physics) with the receiver's scripts removed, lined up Start Cushion Yards off him on the
/// downfield side. To place him yourself, add this component to a GameObject that has the UMA character,
/// Animator, Rigidbody and collider, and untick Auto Spawn on the copy (or set ManCoverageCB.AutoSpawn = false).
///
/// Coverage: he mirrors the receiver with a short reaction delay (so hard cuts still win a step), staying Cushion
/// Yards deeper and a little inside. When the ball is thrown to his man he breaks on it (after Ball Reaction Time),
/// heading for the spot the pass is aimed at.
///
/// Defending the pass: once he reacts to the throw he works out, from the ball's flight, the earliest point on its
/// path he can get his hands to (running, then leaping if it's above his standing reach). He runs to that spot,
/// gathers and jumps so he peaks as the ball arrives, and his arms blend smoothly from his run into reaching for
/// it (two-bone IK on both arms, aimed at where the ball will meet his hands). When the ball reaches his hands he
/// either catches it (Intercept Chance, decided when he commits to the play; the ball is then pulled into his
/// chest and tucked under his arm as he lands) or knocks it away. If no point is reachable he just breaks on the
/// ball and can only swat it if it comes within Swat Reach Meters.
/// </summary>
public class ManCoverageCB : MonoBehaviour
{
    public static bool AutoSpawn = true;

    [Header("Assignment")]
    [Tooltip("The receiver he covers (empty = the QB's WRX).")]
    public WR coveredReceiver;

    [Header("Coverage")]
    [Tooltip("Yards off the receiver (downfield) at the start of the play.")]
    public float startCushionYards = 6f;
    [Tooltip("Yards he tries to stay deeper (downfield) than the receiver while covering. Smaller = tighter.")]
    public float cushionYards = 0.75f;
    [Tooltip("Yards he stays to the inside (towards the middle of the field) of the receiver.")]
    public float insideLeverageYards = 0.5f;
    [Tooltip("Seconds behind the receiver his reactions are (higher = the receiver gets more separation on cuts).")]
    public float reactionTime = 0.12f;
    [Tooltip("His top speed as a fraction of the receiver's top speed.")]
    [Range(0.5f, 1.3f)] public float speedRatio = 1f;
    [Tooltip("His acceleration as a fraction of the receiver's.")]
    [Range(0.5f, 2f)] public float accelerationRatio = 1.2f;

    [Header("Playing the ball")]
    [Tooltip("Seconds after the throw before he breaks on the ball.")]
    public float ballReactionTime = 0.15f;
    [Tooltip("Speed multiplier while he breaks on the ball (an all-out sprint).")]
    public float breakSpeedMultiplier = 1.05f;
    [Tooltip("How far (meters, real size) from his hands he can knock the ball away. About an arm's length plus a reach.")]
    public float swatReachMeters = 1f;
    [Tooltip("Within this (meters) of his hands, and facing the ball, he can intercept it instead.")]
    public float interceptReachMeters = 0.45f;
    [Tooltip("Chance (0..1) he holds on for the interception when he's in position for one; otherwise he swats it.")]
    [Range(0f, 1f)] public float interceptChance = 0.5f;
    [Tooltip("How hard he knocks the ball away (meters per second, real size).")]
    public float swatSpeedMetersPerSecond = 7f;
    [Tooltip("Seconds he runs the interception back before stopping.")]
    public float returnSeconds = 4f;

    [Header("Going up for the ball")]
    [Tooltip("Highest he can jump (meters, real size: how far his hands rise above his standing reach).")]
    public float maxJumpMeters = 0.8f;
    [Tooltip("Seconds before the ball arrives that his arms start coming up from his run to reach for it.")]
    public float armRaiseSeconds = 0.5f;
    [Tooltip("How quickly (seconds) his arms ease towards their target (smaller = snappier).")]
    public float armSmoothTime = 0.08f;
    [Tooltip("Once his arms are up, he plays the ball if it passes within this (meters) of his hands.")]
    public float handsSwatReachMeters = 0.5f;
    [Tooltip("Seconds to pull a picked ball from his hands into his chest.")]
    public float secureSeconds = 0.3f;
    [Tooltip("Seconds for his arms to ease back into his run after securing the ball (or after the play on the ball).")]
    public float armReleaseSeconds = 0.4f;
    [Tooltip("How much his run animation slows while he's in the air (0 = holds the pose, 1 = normal).")]
    [Range(0f, 1f)] public float airAnimationSpeed = 0.25f;

    [Header("Tackling")]
    [Tooltip("After the catch he chases the receiver; within this (meters, real size) he tackles him.")]
    public float tackleReachMeters = 1f;
    [Tooltip("How hard the hit is: the change of speed (meters per second, real size) given to the receiver, on top of the CB's own speed into him.")]
    public float tackleHitMetersPerSecond = 3f;
    [Tooltip("How far above the receiver's center of gravity the hit lands, as a fraction of his height (higher = he tips over more).")]
    [Range(0f, 0.5f)] public float tackleHitHeight = 0.2f;
    [Tooltip("Extra downward share of the hit (driving him into the turf).")]
    [Range(0f, 2f)] public float tackleDownward = 0.6f;
    [Tooltip("Spin (radians per second) added to tip him over towards the ground.")]
    public float tackleSpin = 3.5f;
    [Tooltip("Seconds he keeps driving through the receiver after the hit.")]
    public float driveSeconds = 0.4f;

    QB qb;
    Rigidbody body;
    bool tackled;
    float tackleTime;
    Animator animator;
    Vector3 downfield = Vector3.forward;
    bool started;
    bool defended;
    bool intercepted;
    float interceptTime;
    Rigidbody lastBall;
    Vector3 lastBallPosition;
    Transform carryHand;
    readonly List<Sample> history = new List<Sample>();

    // The play on the ball
    bool planning;            // he has a reachable catch point and is going for it
    bool rolledPick;          // wantsPick has been decided for this throw
    bool wantsPick;           // catch it (else knock it away) when it reaches his hands
    float catchTime;          // Time.time the ball reaches the catch point
    Vector3 catchPoint;       // where on its path he meets it
    Vector3 catchFacing = Vector3.forward; // horizontal, from his body towards the ball at the catch
    Vector3 handsOffset;      // catch point minus where his body (pivot) will be at the catch
    float catchRise;          // how far he jumps for it (units)
    float groundY;
    bool leaping;
    float leapStart;
    Vector3 leapHorizontal;
    float leapUpSpeed;

    // Arms (two-bone IK over the animation)
    Transform leftUpper, leftLower, leftHandBone, rightUpper, rightLower, rightHandBone;
    float armLength;          // shoulder to wrist (units)
    float shoulderHeight;     // shoulder above his pivot (units), standing
    float armWeight, armWeightVelocity, armWantWeight;
    Vector3 smoothOffset, smoothOffsetVelocity;
    bool offsetInitialized;
    bool securing;
    float secureStart;
    Vector3 secureFrom;       // ball position at the catch, relative to his body (body rotation frame)
    float releaseStart = -1f;
    Transform pickedBall;
    float baseAnimatorSpeed = 1f;

    struct Sample
    {
        public float time;
        public Vector3 position;
        public Vector3 velocity;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void HookSceneLoads()
    {
        // Every time a scene loads (including Restart Play, which reloads it), not just the first time.
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => SpawnOnSceneLoad();
    }

    static void SpawnOnSceneLoad()
    {
        if (!AutoSpawn || FindObjectOfType<ManCoverageCB>() != null)
        {
            return;
        }
        QB qb = FindObjectOfType<QB>();
        if (qb == null || qb.wrX == null)
        {
            return;
        }
        CreateFromReceiver(qb.wrX);
    }

    /// <summary>
    /// Makes a CB from a copy of the receiver: copied while inactive (so none of the receiver's scripts start), then
    /// every non-UMA script is removed, his hands are untagged (so they never count as the receiver's catch), and
    /// this component is added.
    /// </summary>
    public static ManCoverageCB CreateFromReceiver(GameObject receiver)
    {
        GameObject holder = new GameObject("CB spawn holder");
        holder.SetActive(false);
        GameObject copy = Instantiate(receiver, holder.transform);
        copy.name = "CB";
        foreach (MonoBehaviour script in copy.GetComponentsInChildren<MonoBehaviour>(true))
        {
            string ns = script.GetType().Namespace ?? "";
            if (!ns.StartsWith("UMA"))
            {
                DestroyImmediate(script);
            }
        }
        foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
        {
            if (part.CompareTag("Hand") || part.CompareTag("Cube"))
            {
                part.tag = "Untagged";
            }
        }
        ManCoverageCB cb = copy.AddComponent<ManCoverageCB>();
        cb.coveredReceiver = receiver.GetComponent<WR>();
        copy.transform.SetParent(null, true);
        Destroy(holder);
        return cb;
    }

    void Start()
    {
        qb = FindObjectOfType<QB>();
        body = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (coveredReceiver == null && qb != null && qb.wrX != null)
        {
            coveredReceiver = qb.wrX.GetComponent<WR>();
        }
        if (body != null)
        {
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }
        if (qb != null)
        {
            downfield = qb.transform.forward.z >= 0f ? Vector3.forward : Vector3.back;
        }
        if (animator != null)
        {
            baseAnimatorSpeed = animator.speed;
        }
        if (coveredReceiver != null)
        {
            // Line up off the receiver, a little inside.
            Vector3 start = coveredReceiver.transform.position + downfield * (startCushionYards * Yard()) + InsideDirection() * (insideLeverageYards * Yard());
            transform.position = new Vector3(start.x, coveredReceiver.transform.position.y, start.z);
            transform.rotation = Quaternion.LookRotation(-downfield, Vector3.up); // facing the QB
        }
        groundY = transform.position.y;
    }

    float UnitsPerMeter()
    {
        return coveredReceiver != null ? Mathf.Max(0.01f, coveredReceiver.GetUnitsPerMeter()) : 33.5f;
    }

    float Yard()
    {
        return UnitsPerMeter() * 0.9144f;
    }

    /// <summary>Towards the middle of the field (x = the QB's x) from the receiver.</summary>
    Vector3 InsideDirection()
    {
        float qbX = qb != null ? qb.transform.position.x : 0f;
        float receiverX = coveredReceiver != null ? coveredReceiver.transform.position.x : 0f;
        return qbX >= receiverX ? Vector3.right : Vector3.left;
    }

    void FixedUpdate()
    {
        if (coveredReceiver == null || body == null)
        {
            return;
        }
        RecordReceiver();

        float topSpeed = coveredReceiver.GetTargetSpeed() * speedRatio;
        float acceleration = coveredReceiver.GetRunAcceleration() * accelerationRatio;
        Vector3 wantVelocity;
        armWantWeight = 0f;

        if (leaping)
        {
            LeapStep();
        }
        else if (Mathf.Abs(body.velocity.y) < 0.05f * UnitsPerMeter())
        {
            groundY = transform.position.y;
        }

        if (tackled)
        {
            // Drive through him for a moment, then stop.
            wantVelocity = Time.time - tackleTime < driveSeconds ? Vector3.ProjectOnPlane(body.velocity, Vector3.up) : Vector3.zero;
        }
        else if (coveredReceiver.caughtFootball != null)
        {
            planning = false;
            wantVelocity = PursueAndTackle(topSpeed * breakSpeedMultiplier);
        }
        else if (intercepted)
        {
            // Run it back upfield for a few seconds, then stop.
            wantVelocity = Time.time - interceptTime < returnSeconds ? -downfield * topSpeed : Vector3.zero;
        }
        else
        {
            Rigidbody ball = qb != null ? qb.GetBallInFlight() : null;
            bool ballToMyMan = ball != null && qb.GetTargetReceiver() == coveredReceiver.gameObject;
            bool reacted = ballToMyMan && Time.time - qb.GetTimeThrown() >= ballReactionTime;
            if (!ballToMyMan)
            {
                planning = false;
                rolledPick = false;
            }
            if (reacted && !defended && !leaping)
            {
                PlanCatch(ball, topSpeed * breakSpeedMultiplier);
            }
            if (ballToMyMan && !defended)
            {
                DefendPass(ball);
            }
            if (planning && !defended)
            {
                wantVelocity = PlayTheBall(topSpeed * breakSpeedMultiplier);
            }
            else if (reacted && !defended)
            {
                // No reachable point: break on the ball, to where it's going (a touch in front, to undercut it).
                Vector3 target = qb.GetThrowTarget();
                Vector3 fromBall = Vector3.ProjectOnPlane(ball.position - target, Vector3.up);
                Vector3 undercut = fromBall.sqrMagnitude > 1e-4f ? fromBall.normalized * (0.5f * Yard()) : Vector3.zero;
                wantVelocity = Steer(target + undercut, Vector3.zero, topSpeed * breakSpeedMultiplier);
            }
            else
            {
                wantVelocity = CoverageVelocity(topSpeed);
            }
        }

        Vector3 current = Vector3.ProjectOnPlane(body.velocity, Vector3.up);
        Vector3 next = current;
        if (!leaping)
        {
            // Accelerate towards it (horizontal only; the Rigidbody's drag is made up for, like the WR's run).
            next = Vector3.MoveTowards(current, wantVelocity, acceleration * Time.fixedDeltaTime);
            float dragKeep = Mathf.Max(0.05f, 1f - body.drag * Time.fixedDeltaTime);
            body.AddForce((next / dragKeep - current) / Time.fixedDeltaTime * body.mass, ForceMode.Force);
        }

        Face(next);
        if (animator != null)
        {
            animator.SetBool("runningForward", leaping || next.magnitude > 0.15f * Mathf.Max(1f, topSpeed));
            float wantSpeed = leaping ? baseAnimatorSpeed * airAnimationSpeed : baseAnimatorSpeed;
            animator.speed = Mathf.MoveTowards(animator.speed, wantSpeed, 4f * Time.fixedDeltaTime);
        }
        TrackBall();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Going up for the ball
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>His gravity in the air: real-size (scaled like the WR's), so his jump takes a real amount of time.</summary>
    float PlayerGravity()
    {
        return Mathf.Abs(Physics.gravity.y) * Mathf.Max(1f, UnitsPerMeter());
    }

    /// <summary>The ball's position t seconds from now (its exact physics-step path: no drag, scaled gravity).</summary>
    static Vector3 BallPositionIn(Rigidbody ball, float t)
    {
        Vector3 gravity = ball.useGravity ? Physics.gravity * Mathf.Max(1f, QB.BallGravityMultiplier) : Vector3.zero;
        return ball.position + ball.velocity * t + 0.5f * gravity * t * (t + Time.fixedDeltaTime);
    }

    static Vector3 BallVelocityIn(Rigidbody ball, float t)
    {
        Vector3 gravity = ball.useGravity ? Physics.gravity * Mathf.Max(1f, QB.BallGravityMultiplier) : Vector3.zero;
        return ball.velocity + gravity * t;
    }

    /// <summary>Hands height above his pivot with his arms straight up / lowest he'd try to pick it.</summary>
    float StandingReach()
    {
        float height = coveredReceiver.GetPlayerHeightUnits();
        return armLength > 0f ? shoulderHeight + 0.95f * armLength : 1.25f * height;
    }

    float LowestCatch()
    {
        float height = coveredReceiver.GetPlayerHeightUnits();
        return armLength > 0f ? shoulderHeight - 0.5f * armLength : 0.5f * height;
    }

    /// <summary>
    /// Finds the earliest point on the ball's path he can get to with his hands: high enough to reach (jumping if
    /// needed), and a spot he can run to by then. Re-done every step until he leaves the ground.
    /// </summary>
    void PlanCatch(Rigidbody ball, float runSpeed)
    {
        float unitsPerMeter = UnitsPerMeter();
        float reach = StandingReach();
        float highest = reach + maxJumpMeters * unitsPerMeter;
        float lowest = LowestCatch();
        float handsForward = 0.35f * unitsPerMeter;
        float slack = (armLength > 0f ? 0.5f * armLength : 0.3f * unitsPerMeter);
        Vector3 me = Vector3.ProjectOnPlane(transform.position, Vector3.up);
        float step = Time.fixedDeltaTime;
        bool found = false;
        for (float t = step; t < 4f; t += step)
        {
            Vector3 point = BallPositionIn(ball, t);
            float height = point.y - groundY;
            if (height < 0f)
            {
                break; // it's landed
            }
            if (height > highest || height < lowest)
            {
                continue;
            }
            Vector3 incoming = Vector3.ProjectOnPlane(BallVelocityIn(ball, t), Vector3.up);
            Vector3 facing = incoming.sqrMagnitude > 1e-4f ? -incoming.normalized : transform.forward;
            Vector3 spot = Vector3.ProjectOnPlane(point, Vector3.up) - facing * handsForward;
            float distance = Vector3.Distance(spot, me);
            if (distance > runSpeed * Mathf.Max(0f, t - 0.05f) + slack)
            {
                continue;
            }
            found = true;
            catchTime = Time.time + t;
            catchPoint = point;
            catchFacing = facing;
            catchRise = Mathf.Clamp(height - reach, 0f, maxJumpMeters * unitsPerMeter);
            Vector3 bodyAtCatch = new Vector3(spot.x, groundY + catchRise, spot.z);
            handsOffset = catchPoint - bodyAtCatch;
            break;
        }
        planning = found;
        if (found && !rolledPick)
        {
            rolledPick = true;
            wantsPick = Random.value < interceptChance;
        }
    }

    /// <summary>Run to his spot under the catch point, raise his arms as it gets close, and leap so he peaks as it arrives.</summary>
    Vector3 PlayTheBall(float runSpeed)
    {
        float timeLeft = catchTime - Time.time;
        Vector3 spot = Vector3.ProjectOnPlane(catchPoint - handsOffset, Vector3.up);
        Vector3 toSpot = Vector3.ProjectOnPlane(spot - transform.position, Vector3.up);

        armWantWeight = Mathf.SmoothStep(0f, 1f, 1f - timeLeft / Mathf.Max(0.05f, armRaiseSeconds));

        float gravity = PlayerGravity();
        float timeToPeak = catchRise > 0.02f * UnitsPerMeter() ? Mathf.Sqrt(2f * catchRise / gravity) : 0f;
        if (timeToPeak > 0f && timeLeft <= timeToPeak + 0.5f * Time.fixedDeltaTime && !leaping)
        {
            StartLeap(toSpot / Mathf.Max(timeLeft, Time.fixedDeltaTime), gravity * timeToPeak, runSpeed);
            return leapHorizontal;
        }
        if (timeLeft < -0.2f)
        {
            planning = false; // it got past him
        }
        // Get there a touch early (then gather), never faster than he can run.
        return Vector3.ClampMagnitude(toSpot / Mathf.Max(0.7f * timeLeft, 0.05f), runSpeed);
    }

    void StartLeap(Vector3 horizontal, float upSpeed, float runSpeed)
    {
        leaping = true;
        leapStart = Time.time;
        leapHorizontal = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(horizontal, Vector3.up), runSpeed * 1.15f);
        leapUpSpeed = upSpeed;
        LeapStep();
    }

    /// <summary>
    /// In the air his path is set directly (a real-size jump arc with the run's speed carried through), so he
    /// peaks exactly when planned; he lands when he's back down to the ground he left.
    /// </summary>
    void LeapStep()
    {
        float t = Time.time - leapStart;
        float gravity = PlayerGravity();
        float up = leapUpSpeed - gravity * t;
        if ((t > 0.1f && up < 0f && transform.position.y <= groundY + 0.01f * UnitsPerMeter()) || t > 2.5f)
        {
            leaping = false;
            body.velocity = new Vector3(leapHorizontal.x * 0.7f, 0f, leapHorizontal.z * 0.7f);
            return;
        }
        float dragKeep = Mathf.Max(0.05f, 1f - body.drag * Time.fixedDeltaTime);
        float unityGravityThisStep = body.useGravity ? -Physics.gravity.y * Time.fixedDeltaTime : 0f; // physics adds it after this
        body.velocity = new Vector3(leapHorizontal.x, up + unityGravityThisStep, leapHorizontal.z) / dragKeep;
    }

    /// <summary>
    /// After the catch: take a pursuit angle (to where he'll be when the CB gets there, up to a second ahead) and
    /// tackle him once he's within reach.
    /// </summary>
    Vector3 PursueAndTackle(float topSpeed)
    {
        Rigidbody receiverBody = coveredReceiver.GetComponent<Rigidbody>();
        Vector3 receiverVelocity = receiverBody != null ? Vector3.ProjectOnPlane(receiverBody.velocity, Vector3.up) : Vector3.zero;
        Vector3 toReceiver = Vector3.ProjectOnPlane(coveredReceiver.transform.position - transform.position, Vector3.up);
        if (toReceiver.magnitude <= tackleReachMeters * UnitsPerMeter() + 0.1f * coveredReceiver.GetPlayerHeightUnits())
        {
            Tackle(receiverBody, toReceiver);
            return Vector3.ProjectOnPlane(body.velocity, Vector3.up);
        }
        float lead = Mathf.Min(1f, toReceiver.magnitude / Mathf.Max(1f, topSpeed));
        Vector3 aim = coveredReceiver.transform.position + receiverVelocity * lead;
        Vector3 toAim = Vector3.ProjectOnPlane(aim - transform.position, Vector3.up);
        return toAim.sqrMagnitude > 1e-4f ? toAim.normalized * topSpeed : Vector3.zero;
    }

    /// <summary>
    /// The hit: aimed through his center of gravity, landing a little above it (tackleHitHeight of his height) and
    /// angled down, so it both drives him back and tips his body over towards the turf; plus a spin about the axis
    /// that rolls his top in the direction of the hit.
    /// </summary>
    void Tackle(Rigidbody receiverBody, Vector3 toReceiver)
    {
        tackled = true;
        tackleTime = Time.time;
        float unitsPerMeter = UnitsPerMeter();
        Vector3 direction = toReceiver.sqrMagnitude > 1e-4f ? toReceiver.normalized : transform.forward;
        Vector3 myVelocity = Vector3.ProjectOnPlane(body.velocity, Vector3.up);
        Vector3 receiverVelocity = receiverBody != null ? Vector3.ProjectOnPlane(receiverBody.velocity, Vector3.up) : Vector3.zero;
        float closing = Mathf.Max(0f, Vector3.Dot(myVelocity - receiverVelocity, direction));
        float hit = closing + tackleHitMetersPerSecond * unitsPerMeter;
        Vector3 deltaVelocity = (direction + Vector3.down * tackleDownward).normalized * hit;
        Vector3 centerOfGravity = receiverBody != null ? receiverBody.worldCenterOfMass : coveredReceiver.transform.position;
        Vector3 hitPoint = centerOfGravity + Vector3.up * (tackleHitHeight * coveredReceiver.GetPlayerHeightUnits());
        Vector3 spin = Vector3.Cross(Vector3.up, direction) * tackleSpin;
        coveredReceiver.BeTackled(deltaVelocity, hitPoint, spin);
        PlayFlow.ShowBanner("TACKLED");
    }

    /// <summary>Velocity to stay with the receiver: mirror where he was reactionTime ago, a cushion deeper and inside.</summary>
    Vector3 CoverageVelocity(float topSpeed)
    {
        Sample seen = SeenReceiver();
        if (!started)
        {
            if (seen.velocity.magnitude < 0.05f * Mathf.Max(1f, topSpeed))
            {
                return Vector3.zero; // hold until the receiver starts his route
            }
            started = true;
        }
        Vector3 spot = seen.position + downfield * (cushionYards * Yard()) + InsideDirection() * (insideLeverageYards * Yard());
        return Steer(spot, seen.velocity, topSpeed);
    }

    /// <summary>Match the given velocity and close the gap to the spot (strongly), capped at top speed.</summary>
    Vector3 Steer(Vector3 spot, Vector3 matchVelocity, float topSpeed)
    {
        Vector3 toSpot = Vector3.ProjectOnPlane(spot - transform.position, Vector3.up);
        Vector3 want = Vector3.ProjectOnPlane(matchVelocity, Vector3.up) + toSpot * 3f;
        return Vector3.ClampMagnitude(want, topSpeed);
    }

    void RecordReceiver()
    {
        Rigidbody receiverBody = coveredReceiver.GetComponent<Rigidbody>();
        history.Add(new Sample
        {
            time = Time.time,
            position = coveredReceiver.transform.position,
            velocity = receiverBody != null ? Vector3.ProjectOnPlane(receiverBody.velocity, Vector3.up) : Vector3.zero
        });
        float keep = Time.time - reactionTime - 0.5f;
        while (history.Count > 2 && history[1].time < keep)
        {
            history.RemoveAt(0);
        }
    }

    /// <summary>The receiver as the CB "sees" him: reactionTime ago.</summary>
    Sample SeenReceiver()
    {
        float when = Time.time - reactionTime;
        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (history[i].time <= when)
            {
                return history[i];
            }
        }
        return history.Count > 0 ? history[0] : new Sample { position = coveredReceiver.transform.position };
    }

    /// <summary>Face where he's running, or the ball while it's on its way to his man.</summary>
    void Face(Vector3 velocity)
    {
        Vector3 look = velocity;
        Rigidbody ball = qb != null ? qb.GetBallInFlight() : null;
        if (planning && !defended && catchTime - Time.time < armRaiseSeconds + 0.3f)
        {
            look = catchFacing; // square up to the ball for the catch
        }
        else if (ball != null && !intercepted && Time.time - qb.GetTimeThrown() >= ballReactionTime)
        {
            look = Vector3.ProjectOnPlane(ball.position - transform.position, Vector3.up);
        }
        if (look.sqrMagnitude < 1e-4f)
        {
            return;
        }
        Quaternion want = Quaternion.LookRotation(look.normalized, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, want, 540f * Time.fixedDeltaTime));
    }

    /// <summary>Where his hands can get to: chest height, plus his reach (an arm's length) in any direction.</summary>
    Vector3 HandsCenter()
    {
        float height = coveredReceiver.GetPlayerHeightUnits();
        return transform.position + Vector3.up * (0.8f * height);
    }

    /// <summary>
    /// If the ball passed within reach of his hands this physics step: intercept or knock it away. Once his arms are
    /// up for a planned play on the ball, that's measured from his actual hands (Hands Swat Reach); otherwise from
    /// a general reach around his chest (Swat Reach). Checks the whole path since the last step, so a fast ball
    /// can't skip past.
    /// </summary>
    void DefendPass(Rigidbody ball)
    {
        bool armsUp = planning && armWeight > 0.5f && leftHandBone != null && rightHandBone != null;
        Vector3 hands = armsUp ? 0.5f * (leftHandBone.position + rightHandBone.position) : HandsCenter();
        Vector3 from = ball == lastBall ? lastBallPosition : ball.position;
        Vector3 closest = ClosestPointOnSegment(from, ball.position, hands);
        float distance = Vector3.Distance(closest, hands);
        float unitsPerMeter = UnitsPerMeter();
        float swatReach = (armsUp ? handsSwatReachMeters : swatReachMeters) * unitsPerMeter;
        if (distance > swatReach)
        {
            return;
        }
        Vector3 toBall = Vector3.ProjectOnPlane(closest - transform.position, Vector3.up);
        bool facing = toBall.sqrMagnitude < 1e-4f || Vector3.Dot(transform.forward, toBall.normalized) > 0.2f;
        bool pick = armsUp ? wantsPick : Random.value < interceptChance;
        if (distance <= interceptReachMeters * unitsPerMeter && facing && pick)
        {
            Intercept(ball, closest);
        }
        else
        {
            Swat(ball, hands);
        }
    }

    void Swat(Rigidbody ball, Vector3 hands)
    {
        defended = true;
        Vector3 away = ball.position - hands;
        if (away.sqrMagnitude < 1e-4f)
        {
            away = -ball.velocity;
        }
        away = Vector3.ProjectOnPlane(away, Vector3.up).normalized + Vector3.up * 0.6f;
        float speed = swatSpeedMetersPerSecond * UnitsPerMeter();
        ball.velocity = ball.velocity * 0.15f + away.normalized * speed;
        ball.angularVelocity = Random.onUnitSphere * 20f;
        MakeBallDead(ball);
        planning = false;
        releaseStart = Time.time;
        qb.NotePassDefended("SWATTED by the CB", null);
        PlayFlow.ShowBanner("PASS BROKEN UP");
    }

    void Intercept(Rigidbody ball, Vector3 where)
    {
        defended = true;
        intercepted = true;
        interceptTime = Time.time;
        MakeBallDead(ball);
        TestCollision flight = ball.GetComponent<TestCollision>();
        if (flight != null)
        {
            flight.stopFootball = true;
        }
        ball.velocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        ball.isKinematic = true;
        ball.detectCollisions = false;
        // Caught where it met his hands; LateUpdate then pulls it into his chest and tucks it under his arm.
        ball.transform.position = where;
        ball.transform.SetParent(transform, true);
        pickedBall = ball.transform;
        securing = true;
        secureStart = Time.time;
        secureFrom = Quaternion.Inverse(transform.rotation) * (where - transform.position);
        carryHand = rightHandBone != null ? rightHandBone : (FindChild(transform, "RightHand") ?? transform);
        qb.NotePassDefended("INTERCEPTED by the CB", transform);
        PlayFlow.Interception();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Arms: two-bone IK blended over the animation
    // ---------------------------------------------------------------------------------------------------------

    void LateUpdate()
    {
        if (coveredReceiver == null || !FindArmBones())
        {
            return;
        }
        if (!leaping && !securing && armWeight < 0.01f)
        {
            MeasureArms();
        }

        float unitsPerMeter = UnitsPerMeter();
        Vector3 ballSpot;
        Vector3 facing = catchFacing;
        float wantWeight;

        if (securing)
        {
            // Pull the ball from where he caught it into his chest, then tuck it in his right hand and let the arms go.
            float s = Mathf.SmoothStep(0f, 1f, (Time.time - secureStart) / Mathf.Max(0.05f, secureSeconds));
            Vector3 chest = Vector3.up * (shoulderHeight - 0.25f * armLength) + Vector3.forward * (0.25f * unitsPerMeter);
            Vector3 local = Vector3.Lerp(secureFrom, chest, s);
            ballSpot = transform.position + transform.rotation * local;
            facing = transform.forward;
            Transform ballTransform = pickedBall;
            if (ballTransform != null)
            {
                ballTransform.position = ballSpot;
            }
            wantWeight = 1f;
            if (s >= 1f && securing)
            {
                securing = false;
                releaseStart = Time.time;
                if (ballTransform != null && carryHand != null)
                {
                    ballTransform.SetParent(carryHand, true); // from here it goes with his hand as he runs
                }
            }
        }
        else
        {
            wantWeight = armWantWeight;
            if (releaseStart >= 0f)
            {
                wantWeight = Mathf.Min(wantWeight, 1f - Mathf.Clamp01((Time.time - releaseStart) / Mathf.Max(0.05f, armReleaseSeconds)));
            }
            ballSpot = transform.position + SmoothedOffset(handsOffset);
        }

        armWeight = Mathf.SmoothDamp(armWeight, wantWeight, ref armWeightVelocity, armSmoothTime);
        if (armWeight < 0.001f)
        {
            offsetInitialized = false;
            return;
        }

        // Hands either side of the ball, wrists a touch behind it (palms on the ball).
        Vector3 side = Vector3.Cross(Vector3.up, facing.sqrMagnitude > 1e-4f ? facing : transform.forward).normalized;
        float halfBall = 0.11f * unitsPerMeter;
        Vector3 behind = -facing * (0.07f * unitsPerMeter);
        Vector3 back = -transform.forward;
        SolveArm(leftUpper, leftLower, leftHandBone, ballSpot - side * halfBall + behind,
                 leftUpper.position + (-transform.right + Vector3.down * 0.8f + back * 0.3f) * armLength, armWeight);
        SolveArm(rightUpper, rightLower, rightHandBone, ballSpot + side * halfBall + behind,
                 rightUpper.position + (transform.right + Vector3.down * 0.8f + back * 0.3f) * armLength, armWeight);
    }

    /// <summary>The hands' target relative to his body, eased so a changing plan never snaps his arms.</summary>
    Vector3 SmoothedOffset(Vector3 offset)
    {
        if (!offsetInitialized)
        {
            // Start from where his hands are now, so they rise out of the run.
            smoothOffset = 0.5f * (leftHandBone.position + rightHandBone.position) - transform.position;
            smoothOffsetVelocity = Vector3.zero;
            offsetInitialized = true;
        }
        smoothOffset = Vector3.SmoothDamp(smoothOffset, offset, ref smoothOffsetVelocity, armSmoothTime);
        return smoothOffset;
    }

    bool FindArmBones()
    {
        if (leftUpper != null && leftLower != null && leftHandBone != null && rightUpper != null && rightLower != null && rightHandBone != null)
        {
            return true;
        }
        if (animator != null && animator.isHuman && animator.avatar != null)
        {
            leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLower = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLower = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }
        if (leftLower == null || rightLower == null)
        {
            leftLower = FindChild(transform, "LeftForeArm");
            rightLower = FindChild(transform, "RightForeArm");
            leftUpper = leftLower != null ? leftLower.parent : null;
            rightUpper = rightLower != null ? rightLower.parent : null;
            leftHandBone = leftLower != null ? FindChild(leftLower, "LeftHand") : null;
            rightHandBone = rightLower != null ? FindChild(rightLower, "RightHand") : null;
        }
        bool found = leftUpper != null && leftLower != null && leftHandBone != null && rightUpper != null && rightLower != null && rightHandBone != null;
        if (found)
        {
            MeasureArms();
        }
        return found;
    }

    void MeasureArms()
    {
        float length = Vector3.Distance(leftUpper.position, leftLower.position) + Vector3.Distance(leftLower.position, leftHandBone.position);
        if (length > 0.01f)
        {
            armLength = length;
            shoulderHeight = 0.5f * (leftUpper.position.y + rightUpper.position.y) - transform.position.y;
        }
    }

    /// <summary>
    /// Two-bone IK: turns the upper arm and forearm so the wrist reaches the target (as far as the arm allows), with
    /// the elbow bending towards the hint; blended over the animated pose by weight.
    /// </summary>
    static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 hint, float weight)
    {
        Quaternion animatedUpper = upper.rotation;
        Quaternion animatedLower = lower.rotation;

        Vector3 shoulder = upper.position;
        float upperLength = Vector3.Distance(shoulder, lower.position);
        float lowerLength = Vector3.Distance(lower.position, hand.position);
        Vector3 toTarget = target - shoulder;
        float reach = Mathf.Clamp(toTarget.magnitude, 0.05f * (upperLength + lowerLength), 0.999f * (upperLength + lowerLength));
        Vector3 direction = toTarget.sqrMagnitude > 1e-8f ? toTarget.normalized : upper.forward;
        float cosShoulder = Mathf.Clamp((upperLength * upperLength + reach * reach - lowerLength * lowerLength) / (2f * upperLength * reach), -1f, 1f);
        Vector3 bend = Vector3.ProjectOnPlane(hint - shoulder, direction);
        if (bend.sqrMagnitude < 1e-8f)
        {
            bend = Vector3.ProjectOnPlane(Vector3.down, direction);
        }
        bend.Normalize();
        Vector3 elbow = shoulder + direction * (upperLength * cosShoulder) + bend * (upperLength * Mathf.Sqrt(1f - cosShoulder * cosShoulder));
        Vector3 wrist = shoulder + direction * reach;

        upper.rotation = Quaternion.FromToRotation(lower.position - shoulder, elbow - shoulder) * animatedUpper;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, wrist - lower.position) * lower.rotation;
        Quaternion solvedUpper = upper.rotation;
        Quaternion solvedLower = lower.rotation;

        upper.rotation = Quaternion.Slerp(animatedUpper, solvedUpper, weight);
        lower.rotation = Quaternion.Slerp(animatedLower, solvedLower, weight); // (the hand keeps its animated pose on the wrist)
    }

    /// <summary>The ball now "belongs" to the CB: the receiver's hands and catch checks ignore it from here on.</summary>
    void MakeBallDead(Rigidbody ball)
    {
        TestCollision flight = ball.GetComponent<TestCollision>();
        if (flight != null)
        {
            flight.wrBodyPosition = transform;
        }
    }

    void TrackBall()
    {
        Rigidbody ball = qb != null ? qb.GetBallInFlight() : null;
        lastBall = ball;
        if (ball != null)
        {
            lastBallPosition = ball.position;
        }
    }

    static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        Vector3 ab = b - a;
        float length = ab.sqrMagnitude;
        if (length < 1e-8f)
        {
            return a;
        }
        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / length);
        return a + ab * t;
    }

    static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }
        return null;
    }
}
