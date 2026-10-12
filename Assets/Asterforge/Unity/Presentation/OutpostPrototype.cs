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
			MachineType.CoalGenerator,
			MachineType.Steelworks
		};

		private static readonly Color textColor = new Color(0.91f, 0.93f, 0.95f);
		private static readonly Color mutedColor = new Color(0.55f, 0.62f, 0.67f);
		private static readonly Color amberColor = new Color(0.96f, 0.57f, 0.17f);
		private static readonly Color cyanColor = new Color(0.28f, 0.77f, 0.86f);
		private static readonly Color greenColor = new Color(0.39f, 0.81f, 0.53f);

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
			EnsureWorld();
		}

		private void Update()
		{
			EnsureWorld();
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
			EnsureWorld();
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
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), skyTexture);

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
			DrawRect(new Rect(0f, 0f, rect.width, rect.height), new Color(0.025f, 0.04f, 0.055f, 0.62f));
			DrawRect(new Rect(0f, rect.height - 2f, rect.width, 2f), new Color(0.30f, 0.40f, 0.44f, 0.35f));

			var planetSize = Mathf.Clamp(rect.height * 0.55f, 100f, 210f);
			var previousColor = GUI.color;
			GUI.color = new Color(0.22f, 0.34f, 0.38f, 0.28f);
			GUI.DrawTexture(new Rect(rect.width - planetSize * 0.78f, -planetSize * 0.22f, planetSize, planetSize), circleTexture);
			GUI.color = previousColor;

			var floor = rect.height * 0.76f;
			DrawDistantSilhouette(rect.width, floor);
			DrawFactory(rect.width, floor);
			DrawConveyor(rect.width, floor + 20f);
			DrawVistaStatus(rect.width, rect.height);
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
			var machineSlots = outpostMachines.Length;
			if (world.IsIndustrialized)
			{
				machineSlots += industryMachines.Length;
			}

			var spacing = width / (machineSlots + 1f);
			for (var index = 0; index < machineSlots; index++)
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

				DrawMachineSilhouette(machine, spacing * (index + 1f), floor);
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
			else if (machine == MachineType.CoalGenerator)
			{
				height = 62f;
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
				GUI.DrawTexture(new Rect(centerX - 8f, body.y - 36f, 16f, 16f), circleTexture);
				GUI.color = previousColor;
			}
			else if (machine == MachineType.CoalMine)
			{
				DrawRect(new Rect(centerX - 2f, body.y - 30f, 4f, 30f), trimColor);
				DrawRect(new Rect(centerX - 18f, body.y - 25f, 36f, 3f), trimColor);
			}
			else if (machine == MachineType.CoalGenerator)
			{
				DrawRect(new Rect(centerX - 2f, body.y - 22f, 4f, 22f), trimColor);
				DrawRect(new Rect(centerX - 19f, body.y - 20f, 38f, 3f), trimColor);
				DrawRect(new Rect(centerX - 16f, body.y - 15f, 4f, 10f), trimColor);
				DrawRect(new Rect(centerX + 12f, body.y - 15f, 4f, 10f), trimColor);
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
				GUI.color = new Color(0.35f, 0.38f, 0.39f, 0.22f * (1f - travel));
				GUI.DrawTexture(new Rect(x - size * 0.5f + index * 4f, y - travel * 58f, size, size), circleTexture);
			}

			GUI.color = previousColor;
		}

		private void DrawConveyor(float width, float y)
		{
			DrawRect(new Rect(12f, y, width - 24f, 3f), new Color(0.22f, 0.28f, 0.29f, 0.75f));
			if (GetTotalMachineCount() == 0)
			{
				return;
			}

			var previousColor = GUI.color;
			for (var index = 0; index < 9; index++)
			{
				var travel = Mathf.Repeat(Time.unscaledTime * 0.08f + index / 9f, 1f);
				var x = 14f + travel * (width - 36f);
				GUI.color = new Color(0.93f, 0.55f, 0.18f, 0.72f);
				GUI.DrawTexture(new Rect(x, y - 3f, 7f, 7f), Texture2D.whiteTexture);
			}

			GUI.color = previousColor;
		}

		private void DrawVistaStatus(float width, float height)
		{
			var phase = "OUTPOST";
			if (world.IsIndustrialized)
			{
				phase = "INDUSTRY";
			}

			GUI.Label(new Rect(16f, height - 38f, width - 32f, 20f), $"{phase} PHASE   //   {GetTotalMachineCount()} MACHINES ONLINE", vistaLabelStyle);
		}

		private int GetTotalMachineCount()
		{
			var count = 0;
			foreach (var machine in outpostMachines)
			{
				count += world.Machines.Get(machine);
			}

			foreach (var machine in industryMachines)
			{
				count += world.Machines.Get(machine);
			}

			return count;
		}

		private void DrawControlDeck(Rect rect)
		{
			GUILayout.BeginArea(rect, panelStyle);
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);
			GUILayout.Label("RESOURCE TELEMETRY", eyebrowStyle);
			DrawResources(baseResources);
			if (world.IsIndustrialized)
			{
				GUILayout.Space(6f);
				DrawResources(industryResources);
				GUILayout.Space(8f);
				DrawPowerTelemetry();
			}

			GUILayout.Space(12f);
			GUILayout.Label("MANUAL EXTRACTION", eyebrowStyle);
			if (GUILayout.Button("MINE ORE  +1", buttonStyle, GUILayout.Height(40f)))
			{
				world.MineOre();
				status = "Manual extraction logged. Ore reserve increased by one unit.";
			}

			GUILayout.Space(14f);
			GUILayout.Label("PRODUCTION ARRAY", headingStyle);
			foreach (var machine in outpostMachines)
			{
				DrawMachine(machine);
				GUILayout.Space(6f);
			}

			GUILayout.Space(8f);
			DrawIndustrialization();

			if (world.IsIndustrialized)
			{
				GUILayout.Space(14f);
				GUILayout.Label("HEAVY INDUSTRY", headingStyle);
				foreach (var machine in industryMachines)
				{
					DrawMachine(machine);
					GUILayout.Space(6f);
				}

				DrawSteelProgress();
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawResources(IEnumerable<ResourceType> resources)
		{
			GUILayout.BeginHorizontal();
			foreach (var resource in resources)
			{
				GUILayout.BeginVertical(resourceCardStyle, GUILayout.MinWidth(90f));
				GUILayout.Label(resource.ToString().ToUpperInvariant(), eyebrowStyle);

				var valueStyle = new GUIStyle(machineTitleStyle);
				valueStyle.normal.textColor = GetResourceAccent(resource);
				GUILayout.Label(FormatAmount(world.Resources.Get(resource)), valueStyle);
				GUILayout.EndVertical();
			}

			GUILayout.EndHorizontal();
		}

		private void DrawPowerTelemetry()
		{
			var capacity = world.PowerCapacity;
			var fuelBackedCapacity = world.FuelBackedPowerCapacity;
			var demand = world.PowerDemand;
			var delivery = GetPowerDelivery(fuelBackedCapacity, demand);
			var state = "STANDBY";
			var stateColor = mutedColor;
			var fillColor = greenColor;
			var message = "No powered facilities are drawing from the grid.";
			if (demand > 0 && capacity == 0)
			{
				state = "OFFLINE";
				stateColor = amberColor;
				fillColor = amberColor;
				message = "Commission a Coal Generator before Steelworks can produce.";
			}
			else if (demand > 0 && fuelBackedCapacity == 0)
			{
				state = "FUEL STARVED";
				stateColor = amberColor;
				fillColor = amberColor;
				message = "Generator capacity exists, but the current Coal reserve cannot fuel it.";
			}
			else if (demand > 0 && delivery < 0.999f)
			{
				state = "BROWNOUT";
				stateColor = amberColor;
				fillColor = amberColor;
				message = "Fuel-backed generation cannot meet current powered demand.";
			}
			else if (demand > 0)
			{
				state = "NOMINAL";
				stateColor = greenColor;
				message = "Grid nominal. Coal Generators consume fuel only while supplying powered demand.";
			}

			GUILayout.BeginVertical(machineCardStyle);
			GUILayout.BeginHorizontal();
			GUILayout.Label("POWER GRID", machineTitleStyle);
			GUILayout.FlexibleSpace();
			var stateStyle = new GUIStyle(eyebrowStyle);
			stateStyle.normal.textColor = stateColor;
			GUILayout.Label(state, stateStyle, GUILayout.Width(88f));
			GUILayout.EndHorizontal();
			GUILayout.Label($"CAPACITY {FormatAmount(capacity)}  //  FUELED {FormatAmount(fuelBackedCapacity)}  //  DEMAND {FormatAmount(demand)}", bodyStyle);
			DrawProgressBar(delivery, fillColor);
			GUILayout.Label($"AVAILABLE {(delivery * 100f):0.#}%  //  {message}", mutedStyle);
			GUILayout.EndVertical();
		}

		private static float GetPowerDelivery(long fuelBackedCapacity, long demand)
		{
			if (demand == 0)
			{
				return 1f;
			}

			return Mathf.Clamp01((float)(fuelBackedCapacity / (double)demand));
		}

		private void DrawMachine(MachineType machine)
		{
			var unlocked = world.IsMachineUnlocked(machine);
			var costs = world.GetMachineCost(machine);
			var affordable = CanAfford(costs);
			var owned = world.Machines.Get(machine);
			var active = owned > 0;

			GUILayout.BeginVertical(machineCardStyle);
			GUILayout.BeginHorizontal();
			GUILayout.Label($"{GetMachineName(machine)}  x{owned}", machineTitleStyle);
			GUILayout.FlexibleSpace();

			var state = "STANDBY";
			var stateColor = mutedColor;
			if (!unlocked)
			{
				state = "LOCKED";
			}
			else if (active)
			{
				state = "ONLINE";
				stateColor = greenColor;
			}

			var stateStyle = new GUIStyle(eyebrowStyle);
			stateStyle.normal.textColor = stateColor;
			GUILayout.Label(state, stateStyle, GUILayout.Width(62f));
			GUILayout.EndHorizontal();
			GUILayout.Label(GetMachineFlow(machine), bodyStyle);
			GUILayout.Label($"NEXT UNIT  //  {FormatCosts(costs)}", mutedStyle);

			var previousEnabled = GUI.enabled;
			GUI.enabled = unlocked && affordable;
			var buttonText = "COMMISSION UNIT";
			if (!unlocked)
			{
				buttonText = "PRODUCTION STAGE LOCKED";
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
		}

		private void DrawIndustrialization()
		{
			GUILayout.BeginVertical(machineCardStyle);
			GUILayout.Label("INDUSTRIALIZATION", machineTitleStyle);
			if (world.IsIndustrialized)
			{
				GUILayout.Label("BREAKTHROUGH COMPLETE  //  COAL + POWER + STEEL ONLINE", bodyStyle);
				DrawProgressBar(1f, greenColor);
				GUILayout.EndVertical();

				return;
			}

			var research = world.Resources.Get(ResourceType.Research);
			var required = world.IndustrializationCost[0].Amount;
			var progress = Mathf.Clamp01(research / (float)required);
			GUILayout.Label($"RESEARCH PROGRESS  //  {FormatAmount(research)} / {FormatAmount(required)}", bodyStyle);
			DrawProgressBar(progress, cyanColor);
			GUILayout.Label("Transform the outpost into a heavy-industry colony. Existing production can keep running while Research accumulates.", mutedStyle);

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

		private void DrawSteelProgress()
		{
			GUILayout.BeginVertical(machineCardStyle);
			GUILayout.Label("VERTICAL-SLICE OBJECTIVE", eyebrowStyle);
			var steelworks = world.Machines.Get(MachineType.Steelworks);
			var progress = Mathf.Clamp01(steelworks);
			GUILayout.Label("Sustain at least 1 Steel/s", machineTitleStyle);
			DrawProgressBar(progress, greenColor);

			var message = "Commission a Steelworks to establish steel production.";
			if (steelworks > 0 && world.PowerCapacity == 0)
			{
				message = "Steelworks commissioned, but production requires a Coal Generator.";
			}
			else if (steelworks > 0 && world.FuelBackedPowerCapacity < world.PowerDemand)
			{
				message = "Steel capacity is power-limited. Add generation or restore Coal fuel before calling output sustained.";
			}
			else if (steelworks > 0)
			{
				message = "Steel capacity established. Keep Metal, Coal, and grid power supplied to sustain output.";
			}

			GUILayout.Label(message, mutedStyle);
			GUILayout.EndVertical();
		}

		private void DrawProgressBar(float progress, Color fillColor)
		{
			var rect = GUILayoutUtility.GetRect(10f, 10f, GUILayout.ExpandWidth(true));
			DrawRect(rect, new Color(0.10f, 0.12f, 0.13f, 1f));
			DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(progress), rect.height), fillColor);
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
				MachineType.CoalGenerator => "Coal Generator",
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
				MachineType.CoalGenerator => "GEN",
				MachineType.Steelworks => "STEEL",
				_ => "UNIT"
			};

			return name;
		}

		private static string GetMachineFlow(MachineType machine)
		{
			var flow = machine switch
			{
				MachineType.Extractor => "+1 Ore/s",
				MachineType.Smelter => "2 Ore -> 1 Metal/s",
				MachineType.Assembler => "3 Metal -> 1 Component/s",
				MachineType.ResearchStation => "2 Components -> 1 Research/s",
				MachineType.CoalMine => "+1 Coal/s",
				MachineType.CoalGenerator => "1 Coal/s while loaded -> 4 Power",
				MachineType.Steelworks => "4 Metal + 2 Coal + 4 Power -> 1 Steel/s",
				_ => string.Empty
			};

			return flow;
		}

		private static Color GetMachineAccent(MachineType machine)
		{
			var color = machine switch
			{
				MachineType.Extractor => new Color(0.88f, 0.56f, 0.24f),
				MachineType.Smelter => new Color(0.95f, 0.35f, 0.13f),
				MachineType.Assembler => new Color(0.83f, 0.68f, 0.27f),
				MachineType.ResearchStation => cyanColor,
				MachineType.CoalMine => new Color(0.56f, 0.59f, 0.60f),
				MachineType.CoalGenerator => new Color(0.95f, 0.72f, 0.22f),
				MachineType.Steelworks => new Color(0.73f, 0.82f, 0.88f),
				_ => textColor
			};

			return color;
		}

		private static Color GetResourceAccent(ResourceType resource)
		{
			var color = resource switch
			{
				ResourceType.Ore => new Color(0.88f, 0.56f, 0.24f),
				ResourceType.Metal => new Color(0.78f, 0.82f, 0.83f),
				ResourceType.Components => new Color(0.86f, 0.70f, 0.27f),
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

		private void EnsureWorld()
		{
			if (world != null)
			{
				return;
			}

			world = new OutpostWorld();
			elapsedSimulationSeconds = 0f;
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
				border = new RectOffset(1, 1, 1, 1),
				padding = new RectOffset(12, 12, 12, 12)
			};
			panelStyle.normal.background = panelTexture;

			titleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 24,
				fontStyle = FontStyle.Bold
			};
			titleStyle.normal.textColor = textColor;

			eyebrowStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
			eyebrowStyle.normal.textColor = mutedColor;

			headingStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 19,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
			headingStyle.normal.textColor = textColor;

			machineTitleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
			machineTitleStyle.normal.textColor = textColor;

			bodyStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 12,
				wordWrap = true
			};
			bodyStyle.normal.textColor = new Color(0.76f, 0.81f, 0.83f);

			mutedStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				wordWrap = true
			};
			mutedStyle.normal.textColor = mutedColor;

			resourceCardStyle = new GUIStyle(GUI.skin.box)
			{
				padding = new RectOffset(9, 9, 8, 8),
				margin = new RectOffset(0, 6, 0, 0)
			};
			resourceCardStyle.normal.background = cardRaisedTexture;

			machineCardStyle = new GUIStyle(GUI.skin.box)
			{
				padding = new RectOffset(11, 11, 9, 9),
				margin = new RectOffset(0, 0, 0, 0)
			};
			machineCardStyle.normal.background = cardTexture;

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
			buttonDisabledStyle.hover.background = buttonDisabledTexture;
			buttonDisabledStyle.hover.textColor = new Color(0.43f, 0.48f, 0.49f);
			buttonDisabledStyle.active.background = buttonDisabledTexture;
			buttonDisabledStyle.active.textColor = new Color(0.43f, 0.48f, 0.49f);
			buttonDisabledStyle.focused.background = buttonDisabledTexture;
			buttonDisabledStyle.focused.textColor = new Color(0.43f, 0.48f, 0.49f);

			badgeStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 10,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter
			};
			badgeStyle.normal.background = badgeTexture;
			badgeStyle.normal.textColor = new Color(0.69f, 0.76f, 0.78f);

			vistaLabelStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 9,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter
			};
			vistaLabelStyle.normal.textColor = new Color(0.52f, 0.62f, 0.64f);
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
			var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			for (var y = 0; y < 64; y++)
			{
				var progress = y / 63f;
				texture.SetPixel(0, y, Color.Lerp(bottom, top, progress));
			}

			texture.Apply(false, true);

			return texture;
		}

		private static Texture2D CreateCircleTexture(int size)
		{
			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			var center = (size - 1) * 0.5f;
			var radius = center;
			for (var y = 0; y < size; y++)
			{
				for (var x = 0; x < size; x++)
				{
					var dx = x - center;
					var dy = y - center;
					var alpha = dx * dx + dy * dy <= radius * radius ? 1f : 0f;
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
