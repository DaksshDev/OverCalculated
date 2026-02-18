using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

// 🧩 Use like: [SettingsHeader("My Settings {}")]
public class SettingsHeaderAttribute : PropertyAttribute
{
    public string title;
    public SettingsHeaderAttribute(string title)
    {
        this.title = title;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SettingsHeaderAttribute))]
public class SettingsHeaderDrawer : DecoratorDrawer
{
    private static Font customFont;

    public override float GetHeight()
    {
        return 62f; // Just header height
    }

    public override void OnGUI(Rect position)
    {
        SettingsHeaderAttribute header = (SettingsHeaderAttribute)attribute;

        // 🧱 Rect for header area
        Rect headerRect = new Rect(position.x + 7, position.y + 8, position.width - 14, 34);

        // 💛 Yellow fill
        Color yellowFill = new Color(1f, 0.92f, 0.016f, 1f);
        EditorGUI.DrawRect(headerRect, yellowFill);

        // ⬛ Black border
        Handles.BeginGUI();
        Handles.color = Color.black;
        Handles.DrawAAPolyLine(2.5f, new Vector3[]
        {
            new(headerRect.xMin, headerRect.yMin),
            new(headerRect.xMax, headerRect.yMin),
            new(headerRect.xMax, headerRect.yMax),
            new(headerRect.xMin, headerRect.yMax),
            new(headerRect.xMin, headerRect.yMin)
        });
        Handles.EndGUI();

        // 🔠 Load custom font once
        if (customFont == null)
        {
            customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/EditorTools/InspectorGUIS/fonts/CNR.ttf");
            if (customFont == null)
                Debug.LogWarning("⚠️ Could not load font for custom EditorGUI");
        }

        // ✨ Text style
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            font = customFont != null ? customFont : EditorStyles.boldLabel.font,
            normal = { textColor = Color.black },
            hover = { textColor = Color.black },
            active = { textColor = Color.black },
            focused = { textColor = Color.black },
            padding = new RectOffset(0, 0, 2, 2)
        };

        // 🧩 Replace {} with filename - get from current selection
        string finalTitle = header.title;
        if (finalTitle.Contains("{}"))
        {
            Object target = Selection.activeObject;
            if (target != null)
            {
                string path = AssetDatabase.GetAssetPath(target);
                if (!string.IsNullOrEmpty(path))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    finalTitle = finalTitle.Replace("{}", fileName);
                }
            }
        }

        // 🔤 Draw header text
        GUI.Label(headerRect, finalTitle.ToUpper(), style);
    }
}
#endif