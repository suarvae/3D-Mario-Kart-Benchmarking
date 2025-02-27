using UnityEngine;
using UnityEditor;


public class UnityCommandServerWindow : EditorWindow
{
    private Vector2 scrollPosition = Vector2.zero;

    [MenuItem("Unakin/Open Unakin Bridge Window")]
    public static void ShowWindow() 
    {
        GetWindow<UnityCommandServerWindow>("Unakin Bridge");
    }

    void OnEnable()
    {
        // init UI
        borderStyle = new GUIStyle();
        borderStyle.border = new RectOffset(6, 6, 6, 6);  // Set the thickness of the border
        borderStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f));  // Grey colour
    }

    void OnDisable()
    {
    }   

    // Function to create a 1x1 texture
    Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    GUIStyle borderStyle;

    void OnGUI()
    {
        int boxWidth = 12;

        // Connection Status
        GUILayout.BeginHorizontal();
        GUILayout.Label("Connection Status:", EditorStyles.boldLabel);
        if (UnakinBridgeServer.IsConnected)
        {
            GUILayout.Box("", GUILayout.Width(boxWidth), GUILayout.Height(boxWidth));
            EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), Color.green); // Green light
            GUILayout.Label("Connected", EditorStyles.label);
        }
        else
        {
            GUILayout.Box("", GUILayout.Width(boxWidth), GUILayout.Height(boxWidth));
            EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), Color.red); // Red light
            GUILayout.Label("Not Connected", EditorStyles.label);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Display Port Number
        GUILayout.BeginHorizontal();
        GUILayout.Label("Active Port:", EditorStyles.boldLabel);
        GUILayout.Label(UnakinBridgeServer.ActivePort != -1 ? UnakinBridgeServer.ActivePort.ToString() : "Not Assigned", EditorStyles.label);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Log Section
        GUILayout.Label("Log:", EditorStyles.boldLabel);
        GUILayout.BeginVertical(borderStyle);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        for (int i = UnakinBridgeServer.LogMessages.Count - 1; i >= 0; i--)
        {
            string message = UnakinBridgeServer.LogMessages[i].TrimEnd('\r', '\n');
            GUILayout.Label(message, EditorStyles.label);
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // Logging Toggle
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Logging Enabled:");
        bool newLoggingState = GUILayout.Toggle(UnakinBridgeServer.LoggingEnabled, "");
        if (newLoggingState != UnakinBridgeServer.LoggingEnabled)
        {
            UnakinBridgeServer.LoggingEnabled = newLoggingState;
            EditorPrefs.SetBool(UnakinBridgeServer.LoggingEnabledKey, newLoggingState); 
        }
        GUILayout.EndHorizontal();
    }
}
