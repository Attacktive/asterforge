using UnityEngine;

namespace Asterforge.Unity.Presentation
{
	// Prototype-only facade that keeps the disposable IMGUI screen simple while adding controlled hold-to-repeat to high-frequency buttons.
	internal static class GUILayout
	{
		private const float HoldDelaySeconds = 0.4f;
		private const float StartingRepeatIntervalSeconds = 0.125f;
		private const float MinimumRepeatIntervalSeconds = 0.02f;
		private const float AccelerationDurationSeconds = 3f;

		private static int activeButtonId;
		private static float holdStartedAt;
		private static float nextRepeatAt;

		public static void BeginArea(Rect screenRect, GUIStyle style)
		{
			UnityEngine.GUILayout.BeginArea(screenRect, style);
		}

		public static Vector2 BeginScrollView(Vector2 scrollPosition)
		{
			return UnityEngine.GUILayout.BeginScrollView(scrollPosition);
		}

		public static void EndScrollView()
		{
			UnityEngine.GUILayout.EndScrollView();
		}

		public static void EndArea()
		{
			UnityEngine.GUILayout.EndArea();
		}

		public static void BeginHorizontal(params GUILayoutOption[] options)
		{
			UnityEngine.GUILayout.BeginHorizontal(options);
		}

		public static void EndHorizontal()
		{
			UnityEngine.GUILayout.EndHorizontal();
		}

		public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options)
		{
			UnityEngine.GUILayout.BeginVertical(style, options);
		}

		public static void EndVertical()
		{
			UnityEngine.GUILayout.EndVertical();
		}

		public static void FlexibleSpace()
		{
			UnityEngine.GUILayout.FlexibleSpace();
		}

		public static void Space(float pixels)
		{
			UnityEngine.GUILayout.Space(pixels);
		}

		public static void Label(string text, GUIStyle style, params GUILayoutOption[] options)
		{
			UnityEngine.GUILayout.Label(text, style, options);
		}

		public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options)
		{
			if (!SupportsHold(text))
			{
				return UnityEngine.GUILayout.Button(text, style, options);
			}

			var content = new GUIContent(text);
			var rect = GUILayoutUtility.GetRect(content, style, options);
			var controlId = GUIUtility.GetControlID(text.GetHashCode(), FocusType.Passive, rect);
			var held = GUI.RepeatButton(rect, content, style);
			if (!GUI.enabled)
			{
				ResetIfActive(controlId);

				return false;
			}

			if (activeButtonId == controlId && Event.current.rawType == EventType.MouseUp)
			{
				ResetActiveButton();

				return false;
			}

			if (!held)
			{
				return false;
			}

			var now = Time.unscaledTime;
			if (activeButtonId != controlId)
			{
				activeButtonId = controlId;
				holdStartedAt = now;
				nextRepeatAt = now + HoldDelaySeconds;

				return true;
			}

			if (now < nextRepeatAt)
			{
				return false;
			}

			var acceleratedHoldSeconds = Mathf.Max(0f, now - holdStartedAt - HoldDelaySeconds);
			var acceleration = Mathf.Clamp01(acceleratedHoldSeconds / AccelerationDurationSeconds);
			var repeatInterval = Mathf.Lerp(StartingRepeatIntervalSeconds, MinimumRepeatIntervalSeconds, acceleration);
			nextRepeatAt = now + repeatInterval;

			return true;
		}

		public static GUILayoutOption Height(float height)
		{
			return UnityEngine.GUILayout.Height(height);
		}

		public static GUILayoutOption MinWidth(float minWidth)
		{
			return UnityEngine.GUILayout.MinWidth(minWidth);
		}

		public static GUILayoutOption Width(float width)
		{
			return UnityEngine.GUILayout.Width(width);
		}

		public static GUILayoutOption ExpandWidth(bool expand)
		{
			return UnityEngine.GUILayout.ExpandWidth(expand);
		}

		private static bool SupportsHold(string text)
		{
			return text == "MINE ORE  +1" || text == "COMMISSION UNIT";
		}

		private static void ResetIfActive(int controlId)
		{
			if (activeButtonId == controlId)
			{
				ResetActiveButton();
			}
		}

		private static void ResetActiveButton()
		{
			activeButtonId = 0;
			holdStartedAt = 0f;
			nextRepeatAt = 0f;
		}
	}
}
