using System.Collections.Generic;
using UnityEngine;

namespace Asterforge.Unity.Presentation
{
	[DefaultExecutionOrder(1000)]
	public sealed class FacilityManagerOverlay : MonoBehaviour
	{
		private static readonly Color panelColor = new Color(0.025f, 0.04f, 0.05f, 0.94f);
		private static readonly Color cardColor = new Color(0.055f, 0.07f, 0.075f, 0.96f);
		private static readonly Color textColor = new Color(0.92f, 0.94f, 0.95f);
		private static readonly Color mutedColor = new Color(0.48f, 0.60f, 0.64f);
		private static readonly Color amberColor = new Color(0.95f, 0.47f, 0.10f);
		private static readonly Color cyanColor = new Color(0.25f, 0.76f, 0.85f);

		private readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
		private GUIStyle headingStyle;
		private GUIStyle nameStyle;
		private GUIStyle roleStyle;
		private GUIStyle tinyStyle;
		private GUIStyle panelStyle;
		private Texture2D panelTexture;
		private Texture2D cardTexture;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (FindAnyObjectByType<FacilityManagerOverlay>() != null)
			{
				return;
			}

			var overlayObject = new GameObject("Asterforge Facility Managers");
			DontDestroyOnLoad(overlayObject);
			overlayObject.AddComponent<FacilityManagerOverlay>();
		}

		private void Awake()
		{
			foreach (var manager in FacilityManagers.All)
			{
				portraits[manager.PortraitResourcePath] = Resources.Load<Texture2D>(manager.PortraitResourcePath);
			}
		}

		private void OnGUI()
		{
			EnsureStyles();

			var previousDepth = GUI.depth;
			GUI.depth = -100;
			var rect = GetCrewRect();
			GUI.Box(rect, GUIContent.none, panelStyle);
			GUI.Label(new Rect(rect.x + 12f, rect.y + 9f, rect.width - 24f, 20f), "FACILITY MANAGERS // CREW MANIFEST", headingStyle);

			if (Screen.width >= 1080)
			{
				DrawWideRoster(rect);
			}
			else
			{
				DrawCompactRoster(rect);
			}

			GUI.depth = previousDepth;
		}

		private Rect GetCrewRect()
		{
			if (Screen.width >= 1080)
			{
				var controlDeckWidth = Mathf.Clamp(Screen.width * 0.52f, 520f, 650f);
				var vistaWidth = Screen.width - controlDeckWidth - 56f;
				var width = Mathf.Max(360f, vistaWidth - 28f);
				var height = Mathf.Clamp(Screen.height * 0.30f, 190f, 245f);

				return new Rect(36f, Screen.height - height - 34f, width, height);
			}

			var compactWidth = Screen.width - 36f;
			var compactHeight = Mathf.Clamp(Screen.height * 0.30f, 180f, 230f);

			return new Rect(18f, 98f, compactWidth, compactHeight);
		}

		private void DrawWideRoster(Rect rect)
		{
			var contentRect = new Rect(rect.x + 10f, rect.y + 34f, rect.width - 20f, rect.height - 44f);
			var gap = 6f;
			var cardWidth = (contentRect.width - gap * 5f) / 6f;

			for (var index = 0; index < FacilityManagers.All.Count; index++)
			{
				var manager = FacilityManagers.All[index];
				var cardRect = new Rect(contentRect.x + index * (cardWidth + gap), contentRect.y, cardWidth, contentRect.height);
				DrawManagerCard(cardRect, manager, index >= 4);
			}
		}

		private void DrawCompactRoster(Rect rect)
		{
			var contentRect = new Rect(rect.x + 10f, rect.y + 34f, rect.width - 20f, rect.height - 44f);
			var gap = 6f;
			var cardWidth = (contentRect.width - gap * 2f) / 3f;
			var cardHeight = (contentRect.height - gap) / 2f;

			for (var index = 0; index < FacilityManagers.All.Count; index++)
			{
				var row = index / 3;
				var column = index % 3;
				var manager = FacilityManagers.All[index];
				var cardRect = new Rect(
					contentRect.x + column * (cardWidth + gap),
					contentRect.y + row * (cardHeight + gap),
					cardWidth,
					cardHeight
				);

				DrawManagerCard(cardRect, manager, index >= 4);
			}
		}

		private void DrawManagerCard(Rect rect, FacilityManagerDefinition manager, bool industryManager)
		{
			GUI.DrawTexture(rect, cardTexture);

			var accent = amberColor;
			if (industryManager)
			{
				accent = cyanColor;
			}

			DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accent);

			var portraitHeight = Mathf.Max(54f, rect.height - 52f);
			var portraitRect = new Rect(rect.x + 7f, rect.y + 7f, rect.width - 14f, portraitHeight);
			if (portraits.TryGetValue(manager.PortraitResourcePath, out var portrait) && portrait != null)
			{
				GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleAndCrop);
			}
			else
			{
				DrawRect(portraitRect, new Color(0.08f, 0.10f, 0.11f, 1f));
				GUI.Label(portraitRect, manager.Initials, nameStyle);
			}

			DrawRect(new Rect(portraitRect.x, portraitRect.yMax - 2f, portraitRect.width, 2f), accent);

			var textY = portraitRect.yMax + 5f;
			GUI.Label(new Rect(rect.x + 7f, textY, rect.width - 14f, 17f), manager.Name.ToUpperInvariant(), nameStyle);
			GUI.Label(new Rect(rect.x + 7f, textY + 17f, rect.width - 14f, 15f), manager.Role.ToUpperInvariant(), roleStyle);

			var facilityLabel = manager.Machine.ToString().ToUpperInvariant();
			GUI.Label(new Rect(rect.x + 7f, rect.yMax - 17f, rect.width - 14f, 12f), facilityLabel, tinyStyle);
		}

		private void EnsureStyles()
		{
			if (headingStyle != null)
			{
				return;
			}

			panelTexture = CreateTexture(panelColor);
			cardTexture = CreateTexture(cardColor);

			panelStyle = new GUIStyle(GUI.skin.box)
			{
				border = new RectOffset(1, 1, 1, 1)
			};
			panelStyle.normal.background = panelTexture;

			headingStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft
			};
			headingStyle.normal.textColor = mutedColor;

			nameStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft,
				clipping = TextClipping.Clip
			};
			nameStyle.normal.textColor = textColor;

			roleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 8,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft,
				clipping = TextClipping.Clip
			};
			roleStyle.normal.textColor = amberColor;

			tinyStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 7,
				alignment = TextAnchor.MiddleLeft,
				clipping = TextClipping.Clip
			};
			tinyStyle.normal.textColor = mutedColor;
		}

		private static Texture2D CreateTexture(Color color)
		{
			var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			texture.SetPixel(0, 0, color);
			texture.Apply(false, true);

			return texture;
		}

		private static void DrawRect(Rect rect, Color color)
		{
			var previousColor = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, Texture2D.whiteTexture);
			GUI.color = previousColor;
		}

		private void OnDestroy()
		{
			if (panelTexture != null)
			{
				Destroy(panelTexture);
			}

			if (cardTexture != null)
			{
				Destroy(cardTexture);
			}
		}
	}
}
