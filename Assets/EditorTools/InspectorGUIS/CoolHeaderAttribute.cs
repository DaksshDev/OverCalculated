using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// 🧩 Use like: [CoolHeader("My Header")]
public class CoolHeaderAttribute : PropertyAttribute
{
    public string title;
    public CoolHeaderAttribute(string title)
    {
        this.title = title;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(CoolHeaderAttribute))]
public class CoolHeaderDrawer : DecoratorDrawer
{
    private static Font customFont;

    public override float GetHeight() => 62f; // ⬆ taller header

    public override void OnGUI(Rect position)
    {
        CoolHeaderAttribute header = (CoolHeaderAttribute)attribute;

        // 🧱 Rect with extra spacing
        Rect bgRect = new Rect(position.x + 7, position.y + 18, position.width - 14, 34);

        // 🌫️ Smooth dark gradient background
        Color top = new Color(0.13f, 0.13f, 0.13f);
        Color bottom = new Color(0.09f, 0.09f, 0.09f);
        EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, bgRect.width, bgRect.height / 2), top);
        EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y + bgRect.height / 2, bgRect.width, bgRect.height / 2), bottom);

        // 💙 Light blue border (more visible)
        Color borderColor = new Color(0.4f, 0.8f, 1f, 1f);
        Handles.BeginGUI();
        Handles.color = borderColor;
        Handles.DrawAAPolyLine(2.5f, new Vector3[] {
            new(bgRect.xMin, bgRect.yMin),
            new(bgRect.xMax, bgRect.yMin),
            new(bgRect.xMax, bgRect.yMax),
            new(bgRect.xMin, bgRect.yMax),
            new(bgRect.xMin, bgRect.yMin)
        });
        Handles.EndGUI();

        // 🔠 Load custom font
        if (customFont == null)
        {
            customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/EditorTools/InspectorGUIS/fonts/CNR.ttf");
            if (customFont == null)
                Debug.LogWarning("⚠️ Could not load font for custom EditorGUI");
        }

        // ✨ Text style – bold & big
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 28, // ⬆ bigger text size
            font = customFont != null ? customFont : EditorStyles.boldLabel.font,
            normal = { textColor = Color.white },
            hover = { textColor = Color.white },
            active = { textColor = Color.white },
            focused = { textColor = Color.white },
            padding = new RectOffset(0, 0, 2, 2)
        };

        GUI.Label(bgRect, header.title.ToUpper(), style); // 🔊 uppercase for authority 😎
    }
}
#endif
