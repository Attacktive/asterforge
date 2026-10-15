using System;
using System.Collections.Generic;
using System.Numerics;
using Asterforge.Game.Persistence;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Progression
{
	public readonly struct SteelCapability
	{
		public double InstalledRate { get; }

		public double PowerSupportedRate { get; }

		public double MetalSupportedRate { get; }

		public double CoalSupportedRate { get; }

		public double SustainableRate => Math.Min(
			Math.Min(InstalledRate, PowerSupportedRate),
			Math.Min(MetalSupportedRate, CoalSupportedRate)
		);

		public SteelCapability(double installedRate, double powerSupportedRate, double metalSupportedRate, double coalSupportedRate)
		{
			InstalledRate = installedRate;
			PowerSupportedRate = powerSupportedRate;
			MetalSupportedRate = metalSupportedRate;
			CoalSupportedRate = coalSupportedRate;
		}
	}

	public sealed class OutpostWorld
	{
		private static readonly PowerGrid powerGrid = new PowerGrid(
			MachineType.CoalGenerator,
			4,
			new ResourceQuantity(ResourceType.Coal, 1)
		);

		private readonly ProductionSimulator productionSimulator = new ProductionSimulator(OutpostRecipes.All, powerGrid);
		private readonly long[] lastResourceDeltas = new long[Enum.GetValues(typeof(ResourceType)).Length];
		private long lastAdvanceSeconds;

		public ResourceLedger Resources { get; } = new ResourceLedger();

		public MachineFleet Machines { get; } = new MachineFleet();

		public bool IsIndustrialized { get; private set; }

		public IReadOnlyList<ResourceCost> IndustrializationCost => OutpostRules.IndustrializationCost;

		public PowerStatus LastPowerStatus { get; private set; }

		public long PowerCapacity => powerGrid.GetCapacity(Machines);

		public long PowerDemand => powerGrid.GetDemand(Machines, OutpostRecipes.All);

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

		public bool IsResourceUnlocked(ResourceType resource)
		{
			return OutpostRules.IsResourceUnlocked(resource, IsIndustrialized);
		}

		public double GetLastNetRate(ResourceType resource)
		{
			if (lastAdvanceSeconds <= 0)
			{
				return 0d;
			}

			return lastResourceDeltas[(int)resource] / (double)lastAdvanceSeconds;
		}

		public double GetNominalProductionRate(ResourceType resource)
		{
			var rate = 0d;
			foreach (var recipe in OutpostRecipes.All)
			{
				if (recipe.PowerDemand != 0 || recipe.Inputs.Count != 0 || recipe.Output.Resource != resource)
				{
					continue;
				}

				rate += Machines.Get(recipe.Machine) * (double)recipe.Output.Amount;
			}

			return rate;
		}

		public SteelCapability GetSteelCapability()
		{
			var steelRecipe = GetRecipe(MachineType.Steelworks);
			var outputPerRun = steelRecipe.Output.Amount;
			var installedRate = Machines.Get(MachineType.Steelworks) * (double)outputPerRun;
			var powerSupportedRate = PowerCapacity / (double)steelRecipe.PowerDemand * outputPerRun;
			var metalPerRun = GetInputAmount(steelRecipe, ResourceType.Metal);
			var coalPerRun = GetInputAmount(steelRecipe, ResourceType.Coal);
			if (powerGrid.FuelPerGeneratorSecond.Resource == ResourceType.Coal)
			{
				coalPerRun += steelRecipe.PowerDemand * (double)powerGrid.FuelPerGeneratorSecond.Amount / powerGrid.CapacityPerGenerator;
			}

			var metalSupportedRate = GetNominalProductionRate(ResourceType.Metal) / metalPerRun * outputPerRun;
			var coalSupportedRate = GetNominalProductionRate(ResourceType.Coal) / coalPerRun * outputPerRun;

			return new SteelCapability(
				installedRate,
				powerSupportedRate,
				metalSupportedRate,
				coalSupportedRate
			);
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
			var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
			var balancesBefore = new long[resourceTypes.Length];
			foreach (var resource in resourceTypes)
			{
				balancesBefore[(int)resource] = Resources.Get(resource);
			}

			LastPowerStatus = productionSimulator.Advance(Resources, Machines, elapsedSeconds);
			lastAdvanceSeconds = elapsedSeconds;
			foreach (var resource in resourceTypes)
			{
				lastResourceDeltas[(int)resource] = Resources.Get(resource) - balancesBefore[(int)resource];
			}
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
				PoweredRunRemainders = productionSimulator.CapturePoweredRunRemainders(),
				PoweredAllocationWeights = productionSimulator.CapturePoweredAllocationWeights(),
				PoweredAllocationPhase = productionSimulator.CapturePoweredAllocationPhase(),
				PowerFuelCreditUnits = productionSimulator.CaptureFuelCreditUnits(),
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
			foreach (var machine in machineTypes)
			{
				world.Machines.Set(machine, snapshot.MachineCounts[(int)machine]);
			}

			world.productionSimulator.RestorePoweredState(
				snapshot.PoweredRunRemainders,
				snapshot.PoweredAllocationWeights,
				snapshot.PoweredAllocationPhase,
				snapshot.PowerFuelCreditUnits
			);
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

			if (snapshot.Version != OutpostSnapshot.CurrentVersion)
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
			if (snapshot.MachineCounts == null || snapshot.MachineCounts.Length != machineCount)
			{
				throw new InvalidOperationException("Outpost snapshot machine data does not match this game version.");
			}

			if (snapshot.PoweredRunRemainders == null || snapshot.PoweredRunRemainders.Length != machineCount)
			{
				throw new InvalidOperationException("Outpost snapshot powered production data does not match this game version.");
			}

			if (snapshot.PoweredAllocationWeights == null || snapshot.PoweredAllocationWeights.Length != machineCount)
			{
				throw new InvalidOperationException("Outpost snapshot powered allocation data does not match this game version.");
			}

			var totalAllocationWeight = BigInteger.Zero;
			var machineTypes = (MachineType[])Enum.GetValues(typeof(MachineType));
			foreach (var machine in machineTypes)
			{
				var remainder = snapshot.PoweredRunRemainders[(int)machine];
				var limit = GetPoweredRunRemainderLimit(machine);
				if (remainder < 0 || remainder >= limit)
				{
					throw new InvalidOperationException("Outpost snapshot contains invalid powered production progress.");
				}

				var allocationWeight = snapshot.PoweredAllocationWeights[(int)machine];
				if (allocationWeight < 0)
				{
					throw new InvalidOperationException("Outpost snapshot contains a negative powered allocation weight.");
				}

				var powerDemand = GetPoweredPowerDemand(machine);
				if (powerDemand == 0)
				{
					if (allocationWeight != 0)
					{
						throw new InvalidOperationException("Outpost snapshot assigns powered allocation weight to an unpowered machine.");
					}
				}
				else if (allocationWeight % powerDemand != 0)
				{
					throw new InvalidOperationException("Outpost snapshot powered allocation weight does not align with machine demand.");
				}

				totalAllocationWeight += allocationWeight;
			}

			if (snapshot.PoweredAllocationPhase < 0)
			{
				throw new InvalidOperationException("Outpost snapshot contains a negative powered allocation phase.");
			}

			if (totalAllocationWeight == 0)
			{
				if (snapshot.PoweredAllocationPhase != 0)
				{
					throw new InvalidOperationException("Outpost snapshot powered allocation phase must be zero without allocation weight.");
				}
			}
			else if (snapshot.PoweredAllocationPhase >= totalAllocationWeight)
			{
				throw new InvalidOperationException("Outpost snapshot powered allocation phase is outside its allocation cycle.");
			}

			if (snapshot.PowerFuelCreditUnits < 0 || snapshot.PowerFuelCreditUnits >= powerGrid.CapacityPerGenerator)
			{
				throw new InvalidOperationException("Outpost snapshot contains invalid generator fuel credit.");
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

		private static int GetPoweredRunRemainderLimit(MachineType machine)
		{
			var powerDemand = GetPoweredPowerDemand(machine);
			if (powerDemand > 0)
			{
				return powerDemand;
			}

			return 1;
		}

		private static int GetPoweredPowerDemand(MachineType machine)
		{
			foreach (var recipe in OutpostRecipes.All)
			{
				if (recipe.Machine == machine && recipe.PowerDemand > 0)
				{
					return recipe.PowerDemand;
				}
			}

			return 0;
		}

		private static ProductionRecipe GetRecipe(MachineType machine)
		{
			foreach (var recipe in OutpostRecipes.All)
			{
				if (recipe.Machine == machine)
				{
					return recipe;
				}
			}

			throw new InvalidOperationException($"No production recipe exists for {machine}.");
		}

		private static double GetInputAmount(ProductionRecipe recipe, ResourceType resource)
		{
			foreach (var input in recipe.Inputs)
			{
				if (input.Resource == resource)
				{
					return input.Amount;
				}
			}

			return 0d;
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
