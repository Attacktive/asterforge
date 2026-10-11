using System.Collections.Generic;
using System.Reflection;
using Asterforge.Game.Simulation.Progression;
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
		private static readonly Color greenColor = new Color(0.39f, 0.81f, 0.53f);

		private readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
		private readonly List<FacilityManagerDefinition> visibleManagers = new List<FacilityManagerDefinition>();
		private OutpostPrototype prototype;
		private FieldInfo worldField;
		private GUIStyle headingStyle;
		private GUIStyle nameStyle;
		private GUIStyle roleStyle;
		private GUIStyle statusStyle;
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

		private void Start()
		{
			prototype = FindAnyObjectByType<OutpostPrototype>();
			worldField = typeof(OutpostPrototype).GetField("world", BindingFlags.Instance | BindingFlags.NonPublic);
			if (prototype == null || worldField == null)
			{
				Debug.LogWarning("Asterforge facility managers could not connect to the temporary Outpost prototype.");
				enabled = false;
			}
		}

		private void OnGUI()
		{
			var world = GetWorld();
			if (world == null)
			{
				return;
			}

			BuildVisibleManagers(world);
			if (visibleManagers.Count == 0)
			{
				return;
			}

			EnsureStyles();

			var previousDepth = GUI.depth;
			GUI.depth = -100;
			var rect = GetCrewRect();
			GUI.Box(rect, GUIContent.none, panelStyle);
			GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 18f), "FACILITY MANAGERS // SHIFT LEADS", headingStyle);
			DrawRoster(rect, world);
			GUI.depth = previousDepth;
		}

		private OutpostWorld GetWorld()
		{
			if (prototype == null || worldField == null)
			{
				return null;
			}

			return worldField.GetValue(prototype) as OutpostWorld;
		}

		private void BuildVisibleManagers(OutpostWorld world)
		{
			visibleManagers.Clear();
			foreach (var manager in FacilityManagers.All)
			{
				if (world.IsMachineUnlocked(manager.Machine))
				{
					visibleManagers.Add(manager);
				}
			}
		}

		private Rect GetCrewRect()
		{
			if (Screen.width >= 1080)
			{
				var vistaWidth = Screen.width - 584f;
				var width = Mathf.Max(360f, vistaWidth - 28f);
				var height = Mathf.Clamp(Screen.height * 0.30f, 200f, 250f);

				return new Rect(36f, Screen.height - height - 34f, width, height);
			}

			var compactWidth = Screen.width - 36f;

			return new Rect(18f, 96f, compactWidth, 150f);
		}

		private void DrawRoster(Rect rect, OutpostWorld world)
		{
			var contentRect = new Rect(rect.x + 10f, rect.y + 30f, rect.width - 20f, rect.height - 40f);
			var columns = Mathf.Min(3, visibleManagers.Count);
			var rows = Mathf.CeilToInt(visibleManagers.Count / (float)columns);
			var gap = 6f;
			var cardWidth = (contentRect.width - gap * (columns - 1)) / columns;
			var cardHeight = (contentRect.height - gap * (rows - 1)) / rows;

			for (var index = 0; index < visibleManagers.Count; index++)
			{
				var row = index / columns;
				var column = index % columns;
				var manager = visibleManagers[index];
				var cardRect = new Rect(
					contentRect.x + column * (cardWidth + gap),
					contentRect.y + row * (cardHeight + gap),
					cardWidth,
					cardHeight
				);

				DrawManagerCard(cardRect, manager, world);
			}
		}

		private void DrawManagerCard(Rect rect, FacilityManagerDefinition manager, OutpostWorld world)
		{
			GUI.DrawTexture(rect, cardTexture);

			var industryManager = manager.Machine == Game.Simulation.Production.MachineType.CoalMine || manager.Machine == Game.Simulation.Production.MachineType.Steelworks;
			var accent = amberColor;
			if (industryManager)
			{
				accent = cyanColor;
			}

			DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), accent);

			var portraitWidth = Mathf.Clamp(rect.width * 0.38f, 48f, 104f);
			var portraitRect = new Rect(rect.x + 7f, rect.y + 7f, portraitWidth, rect.height - 14f);
			if (portraits.TryGetValue(manager.PortraitResourcePath, out var portrait) && portrait != null)
			{
				GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleAndCrop);
			}
			else
			{
				DrawRect(portraitRect, new Color(0.08f, 0.10f, 0.11f, 1f));
				GUI.Label(portraitRect, manager.Initials, nameStyle);
			}

			DrawRect(new Rect(portraitRect.xMax - 2f, portraitRect.y, 2f, portraitRect.height), accent);

			var textX = portraitRect.xMax + 8f;
			var textWidth = rect.xMax - textX - 7f;
			GUI.Label(new Rect(textX, rect.y + 7f, textWidth, 17f), manager.Name.ToUpperInvariant(), nameStyle);
			GUI.Label(new Rect(textX, rect.y + 24f, textWidth, 15f), manager.Role.ToUpperInvariant(), roleStyle);

			var owned = world.Machines.Get(manager.Machine);
			var state = "STANDBY";
			var stateColor = mutedColor;
			if (owned > 0)
			{
				state = $"ONLINE  //  x{owned}";
				stateColor = greenColor;
			}

			statusStyle.normal.textColor = stateColor;
			GUI.Label(new Rect(textX, rect.y + 42f, textWidth, 14f), state, statusStyle);
			GUI.Label(new Rect(textX, rect.y + 59f, textWidth, Mathf.Max(12f, rect.height - 66f)), manager.Tagline.ToUpperInvariant(), tinyStyle);
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

			statusStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 8,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft,
				clipping = TextClipping.Clip
			};

			tinyStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 7,
				alignment = TextAnchor.UpperLeft,
				wordWrap = true,
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
