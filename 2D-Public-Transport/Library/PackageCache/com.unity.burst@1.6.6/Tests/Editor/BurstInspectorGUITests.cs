using System.Collections;
using NUnit.Framework;
using Unity.Burst.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class BurstInspectorGUITests
{
    [UnityTest]
    [UnityPlatform(RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
    public IEnumerator TestInspectorOpenDuringDomainReloadDoesNotLogErrors()
    {
        // Show Inspector window
        EditorWindow.GetWindow<BurstInspectorGUI>().Show();

        Assert.IsTrue(EditorWindow.HasOpenInstances<BurstInspectorGUI>());

        // Ask for domain reload
        EditorUtility.RequestScriptReload();

        // Wait for the domain reload to be completed
        yield return new WaitForDomainReload();

        Assert.IsTrue(EditorWindow.HasOpenInstances<BurstInspectorGUI>());

        // Hide Inspector window
        EditorWindow.GetWindow<BurstInspectorGUI>().Close();

        Assert.IsFalse(EditorWindow.HasOpenInstances<BurstInspectorGUI>());
    }
}
