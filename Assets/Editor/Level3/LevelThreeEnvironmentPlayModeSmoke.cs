using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LevelThreeEnvironmentPlayModeSmoke
{
    private const string ScenePath = "Assets/Scene/Scene_Level3.unity";
    private const string FlagPrefabPath = "Assets/Level3/Prefabs/Props/Malaysia Flag.prefab";
    private const string ActiveKey = "Defender.Level3.EnvironmentSmoke.Active";
    private const string ResultKey = "Defender.Level3.EnvironmentSmoke.Result";

    private static GameObject _flagInstance;
    private static Animator _animator;
    private static Transform[] _animatedTransforms;
    private static Vector3[] _startPositions;
    private static Quaternion[] _startRotations;
    private static Vector3[] _startScales;
    private static double _sampleAt;
    private static double _deadline;

    static LevelThreeEnvironmentPlayModeSmoke()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += ResumeAfterReload;
    }

    [MenuItem("Project Tools/Level 3/Validate Environment in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before starting the Level 3 environment smoke test.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ResultKey, string.Empty);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.isPlaying = true;
    }

    public static void RunFromCommandLine()
    {
        try { Run(); }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }

    private static void ResumeAfterReload()
    {
        string result = SessionState.GetString(ResultKey, string.Empty);
        if (!string.IsNullOrEmpty(result) && !EditorApplication.isPlaying)
        {
            Finish(result == "PASS");
            return;
        }
        if (EditorApplication.isPlaying) EditorApplication.update += BeginWhenReady;
    }

    private static void BeginWhenReady()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlagPrefabPath);
        if (prefab == null) { Fail("The Malaysia Flag prefab could not be loaded."); return; }

        _flagInstance = UnityEngine.Object.Instantiate(prefab);
        _flagInstance.name = "Malaysia Flag - Runtime Validation";
        _animator = _flagInstance.GetComponentInChildren<Animator>(true);
        if (_animator?.runtimeAnimatorController == null) { Fail("The flag Animator has no controller."); return; }

        _animatedTransforms = _animator.GetComponentsInChildren<Transform>(true)
            .Where(item => item != _animator.transform)
            .ToArray();
        if (_animatedTransforms.Length == 0) { Fail("The flag has no animated child transforms."); return; }

        _startPositions = _animatedTransforms.Select(item => item.localPosition).ToArray();
        _startRotations = _animatedTransforms.Select(item => item.localRotation).ToArray();
        _startScales = _animatedTransforms.Select(item => item.localScale).ToArray();
        _sampleAt = EditorApplication.timeSinceStartup + 1.25d;
        _deadline = EditorApplication.timeSinceStartup + 12d;
        EditorApplication.update -= BeginWhenReady;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > _deadline) { Fail("Timed out while sampling the flag animation."); return; }
        if (EditorApplication.timeSinceStartup < _sampleAt) return;

        bool moved = false;
        for (int index = 0; index < _animatedTransforms.Length; index++)
        {
            Transform current = _animatedTransforms[index];
            if (current == null) continue;
            if (Vector3.SqrMagnitude(current.localPosition - _startPositions[index]) > 0.00000001f ||
                Quaternion.Angle(current.localRotation, _startRotations[index]) > 0.001f ||
                Vector3.SqrMagnitude(current.localScale - _startScales[index]) > 0.00000001f)
            {
                moved = true;
                break;
            }
        }

        if (!moved) { Fail("The Animator ran, but no flag rig transform changed."); return; }
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime <= 0f) { Fail("The flag animation state did not advance."); return; }

        Debug.Log($"LEVEL3_ENVIRONMENT_PLAYMODE_OK: Malaysia flag rig moved and Wave Loop advanced to normalized time {state.normalizedTime:F3}.");
        Complete("PASS");
    }

    private static void Fail(string message)
    {
        Debug.LogError("LEVEL3_ENVIRONMENT_PLAYMODE_FAILED: " + message);
        Complete("FAIL");
    }

    private static void Complete(string result)
    {
        EditorApplication.update -= BeginWhenReady;
        EditorApplication.update -= Tick;
        if (_flagInstance != null) UnityEngine.Object.Destroy(_flagInstance);
        SessionState.SetString(ResultKey, result);
        EditorApplication.isPlaying = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false)) return;
        string result = SessionState.GetString(ResultKey, string.Empty);
        if (!string.IsNullOrEmpty(result)) Finish(result == "PASS");
    }

    private static void Finish(bool passed)
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseString(ResultKey);
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        if (!passed) throw new InvalidOperationException("Level 3 environment Play Mode smoke test failed.");
    }
}
