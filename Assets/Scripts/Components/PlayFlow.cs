using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Play flow UI (adds itself; works in the Editor and in builds):
///
///  - A "Restart Play" button (top right) on screen at all times. It, the R key, or a controller's Select / Back
///    button reloads the scene, so every player, the ball and the camera go back to where they start.
///  - Interception: the screen flashes with "INTERCEPTION!", every player's controls switch off, and a big Restart
///    Play button (or R / Select / Start / A on any controller) starts the play again.
///  - Short banners (e.g. "TACKLED", "PASS BROKEN UP") for other play results.
/// </summary>
public class PlayFlow : MonoBehaviour
{
    static PlayFlow instance;

    bool interception;
    float interceptionTime;
    string banner;
    float bannerTime;
    GUIStyle bigStyle;
    GUIStyle bannerStyle;
    GUIStyle buttonStyle;
    GUIStyle bigButtonStyle;
    Texture2D white;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null)
        {
            return;
        }
        GameObject flow = new GameObject("PlayFlow");
        DontDestroyOnLoad(flow);
        instance = flow.AddComponent<PlayFlow>();
        SceneManager.sceneLoaded += (scene, mode) => instance.ResetPlay();
    }

    /// <summary>Show a short result banner for a few seconds.</summary>
    public static void ShowBanner(string text)
    {
        if (instance != null)
        {
            instance.banner = text;
            instance.bannerTime = Time.unscaledTime;
        }
    }

    /// <summary>Interception: flash the screen and end everyone's controls until the play is restarted.</summary>
    public static void Interception()
    {
        if (instance == null || instance.interception)
        {
            return;
        }
        instance.interception = true;
        instance.interceptionTime = Time.unscaledTime;
        foreach (PlayerInput input in FindObjectsOfType<PlayerInput>())
        {
            input.DeactivateInput();
        }
    }

    /// <summary>Reload the scene: everything goes back to its starting place.</summary>
    public static void RestartPlay()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
        }
        else
        {
            SceneManager.LoadScene(scene.name); // (the scene needs to be in File > Build Settings to be reloaded)
        }
    }

    void ResetPlay()
    {
        interception = false;
        banner = null;
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        bool restart = keyboard != null && keyboard.rKey.wasPressedThisFrame;
        foreach (Gamepad pad in Gamepad.all)
        {
            restart |= pad.selectButton.wasPressedThisFrame;
            if (interception && Time.unscaledTime - interceptionTime > 1f)
            {
                restart |= pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
            }
        }
        if (restart)
        {
            RestartPlay();
        }
    }

    void OnGUI()
    {
        MakeStyles();
        float w = Screen.width;
        float h = Screen.height;

        if (interception)
        {
            // Flash: red / gold, about three times a second.
            float flash = Mathf.PingPong((Time.unscaledTime - interceptionTime) * 3f, 1f);
            GUI.color = Color.Lerp(new Color(0.85f, 0.05f, 0.05f, 0.55f), new Color(1f, 0.8f, 0.1f, 0.55f), flash);
            GUI.DrawTexture(new Rect(0, 0, w, h), white);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, h * 0.25f, w, h * 0.2f), "INTERCEPTION!", bigStyle);
            GUI.Label(new Rect(0, h * 0.42f, w, h * 0.08f), "The CB picked it off. Controls are off for this play.", bannerStyle);
            if (GUI.Button(new Rect(w * 0.5f - 180, h * 0.58f, 360, 80), "Restart Play", bigButtonStyle))
            {
                RestartPlay();
            }
            GUI.Label(new Rect(0, h * 0.58f + 90, w, 40), "or press R / Select / Start / A", bannerStyle);
            return;
        }

        if (banner != null && Time.unscaledTime - bannerTime < 3f)
        {
            GUI.Label(new Rect(0, h * 0.12f, w, h * 0.12f), banner, bigStyle);
        }
        if (GUI.Button(new Rect(w - 230, 12, 218, 44), "Restart Play (R)", buttonStyle))
        {
            RestartPlay();
        }
    }

    void MakeStyles()
    {
        if (bigStyle != null)
        {
            return;
        }
        white = Texture2D.whiteTexture;
        bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bigStyle.normal.textColor = Color.white;
        bannerStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bannerStyle.normal.textColor = Color.white;
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
        bigButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 34, fontStyle = FontStyle.Bold };
    }
}
