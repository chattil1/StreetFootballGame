using UnityEngine;

/// <summary>
/// Added to a tackled player: real-size gravity on his body (the players are ~35x scale, so normal gravity would
/// let him float down in slow motion) and some extra drag on his spin once he's down, so he settles on the turf.
/// </summary>
public class TackledBody : MonoBehaviour
{
    public float gravityMultiplier = 33f;
    public float settleAfterSeconds = 1.2f;
    Rigidbody body;
    float startTime;

    void Start()
    {
        body = GetComponent<Rigidbody>();
        startTime = Time.time;
    }

    void FixedUpdate()
    {
        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        if (Time.time - startTime > settleAfterSeconds)
        {
            body.angularDrag = 5f;
            body.drag = Mathf.Max(body.drag, 2f);
        }
    }
}
