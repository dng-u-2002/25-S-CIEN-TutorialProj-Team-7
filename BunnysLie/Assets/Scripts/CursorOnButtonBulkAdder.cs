#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class CursorOnButtonBulkAdder
{
    // 1) 열린 모든 씬의 Button에 CursorOnButton 추가
    [MenuItem("Tools/Cursor/Attach 'CursorOnButton' to All Buttons (Open Scenes)")]
    public static void AttachAll()
    {
        int total = 0, added = 0, skipped = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var btn in root.GetComponentsInChildren<Button>(true))
                {
                    total++;
                    if (btn.GetComponent<CursorOnButton>() != null) { skipped++; continue; }

                    Undo.AddComponent<CursorOnButton>(btn.gameObject);
                    EditorUtility.SetDirty(btn.gameObject);
                    added++;
                }
            }
        }

        Debug.Log($"[Cursor] Buttons: {total}, Added: {added}, Skipped(already had): {skipped}");
    }

    // 2) 선택한 오브젝트(들) 하위의 Button에만 추가
    [MenuItem("Tools/Cursor/Attach to Selected Hierarchy (Buttons only)")]
    public static void AttachToSelected()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("[Cursor] No selection.");
            return;
        }

        int added = 0, skipped = 0;
        foreach (var go in selection)
        {
            foreach (var btn in go.GetComponentsInChildren<Button>(true))
            {
                if (btn.GetComponent<CursorOnButton>() != null) { skipped++; continue; }

                Undo.AddComponent<CursorOnButton>(btn.gameObject);
                EditorUtility.SetDirty(btn.gameObject);
                added++;
            }
        }

        Debug.Log($"[Cursor] Added: {added}, Skipped: {skipped} (on selection)");
    }

    // 3) 열린 모든 씬에서 CursorOnButton 제거(되돌리기용)
    [MenuItem("Tools/Cursor/Remove 'CursorOnButton' from All Buttons (Open Scenes)")]
    public static void RemoveAll()
    {
        int removed = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var comp in root.GetComponentsInChildren<CursorOnButton>(true))
                {
                    Undo.DestroyObjectImmediate(comp);
                    EditorUtility.SetDirty(comp.gameObject);
                    removed++;
                }
            }
        }

        Debug.Log($"[Cursor] Removed CursorOnButton components: {removed}");
    }

    // 4) CursorManager가 없으면 만들어 주는 유틸(선택 사항)
    [MenuItem("Tools/Cursor/Create CursorManager (if missing)")]
    public static void EnsureCursorManager()
    {
        if (Object.FindObjectOfType<CursorManager>() != null)
        {
            Debug.Log("[Cursor] CursorManager already exists.");
            return;
        }

        var go = new GameObject("CursorManager");
        Undo.RegisterCreatedObjectUndo(go, "Create CursorManager");
        go.AddComponent<CursorManager>();
        Debug.Log("[Cursor] Created CursorManager in the active scene.");
    }
}
#endif
