# Modding Support 

# Overview

Beginning with the 2021.1 release of Unity, it is now possible to load additional Burst compiled libraries which can provide a way to allow users to create "Mods" that utilise Burst compiled code. Please note, Burst only provides a method to load additional libraries, it does not provide any tooling in order to allow the creation of these "Mods", and a copy of the Unity Editor will be required in order to compile the additional libaries. This section documents one possible approach to allowing modding with Burst, with the caveat that it should be considered a proof of concept (and not a complete solution).

# Supported Uses

This function is designed to be used in Play Mode (or Standalone Players) only.

You should make sure you load the libraries as early as possible (and before the first use of C# method that is Burst compiled), any Burst libraries that are loaded via `BurstRuntime.LoadAdditionalLibraries` will be unloaded at exit from Play Mode in the Editor, or on application quit in a Standalone Player.

# An Example Modding system

> Note: As has been stated this example is extremely limited in scope.

> Note: This is an advanced usage, and knowledge of assemblies/asmdefs and etc. is assumed.

## The Main Application Side (aka the game that wants to allow mods)

### An interface

First up we need to declare an interface that all our mods will abide by :

```c#
using UnityEngine;

public interface PluginModule
{
    void Startup(GameObject gameObject);
    void Update(GameObject gameObject);
}
```

This interface allows us to create new classes which adhere to these specifications and can be shipped seperate to the main game. Obviously passing a single `GameObject` along is going to limit the state that our plugins can affect. 

### Modding manager

```c#
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Unity.Burst;

public class PluginManager : MonoBehaviour
{
    public bool modsEnabled;
    public GameObject objectForPlugins;

    List<PluginModule> plugins;

    void Start()
    {
        plugins = new List<PluginModule>();

        // If mods are disabled, early out - this allows us to disable mods, enter Play Mode, exit Play Mode
        //and be sure that the managed assemblies have been unloaded (assuming DomainReload occurs)
        if (!modsEnabled)
            return;

        var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Mods"));
        if (Directory.Exists(folder))
        {
            var mods = Directory.GetDirectories(folder);

            foreach (var mod in mods)
            {
                var modName = Path.GetFileName(mod);
                var monoAssembly = Path.Combine(mod, $"{modName}_managed.dll");

                if (File.Exists(monoAssembly))
                {
                    var managedPlugin = Assembly.LoadFile(monoAssembly);

                    var pluginModule = managedPlugin.GetType("MyPluginModule");

                    var plugin = Activator.CreateInstance(pluginModule) as PluginModule;

                    plugins.Add(plugin);
                }

                var burstedAssembly = Path.Combine(mod, $"{modName}_win_x86_64.dll");      // Burst dll (assuming windows 64bit)
                if (File.Exists(burstedAssembly))
                {
                    BurstRuntime.LoadAdditionalLibrary(burstedAssembly);
                }
            }
        }

        foreach (var plugin in plugins)
        {
            plugin.Startup(objectForPlugins);
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var plugin in plugins)
        {
            plugin.Update(objectForPlugins);
        }
    }
}

```

This code scans the "Mods" folder, and for each folder it finds within, attempts to load both a managed dll and a bursted dll by adding them to an internal list that it can then iterate on and call the respective interface functions. The names of the files are arbitrary - see [Simple Create Mod Menu Button](#simple-create-mod-menu-button), which is the code that generated those files.

Note - because this code makes use of loading the managed assemblies into the current domain, you will need a domain reload to unload those before you can overwrite them. The Burst dll's are automatically unloaded when exiting Play Mode. This is why a boolean to disable the modding system is included, for testing in editor.

## The Mod

A seperate project should be created for this (we use the project to produce the mod).

### A mod that uses Burst 

Note: this script is assumed to be attached to a UI Canvas that contains a text component called "Main UI Label", and proceeds to change the text when the mod is used. The text will either be "Plugin Updated : Bursted" or "Plugin Updated : Not Bursted". You will see the "Plugin Updated : Bursted" by default, but if you were to comment out the lines that load the Burst library in the PluginManager above, then the bursted code will not be loaded the message will change appropriately. 

```c#
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;

public class MyPluginModule : PluginModule
{
    Text textComponent;

    public void Startup(GameObject gameObject)
    {
        var childTextComponents = gameObject.GetComponentsInChildren<Text>();
        textComponent = null;
        foreach (var child in childTextComponents)
        {
            if (child.name == "Main UI Label")
            {
                textComponent = child;
            }
        }

        if (textComponent==null)
        {
            Debug.LogError("something went wrong and i couldn't find the UI component i wanted to modify");
        }
    }

    public void Update(GameObject gameObject)
    {
        if (textComponent != null)
        {
            var t = new CheckBurstedJob { flag = new NativeArray<int>(1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory) };

            t.Run();

            if (t.flag[0] == 0)
                textComponent.text = "Plugin Updated : Not Bursted";
            else
                textComponent.text = "Plugin Updated : Bursted";

            t.flag.Dispose();
        }
    }

    [BurstCompile]
    struct CheckBurstedJob : IJob
    {
        public NativeArray<int> flag;

        [BurstDiscard]
        void CheckBurst()
        {
            flag[0] = 0;
        }

        public void Execute()
        {
            flag[0] = 1;
            CheckBurst();
        }
    }

}

```

The above script should be placed into a folder along with an assembly definition file with an assembly name of TestMod_Managed in order for the below script to correctly locate the managed part.

### Simple Create Mod Menu Button

The below script adds a menu button which when pushed will build a Standalone Player, then copy the C# managed dll and the lib_burst_generated.dll (again, this example assumes Windows) into a chosen Mod folder. 

```c#
using UnityEditor;
using System.IO;
using UnityEngine;

public class ScriptBatch
{
    [MenuItem("Modding/Build X64 Mod (Example)")]
    public static void BuildGame()
    {
        string modName = "TestMod";

        string projectFolder = Path.Combine(Application.dataPath, "..");
        string buildFolder = Path.Combine(projectFolder, "PluginTemp");

        // Get filename.
        string path = EditorUtility.SaveFolderPanel("Choose Final Mod Location", "", "");

        FileUtil.DeleteFileOrDirectory(buildFolder);
        Directory.CreateDirectory(buildFolder);

        // Build player.
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/SampleScene.unity" }, Path.Combine(buildFolder, $"{modName}.exe"), BuildTarget.StandaloneWindows64, BuildOptions.Development);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            // Copy Managed library
            var managedDest = Path.Combine(path, $"{modName}_Managed.dll");
            var managedSrc = Path.Combine(buildFolder, $"{modName}_Data/Managed/{modName}_Managed.dll");
            FileUtil.DeleteFileOrDirectory(managedDest);
            if (!File.Exists(managedDest))  // Managed side not unloaded
                FileUtil.CopyFileOrDirectory(managedSrc, managedDest);
            else
                Debug.LogWarning($"Couldn't update manged dll, {managedDest} is it currently in use?");

            // Copy Burst library
            var burstedDest = Path.Combine(path, $"{modName}_win_x86_64.dll");
            var burstedSrc = Path.Combine(buildFolder, $"{modName}_Data/Plugins/x86_64/lib_burst_generated.dll");
            FileUtil.DeleteFileOrDirectory(burstedDest);
            if (!File.Exists(burstedDest))
                FileUtil.CopyFileOrDirectory(burstedSrc, burstedDest);
            else
                Debug.LogWarning($"Couldn't update bursted dll, {burstedDest} is it currently in use?");
        }
    }
}
```
