using UnityEngine;

/// <summary>
/// Scales the football field so it is the right size for the players: a real field is 120 yards from end line to
/// end line (100 yards plus two 10-yard end zones), and an average NFL player is about 6'2" (74 inches) tall. It
/// measures a receiver's height in the scene and the distance between the two goal posts (which stand on the end
/// lines), then scales the field so that distance is 120 yards of that player's size.
///
/// It needs no setup: it adds itself when the scene loads, finds the field by its "Field" tag, and waits until the
/// UMA characters have been built (so the measured height is real) before scaling, once. The field is scaled about
/// the top of its collider, so the surface the players stand on stays at the same height and nobody falls or floats.
/// To turn it off, set Enabled to false (or remove this file).
/// </summary>
[DefaultExecutionOrder(-1000)]
public class FieldScaler : MonoBehaviour
{
    public static bool Enabled = true;
    [Tooltip("Average NFL player height, in inches (6'2\" = 74).")]
    public float playerHeightInches = 74f;
    [Tooltip("End line to end line, in yards (100-yard field + two 10-yard end zones).")]
    public float fieldLengthYards = 120f;
    [Tooltip("Seconds to wait for the UMA characters to build before falling back to 1.8 x the player's scale for his height.")]
    public float maxWaitForPlayers = 3f;

    Transform field;
    Collider fieldCollider;
    float startTime;
    bool done;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnSceneLoad()
    {
        if (!Enabled || FindObjectOfType<FieldScaler>() != null)
        {
            return;
        }
        new GameObject("FieldScaler").AddComponent<FieldScaler>();
    }

    void Start()
    {
        startTime = Time.time;
        GameObject tagged = null;
        try
        {
            tagged = GameObject.FindWithTag("Field");
        }
        catch (UnityException)
        {
            tagged = null; // no "Field" tag in this project
        }
        if (tagged == null)
        {
            Debug.LogWarning("FieldScaler: no object tagged Field, so the field was not scaled.");
            done = true;
            return;
        }
        fieldCollider = tagged.GetComponent<Collider>();
        field = tagged.transform.root; // the whole field (turf, lines, goal posts)
    }

    void Update()
    {
        if (done || !Enabled)
        {
            return;
        }
        QB qb = FindObjectOfType<QB>();
        WR receiver = qb != null && qb.wrX != null ? qb.wrX.GetComponent<WR>() : null;
        if (receiver == null)
        {
            receiver = FindObjectOfType<WR>();
        }
        if (receiver == null)
        {
            return;
        }
        bool playersBuilt = receiver.GetComponentInChildren<SkinnedMeshRenderer>() != null;
        if (!playersBuilt && Time.time - startTime < maxWaitForPlayers)
        {
            return; // UMA hasn't built the character yet
        }
        done = true;

        float playerHeight = QB.MeasurePlayerHeight(receiver.gameObject);
        float yard = playerHeight / (playerHeightInches / 36f);
        float wantedLength = fieldLengthYards * yard;
        float currentLength = MeasureEndLineToEndLine();
        if (currentLength <= 0f)
        {
            Debug.LogWarning("FieldScaler: couldn't find the goal posts, so the field was not scaled.");
            return;
        }
        float factor = wantedLength / currentLength;

        // Scale about the top of the field's collider (at the field's x / z), so the playing surface keeps its height.
        float surfaceHeight = fieldCollider != null ? fieldCollider.bounds.max.y : field.position.y;
        Vector3 pivot = new Vector3(field.position.x, surfaceHeight, field.position.z);
        field.localScale *= factor;
        field.position = pivot + (field.position - pivot) * factor;
        Physics.SyncTransforms();

        Debug.Log(string.Format("FieldScaler: player {0:0.0} units tall = {1:0.0} units per yard; field was {2:0} units ({3:0.0} yards) " +
            "end line to end line, scaled x{4:0.00} to {5:0} units ({6} yards). Field scale is now {7:0.0}.",
            playerHeight, yard, currentLength, currentLength / yard, factor, wantedLength, fieldLengthYards, field.localScale.x));
    }

    /// <summary>
    /// The distance between the goal posts' uprights (on the end lines) along the field's length, in world units. The
    /// posts are the field's active meshes named "...Gaol..." ("goal" in the asset); each one's upright is its edge
    /// nearest midfield. Falls back to the asset's proportions of its collider if the posts aren't found.
    /// </summary>
    float MeasureEndLineToEndLine()
    {
        Vector3 center = field.position;
        Vector3 length = field.forward;
        float nearestFar = float.MaxValue;
        float nearestNear = float.MaxValue;
        foreach (Renderer post in field.GetComponentsInChildren<Renderer>(false))
        {
            if (!post.name.ToLower().Contains("gaol") && !post.name.ToLower().Contains("goal"))
            {
                continue;
            }
            Bounds bounds = post.bounds;
            float min = Vector3.Dot(bounds.min - center, length);
            float max = Vector3.Dot(bounds.max - center, length);
            float mid = (min + max) * 0.5f;
            if (mid > 0f)
            {
                nearestFar = Mathf.Min(nearestFar, Mathf.Min(min, max));
            }
            else
            {
                nearestNear = Mathf.Min(nearestNear, -Mathf.Max(min, max));
            }
        }
        if (nearestFar < float.MaxValue && nearestNear < float.MaxValue)
        {
            return nearestFar + nearestNear;
        }
        if (fieldCollider != null)
        {
            // In the asset the uprights are 9275 of the turf's 12236 length.
            return fieldCollider.bounds.size.z * (9275f / 12236f);
        }
        return 0f;
    }
}
