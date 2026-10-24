using System;
using System.Collections.Generic;
using Asterforge.Game.Persistence;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Progression
{
	[Flags]
	public enum SteelBottleneck
	{
		None = 0,
		Steelworks = 1,
		Power = 2,
		Metal = 4,
		Coal = 8
	}

	public enum PurchaseBlock
	{
		None,
		Locked,
		Materials,
		MachineLimit,
		CostOverflow
	}

	public sealed class MachinePurchaseQuote
	{
		public int Quantity { get; }
		public IReadOnlyList<ResourceCost> Costs { get; }
		public PurchaseBlock Block { get; }
		public double? SecondsUntilAffordable { get; }
		public bool CanPurchase => Block == PurchaseBlock.None && Quantity > 0;

		internal MachinePurchaseQuote(int quantity, IReadOnlyList<ResourceCost> costs, PurchaseBlock block, double? secondsUntilAffordable = null)
		{
			Quantity = quantity;
			Costs = costs;
			Block = block;
			SecondsUntilAffordable = secondsUntilAffordable;
		}
	}

	// The Unity presentation can query values and issue commands, but cannot mutate the live world or its ledgers.
	public sealed class OutpostSession
	{
		public const int BuyMax = 0;
		private OutpostWorld world = new OutpostWorld();

		public long Revision { get; private set; }
		public bool IsIndustrialized => world.IsIndustrialized;
		public long PowerCapacity => world.PowerCapacity;
		public long PowerDemand => world.PowerDemand;
		public SteelCapability SteelCapability => world.GetSteelCapability();
		public IReadOnlyList<ResourceCost> IndustrializationCost => world.IndustrializationCost;

		public long GetResource(ResourceType resource) => world.Resources.Get(resource);
		public double GetNetRate(ResourceType resource) => world.GetLastNetRate(resource);
		public double GetProductionRate(ResourceType resource) => world.GetNominalProductionRate(resource);
		public int GetMachineCount(MachineType machine) => world.Machines.Get(machine);
		public bool IsMachineUnlocked(MachineType machine) => world.IsMachineUnlocked(machine);
		public bool IsResourceUnlocked(ResourceType resource) => world.IsResourceUnlocked(resource);

		public SteelBottleneck GetSteelBottlenecks(double targetRate)
		{
			if (targetRate <= 0 || double.IsNaN(targetRate) || double.IsInfinity(targetRate))
			{
				throw new ArgumentOutOfRangeException(nameof(targetRate));
			}

			var capability = SteelCapability;
			var result = SteelBottleneck.None;
			if (capability.InstalledRate < targetRate)
			{
				result |= SteelBottleneck.Steelworks;
			}

			if (capability.PowerSupportedRate < targetRate)
			{
				result |= SteelBottleneck.Power;
			}

			if (capability.MetalSupportedRate < targetRate)
			{
				result |= SteelBottleneck.Metal;
			}

			if (capability.CoalSupportedRate < targetRate)
			{
				result |= SteelBottleneck.Coal;
			}

			return result;
		}

		// Zero requests the largest currently affordable batch; fixed positive quantities are all-or-nothing.
		public MachinePurchaseQuote QuotePurchase(MachineType machine, int quantity)
		{
			if (quantity < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(quantity));
			}

			if (!Enum.IsDefined(typeof(MachineType), machine))
			{
				throw new ArgumentOutOfRangeException(nameof(machine));
			}

			var owned = GetMachineCount(machine);
			if (owned == int.MaxValue || quantity > int.MaxValue - owned)
			{
				return new MachinePurchaseQuote(0, Array.Empty<ResourceCost>(), PurchaseBlock.MachineLimit);
			}

			var unlocked = IsMachineUnlocked(machine);
			var count = quantity;
			if (quantity == BuyMax && unlocked)
			{
				count = GetMaximumAffordable(machine, owned);
			}

			try
			{
				var costs = OutpostRules.GetMachineCost(machine, owned, Math.Max(1, count));
				if (!unlocked)
				{
					return new MachinePurchaseQuote(count, costs, PurchaseBlock.Locked);
				}

				if (count == 0 || !CanAfford(costs))
				{
					return new MachinePurchaseQuote(count, costs, PurchaseBlock.Materials, EstimateWait(costs));
				}

				return new MachinePurchaseQuote(count, costs, PurchaseBlock.None, 0);
			}
			catch (OverflowException)
			{
				return new MachinePurchaseQuote(0, Array.Empty<ResourceCost>(), PurchaseBlock.CostOverflow);
			}
		}

		public bool TryBuyMachines(MachineType machine, int quantity)
		{
			// Re-quote at execution time so a displayed quote is never an authorization to spend stale balances.
			var quote = QuotePurchase(machine, quantity);
			if (!quote.CanPurchase)
			{
				return false;
			}

			foreach (var cost in quote.Costs)
			{
				if (!world.Resources.TrySpend(cost.Resource, cost.Amount))
				{
					throw new InvalidOperationException("Resource balances changed while applying a batch purchase.");
				}
			}

			world.Machines.Add(machine, quote.Quantity);
			Revision++;

			return true;
		}

		public void MineOre()
		{
			if (GetResource(ResourceType.Ore) == long.MaxValue)
			{
				return;
			}

			world.MineOre();
			Revision++;
		}

		public bool TryIndustrialize()
		{
			if (!world.TryIndustrialize())
			{
				return false;
			}

			Revision++;

			return true;
		}

		public void Advance(long seconds)
		{
			if (seconds < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(seconds));
			}

			if (seconds == 0)
			{
				return;
			}

			world.Advance(seconds);
			Revision++;
		}

		public OutpostSnapshot CaptureSnapshot(long savedAtUnixSeconds) => world.CaptureSnapshot(savedAtUnixSeconds);

		public long Restore(OutpostSnapshot snapshot, long nowUnixSeconds)
		{
			var result = OutpostWorld.RestoreWithOfflineProgress(snapshot, nowUnixSeconds);
			world = result.World;
			Revision++;

			return result.OfflineSeconds;
		}

		private int GetMaximumAffordable(MachineType machine, int owned)
		{
			var low = 0;
			var high = int.MaxValue - owned;
			while (low < high)
			{
				var middle = low + (int)(((long)high - low + 1) / 2);
				var affordable = false;
				try
				{
					affordable = CanAfford(OutpostRules.GetMachineCost(machine, owned, middle));
				}
				catch (OverflowException)
				{
					// A cost beyond the ledger's range cannot be affordable.
				}

				if (affordable)
				{
					low = middle;
				}
				else
				{
					high = middle - 1;
				}
			}

			return low;
		}

		private bool CanAfford(IReadOnlyList<ResourceCost> costs)
		{
			foreach (var cost in costs)
			{
				if (GetResource(cost.Resource) < cost.Amount)
				{
					return false;
				}
			}

			return true;
		}

		private double? EstimateWait(IReadOnlyList<ResourceCost> costs)
		{
			var seconds = 0d;
			foreach (var cost in costs)
			{
				var deficit = cost.Amount - GetResource(cost.Resource);
				if (deficit <= 0)
				{
					continue;
				}

				var rate = GetNetRate(cost.Resource);
				if (rate <= 0)
				{
					return null;
				}

				seconds = Math.Max(seconds, deficit / rate);
			}

			return Math.Ceiling(seconds);
		}
	}
}
