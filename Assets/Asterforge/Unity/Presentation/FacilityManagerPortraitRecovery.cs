using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Asterforge.Unity.Presentation
{
	[DefaultExecutionOrder(1100)]
	public sealed class FacilityManagerPortraitRecovery : MonoBehaviour
	{
		private static readonly FieldInfo portraitsField = typeof(FacilityManagerOverlay).GetField("portraits", BindingFlags.Instance | BindingFlags.NonPublic);

		private FacilityManagerOverlay overlay;
		private float nextRetryTime;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (FindAnyObjectByType<FacilityManagerPortraitRecovery>() != null)
			{
				return;
			}

			var recoveryObject = new GameObject("Asterforge Facility Manager Portrait Recovery");
			DontDestroyOnLoad(recoveryObject);
			recoveryObject.AddComponent<FacilityManagerPortraitRecovery>();
		}

		private void Start()
		{
			overlay = FindAnyObjectByType<FacilityManagerOverlay>();
			if (overlay == null || portraitsField == null)
			{
				enabled = false;

				return;
			}

			TryRecoverPortraits();
		}

		private void Update()
		{
			if (Time.unscaledTime < nextRetryTime)
			{
				return;
			}

			nextRetryTime = Time.unscaledTime + 1f;
			if (TryRecoverPortraits())
			{
				enabled = false;
			}
		}

		private bool TryRecoverPortraits()
		{
			var portraits = portraitsField.GetValue(overlay) as Dictionary<string, Texture2D>;
			if (portraits == null)
			{
				return false;
			}

			var allLoaded = true;
			foreach (var manager in FacilityManagers.All)
			{
				if (portraits.TryGetValue(manager.PortraitResourcePath, out var portrait) && portrait != null)
				{
					continue;
				}

				portrait = Resources.Load<Texture2D>(manager.PortraitResourcePath);
				if (portrait == null)
				{
					allLoaded = false;

					continue;
				}

				portraits[manager.PortraitResourcePath] = portrait;
			}

			return allLoaded;
		}
	}
}
