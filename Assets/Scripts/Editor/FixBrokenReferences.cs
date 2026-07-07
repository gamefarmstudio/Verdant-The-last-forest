using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixBrokenReferences
{
    [MenuItem("Tools/Fix Broken Object References in Scene")]
    static void Fix()
    {
        int fixed_ = 0;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.name == null) continue;
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var prop = so.GetIterator();
                bool changed = false;
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference
                        && prop.objectReferenceValue == null
                        && prop.objectReferenceInstanceIDValue != 0)
                    {
                        Debug.LogWarning($"Clearing broken ref on {go.name} / {comp.GetType().Name} / {prop.propertyPath}", go);
                        prop.objectReferenceValue = null;
                        changed = true;
                        fixed_++;
                    }
                }
                if (changed) so.ApplyModifiedProperties();
            }
        }

        if (fixed_ == 0) Debug.Log("[FixBrokenReferences] No broken references found.");
        else
        {
            Debug.Log($"[FixBrokenReferences] Cleared {fixed_} broken reference(s). Save the scene now (Ctrl+S).");
            EditorSceneManager.MarkAllScenesDirty();
        }
    }
}
