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

		private OutpostWorld world;
		private Vector2 scrollPosition;
		private float elapsedSimulationSeconds;
		private string status = "Mine enough ore to build your first Extractor.";
		private GUIStyle titleStyle;
		private GUIStyle headingStyle;
		private GUIStyle mutedStyle;

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

			var margin = 24f;
			var width = Mathf.Min(760f, Screen.width - margin * 2f);
			var height = Mathf.Max(200f, Screen.height - margin * 2f);
			GUILayout.BeginArea(new Rect(margin, margin, width, height), GUI.skin.box);
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			GUILayout.Label("ASTERFORGE / OUTPOST PROTOTYPE", titleStyle);
			GUILayout.Label(status, mutedStyle);
			GUILayout.Space(12f);

			DrawResources(baseResources);
			if (world.IsIndustrialized)
			{
				DrawResources(industryResources);
			}

			GUILayout.Space(12f);
			if (GUILayout.Button("Mine Ore  +1", GUILayout.Height(42f)))
			{
				world.MineOre();
				status = "You chipped one heroic unit of ore out of the ground.";
			}

			GUILayout.Space(16f);
			GUILayout.Label("Production", headingStyle);
			foreach (var machine in outpostMachines)
			{
				DrawMachine(machine);
			}

			GUILayout.Space(16f);
			DrawIndustrialization();

			if (world.IsIndustrialized)
			{
				GUILayout.Space(16f);
				GUILayout.Label("Industry", headingStyle);
				foreach (var machine in industryMachines)
				{
					DrawMachine(machine);
				}

				GUILayout.Space(12f);
				GUILayout.Label($"Steel capacity: up to {world.Machines.GetActive(MachineType.Steelworks)} /s", headingStyle);
				GUILayout.Label("Vertical-slice goal: sustain at least 1 Steel/s. Power, saving, and prettier machinery come after this prototype loop proves itself.", mutedStyle);
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawResources(IEnumerable<ResourceType> resources)
		{
			GUILayout.BeginHorizontal();
			foreach (var resource in resources)
			{
				GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(110f));
				GUILayout.Label(resource.ToString(), mutedStyle);
				GUILayout.Label(FormatAmount(world.Resources.Get(resource)), headingStyle);
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
			var paused = world.IsMachinePaused(machine);

			GUILayout.BeginVertical(GUI.skin.box);
			GUILayout.BeginHorizontal();
			var state = string.Empty;
			if (paused && owned > 0)
			{
				state = "  [PAUSED]";
			}

			GUILayout.Label($"{GetMachineName(machine)}  x{owned}{state}", headingStyle);
			GUILayout.FlexibleSpace();
			GUILayout.Label(GetMachineFlow(machine), mutedStyle);
			GUILayout.EndHorizontal();
			GUILayout.Label($"Next cost: {FormatCosts(costs)}", mutedStyle);

			GUILayout.BeginHorizontal();
			var previousEnabled = GUI.enabled;
			GUI.enabled = unlocked && affordable;
			var buttonText = "Build";
			if (!unlocked)
			{
				buttonText = "Locked";
			}
			else if (!affordable)
			{
				buttonText = "Need resources";
			}

			if (GUILayout.Button(buttonText, GUILayout.Height(34f)) && world.TryBuyMachine(machine))
			{
				status = $"Built {GetMachineName(machine)}.";
			}

			GUI.enabled = owned > 0;
			var pauseButtonText = paused switch
			{
				true => "Resume",
				false => "Pause"
			};

			if (GUILayout.Button(pauseButtonText, GUILayout.Width(100f), GUILayout.Height(34f)))
			{
				world.SetMachinePaused(machine, !paused);
				status = paused switch
				{
					true => $"Resumed {GetMachineName(machine)}.",
					false => $"Paused {GetMachineName(machine)}."
				};
			}

			GUI.enabled = previousEnabled;
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		private void DrawIndustrialization()
		{
			GUILayout.Label("Industrialization", headingStyle);
			if (world.IsIndustrialized)
			{
				GUILayout.Label("Industry unlocked. Coal and Steel production are now available.", mutedStyle);

				return;
			}

			foreach (var cost in world.IndustrializationCost)
			{
				var contributed = world.GetIndustrializationContribution(cost.Resource);
				GUILayout.Label($"{cost.Resource}: {FormatAmount(contributed)} / {FormatAmount(cost.Amount)}", mutedStyle);
			}

			GUILayout.Label("Funding is permanent. Pause downstream machine groups when you want to stockpile intermediate resources for the project.", mutedStyle);
			var previousEnabled = GUI.enabled;
			GUI.enabled = CanContributeToIndustrialization();
			if (GUILayout.Button("Contribute available resources", GUILayout.Height(38f)))
			{
				var contributed = world.ContributeAvailableToIndustrialization();
				if (world.IsIndustrialized)
				{
					status = "Industrialization complete. The outpost has become alarmingly serious.";
				}
				else
				{
					status = $"Banked {FormatAmount(contributed)} units toward Industrialization.";
				}
			}

			GUI.enabled = previousEnabled;
		}

		private bool CanContributeToIndustrialization()
		{
			foreach (var cost in world.IndustrializationCost)
			{
				var contributed = world.GetIndustrializationContribution(cost.Resource);
				if (contributed < cost.Amount && world.Resources.Get(cost.Resource) > 0)
				{
					return true;
				}
			}

			return false;
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

		private static string GetMachineFlow(MachineType machine)
		{
			var flow = machine switch
			{
				MachineType.Extractor => "+1 Ore/s",
				MachineType.Smelter => "2 Ore -> 1 Metal/s",
				MachineType.Assembler => "3 Metal -> 1 Component/s",
				MachineType.ResearchStation => "2 Components -> 1 Research/s",
				MachineType.CoalMine => "+1 Coal/s",
				MachineType.Steelworks => "4 Metal + 2 Coal -> 1 Steel/s",
				_ => string.Empty
			};

			return flow;
		}

		private void EnsureStyles()
		{
			if (titleStyle != null)
			{
				return;
			}

			titleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 22,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
			headingStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 16,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
			mutedStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 13,
				wordWrap = true
			};
		}
	}
}
