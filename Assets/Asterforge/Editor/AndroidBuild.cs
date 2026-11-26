using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Asterforge.Editor
{
	public static class AndroidBuild
	{
		private const string ApplicationIdentifier = "com.attacktive.asterforge";
		private const string OutputPath = "build/Android/asterforge.apk";
		private const string TemporaryFolderPath = "Assets/Asterforge/GeneratedBuild";
		private const string TemporaryScenePath = TemporaryFolderPath + "/AndroidBootstrap.unity";

		[MenuItem("Asterforge/Build/Android Development APK")]
		public static void BuildFromMenu()
		{
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				return;
			}

			BuildDevelopmentApk();
		}

		public static void BuildFromCommandLine()
		{
			BuildDevelopmentApk();
		}

		private static void BuildDevelopmentApk()
		{
			ConfigurePlayerSettings();

			var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
			var createdTemporaryFolder = false;
			try
			{
				createdTemporaryFolder = EnsureTemporaryFolder();
				CreateTemporaryScene();
				Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

				var buildOptions = new BuildPlayerOptions
				{
					scenes = new[] { TemporaryScenePath },
					locationPathName = OutputPath,
					target = BuildTarget.Android,
					targetGroup = BuildTargetGroup.Android,
					options = BuildOptions.Development
				};

				var report = BuildPipeline.BuildPlayer(buildOptions);
				if (report.summary.result != BuildResult.Succeeded)
				{
					throw new BuildFailedException($"Android build failed with result {report.summary.result}.");
				}
			}
			finally
			{
				try
				{
					RestoreSceneSetup(sceneSetup);
				}
				finally
				{
					DeleteTemporaryScene(createdTemporaryFolder);
				}
			}
		}

		private static void ConfigurePlayerSettings()
		{
			PlayerSettings.companyName = "Attacktive";
			PlayerSettings.productName = "Asterforge";
			PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationIdentifier);
			PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
			PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
			PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			PlayerSettings.Android.renderOutsideSafeArea = false;
			PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
			PlayerSettings.allowedAutorotateToPortrait = false;
			PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
			PlayerSettings.allowedAutorotateToLandscapeLeft = true;
			PlayerSettings.allowedAutorotateToLandscapeRight = true;
			PlayerSettings.SetGraphicsAPIs(
				BuildTarget.Android,
				new[]
				{
					GraphicsDeviceType.Vulkan,
					GraphicsDeviceType.OpenGLES3
				}
			);

			EditorUserBuildSettings.buildAppBundle = false;
		}

		private static bool EnsureTemporaryFolder()
		{
			if (AssetDatabase.IsValidFolder(TemporaryFolderPath))
			{
				return false;
			}

			AssetDatabase.CreateFolder("Assets/Asterforge", "GeneratedBuild");

			return true;
		}

		private static void CreateTemporaryScene()
		{
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			if (!EditorSceneManager.SaveScene(scene, TemporaryScenePath))
			{
				throw new BuildFailedException($"Could not create temporary Android bootstrap scene at {TemporaryScenePath}.");
			}
		}

		private static void DeleteTemporaryScene(bool createdTemporaryFolder)
		{
			AssetDatabase.DeleteAsset(TemporaryScenePath);
			if (createdTemporaryFolder)
			{
				AssetDatabase.DeleteAsset(TemporaryFolderPath);
			}
		}

		private static void RestoreSceneSetup(SceneSetup[] sceneSetup)
		{
			foreach (var scene in sceneSetup)
			{
				if (string.IsNullOrEmpty(scene.path))
				{
					EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

					return;
				}
			}

			if (sceneSetup.Length > 0)
			{
				EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);

				return;
			}

			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}
	}
}
