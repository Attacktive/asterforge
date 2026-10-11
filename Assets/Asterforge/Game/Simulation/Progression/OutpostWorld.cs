using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Progression
{
	public sealed class OutpostWorld
	{
		private readonly ProductionSimulator productionSimulator = new ProductionSimulator(OutpostRecipes.All);
		private readonly long[] industrializationContributions = new long[Enum.GetValues(typeof(ResourceType)).Length];

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

		public bool IsMachinePaused(MachineType machine)
		{
			return Machines.IsPaused(machine);
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

		public bool SetMachinePaused(MachineType machine, bool isPaused)
		{
			if (Machines.Get(machine) == 0)
			{
				return false;
			}

			Machines.SetPaused(machine, isPaused);

			return true;
		}

		public long GetIndustrializationContribution(ResourceType resource)
		{
			return industrializationContributions[(int)resource];
		}

		public long GetIndustrializationRequirement(ResourceType resource)
		{
			foreach (var cost in IndustrializationCost)
			{
				if (cost.Resource == resource)
				{
					return cost.Amount;
				}
			}

			return 0;
		}

		public long ContributeToIndustrialization(ResourceType resource)
		{
			if (IsIndustrialized)
			{
				return 0;
			}

			var required = GetIndustrializationRequirement(resource);
			if (required == 0)
			{
				return 0;
			}

			var contributed = GetIndustrializationContribution(resource);
			var remaining = required - contributed;
			var amount = Math.Min(Resources.Get(resource), remaining);
			if (amount == 0)
			{
				return 0;
			}

			var spent = Resources.TrySpend(resource, amount);
			if (!spent)
			{
				throw new InvalidOperationException("Resource balances changed while funding Industrialization.");
			}

			industrializationContributions[(int)resource] = checked(contributed + amount);
			CompleteIndustrializationIfFunded();

			return amount;
		}

		public long ContributeAvailableToIndustrialization()
		{
			var total = 0L;
			foreach (var cost in IndustrializationCost)
			{
				total = checked(total + ContributeToIndustrialization(cost.Resource));
			}

			return total;
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

		private void CompleteIndustrializationIfFunded()
		{
			foreach (var cost in IndustrializationCost)
			{
				if (GetIndustrializationContribution(cost.Resource) < cost.Amount)
				{
					return;
				}
			}

			IsIndustrialized = true;
		}
	}
}
