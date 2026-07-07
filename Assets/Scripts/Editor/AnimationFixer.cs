using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class AnimationFixer
{
    // ── Player ────────────────────────────────────────────────────────────────────
    // Creates a new controller that uses the "IsRunning" bool Player.cs already sets.
    // Idle default → Run on IsRunning=true → Idle on IsRunning=false.

    [MenuItem("Verdant/Fix Animations/Create Player Animator")]
    static void CreatePlayerAnimator()
    {
        const string CTRL_PATH = "Assets/Game Assets/Animations/Player_Animator.controller";
        const string IDLE_PATH = "Assets/Gif/Super_Retro_Collection/Resources/Characters/Animations/Chara_IDLE_N.anim";
        const string RUN_PATH  = "Assets/Gif/Super_Retro_Collection/Resources/Characters/Animations/Chara_RUN_N.anim";

        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IDLE_PATH);
        var runClip  = AssetDatabase.LoadAssetAtPath<AnimationClip>(RUN_PATH);

        if (idleClip == null) { Debug.LogError("[AnimFixer] Missing idle clip: " + IDLE_PATH); return; }
        if (runClip  == null) { Debug.LogError("[AnimFixer] Missing run clip: "  + RUN_PATH);  return; }

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CTRL_PATH);
        ctrl.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;

        var idle = sm.AddState("Idle");
        idle.motion = idleClip;

        var run = sm.AddState("Run");
        run.motion = runClip;

        sm.defaultState = idle;

        var toRun = idle.AddTransition(run);
        toRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");
        toRun.hasExitTime = false;
        toRun.duration = 0f;

        var toIdle = run.AddTransition(idle);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnimFixer] Player controller saved to " + CTRL_PATH);
        Debug.Log("[AnimFixer] Now drag it onto the Animator component on your Player GameObject.");
    }

    // ── Skull ─────────────────────────────────────────────────────────────────────
    // Finds the auto-generated clip inside the .aseprite file and creates a
    // standalone controller with that clip as the default (Run) state.

    [MenuItem("Verdant/Fix Animations/Create Skull Animator")]
    static void CreateSkullAnimator()
    {
        const string ASEPRITE_PATH = "Assets/Resources/EnemyPrefabs/Skull/Skull.aseprite";
        const string CTRL_PATH     = "Assets/Resources/EnemyPrefabs/Skull/Skull_Animator.controller";

        AnimationClip foundClip = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ASEPRITE_PATH))
        {
            if (asset is AnimationClip clip)
            {
                foundClip = clip;
                Debug.Log("[AnimFixer] Found skull clip: " + clip.name);
                break;
            }
        }

        if (foundClip == null)
        {
            // Fall back to the other aseprite location
            const string ALT = "Assets/Game Assets/Animations/Skull.aseprite";
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ALT))
            {
                if (asset is AnimationClip clip) { foundClip = clip; break; }
            }
        }

        if (foundClip == null) { Debug.LogError("[AnimFixer] No AnimationClip found inside Skull.aseprite"); return; }

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CTRL_PATH);
        var sm   = ctrl.layers[0].stateMachine;

        var runState = sm.AddState("Run");
        runState.motion = foundClip;
        sm.defaultState = runState;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnimFixer] Skull controller saved to " + CTRL_PATH);
        Debug.Log("[AnimFixer] Now drag it onto the Animator on your Skull prefab.");
    }
}
