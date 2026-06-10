using System.IO;
using UnityEditor;
using UnityEngine;

public static class RTSScreens
{
    const string OutDir = @"C:\Users\blind\AppData\Roaming\Claude\local-agent-mode-sessions\1be9def4-ae4b-4634-bfeb-f0688cac2fc1\c3728a67-9427-425c-b84a-5cf67ce6eda9\local_2b6c8d99-3950-422c-ba0f-71ef387e580d\outputs";

    [MenuItem("RTS/Screenshot Game")]
    public static void ShotGame()
    {
        Directory.CreateDirectory(OutDir);
        string p = Path.Combine(OutDir, "unity_game.png");
        if (File.Exists(p)) File.Delete(p);
        ScreenCapture.CaptureScreenshot(p, 1);
        var t = System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if (t != null)
        {
            var gv = EditorWindow.GetWindow(t);
            if (gv != null) gv.Repaint();
        }
        Debug.Log("Game-Screenshot angefordert: " + p);
    }
}
