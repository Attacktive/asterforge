using System;
using System.Collections.Generic;
using Asterforge.Game.Persistence;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Progression
{
	public sealed class OutpostWorld
	{
		private static readonly PowerGrid powerGrid = new PowerGrid(
			MachineType.CoalGenerator,
			4,
			new ResourceQuantity(ResourceType.Coal, 1)
		);

		private readonly ProductionSimulator productionSimulator = new ProductionSimulator(OutpostRecipes.All, powerGrid);

		public ResourceLedger Resources { get; } = new ResourceLedger();

		public MachineFleet Machines { get; } = new MachineFleet();

		public bool IsIndustrialized { get; private set; }

		public PowerStatus LastPowerStatus { get; private set; }

		public long PowerCapacity => powerGrid.GetCapacity(Machines);

		public long PowerDemand => powerGrid.GetDemand(Machines, OutpostRecipes.All);

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
			LastPowerStatus = productionSimulator.Advance(Resources, Machines, elapsedSeconds);
		}

		public OutpostSnapshot CaptureSnapshot(long savedAtUnixSeconds)
		{
			if (savedAtUnixSeconds < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(savedAtUnixSeconds), savedAtUnixSeconds, "Save timestamps cannot be negative.");
			}

			var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
			var resourceAmounts = new long[resourceTypes.Length];
			foreach (var resource in resourceTypes)
			{
				resourceAmounts[(int)resource] = Resources.Get(resource);
			}

			var machineTypes = (MachineType[])Enum.GetValues(typeof(MachineType));
			var machineCounts = new int[machineTypes.Length];
			foreach (var machine in machineTypes)
			{
				machineCounts[(int)machine] = Machines.Get(machine);
			}

			return new OutpostSnapshot
			{
				SavedAtUnixSeconds = savedAtUnixSeconds,
				ResourceAmounts = resourceAmounts,
				MachineCounts = machineCounts,
				IsIndustrialized = IsIndustrialized
			};
		}

		public static OutpostWorld Restore(OutpostSnapshot snapshot)
		{
			ValidateSnapshot(snapshot);

			var world = new OutpostWorld();
			var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
			foreach (var resource in resourceTypes)
			{
				world.Resources.Set(resource, snapshot.ResourceAmounts[(int)resource]);
			}

			var machineTypes = (MachineType[])Enum.GetValues(typeof(MachineType));
			for (var index = 0; index < snapshot.MachineCounts.Length; index++)
			{
				world.Machines.Set(machineTypes[index], snapshot.MachineCounts[index]);
			}

			world.IsIndustrialized = snapshot.IsIndustrialized;

			return world;
		}

		public static OutpostLoadResult RestoreWithOfflineProgress(OutpostSnapshot snapshot, long nowUnixSeconds)
		{
			if (nowUnixSeconds < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(nowUnixSeconds), nowUnixSeconds, "Current timestamps cannot be negative.");
			}

			var world = Restore(snapshot);
			var offlineSeconds = 0L;
			if (nowUnixSeconds > snapshot.SavedAtUnixSeconds)
			{
				offlineSeconds = checked(nowUnixSeconds - snapshot.SavedAtUnixSeconds);
				world.Advance(offlineSeconds);
			}

			return new OutpostLoadResult(world, offlineSeconds);
		}

		private static void ValidateSnapshot(OutpostSnapshot snapshot)
		{
			if (snapshot == null)
			{
				throw new ArgumentNullException(nameof(snapshot));
			}

			if (snapshot.Version != OutpostSnapshot.VersionWithoutPower && snapshot.Version != OutpostSnapshot.CurrentVersion)
			{
				throw new InvalidOperationException($"Unsupported Outpost snapshot version {snapshot.Version}.");
			}

			if (snapshot.SavedAtUnixSeconds < 0)
			{
				throw new InvalidOperationException("Outpost snapshot timestamp cannot be negative.");
			}

			var resourceCount = Enum.GetValues(typeof(ResourceType)).Length;
			if (snapshot.ResourceAmounts == null || snapshot.ResourceAmounts.Length != resourceCount)
			{
				throw new InvalidOperationException("Outpost snapshot resource data does not match this game version.");
			}

			var machineCount = Enum.GetValues(typeof(MachineType)).Length;
			var expectedMachineCount = snapshot.Version == OutpostSnapshot.VersionWithoutPower ? machineCount - 1 : machineCount;
			if (snapshot.MachineCounts == null || snapshot.MachineCounts.Length != expectedMachineCount)
			{
				throw new InvalidOperationException("Outpost snapshot machine data does not match this game version.");
			}

			foreach (var amount in snapshot.ResourceAmounts)
			{
				if (amount < 0)
				{
					throw new InvalidOperationException("Outpost snapshot contains a negative resource balance.");
				}
			}

			foreach (var count in snapshot.MachineCounts)
			{
				if (count < 0)
				{
					throw new InvalidOperationException("Outpost snapshot contains a negative machine count.");
				}
			}
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
