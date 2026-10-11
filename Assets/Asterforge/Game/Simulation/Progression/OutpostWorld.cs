using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Progression
{
	public sealed class OutpostWorld
	{
		private readonly ProductionSimulator productionSimulator = new ProductionSimulator(OutpostRecipes.All);

		public ResourceLedger Resources { get; } = new ResourceLedger();

		public MachineFleet Machines { get; } = new MachineFleet();

		public bool IsIndustrialized { get; private set; }

		public IReadOnlyList<ResourceCost> IndustrializationCost => OutpostRules.IndustrializationCost;

		public void MineOre()
		{
			Resources.Add(ResourceType.Ore, 1);
		}

		public IReadOnlyList<ResourceCost> GetMachineCost(MachineType machine)
		{
			return OutpostRules.GetMachineCost(machine, Machines.Get(machine));
		}

		public bool IsMachineUnlocked(MachineType machine)
		{
			return OutpostRules.IsMachineUnlocked(machine, Machines, IsIndustrialized);
		}

		public bool TryBuyMachine(MachineType machine)
		{
			if (!IsMachineUnlocked(machine))
			{
				return false;
			}

			var costs = GetMachineCost(machine);
			if (!CanAfford(costs))
			{
				return false;
			}

			Spend(costs);
			Machines.Add(machine);

			return true;
		}

		public bool TryIndustrialize()
		{
			if (IsIndustrialized)
			{
				return false;
			}

			if (!CanAfford(IndustrializationCost))
			{
				return false;
			}

			Spend(IndustrializationCost);
			IsIndustrialized = true;

			return true;
		}

		public void Advance(long elapsedSeconds)
		{
			productionSimulator.Advance(Resources, Machines, elapsedSeconds);
		}

		private bool CanAfford(IReadOnlyList<ResourceCost> costs)
		{
			foreach (var cost in costs)
			{
				if (Resources.Get(cost.Resource) < cost.Amount)
				{
					return false;
				}
			}

			return true;
		}

		private void Spend(IReadOnlyList<ResourceCost> costs)
		{
			foreach (var cost in costs)
			{
				var spent = Resources.TrySpend(cost.Resource, cost.Amount);
				if (!spent)
				{
					throw new InvalidOperationException("Resource balances changed while applying an Outpost action.");
				}
			}
		}
	}
}
