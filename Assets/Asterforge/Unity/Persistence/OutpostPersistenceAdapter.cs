using System;
using System.IO;
using System.Reflection;
using Asterforge.Game.Persistence;
using Asterforge.Game.Simulation.Progression;
using Asterforge.Unity.Presentation;
using UnityEngine;

namespace Asterforge.Unity.Persistence
{
	[DefaultExecutionOrder(1000)]
	public sealed class OutpostPersistenceAdapter : MonoBehaviour
	{
		private const float autosaveIntervalSeconds = 10f;
		private const string saveFileName = "asterforge-outpost.json";

		private OutpostPrototype prototype;
		private FieldInfo worldField;
		private float autosaveElapsedSeconds;
		private string savePath;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (FindAnyObjectByType<OutpostPersistenceAdapter>() != null)
			{
				return;
			}

			var persistenceObject = new GameObject("Asterforge Outpost Persistence");
			DontDestroyOnLoad(persistenceObject);
			persistenceObject.AddComponent<OutpostPersistenceAdapter>();
		}

		private void Start()
		{
			savePath = Path.Combine(Application.persistentDataPath, saveFileName);
			prototype = FindAnyObjectByType<OutpostPrototype>();
			if (prototype == null)
			{
				Debug.LogError("Asterforge persistence could not find the Outpost prototype.");
				enabled = false;

				return;
			}

			worldField = typeof(OutpostPrototype).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic);
			if (worldField == null)
			{
				Debug.LogError("Asterforge persistence could not access the prototype world. The temporary adapter needs updating.");
				enabled = false;

				return;
			}

			LoadIfPresent();
		}

		private void Update()
		{
			autosaveElapsedSeconds += Time.unscaledDeltaTime;
			if (autosaveElapsedSeconds < autosaveIntervalSeconds)
			{
				return;
			}

			autosaveElapsedSeconds = 0f;
			Save();
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (pauseStatus)
			{
				Save();
			}
		}

		private void OnApplicationQuit()
		{
			Save();
		}

		private void LoadIfPresent()
		{
			if (!File.Exists(savePath))
			{
				return;
			}

			try
			{
				var json = File.ReadAllText(savePath);
				var snapshot = JsonUtility.FromJson<OutpostSnapshot>(json);
				if (snapshot == null)
				{
					throw new InvalidDataException("The Outpost save file did not contain a snapshot.");
				}

				var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				var result = OutpostWorld.RestoreWithOfflineProgress(snapshot, nowUnixSeconds);
				worldField.SetValue(prototype, result.World);

				if (result.OfflineSeconds > 0)
				{
					Debug.Log($"Asterforge applied {result.OfflineSeconds:N0} seconds of offline progress.");
				}
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"Asterforge could not load the Outpost save. Starting from the current prototype state. {exception.Message}");
			}
		}

		private void Save()
		{
			if (prototype == null || worldField == null || string.IsNullOrEmpty(savePath))
			{
				return;
			}

			try
			{
				var world = worldField.GetValue(prototype) as OutpostWorld;
				if (world == null)
				{
					return;
				}

				var nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				var snapshot = world.CaptureSnapshot(nowUnixSeconds);
				var json = JsonUtility.ToJson(snapshot, true);
				File.WriteAllText(savePath, json);
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"Asterforge could not save the Outpost prototype. {exception.Message}");
			}
		}
	}
}
