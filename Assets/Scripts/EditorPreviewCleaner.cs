#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorPreviewCleaner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CleanupEditorOnlyObjects()
    {
        foreach (GameObject obj in Object.FindObjectsOfType<GameObject>())
        {
            if (obj.CompareTag("EditorOnly"))
            {
                Object.Destroy(obj);
            }
        }
    }
}

#endif