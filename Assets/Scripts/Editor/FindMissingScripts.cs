using UnityEditor;
using UnityEngine;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts in Scene")]
    static void FindInScene()
    {
        int found = 0;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.name == null) continue; // skip prefabs
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null)
                {
                    Debug.LogWarning($"Missing script on: {GetPath(go)}", go);
                    found++;
                    break;
                }
            }
        }
        if (found == 0) Debug.Log("[FindMissingScripts] No missing scripts found.");
        else Debug.LogWarning($"[FindMissingScripts] Found {found} GameObject(s) with missing scripts. See warnings above.");
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
