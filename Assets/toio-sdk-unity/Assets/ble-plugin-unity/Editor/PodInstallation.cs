#if UNITY_IOS
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public class PodInstallation {
#if UNITY_2019_3_OR_NEWER
    private static string PODFILE_CONTENT = @"# Uncomment the next line to define a global platform for your project
platform :ios, '10.0'
target 'Unity-iPhone' do
  use_frameworks!
  pod 'MultiplatformBleAdapter', '~> 0.1.5'
  target 'Unity-iPhone Tests' do
    inherit! :search_paths
  end
end
target 'UnityFramework' do
  use_frameworks!
  pod 'MultiplatformBleAdapter', '~> 0.1.5'
end
";

#else
    private static string PODFILE_CONTENT = @"# Uncomment the next line to define a global platform for your project
platform :ios, '10.0'
target 'Unity-iPhone' do
  use_frameworks!
  pod 'MultiplatformBleAdapter', '~> 0.1.5'
  target 'Unity-iPhone Tests' do
    inherit! :search_paths
  end
end
";
#endif

    [PostProcessBuildAttribute (3)]
    public static void OnPostprocessBuild (BuildTarget target, string pathToBuiltProject) {
        if (target != BuildTarget.iOS) {
            return;
        }

        // Add usual ruby runtime manager path to process.
        ShellCommand.AddPossibleRubySearchPaths();

        var podExisting = ShellCommand.Run("which", "pod");
        if (string.IsNullOrEmpty(podExisting)) {
            var text = @"toio-plugin-unity integrating failed. Building toio-plugin-unity for iOS target requires CocoaPods, but it is not installed. Please run ""sudo gem install cocoapods"" and try again.";
            UnityEngine.Debug.LogError(text);
            var clicked = EditorUtility.DisplayDialog("CocoaPods not found", text, "More", "Cancel");
            if (clicked) {
                Application.OpenURL("https://cocoapods.org");
            }
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        var podfileLocation = Path.Combine(pathToBuiltProject, "Podfile");

        if (File.Exists(podfileLocation)) {
            var text = @"A Podfile is already existing under Xcode project root. Skipping copying of toio-plugin-unity's Podfile. Make sure you have setup Podfile correctly if you are using another package also requires CocoaPods.";
            UnityEngine.Debug.Log(text);
        } else {
            // #if UNITY_2019_3_OR_NEWER
            //             var bundledPodfile = "Assets/Assets/Editor/Podfile_2019_3";
            // #else
            //             var bundledPodfile = "Assets/Assets/Editor/Podfile_2017_4";
            // #endif
            //             var podfilePath = Path.Combine(currentDirectory, bundledPodfile);
            //             UnityEngine.Debug.Log(podfilePath);
            //             File.Copy(podfilePath, podfileLocation);

            using (StreamWriter stream = File.CreateText (podfileLocation)) {
                stream.Write (PODFILE_CONTENT);
            }

        }

        Directory.SetCurrentDirectory(pathToBuiltProject);
        var log = ShellCommand.Run("pod", "install");
        UnityEngine.Debug.Log(log);
        Directory.SetCurrentDirectory(currentDirectory);

        //ConfigureXcodeForCocoaPods(pathToBuiltProject);
    }

    static void ConfigureXcodeForCocoaPods(string projectRoot) {
        var path = PBXProject.GetPBXProjectPath(projectRoot);
        var project = new PBXProject();
        project.ReadFromFile(path);
#if UNITY_2019_3_OR_NEWER
        var target = project.GetUnityFrameworkTargetGuid();
#else
        var target = project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif

        project.SetBuildProperty(target, "GCC_PREPROCESSOR_DEFINITIONS", "$(inherited) LINESDK_COCOAPODS=1");

        project.WriteToFile(path);
    }
}
#endif
#endif