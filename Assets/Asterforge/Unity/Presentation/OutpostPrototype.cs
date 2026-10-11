using System.Collections.Generic;
using System.Text;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Progression;
using Asterforge.Game.Simulation.Resources;
using UnityEngine;

namespace Asterforge.Unity.Presentation
{
	public sealed class OutpostPrototype : MonoBehaviour
	{
		private static readonly ResourceType[] baseResources =
		{
			ResourceType.Ore,
			ResourceType.Metal,
			ResourceType.Components,
			ResourceType.Research
		};

		private static readonly ResourceType[] industryResources =
		{
			ResourceType.Coal,
			ResourceType.Steel
		};

		private static readonly MachineType[] outpostMachines =
		{
			MachineType.Extractor,
			MachineType.Smelter,
			MachineType.Assembler,
			MachineType.ResearchStation
		};

		private static readonly MachineType[] industryMachines =
		{
			MachineType.CoalMine,
			MachineType.Steelworks
		};

		private static readonly Color textColor = new Color(0.91f, 0.93f, 0.95f);
		private static readonly Color mutedColor = new Color(0.55f, 0.62f, 0.67f);
		private static readonly Color amberColor = new Color(0.96f, 0.57f, 0.17f);
		private static readonly Color cyanColor = new Color(0.28f, 0.77f, 0.86f);
		private static readonly Color greenColor = new Color(0.39f, 0.81f, 0.53f);
		private static readonly Color steelColor = new Color(0.65f, 0.74f, 0.80f);

		private OutpostWorld world;
		private Vector2 scrollPosition;
		private float elapsedSimulationSeconds;
		private string status = "Survey complete. Mine enough ore to establish the first automated extractor.";
		private GUIStyle panelStyle;
		private GUIStyle titleStyle;
		private GUIStyle eyebrowStyle;
		private GUIStyle headingStyle;
		private GUIStyle machineTitleStyle;
		private GUIStyle bodyStyle;
		private GUIStyle mutedStyle;
		private GUIStyle resourceCardStyle;
		private GUIStyle machineCardStyle;
		private GUIStyle buttonStyle;
		private GUIStyle buttonDisabledStyle;
		private GUIStyle badgeStyle;
		private GUIStyle vistaLabelStyle;
		private Texture2D skyTexture;
		private Texture2D panelTexture;
		private Texture2D cardTexture;
		private Texture2D cardRaisedTexture;
		private Texture2D buttonTexture;
		private Texture2D buttonHoverTexture;
		private Texture2D buttonActiveTexture;
		private Texture2D buttonDisabledTexture;
		private Texture2D badgeTexture;
		private Texture2D circleTexture;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (FindAnyObjectByType<OutpostPrototype>() != null)
			{
				return;
			}

			var prototypeObject = new GameObject("Asterforge Outpost Prototype");
			DontDestroyOnLoad(prototypeObject);
			prototypeObject.AddComponent<OutpostPrototype>();
		}

		private void Awake()
		{
			world = new OutpostWorld();
		}

		private void Update()
		{
			elapsedSimulationSeconds += Time.unscaledDeltaTime;
			var wholeSeconds = (long)elapsedSimulationSeconds;
			if (wholeSeconds <= 0)
			{
				return;
			}

			elapsedSimulationSeconds -= wholeSeconds;
			world.Advance(wholeSeconds);
		}

		private void OnGUI()
		{
			EnsureStyles();
			DrawBackground();

			var wideLayout = Screen.width >= 1120;
			DrawHeader(wideLayout);

			Rect vistaRect;
			Rect panelRect;
			if (wideLayout)
			{
				vistaRect = new Rect(24f, 88f, Screen.width - 584f, Screen.height - 112f);
				panelRect = new Rect(Screen.width - 540f, 88f, 516f, Screen.height - 112f);
			}
			else
			{
				var vistaHeight = Mathf.Clamp(Screen.height * 0.31f, 180f, 270f);
				vistaRect = new Rect(16f, 82f, Screen.width - 32f, vistaHeight);
				panelRect = new Rect(16f, vistaRect.yMax + 14f, Screen.width - 32f, Mathf.Max(180f, Screen.height - vistaRect.yMax - 30f));
			}

			DrawOutpostVista(vistaRect);
			DrawControlDeck(panelRect);
		}

		private void DrawBackground()
		{
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), skyTexture, ScaleMode.StretchToFill);

			var previousColor = GUI.color;
			for (var index = 0; index < 58; index++)
			{
				var normalizedX = ((index * 73) % 997) / 997f;
				var normalizedY = ((index * 193) % 991) / 991f;
				var twinkle = 0.16f + 0.22f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (0.4f + index % 5 * 0.08f) + index));
				var size = 1f + index % 3 * 0.6f;
				GUI.color = new Color(0.74f, 0.83f, 0.88f, twinkle);
				GUI.DrawTexture(new Rect(normalizedX * Screen.width, normalizedY * Screen.height * 0.72f, size, size), Texture2D.whiteTexture);
			}

			GUI.color = new Color(0.10f, 0.16f, 0.20f, 0.11f);
			for (var y = 0f; y < Screen.height; y += 6f)
			{
				GUI.DrawTexture(new Rect(0f, y, Screen.width, 1f), Texture2D.whiteTexture);
			}

			GUI.color = previousColor;
		}

		private void DrawHeader(bool wideLayout)
		{
			var left = 18f;
			if (wideLayout)
			{
				left = 28f;
			}

			GUI.Label(new Rect(left, 16f, Screen.width - 220f, 26f), "ASTERFORGE", titleStyle);
			GUI.Label(new Rect(left + 1f, 45f, Screen.width - 230f, 24f), status, bodyStyle);

			var badgeWidth = 144f;
			var badgeRect = new Rect(Screen.width - badgeWidth - 20f, 22f, badgeWidth, 28f);
			GUI.Label(badgeRect, "OUTPOST 01  //  SOLO", badgeStyle);

			var indicatorColor = greenColor;
			if (world.Machines.Get(MachineType.Extractor) == 0)
			{
				indicatorColor = amberColor;
			}

			DrawRect(new Rect(badgeRect.x - 18f, badgeRect.y + 10f, 8f, 8f), indicatorColor);
		}

		private void DrawOutpostVista(Rect rect)
		{
			GUI.BeginGroup(rect);
			var localRect = new Rect(0f, 0f, rect.width, rect.height);
			DrawRect(localRect, new Color(0.025f, 0.04f, 0.055f, 0.62f));
			DrawRect(new Rect(0f, localRect.height - 2f, localRect.width, 2f), new Color(0.30f, 0.40f, 0.44f, 0.35f));

			var planetSize = Mathf.Clamp(localRect.height * 0.55f, 100f, 210f);
			var previousColor = GUI.color;
			GUI.color = new Color(0.22f, 0.34f, 0.38f, 0.28f);
			GUI.DrawTexture(new Rect(localRect.width - planetSize * 0.78f, -planetSize * 0.22f, planetSize, planetSize), circleTexture, ScaleMode.StretchToFill, true);
			GUI.color = previousColor;

			var floor = localRect.height * 0.76f;
			DrawDistantSilhouette(localRect.width, floor);
			DrawFactory(localRect.width, floor);
			DrawConveyor(localRect.width, floor + 20f);
			DrawVistaStatus(localRect.width, localRect.height);
			GUI.EndGroup();
		}

		private void DrawDistantSilhouette(float width, float floor)
		{
			var silhouette = new Color(0.055f, 0.075f, 0.085f, 0.95f);
			DrawRect(new Rect(0f, floor, width, 200f), new Color(0.025f, 0.035f, 0.04f, 1f));

			for (var index = 0; index < 14; index++)
			{
				var sectionWidth = width / 14f;
				var height = 16f + (index * 29) % 48;
				DrawRect(new Rect(index * sectionWidth - 3f, floor - height, sectionWidth + 8f, height), silhouette);
			}

			DrawRect(new Rect(0f, floor - 2f, width, 2f), new Color(0.27f, 0.42f, 0.45f, 0.22f));
		}

		private void DrawFactory(float width, float floor)
		{
			var machines = 4;
			if (world.IsIndustrialized)
			{
				machines = 6;
			}

			var spacing = width / (machines + 1f);
			for (var index = 0; index < machines; index++)
			{
				MachineType machine;
				if (index < outpostMachines.Length)
				{
					machine = outpostMachines[index];
				}
				else
				{
					machine = industryMachines[index - outpostMachines.Length];
				}

				var x = spacing * (index + 1f);
				DrawMachineSilhouette(machine, x, floor);
			}
		}

		private void DrawMachineSilhouette(MachineType machine, float centerX, float floor)
		{
			var count = world.Machines.Get(machine);
			var active = count > 0;
			var bodyColor = new Color(0.11f, 0.14f, 0.15f, 1f);
			var trimColor = new Color(0.25f, 0.32f, 0.34f, 0.72f);
			var lightColor = GetMachineAccent(machine);
			if (!active)
			{
				bodyColor = new Color(0.075f, 0.09f, 0.10f, 0.78f);
				trimColor = new Color(0.17f, 0.20f, 0.21f, 0.48f);
				lightColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.13f);
			}

			var width = 58f;
			var height = 54f;
			if (machine == MachineType.ResearchStation || machine == MachineType.Steelworks)
			{
				height = 70f;
			}

			var body = new Rect(centerX - width * 0.5f, floor - height, width, height);
			DrawRect(body, bodyColor);
			DrawRect(new Rect(body.x, body.y, body.width, 2f), trimColor);
			DrawRect(new Rect(body.x + 7f, body.y + 9f, body.width - 14f, 5f), lightColor);

			if (machine == MachineType.Smelter || machine == MachineType.Steelworks)
			{
				DrawRect(new Rect(body.x + body.width * 0.64f, body.y - 34f, 11f, 34f), bodyColor);
				DrawSmoke(body.x + body.width * 0.64f + 5f, body.y - 36f, active);
			}
			else if (machine == MachineType.ResearchStation)
			{
				DrawRect(new Rect(centerX - 2f, body.y - 28f, 4f, 28f), trimColor);
				var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
				var pulseAlpha = 0.12f;
				if (active)
				{
					pulseAlpha = 0.46f + pulse * 0.45f;
				}

				var pulseColor = new Color(cyanColor.r, cyanColor.g, cyanColor.b, pulseAlpha);
				var previousColor = GUI.color;
				GUI.color = pulseColor;
				GUI.DrawTexture(new Rect(centerX - 8f, body.y - 36f, 16f, 16f), circleTexture, ScaleMode.StretchToFill, true);
				GUI.color = previousColor;
			}
			else if (machine == MachineType.CoalMine)
			{
				DrawRect(new Rect(centerX - 2f, body.y - 30f, 4f, 30f), trimColor);
				DrawRect(new Rect(centerX - 18f, body.y - 25f, 36f, 3f), trimColor);
			}
			else if (machine == MachineType.Extractor)
			{
				DrawRect(new Rect(centerX - 3f, body.y - 22f, 6f, 22f), trimColor);
				DrawRect(new Rect(centerX - 13f, body.y - 20f, 26f, 3f), trimColor);
			}

			if (active)
			{
				var flicker = 0.62f + 0.38f * Mathf.Sin(Time.unscaledTime * 3.2f + (int)machine * 1.7f);
				DrawRect(new Rect(body.x + 10f, body.yMax - 18f, body.width - 20f, 8f), new Color(lightColor.r, lightColor.g, lightColor.b, 0.24f + flicker * 0.55f));
			}

			GUI.Label(new Rect(centerX - 60f, floor + 7f, 120f, 22f), $"{GetShortMachineName(machine)}  x{count}", vistaLabelStyle);
		}

		private void DrawSmoke(float x, float y, bool active)
		{
			if (!active)
			{
				return;
			}

			var previousColor = GUI.color;
			for (var index = 0; index < 3; index++)
			{
				var travel = Mathf.Repeat(Time.unscaledTime * (0.11f + index * 0.025f) + index * 0.31f, 1f);
				var size = 9f + travel * 17f;
				GUI.color = new Color(0.45f, 0.50f, 0.51f, (1f - travel) * 0.18f);
				GUI.DrawTexture(new Rect(x - size * 0.5f + Mathf.Sin(travel * 8f + index) * 6f, y - travel * 54f, size, size), circleTexture, ScaleMode.StretchToFill, true);
			}

			GUI.color = previousColor;
		}

		private void DrawConveyor(float width, float y)
		{
			DrawRect(new Rect(22f, y, width - 44f, 3f), new Color(0.23f, 0.29f, 0.30f, 0.85f));
			var totalMachines = 0;
			foreach (var machine in outpostMachines)
			{
				totalMachines += world.Machines.Get(machine);
			}

			foreach (var machine in industryMachines)
			{
				totalMachines += world.Machines.Get(machine);
			}

			if (totalMachines == 0)
			{
				return;
			}

			var speed = 42f + Mathf.Min(totalMachines, 30) * 2.4f;
			for (var index = 0; index < 10; index++)
			{
				var x = Mathf.Repeat(Time.unscaledTime * speed + index * 73f, width - 54f) + 27f;
				DrawRect(new Rect(x, y - 2f, 14f, 7f), new Color(amberColor.r, amberColor.g, amberColor.b, 0.72f));
			}
		}

		private void DrawVistaStatus(float width, float height)
		{
			GUI.Label(new Rect(16f, 13f, width - 32f, 20f), "SURFACE INDUSTRIAL NODE // AUTOMATION TELEMETRY", eyebrowStyle);

			var machineCount = 0;
			foreach (var machine in outpostMachines)
			{
				machineCount += world.Machines.Get(machine);
			}

			foreach (var machine in industryMachines)
			{
				machineCount += world.Machines.Get(machine);
			}

			var phase = "OUTPOST";
			if (world.IsIndustrialized)
			{
				phase = "INDUSTRY";
			}

			GUI.Label(new Rect(16f, height - 38f, width - 32f, 20f), $"{phase} PHASE   //   {machineCount} MACHINES ONLINE", vistaLabelStyle);
		}

		private void DrawControlDeck(Rect rect)
		{
			GUI.Box(rect, GUIContent.none, panelStyle);
			var inner = new Rect(rect.x + 14f, rect.y + 13f, rect.width - 28f, rect.height - 26f);
			GUILayout.BeginArea(inner);
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

			GUILayout.Label("CONTROL DECK", eyebrowStyle);
			GUILayout.Label("Outpost Operations", headingStyle);
			GUILayout.Space(8f);

			DrawResources(baseResources);
			if (world.IsIndustrialized)
			{
				GUILayout.Space(6f);
				DrawResources(industryResources);
			}

			GUILayout.Space(12f);
			if (GUILayout.Button("MINE ORE   +1", buttonStyle, GUILayout.Height(44f)))
			{
				world.MineOre();
				status = "Manual extraction pulse complete. Ore added to local storage.";
			}

			GUILayout.Space(18f);
			GUILayout.Label("PRODUCTION ARRAY", eyebrowStyle);
			foreach (var machine in outpostMachines)
			{
				DrawMachine(machine);
			}

			GUILayout.Space(16f);
			DrawIndustrialization();

			if (world.IsIndustrialized)
			{
				GUILayout.Space(16f);
				GUILayout.Label("HEAVY INDUSTRY", eyebrowStyle);
				foreach (var machine in industryMachines)
				{
					DrawMachine(machine);
				}

				GUILayout.Space(10f);
				DrawSteelGoal();
			}

			GUILayout.Space(20f);
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawResources(IEnumerable<ResourceType> resources)
		{
			GUILayout.BeginHorizontal();
			foreach (var resource in resources)
			{
				GUILayout.BeginVertical(resourceCardStyle, GUILayout.MinWidth(92f), GUILayout.Height(66f));
				GUILayout.Label(resource.ToString().ToUpperInvariant(), eyebrowStyle);
				var previousColor = GUI.contentColor;
				GUI.contentColor = GetResourceColor(resource);
				GUILayout.Label(FormatAmount(world.Resources.Get(resource)), machineTitleStyle);
				GUI.contentColor = previousColor;
				GUILayout.EndVertical();
			}

			GUILayout.EndHorizontal();
		}

		private void DrawMachine(MachineType machine)
		{
			var unlocked = world.IsMachineUnlocked(machine);
			var costs = world.GetMachineCost(machine);
			var affordable = CanAfford(costs);
			var owned = world.Machines.Get(machine);

			GUILayout.BeginVertical(machineCardStyle);
			GUILayout.BeginHorizontal();
			GUILayout.Label($"{GetMachineName(machine)}  x{owned}", machineTitleStyle);
			GUILayout.FlexibleSpace();

			var previousColor = GUI.contentColor;
			GUI.contentColor = GetMachineAccent(machine);
			var state = "STANDBY";
			if (owned > 0)
			{
				state = "ONLINE";
			}

			GUILayout.Label(state, eyebrowStyle, GUILayout.Width(68f));
			GUI.contentColor = previousColor;
			GUILayout.EndHorizontal();
			GUILayout.Label(GetMachineFlow(machine), bodyStyle);
			GUILayout.Label($"NEXT BUILD  //  {FormatCosts(costs)}", mutedStyle);
			GUILayout.Space(4f);

			var previousEnabled = GUI.enabled;
			GUI.enabled = unlocked && affordable;
			var buttonText = "BUILD UNIT";
			if (!unlocked)
			{
				buttonText = "LOCKED";
			}
			else if (!affordable)
			{
				buttonText = "INSUFFICIENT MATERIAL";
			}

			GUIStyle style;
			if (GUI.enabled)
			{
				style = buttonStyle;
			}
			else
			{
				style = buttonDisabledStyle;
			}

			if (GUILayout.Button(buttonText, style, GUILayout.Height(34f)) && world.TryBuyMachine(machine))
			{
				status = $"{GetMachineName(machine)} commissioned. Production array expanded.";
			}

			GUI.enabled = previousEnabled;
			GUILayout.EndVertical();
			GUILayout.Space(7f);
		}

		private void DrawIndustrialization()
		{
			GUILayout.BeginVertical(machineCardStyle);
			GUILayout.Label("INDUSTRIALIZATION", machineTitleStyle);
			if (world.IsIndustrialized)
			{
				var previousColor = GUI.contentColor;
				GUI.contentColor = greenColor;
				GUILayout.Label("BREAKTHROUGH COMPLETE", eyebrowStyle);
				GUI.contentColor = previousColor;
				GUILayout.Label("Coal extraction and steel production are now authorized.", bodyStyle);
				GUILayout.EndVertical();

				return;
			}

			var cost = world.IndustrializationCost[0];
			var current = world.Resources.Get(cost.Resource);
			var progress = Mathf.Clamp01(current / (float)cost.Amount);
			GUILayout.Label("Convert the mature Outpost production chain into the knowledge required for heavy industry.", bodyStyle);
			GUILayout.Space(5f);
			GUILayout.Label($"RESEARCH  //  {FormatAmount(current)} / {FormatAmount(cost.Amount)}", mutedStyle);
			var progressRect = GUILayoutUtility.GetRect(10f, 15f, GUILayout.ExpandWidth(true));
			DrawProgress(progressRect, progress, cyanColor);
			GUILayout.Space(7f);

			var previousEnabled = GUI.enabled;
			GUI.enabled = CanAfford(world.IndustrializationCost);
			GUIStyle style;
			if (GUI.enabled)
			{
				style = buttonStyle;
			}
			else
			{
				style = buttonDisabledStyle;
			}

			if (GUILayout.Button("RESEARCH INDUSTRIALIZATION", style, GUILayout.Height(38f)) && world.TryIndustrialize())
			{
				status = "Industrialization breakthrough confirmed. Heavy industry is online.";
			}

			GUI.enabled = previousEnabled;
			GUILayout.EndVertical();
		}

		private void DrawSteelGoal()
		{
			GUILayout.BeginVertical(machineCardStyle);
			GUILayout.Label("VERTICAL SLICE OBJECTIVE", eyebrowStyle);
			GUILayout.Label("Sustain Steel Production", machineTitleStyle);
			var capacity = world.Machines.Get(MachineType.Steelworks);
			var progress = Mathf.Clamp01(capacity);
			GUILayout.Label($"STEELWORKS CAPACITY  //  {capacity} STEEL/s", mutedStyle);
			var progressRect = GUILayoutUtility.GetRect(10f, 15f, GUILayout.ExpandWidth(true));
			DrawProgress(progressRect, progress, steelColor);
			GUILayout.EndVertical();
		}

		private void DrawProgress(Rect rect, float progress, Color color)
		{
			DrawRect(rect, new Color(0.07f, 0.09f, 0.10f, 0.95f));
			var fill = new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, (rect.width - 4f) * progress), rect.height - 4f);
			DrawRect(fill, new Color(color.r, color.g, color.b, 0.86f));
			DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(color.r, color.g, color.b, 0.34f));
		}

		private bool CanAfford(IReadOnlyList<ResourceCost> costs)
		{
			foreach (var cost in costs)
			{
				if (world.Resources.Get(cost.Resource) < cost.Amount)
				{
					return false;
				}
			}

			return true;
		}

		private static string FormatCosts(IReadOnlyList<ResourceCost> costs)
		{
			var builder = new StringBuilder();
			for (var index = 0; index < costs.Count; index++)
			{
				if (index > 0)
				{
					builder.Append(" + ");
				}

				var cost = costs[index];
				builder.Append(FormatAmount(cost.Amount));
				builder.Append(' ');
				builder.Append(cost.Resource);
			}

			return builder.ToString();
		}

		private static string FormatAmount(long amount)
		{
			if (amount < 1_000)
			{
				return amount.ToString("N0");
			}

			if (amount < 1_000_000)
			{
				return $"{amount / 1_000d:0.##}K";
			}

			if (amount < 1_000_000_000)
			{
				return $"{amount / 1_000_000d:0.##}M";
			}

			return $"{amount / 1_000_000_000d:0.##}B";
		}

		private static string GetMachineName(MachineType machine)
		{
			var name = machine switch
			{
				MachineType.Extractor => "Extractor",
				MachineType.Smelter => "Smelter",
				MachineType.Assembler => "Assembler",
				MachineType.ResearchStation => "Research Station",
				MachineType.CoalMine => "Coal Mine",
				MachineType.Steelworks => "Steelworks",
				_ => machine.ToString()
			};

			return name;
		}

		private static string GetShortMachineName(MachineType machine)
		{
			var name = machine switch
			{
				MachineType.Extractor => "EXT",
				MachineType.Smelter => "SMELT",
				MachineType.Assembler => "ASM",
				MachineType.ResearchStation => "R&D",
				MachineType.CoalMine => "COAL",
				MachineType.Steelworks => "STEEL",
				_ => "UNIT"
			};

			return name;
		}

		private static string GetMachineFlow(MachineType machine)
		{
			var flow = machine switch
			{
				MachineType.Extractor => "+1 Ore / s",
				MachineType.Smelter => "2 Ore  >  1 Metal / s",
				MachineType.Assembler => "3 Metal  >  1 Component / s",
				MachineType.ResearchStation => "2 Components  >  1 Research / s",
				MachineType.CoalMine => "+1 Coal / s",
				MachineType.Steelworks => "4 Metal + 2 Coal  >  1 Steel / s",
				_ => string.Empty
			};

			return flow;
		}

		private static Color GetMachineAccent(MachineType machine)
		{
			var color = machine switch
			{
				MachineType.Extractor => new Color(0.72f, 0.58f, 0.34f),
				MachineType.Smelter => amberColor,
				MachineType.Assembler => new Color(0.66f, 0.72f, 0.76f),
				MachineType.ResearchStation => cyanColor,
				MachineType.CoalMine => new Color(0.42f, 0.47f, 0.48f),
				MachineType.Steelworks => steelColor,
				_ => textColor
			};

			return color;
		}

		private static Color GetResourceColor(ResourceType resource)
		{
			var color = resource switch
			{
				ResourceType.Ore => new Color(0.76f, 0.60f, 0.38f),
				ResourceType.Metal => steelColor,
				ResourceType.Components => new Color(0.91f, 0.72f, 0.30f),
				ResourceType.Research => cyanColor,
				ResourceType.Coal => new Color(0.56f, 0.59f, 0.60f),
				ResourceType.Steel => new Color(0.78f, 0.86f, 0.90f),
				_ => textColor
			};

			return color;
		}

		private static void DrawRect(Rect rect, Color color)
		{
			var previousColor = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, Texture2D.whiteTexture);
			GUI.color = previousColor;
		}

		private void EnsureStyles()
		{
			if (titleStyle != null)
			{
				return;
			}

			skyTexture = CreateGradientTexture(new Color(0.025f, 0.045f, 0.065f), new Color(0.075f, 0.105f, 0.12f));
			panelTexture = CreateTexture(new Color(0.035f, 0.048f, 0.055f, 0.96f));
			cardTexture = CreateTexture(new Color(0.065f, 0.078f, 0.083f, 0.96f));
			cardRaisedTexture = CreateTexture(new Color(0.082f, 0.098f, 0.104f, 0.98f));
			buttonTexture = CreateTexture(new Color(0.70f, 0.36f, 0.09f, 1f));
			buttonHoverTexture = CreateTexture(new Color(0.87f, 0.46f, 0.11f, 1f));
			buttonActiveTexture = CreateTexture(new Color(0.57f, 0.28f, 0.07f, 1f));
			buttonDisabledTexture = CreateTexture(new Color(0.12f, 0.14f, 0.15f, 1f));
			badgeTexture = CreateTexture(new Color(0.10f, 0.14f, 0.15f, 0.96f));
			circleTexture = CreateCircleTexture(64);

			panelStyle = new GUIStyle(GUI.skin.box)
			{
				background = panelTexture,
				border = new RectOffset(1, 1, 1, 1),
				padding = new RectOffset(12, 12, 12, 12)
			};

			titleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 24,
				fontStyle = FontStyle.Bold,
				normal = { textColor = textColor }
			};

			eyebrowStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				fontStyle = FontStyle.Bold,
				normal = { textColor = mutedColor },
				wordWrap = true
			};

			headingStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 19,
				fontStyle = FontStyle.Bold,
				normal = { textColor = textColor },
				wordWrap = true
			};

			machineTitleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				fontStyle = FontStyle.Bold,
				normal = { textColor = textColor },
				wordWrap = true
			};

			bodyStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 12,
				normal = { textColor = new Color(0.76f, 0.81f, 0.83f) },
				wordWrap = true
			};

			mutedStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				normal = { textColor = mutedColor },
				wordWrap = true
			};

			resourceCardStyle = new GUIStyle(GUI.skin.box)
			{
				background = cardRaisedTexture,
				padding = new RectOffset(9, 9, 8, 8),
				margin = new RectOffset(0, 6, 0, 0)
			};

			machineCardStyle = new GUIStyle(GUI.skin.box)
			{
				background = cardTexture,
				padding = new RectOffset(11, 11, 9, 9),
				margin = new RectOffset(0, 0, 0, 0)
			};

			buttonStyle = new GUIStyle(GUI.skin.button)
			{
				fontSize = 11,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				padding = new RectOffset(10, 10, 8, 8)
			};
			buttonStyle.normal.background = buttonTexture;
			buttonStyle.normal.textColor = Color.white;
			buttonStyle.hover.background = buttonHoverTexture;
			buttonStyle.hover.textColor = Color.white;
			buttonStyle.active.background = buttonActiveTexture;
			buttonStyle.active.textColor = Color.white;
			buttonStyle.focused.background = buttonTexture;
			buttonStyle.focused.textColor = Color.white;

			buttonDisabledStyle = new GUIStyle(buttonStyle);
			buttonDisabledStyle.normal.background = buttonDisabledTexture;
			buttonDisabledStyle.normal.textColor = new Color(0.43f, 0.48f, 0.49f);
			buttonDisabledStyle.disabled.background = buttonDisabledTexture;
			buttonDisabledStyle.disabled.textColor = new Color(0.43f, 0.48f, 0.49f);

			badgeStyle = new GUIStyle(GUI.skin.label)
			{
				background = badgeTexture,
				fontSize = 10,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = new Color(0.69f, 0.76f, 0.78f) }
			};

			vistaLabelStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 9,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = new Color(0.52f, 0.62f, 0.64f) }
			};
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

		private static Texture2D CreateGradientTexture(Color top, Color bottom)
		{
			var texture = new Texture2D(2, 64, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};

			for (var y = 0; y < texture.height; y++)
			{
				var progress = y / (texture.height - 1f);
				var color = Color.Lerp(bottom, top, progress);
				texture.SetPixel(0, y, color);
				texture.SetPixel(1, y, color);
			}

			texture.Apply(false, true);

			return texture;
		}

		private static Texture2D CreateCircleTexture(int size)
		{
			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear
			};
			var center = (size - 1f) * 0.5f;
			var radius = center;
			for (var y = 0; y < size; y++)
			{
				for (var x = 0; x < size; x++)
				{
					var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
					var alpha = Mathf.Clamp01((radius - distance) / 2f);
					texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
				}
			}

			texture.Apply(false, true);

			return texture;
		}

		private void OnDestroy()
		{
			DestroyTexture(skyTexture);
			DestroyTexture(panelTexture);
			DestroyTexture(cardTexture);
			DestroyTexture(cardRaisedTexture);
			DestroyTexture(buttonTexture);
			DestroyTexture(buttonHoverTexture);
			DestroyTexture(buttonActiveTexture);
			DestroyTexture(buttonDisabledTexture);
			DestroyTexture(badgeTexture);
			DestroyTexture(circleTexture);
		}

		private static void DestroyTexture(Texture2D texture)
		{
			if (texture != null)
			{
				UnityEngine.Object.Destroy(texture);
			}
		}
	}
}
