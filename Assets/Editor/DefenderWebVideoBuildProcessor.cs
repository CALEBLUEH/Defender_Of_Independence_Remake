using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// Preserve the authored Editor VideoClip; Web supports URL video sources.
public sealed class DefenderWebVideoBuildProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null || report.summary.platform != BuildTarget.WebGL) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var controller in root.GetComponentsInChildren<CreditVideoController>(true))
            {
                controller.PrepareWebBuild();
                Debug.Log("Prepared Defender credits URL in Web build scene: " + scene.path);
            }
    }
}
