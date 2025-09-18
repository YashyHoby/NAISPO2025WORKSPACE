using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class HeartAppearancePreview : MonoBehaviour
{
    [Header("Preview Targets")]
    public HeartAppearanceMapper mapper;
    public HeartVisual previewVisual;

    [Header("Preview Profile")]
    public HeartProfile previewProfile = new HeartProfile
    {
        uid = "PREVIEW",
        hr = 72f,
        cv = 0.2f,
        range = 18f,
        mean = 80f
    };

    [Header("Options")]
    public bool autoApply = true;
#if UNITY_EDITOR
    public bool animatePulseInEditMode = true;
#endif

    HeartProfile capturedProfile;
    bool hasCapturedProfile;

#if UNITY_EDITOR
    bool editorSubscribed;
    double lastPreviewTime;
#endif

    void Reset()
    {
        EnsureProfileInstance();
        AutoAssign();
    }

    void OnEnable()
    {
        EnsureProfileInstance();
        AutoAssign();
        CaptureOriginalProfile();
#if UNITY_EDITOR
        SubscribeEditorUpdate(true);
#endif
        if (!Application.isPlaying && autoApply)
        {
            ApplyPreview();
        }
    }

    void OnDisable()
    {
        RestoreOriginalProfile();
#if UNITY_EDITOR
        EditorApplication.delayCall -= ApplyPreviewDelayed;
        SubscribeEditorUpdate(false);
#endif
    }

    void OnValidate()
    {
        EnsureProfileInstance();
        if (Application.isPlaying) return;
#if UNITY_EDITOR
        if (autoApply)
        {
            SchedulePreviewApply();
        }
#else
        if (autoApply)
        {
            ApplyPreview();
        }
#endif
    }

    void EnsureProfileInstance()
    {
        if (previewProfile == null)
        {
            previewProfile = new HeartProfile
            {
                uid = "PREVIEW",
                hr = 72f,
                cv = 0.2f,
                range = 18f,
                mean = 80f
            };
        }
    }

    void AutoAssign()
    {
        if (mapper == null)
        {
            mapper = GetComponentInChildren<HeartAppearanceMapper>(true);
        }

        if (previewVisual == null)
        {
            previewVisual = GetComponentInChildren<HeartVisual>(true);
        }
    }

    void CaptureOriginalProfile()
    {
        if (previewVisual == null || hasCapturedProfile) return;
        capturedProfile = previewVisual.profile;
        hasCapturedProfile = true;
    }

    void RestoreOriginalProfile()
    {
        if (previewVisual != null && hasCapturedProfile)
        {
            previewVisual.profile = capturedProfile;
        }

        hasCapturedProfile = false;
    }

    [ContextMenu("Apply Preview Now")]
    public void ApplyPreview()
    {
        if (Application.isPlaying) return;
        if (mapper == null || previewVisual == null || previewProfile == null) return;

        CaptureOriginalProfile();
        mapper.Apply(previewProfile, previewVisual);
        previewVisual.profile = previewProfile;
#if UNITY_EDITOR
        float previewTime = animatePulseInEditMode ? (float)EditorApplication.timeSinceStartup : 0f;
        previewVisual.RefreshMaterial(previewTime);
        EditorUtility.SetDirty(previewVisual);
        EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    void SchedulePreviewApply()
    {
        EditorApplication.delayCall -= ApplyPreviewDelayed;
        EditorApplication.delayCall += ApplyPreviewDelayed;
    }

    void ApplyPreviewDelayed()
    {
        if (this == null) return;
        if (Application.isPlaying || !autoApply) return;

        EnsureProfileInstance();
        AutoAssign();
        ApplyPreview();
    }

    void SubscribeEditorUpdate(bool subscribe)
    {
        if (subscribe)
        {
            if (!editorSubscribed)
            {
                EditorApplication.update += OnEditorUpdate;
                editorSubscribed = true;
            }
        }
        else if (editorSubscribed)
        {
            EditorApplication.update -= OnEditorUpdate;
            editorSubscribed = false;
        }
    }

    void OnEditorUpdate()
    {
        if (Application.isPlaying || !animatePulseInEditMode) return;
        if (previewVisual == null) return;

        double now = EditorApplication.timeSinceStartup;
        if (Mathf.Approximately((float)now, (float)lastPreviewTime)) return;

        lastPreviewTime = now;
        previewVisual.RefreshMaterial((float)now);
    }
#endif
}

