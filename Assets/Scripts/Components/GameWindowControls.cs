using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Window and performance controls for builds (also works in the Editor). Adds itself when a scene loads, so it
/// needs no setup.
///
///   Esc          - leave full screen (go to a window); Esc again goes back to full screen.
///   Shift + Esc  - quit the game (Cmd + Q also quits on a Mac).
///   F3           - show / hide a frames-per-second counter.
///   F5 / F6      - lower / raise the graphics quality level (shown with the FPS counter).
///
/// For speed it also caps the full-screen resolution at Max Full Screen Height (1080p by default). A 4K TV or a
/// Retina laptop screen otherwise makes the game draw 4x as many pixels for little visible gain.
/// </summary>
public class GameWindowControls : MonoBehaviour
{
    public static bool Enabled = true;
    [Tooltip("Highest full-screen resolution (height in pixels) the game renders at; 0 = the screen's own (no cap).")]
    public int maxFullScreenHeight = 1080;
    [Tooltip("Window size when you leave full screen with Esc.")]
    public Vector2Int windowedSize = new Vector2Int(1280, 720);
    public bool showFps = false;

    float fpsSmoothed;
    GUIStyle fpsStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnSceneLoad()
    {
        if (!Enabled || FindObjectOfType<GameWindowControls>() != null)
        {
            return;
        }
        GameObject controls = new GameObject("GameWindowControls");
        DontDestroyOnLoad(controls);
        controls.AddComponent<GameWindowControls>();
    }

    void Start()
    {
        if (!Application.isEditor && Screen.fullScreen)
        {
            GoFullScreen();
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (keyboard.shiftKey.isPressed)
            {
                Quit();
            }
            else if (Screen.fullScreen)
            {
                Screen.SetResolution(windowedSize.x, windowedSize.y, FullScreenMode.Windowed);
            }
            else
            {
                GoFullScreen();
            }
        }
        if (keyboard.f3Key.wasPressedThisFrame)
        {
            showFps = !showFps;
        }
        if (keyboard.f5Key.wasPressedThisFrame)
        {
            QualitySettings.SetQualityLevel(Mathf.Max(0, QualitySettings.GetQualityLevel() - 1), true);
        }
        if (keyboard.f6Key.wasPressedThisFrame)
        {
            QualitySettings.SetQualityLevel(Mathf.Min(QualitySettings.names.Length - 1, QualitySettings.GetQualityLevel() + 1), true);
        }
        float fps = 1f / Mathf.Max(1e-4f, Time.unscaledDeltaTime);
        fpsSmoothed = fpsSmoothed <= 0f ? fps : Mathf.Lerp(fpsSmoothed, fps, 0.1f);
    }

    /// <summary>Full screen (borderless) at the display's resolution, capped at maxFullScreenHeight.</summary>
    void GoFullScreen()
    {
        Resolution display = Screen.currentResolution;
        int width = display.width;
        int height = display.height;
        if (maxFullScreenHeight > 0 && height > maxFullScreenHeight)
        {
            width = Mathf.RoundToInt(width * (maxFullScreenHeight / (float)height));
            height = maxFullScreenHeight;
        }
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnGUI()
    {
        if (!showFps)
        {
            return;
        }
        if (fpsStyle == null)
        {
            fpsStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            fpsStyle.normal.textColor = Color.yellow;
        }
        string quality = QualitySettings.names.Length > 0 ? QualitySettings.names[QualitySettings.GetQualityLevel()] : "";
        GUI.Label(new Rect(12, 8, 600, 32), string.Format("{0:0} FPS  ({1:0.0} ms)   Quality: {2}  [F5/F6]   {3}x{4}",
            fpsSmoothed, 1000f / Mathf.Max(1f, fpsSmoothed), quality, Screen.width, Screen.height), fpsStyle);
    }
}
