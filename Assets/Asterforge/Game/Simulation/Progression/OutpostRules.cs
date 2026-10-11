using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Progression
{
	public static class OutpostRules
	{
		private static readonly ResourceCost[] extractorBaseCost =
		{
			new ResourceCost(ResourceType.Ore, 10)
		};

		private static readonly ResourceCost[] smelterBaseCost =
		{
			new ResourceCost(ResourceType.Ore, 25)
		};

		private static readonly ResourceCost[] assemblerBaseCost =
		{
			new ResourceCost(ResourceType.Metal, 20)
		};

		private static readonly ResourceCost[] researchStationBaseCost =
		{
			new ResourceCost(ResourceType.Components, 10)
		};

		private static readonly ResourceCost[] coalMineBaseCost =
		{
			new ResourceCost(ResourceType.Metal, 100),
			new ResourceCost(ResourceType.Components, 25)
		};

		private static readonly ResourceCost[] steelworksBaseCost =
		{
			new ResourceCost(ResourceType.Metal, 200),
			new ResourceCost(ResourceType.Components, 50)
		};

		private static readonly IReadOnlyList<ResourceCost> industrializationCost = Array.AsReadOnly(
			new[]
			{
				new ResourceCost(ResourceType.Research, 100)
			}
		);

		public static IReadOnlyList<ResourceCost> IndustrializationCost => industrializationCost;

		public static IReadOnlyList<ResourceCost> GetMachineCost(MachineType machine, int ownedCount)
		{
			if (ownedCount < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(ownedCount), ownedCount, "Owned machine count cannot be negative.");
			}

			var baseCost = machine switch
			{
				MachineType.Extractor => extractorBaseCost,
				MachineType.Smelter => smelterBaseCost,
				MachineType.Assembler => assemblerBaseCost,
				MachineType.ResearchStation => researchStationBaseCost,
				MachineType.CoalMine => coalMineBaseCost,
				MachineType.Steelworks => steelworksBaseCost,
				_ => throw new ArgumentOutOfRangeException(nameof(machine), machine, "Unknown machine type.")
			};

			return ScaleCosts(baseCost, ownedCount);
		}

		public static bool IsMachineUnlocked(MachineType machine, MachineFleet machines, bool isIndustrialized)
		{
			if (machines == null)
			{
				throw new ArgumentNullException(nameof(machines));
			}

			return machine switch
			{
				MachineType.Extractor => true,
				MachineType.Smelter => machines.Get(MachineType.Extractor) > 0,
				MachineType.Assembler => machines.Get(MachineType.Smelter) > 0,
				MachineType.ResearchStation => machines.Get(MachineType.Assembler) > 0,
				MachineType.CoalMine => isIndustrialized,
				MachineType.Steelworks => isIndustrialized,
				_ => false
			};
		}

		private static IReadOnlyList<ResourceCost> ScaleCosts(ResourceCost[] baseCosts, int ownedCount)
		{
			var scaledCosts = new ResourceCost[baseCosts.Length];
			for (var index = 0; index < baseCosts.Length; index++)
			{
				var baseCost = baseCosts[index];
				var numerator = checked(baseCost.Amount * (ownedCount + 2L));
				var amount = checked((numerator + 1L) / 2L);
				scaledCosts[index] = new ResourceCost(baseCost.Resource, amount);
			}

			return Array.AsReadOnly(scaledCosts);
		}
	}
}
