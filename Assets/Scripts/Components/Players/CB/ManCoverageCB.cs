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
/// Defending the pass: if the ball comes within Swat Reach Meters of his hands he knocks it away (the pass is then
/// dead: the receiver can't catch it). If it comes within Intercept Reach Meters while he's facing it, he catches
/// it (Intercept Chance), then runs it back upfield.
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
        if (coveredReceiver != null)
        {
            // Line up off the receiver, a little inside.
            Vector3 start = coveredReceiver.transform.position + downfield * (startCushionYards * Yard()) + InsideDirection() * (insideLeverageYards * Yard());
            transform.position = new Vector3(start.x, coveredReceiver.transform.position.y, start.z);
            transform.rotation = Quaternion.LookRotation(-downfield, Vector3.up); // facing the QB
        }
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

        if (tackled)
        {
            // Drive through him for a moment, then stop.
            wantVelocity = Time.time - tackleTime < driveSeconds ? Vector3.ProjectOnPlane(body.velocity, Vector3.up) : Vector3.zero;
        }
        else if (coveredReceiver.caughtFootball != null)
        {
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
            if (ballToMyMan && !defended)
            {
                DefendPass(ball);
            }
            if (ballToMyMan && !defended && Time.time - qb.GetTimeThrown() >= ballReactionTime)
            {
                // Break on the ball: run to where it's going (a touch in front, to undercut it).
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

        // Accelerate towards it (horizontal only; the Rigidbody's drag is made up for, like the WR's run).
        Vector3 current = Vector3.ProjectOnPlane(body.velocity, Vector3.up);
        Vector3 next = Vector3.MoveTowards(current, wantVelocity, acceleration * Time.fixedDeltaTime);
        float dragKeep = Mathf.Max(0.05f, 1f - body.drag * Time.fixedDeltaTime);
        body.AddForce((next / dragKeep - current) / Time.fixedDeltaTime * body.mass, ForceMode.Force);

        Face(next);
        if (animator != null)
        {
            animator.SetBool("runningForward", next.magnitude > 0.15f * Mathf.Max(1f, topSpeed));
        }
        TrackBall();
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
        if (ball != null && !intercepted && Time.time - qb.GetTimeThrown() >= ballReactionTime)
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
    /// If the ball passed within reach of his hands this physics step: intercept (close, facing it, and he wins the
    /// chance) or swat it away. Checks the whole path since the last step, so a fast ball can't skip past.
    /// </summary>
    void DefendPass(Rigidbody ball)
    {
        Vector3 hands = HandsCenter();
        Vector3 from = ball == lastBall ? lastBallPosition : ball.position;
        Vector3 closest = ClosestPointOnSegment(from, ball.position, hands);
        float distance = Vector3.Distance(closest, hands);
        float unitsPerMeter = UnitsPerMeter();
        if (distance > swatReachMeters * unitsPerMeter)
        {
            return;
        }
        Vector3 toBall = Vector3.ProjectOnPlane(closest - transform.position, Vector3.up);
        bool facing = toBall.sqrMagnitude < 1e-4f || Vector3.Dot(transform.forward, toBall.normalized) > 0.2f;
        if (distance <= interceptReachMeters * unitsPerMeter && facing && Random.value < interceptChance)
        {
            Intercept(ball);
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
        qb.NotePassDefended("SWATTED by the CB", null);
        PlayFlow.ShowBanner("PASS BROKEN UP");
    }

    void Intercept(Rigidbody ball)
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
        carryHand = FindChild(transform, "RightHand") ?? FindChild(transform, "LeftHand") ?? transform;
        ball.transform.SetParent(carryHand, true);
        ball.transform.position = carryHand.position;
        qb.NotePassDefended("INTERCEPTED by the CB", transform);
        PlayFlow.Interception();
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
