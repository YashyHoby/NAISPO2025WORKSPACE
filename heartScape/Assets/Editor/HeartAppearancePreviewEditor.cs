#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeartAppearancePreview))]
public class HeartAppearancePreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            if (!Application.isPlaying)
            {
                foreach (var t in targets)
                {
                    ((HeartAppearancePreview)t).ApplyPreview();
                }
            }
        }

        var preview = (HeartAppearancePreview)target;
        if (preview.mapper == null || preview.previewVisual == null)
        {
            EditorGUILayout.HelpBox("Assign HeartAppearanceMapper and HeartVisual to use the preview.", MessageType.Info);
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !Application.isPlaying;
            if (GUILayout.Button("Apply Preview", GUILayout.Height(24f)))
            {
                foreach (var t in targets)
                {
                    ((HeartAppearancePreview)t).ApplyPreview();
                }
            }
            GUI.enabled = true;
        }
    }
}
#endif

