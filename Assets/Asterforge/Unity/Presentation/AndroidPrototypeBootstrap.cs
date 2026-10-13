using UnityEngine;

namespace Asterforge.Unity.Presentation
{
	// Prototype-only mobile shim. The temporary IMGUI shell is authored in pixels, so Android renders it at a bounded logical resolution until the production UI replaces it.
	internal static class AndroidPrototypeBootstrap
	{
		private const int MaximumLogicalLongSide = 1080;
		private const int TargetFrameRate = 60;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Configure()
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			Application.targetFrameRate = TargetFrameRate;

			var longSide = Mathf.Max(Screen.width, Screen.height);
			if (longSide <= MaximumLogicalLongSide)
			{
				return;
			}

			var scale = MaximumLogicalLongSide / (float)longSide;
			var width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * scale));
			var height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * scale));
			Screen.SetResolution(width, height, true);
#endif
		}
	}
}
