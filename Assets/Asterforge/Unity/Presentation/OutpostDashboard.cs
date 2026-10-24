using System;
using System.Collections.Generic;
using System.Text;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Progression;
using Asterforge.Game.Simulation.Resources;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asterforge.Unity.Presentation
{
	[RequireComponent(typeof(UIDocument))]
	public sealed class OutpostDashboard : MonoBehaviour
	{
		private static readonly MachineType[] machineTypes = { MachineType.Extractor, MachineType.Smelter, MachineType.ResearchStation, MachineType.CoalMine, MachineType.CoalGenerator, MachineType.Steelworks };
		private static readonly ResourceType[] resourceTypes = { ResourceType.Ore, ResourceType.Metal, ResourceType.Research, ResourceType.Coal, ResourceType.Steel };
		private readonly Dictionary<MachineType, MachineCard> machines = new Dictionary<MachineType, MachineCard>();
		private readonly Dictionary<ResourceType, Label> resources = new Dictionary<ResourceType, Label>();
		private readonly Dictionary<int, Button> batchButtons = new Dictionary<int, Button>();
		private OutpostSession session;
		private UIDocument document;
		private PanelSettings panelSettings;
		private VisualElement shell;
		private Label status;
		private Label researchStatus;
		private Label objective;
		private Label constraints;
		private Label power;
		private Button research;
		private ProgressBar steelProgress;
		private long renderedRevision = -1;
		private int batch = 1;

		private void OnEnable()
		{
			session = GetComponent<OutpostPrototype>().Session;
			document = GetComponent<UIDocument>();
			panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
			panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("OutpostTheme");
			panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
			panelSettings.referenceResolution = new Vector2Int(1280, 800);
			panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
			panelSettings.match = 0.5f;
			document.panelSettings = panelSettings;
			Build();
		}

		private void OnDisable()
		{
			if (document != null)
			{
				document.rootVisualElement.Clear();
				document.panelSettings = null;
			}

			Destroy(panelSettings);
			panelSettings = null;
			machines.Clear();
			resources.Clear();
			batchButtons.Clear();
		}

		private void Update()
		{
			var area = Screen.safeArea;
			var width = Math.Max(1, Screen.width);
			var height = Math.Max(1, Screen.height);
			shell.style.left = Length.Percent(area.xMin / width * 100);
			shell.style.right = Length.Percent((width - area.xMax) / width * 100);
			shell.style.top = Length.Percent((height - area.yMax) / height * 100);
			shell.style.bottom = Length.Percent(area.yMin / height * 100);
			if (renderedRevision != session.Revision)
			{
				Refresh();
			}
		}

		private void Build()
		{
			var root = document.rootVisualElement;
			root.Clear();
			root.styleSheets.Clear();
			root.styleSheets.Add(Resources.Load<StyleSheet>("OutpostDashboard"));
			root.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			root.AddToClassList("dashboard");
			shell = Box(root, "shell");
			shell.RegisterCallback<GeometryChangedEvent>(change => shell.EnableInClassList("compact", change.newRect.width < 900));
			Text(shell, "ASTERFORGE  /  OUTPOST 01", "brand");
			status = Text(shell, "Build an Extractor to put manual mining out of a job.", "status");
			var scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.AddToClassList("page-scroll");
			shell.Add(scroll);
			var strip = Box(scroll, "resource-strip");
			foreach (var resource in resourceTypes)
			{
				var tile = Box(strip, "resource-tile");
				Text(tile, resource.ToString().ToUpperInvariant(), "eyebrow");
				resources[resource] = Text(tile, "", "resource-value");
				tile.tooltip = "Net rates measure the last simulation interval, not the maximum installed production.";
			}

			var workbench = Box(scroll, "workbench");
			var production = Box(workbench, "production");
			Text(production, "PRODUCTION ARRAY", "heading");
			var selector = Box(production, "batch-selector");
			foreach (var quantity in new[] { 1, 10, 100, OutpostSession.BuyMax })
			{
				var caption = $"Buy {quantity}";
				if (quantity == OutpostSession.BuyMax)
				{
					caption = "Buy Max";
				}

				var button = ActionButton(selector, caption, () => { batch = quantity; Refresh(); });
				button.AddToClassList("batch-button");
				batchButtons[quantity] = button;
			}

			Text(production, "Fixed batches buy all or nothing. Prices include every unit's increase.", "muted");
			var grid = Box(production, "machine-grid");
			foreach (var machine in machineTypes)
			{
				var card = new MachineCard { Root = Box(grid, "machine-card") };
				card.Title = Text(card.Root, "", "machine-title");
				card.State = Text(card.Root, "", "eyebrow");
				card.Flow = Text(card.Root, "", "flow");
				card.Cost = Text(card.Root, "", "muted");
				card.Hint = Text(card.Root, "", "hint");
				card.Buy = ActionButton(card.Root, "Buy", () => Buy(machine));
				machines[machine] = card;
			}

			var side = Box(workbench, "sidebar");
			var manual = Box(side, "side-card");
			Text(manual, "MANUAL EXTRACTION", "eyebrow");
			var mine = ActionButton(manual, "Mine Ore  +1", session.MineOre);
			mine.clickable = new Clickable(session.MineOre, 350, 100);
			Text(manual, "Click, tap, or hold. Your machines keep working while you plan.", "muted");
			var lab = Box(side, "side-card");
			Text(lab, "INDUSTRIALIZATION", "heading");
			researchStatus = Text(lab, "", "flow");
			research = ActionButton
			(
				lab,
				"Research Industrialization",
				() =>
				{
					if (session.TryIndustrialize())
					{
						status.text = "Industrialization complete. Coal, power, and Steelworks are available.";
						Refresh();
					}
				}
			);

			var goal = Box(side, "side-card");
			Text(goal, "NEXT MILESTONE", "eyebrow");
			objective = Text(goal, "", "heading");
			steelProgress = new ProgressBar { lowValue = 0, highValue = 1 };
			goal.Add(steelProgress);
			constraints = Text(goal, "", "flow");
			power = Text(goal, "", "muted");
			Text(goal, "Sustainable output excludes stockpiles and includes generator fuel. Net rates and purchase estimates use the last recorded interval.", "muted");
			Refresh();
		}

		private void Buy(MachineType machine)
		{
			var before = session.GetMachineCount(machine);
			if (session.TryBuyMachines(machine, batch))
			{
				status.text = $"Commissioned {session.GetMachineCount(machine) - before:N0} {Name(machine)} units.";
			}

			Refresh();
		}

		private void Refresh()
		{
			renderedRevision = session.Revision;
			foreach (var entry in resources)
			{
				entry.Value.parent.style.display = DisplayStyle.None;
				if (session.IsResourceUnlocked(entry.Key))
				{
					entry.Value.parent.style.display = DisplayStyle.Flex;
				}

				entry.Value.text = $"{session.GetResource(entry.Key):N0}\n{session.GetNetRate(entry.Key):+0.##;-0.##;0}/s net";
			}

			foreach (var entry in batchButtons)
			{
				entry.Value.EnableInClassList("selected", entry.Key == batch);
			}

			foreach (var entry in machines)
			{
				RefreshMachine(entry.Key, entry.Value);
			}

			var researchCost = session.IndustrializationCost[0];
			var balance = session.GetResource(researchCost.Resource);
			researchStatus.text = $"{balance:N0} / {researchCost.Amount:N0} {researchCost.Resource}\nUnlock Coal, power, and Steel production.";
			research.SetEnabled(!session.IsIndustrialized && balance >= researchCost.Amount);
			if (session.IsIndustrialized)
			{
				researchStatus.text = "Breakthrough complete. Heavy industry unlocked.";
			}

			var capability = session.SteelCapability;
			objective.text = "Sustain 1 Steel/s";
			if (capability.SustainableRate >= 1)
			{
				objective.text = "Outpost milestone complete";
			}

			steelProgress.value = Mathf.Clamp01((float)capability.SustainableRate);
			steelProgress.title = $"{capability.SustainableRate:0.##} / 1 Steel/s sustainable";
			constraints.text = $"Installed: {capability.InstalledRate:0.##}/s\nMetal supports: {capability.MetalSupportedRate:0.##}/s\nCoal supports: {capability.CoalSupportedRate:0.##}/s\nPower supports: {capability.PowerSupportedRate:0.##}/s\n\n{BottleneckHint(session.GetSteelBottlenecks(1))}";
			power.text = $"POWER GRID\nInstalled capacity {session.PowerCapacity:N0} / demand {session.PowerDemand:N0}\nFuel availability can further limit delivery.";
		}

		private void RefreshMachine(MachineType machine, MachineCard card)
		{
			var count = session.GetMachineCount(machine);
			var unlocked = session.IsMachineUnlocked(machine);
			var quote = session.QuotePurchase(machine, batch);
			card.Title.text = $"{Name(machine)}  x{count:N0}";
			card.State.text = "PRODUCING";
			if (!unlocked)
			{
				card.State.text = "LOCKED";
			}
			else if (count == 0)
			{
				card.State.text = "NOT BUILT";
			}
			else if (machine == MachineType.CoalGenerator)
			{
				card.State.text = "GRID CAPACITY";
				if (session.PowerDemand == 0)
				{
					card.State.text = "IDLE / NO DEMAND";
				}
			}
			else if (machine == MachineType.Steelworks && session.SteelCapability.SustainableRate < count)
			{
				card.State.text = "SUPPLY LIMITED";
				if (session.GetNetRate(ResourceType.Steel) > session.SteelCapability.SustainableRate)
				{
					card.State.text = "STOCKPILE ASSISTED";
				}
			}

			card.Root.EnableInClassList("constrained", machine == MachineType.Steelworks && count > 0 && session.SteelCapability.SustainableRate < count);
			card.Flow.text = Flow(machine, count);
			var costPrefix = "Batch cost";
			if (batch == OutpostSession.BuyMax && quote.Quantity == 0)
			{
				costPrefix = "Next unit";
			}

			card.Cost.text = $"{costPrefix}: {FormatCosts(quote.Costs)}";
			card.Hint.text = PurchaseHint(machine, quote);
			card.Buy.text = $"Buy {quote.Quantity:N0}";
			if (!unlocked)
			{
				card.Buy.text = "Locked";
			}

			card.Buy.SetEnabled(quote.CanPurchase);
		}

		private string Flow(MachineType machine, int count)
		{
			return machine switch
			{
				MachineType.Extractor => $"Produces {count:N0} Ore/s. No input cost.",
				MachineType.Smelter => $"Produces {count:N0} Metal/s. No input cost.",
				MachineType.ResearchStation => $"Produces {count:N0} Research/s. No input cost.",
				MachineType.CoalMine => $"Produces {count:N0} Coal/s. Fuel is shared by generators and Steelworks.",
				MachineType.CoalGenerator => "4 power per unit. Burns Coal only when serving powered work.",
				MachineType.Steelworks => $"Last interval {session.GetNetRate(ResourceType.Steel):0.##} Steel/s / sustainable {session.SteelCapability.SustainableRate:0.##}/s\n{BottleneckHint(session.GetSteelBottlenecks(Math.Max(1, count)))}",
				_ => ""
			};
		}

		private string PurchaseHint(MachineType machine, MachinePurchaseQuote quote)
		{
			if (quote.Block == PurchaseBlock.Locked)
			{
				return machine switch
				{
					MachineType.Smelter => "Build an Extractor first.",
					MachineType.ResearchStation => "Build a Smelter first.",
					MachineType.CoalGenerator when session.IsIndustrialized => "Build a Coal Mine first.",
					MachineType.Steelworks when session.IsIndustrialized => "Build a Coal Generator first.",
					_ => "Research Industrialization first."
				};
			}

			if (quote.Block == PurchaseBlock.MachineLimit || quote.Block == PurchaseBlock.CostOverflow)
			{
				return "This batch exceeds the supported machine or resource limit.";
			}

			if (quote.CanPurchase)
			{
				return "Ready to commission.";
			}

			var missing = new List<string>();
			foreach (var cost in quote.Costs)
			{
				var deficit = cost.Amount - session.GetResource(cost.Resource);
				if (deficit > 0)
				{
					missing.Add($"{deficit:N0} {cost.Resource}");
				}
			}

			var hint = $"Missing {string.Join(", ", missing)}.";
			if (quote.SecondsUntilAffordable.HasValue)
			{
				return $"{hint} About {quote.SecondsUntilAffordable.Value:N0}s at last recorded net rates.";
			}

			return $"{hint} Increase net production or mine manually; no positive net rate for at least one shortage.";
		}

		private static string BottleneckHint(SteelBottleneck bottlenecks)
		{
			var hints = new List<string>();
			if ((bottlenecks & SteelBottleneck.Steelworks) != 0)
			{
				hints.Add("Add Steelworks.");
			}

			if ((bottlenecks & SteelBottleneck.Metal) != 0)
			{
				hints.Add("Metal limited: add Smelters.");
			}

			if ((bottlenecks & SteelBottleneck.Coal) != 0)
			{
				hints.Add("Coal limited: add Coal Mines (including generator fuel).");
			}

			if ((bottlenecks & SteelBottleneck.Power) != 0)
			{
				hints.Add("Power limited: add Coal Generators.");
			}

			if (hints.Count == 0)
			{
				return "Supply and power support the target.";
			}

			return string.Join("\n", hints);
		}

		private static string FormatCosts(IReadOnlyList<ResourceCost> costs)
		{
			var text = new StringBuilder();
			foreach (var cost in costs)
			{
				if (text.Length > 0)
				{
					text.Append(" + ");
				}

				text.Append($"{cost.Amount:N0} {cost.Resource}");
			}

			return text.ToString();
		}

		private static string Name(MachineType machine) => machine switch
		{
			MachineType.ResearchStation => "Research Station",
			MachineType.CoalMine => "Coal Mine",
			MachineType.CoalGenerator => "Coal Generator",
			_ => machine.ToString()
		};

		private static VisualElement Box(VisualElement parent, string className)
		{
			var element = new VisualElement();
			element.AddToClassList(className);
			parent.Add(element);

			return element;
		}

		private static Label Text(VisualElement parent, string text, string className)
		{
			var label = new Label(text);
			label.AddToClassList(className);
			parent.Add(label);

			return label;
		}

		private static Button ActionButton(VisualElement parent, string text, Action action)
		{
			var button = new Button(action) { text = text };
			button.AddToClassList("action");
			parent.Add(button);

			return button;
		}

		private sealed class MachineCard
		{
			public VisualElement Root;
			public Label Title;
			public Label State;
			public Label Flow;
			public Label Cost;
			public Label Hint;
			public Button Buy;
		}
	}
}
