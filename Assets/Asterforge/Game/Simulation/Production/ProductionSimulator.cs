using System;
using System.Collections.Generic;
using System.Numerics;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Production
{
	public sealed class ProductionSimulator
	{
		private readonly IReadOnlyList<ProductionRecipe> recipes;
		private readonly PowerGrid powerGrid;
		private readonly long[] poweredRunRemainders = new long[Enum.GetValues(typeof(MachineType)).Length];
		private readonly long[] poweredAllocationWeights = new long[Enum.GetValues(typeof(MachineType)).Length];
		private long poweredAllocationPhase;
		private long fuelCreditUnits;

		public ProductionSimulator(IReadOnlyList<ProductionRecipe> recipes)
			: this(recipes, null)
		{
		}

		public ProductionSimulator(IReadOnlyList<ProductionRecipe> recipes, PowerGrid powerGrid)
		{
			this.recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
			this.powerGrid = powerGrid;
		}

		public long[] CapturePoweredRunRemainders()
		{
			return (long[])poweredRunRemainders.Clone();
		}

		public long[] CapturePoweredAllocationWeights()
		{
			return (long[])poweredAllocationWeights.Clone();
		}

		public long CapturePoweredAllocationPhase()
		{
			return poweredAllocationPhase;
		}

		public long CaptureFuelCreditUnits()
		{
			return fuelCreditUnits;
		}

		public void RestorePoweredState(long[] runRemainders, long[] allocationWeights, long allocationPhase, long restoredFuelCreditUnits)
		{
			if (runRemainders == null)
			{
				throw new ArgumentNullException(nameof(runRemainders));
			}

			if (allocationWeights == null)
			{
				throw new ArgumentNullException(nameof(allocationWeights));
			}

			if (runRemainders.Length != poweredRunRemainders.Length)
			{
				throw new ArgumentException("Powered production remainder data does not match this game version.", nameof(runRemainders));
			}

			if (allocationWeights.Length != poweredAllocationWeights.Length)
			{
				throw new ArgumentException("Powered allocation weight data does not match this game version.", nameof(allocationWeights));
			}

			var totalAllocationWeight = BigInteger.Zero;
			var machineTypes = (MachineType[])Enum.GetValues(typeof(MachineType));
			foreach (var machine in machineTypes)
			{
				var remainder = runRemainders[(int)machine];
				var limit = GetRunRemainderLimit(machine);
				if (remainder < 0 || remainder >= limit)
				{
					throw new ArgumentOutOfRangeException(nameof(runRemainders), remainder, "Powered production remainder is outside its machine's work boundary.");
				}

				var allocationWeight = allocationWeights[(int)machine];
				if (allocationWeight < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(allocationWeights), allocationWeight, "Powered allocation weight cannot be negative.");
				}

				var powerDemand = GetPowerDemand(machine);
				if (powerDemand == 0)
				{
					if (allocationWeight != 0)
					{
						throw new ArgumentOutOfRangeException(nameof(allocationWeights), allocationWeight, "A machine without powered production cannot have allocation weight.");
					}
				}
				else if (allocationWeight % powerDemand != 0)
				{
					throw new ArgumentOutOfRangeException(nameof(allocationWeights), allocationWeight, "Powered allocation weight does not align with its machine's power demand.");
				}

				totalAllocationWeight += allocationWeight;
			}

			if (allocationPhase < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(allocationPhase), allocationPhase, "Powered allocation phase cannot be negative.");
			}

			if (totalAllocationWeight == 0)
			{
				if (allocationPhase != 0)
				{
					throw new ArgumentOutOfRangeException(nameof(allocationPhase), allocationPhase, "Powered allocation phase must be zero without allocation weight.");
				}
			}
			else if (allocationPhase >= totalAllocationWeight)
			{
				throw new ArgumentOutOfRangeException(nameof(allocationPhase), allocationPhase, "Powered allocation phase must stay inside its allocation cycle.");
			}

			if (restoredFuelCreditUnits < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(restoredFuelCreditUnits), restoredFuelCreditUnits, "Fuel credit cannot be negative.");
			}

			if (powerGrid == null)
			{
				if (totalAllocationWeight != 0 || allocationPhase != 0)
				{
					throw new ArgumentOutOfRangeException(nameof(allocationWeights), allocationWeights, "A simulator without power cannot restore powered allocation state.");
				}

				if (restoredFuelCreditUnits != 0)
				{
					throw new ArgumentOutOfRangeException(nameof(restoredFuelCreditUnits), restoredFuelCreditUnits, "A simulator without power cannot restore fuel credit.");
				}
			}
			else if (restoredFuelCreditUnits >= powerGrid.CapacityPerGenerator)
			{
				throw new ArgumentOutOfRangeException(nameof(restoredFuelCreditUnits), restoredFuelCreditUnits, "Fuel credit must be smaller than one generator-second of capacity.");
			}

			Array.Copy(runRemainders, poweredRunRemainders, poweredRunRemainders.Length);
			Array.Copy(allocationWeights, poweredAllocationWeights, poweredAllocationWeights.Length);
			poweredAllocationPhase = allocationPhase;
			fuelCreditUnits = restoredFuelCreditUnits;
		}

		public PowerStatus Advance(ResourceLedger resources, MachineFleet machines, long elapsedSeconds)
		{
			if (resources == null)
			{
				throw new ArgumentNullException(nameof(resources));
			}

			if (machines == null)
			{
				throw new ArgumentNullException(nameof(machines));
			}

			if (elapsedSeconds < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), elapsedSeconds, "Elapsed time cannot be negative.");
			}

			if (powerGrid == null)
			{
				foreach (var recipe in recipes)
				{
					ProcessRecipe(resources, machines, recipe, elapsedSeconds);
				}

				return default;
			}

			var capacity = powerGrid.GetCapacity(machines);
			var demand = powerGrid.GetDemand(machines, recipes);
			var demandedCapacitySeconds = ToLongChecked(
				(BigInteger)demand * elapsedSeconds,
				"Power demand for this simulation interval exceeds the supported range."
			);

			foreach (var recipe in recipes)
			{
				if (recipe.PowerDemand == 0)
				{
					ProcessRecipe(resources, machines, recipe, elapsedSeconds);
				}
			}

			if (elapsedSeconds == 0 || demand == 0 || capacity == 0)
			{
				return new PowerStatus(capacity, demand, 0, demandedCapacitySeconds, 0);
			}

			var maximumSuppliedCapacitySeconds = ToLongChecked(
				BigInteger.Min(demandedCapacitySeconds, (BigInteger)capacity * elapsedSeconds),
				"Supplied power for this simulation interval exceeds the supported range."
			);
			var plan = FindBestPowerPlan(resources, machines, elapsedSeconds, maximumSuppliedCapacitySeconds);

			ApplyPowerPlan(resources, plan);

			return new PowerStatus(capacity, demand, plan.SuppliedCapacitySeconds, demandedCapacitySeconds, plan.FuelConsumed);
		}

		private PowerPlan FindBestPowerPlan(ResourceLedger resources, MachineFleet machines, long elapsedSeconds, long maximumSuppliedCapacitySeconds)
		{
			var low = 0L;
			var high = maximumSuppliedCapacitySeconds;
			while (low < high)
			{
				var candidate = low + (high - low + 1L) / 2L;
				if (BuildPowerPlan(resources, machines, elapsedSeconds, candidate) != null)
				{
					low = candidate;
				}
				else
				{
					high = candidate - 1L;
				}
			}

			var plan = BuildPowerPlan(resources, machines, elapsedSeconds, low);
			if (plan == null)
			{
				throw new InvalidOperationException("Unable to construct a feasible powered production plan.");
			}

			return plan;
		}

		private PowerPlan BuildPowerPlan(ResourceLedger resources, MachineFleet machines, long elapsedSeconds, long suppliedCapacitySeconds)
		{
			var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
			var spend = new BigInteger[resourceTypes.Length];
			var output = new BigInteger[resourceTypes.Length];
			var remainders = (long[])poweredRunRemainders.Clone();
			var allocationPlan = AllocatePoweredWork(machines, elapsedSeconds, suppliedCapacitySeconds);

			foreach (var allocation in allocationPlan.Allocations)
			{
				var recipe = allocation.Recipe;
				var totalWork = allocation.Work + poweredRunRemainders[(int)recipe.Machine];
				var runs = totalWork / recipe.PowerDemand;
				if (runs > long.MaxValue)
				{
					return null;
				}

				remainders[(int)recipe.Machine] = (long)(totalWork % recipe.PowerDemand);
				foreach (var input in recipe.Inputs)
				{
					spend[(int)input.Resource] += runs * input.Amount;
				}

				output[(int)recipe.Output.Resource] += runs * recipe.Output.Amount;
			}

			var generatorSeconds = BigInteger.Zero;
			var fuelConsumed = BigInteger.Zero;
			BigInteger nextFuelCreditUnits;
			if (suppliedCapacitySeconds <= fuelCreditUnits)
			{
				nextFuelCreditUnits = fuelCreditUnits - suppliedCapacitySeconds;
			}
			else
			{
				var uncreditedWork = (BigInteger)suppliedCapacitySeconds - fuelCreditUnits;
				generatorSeconds = DivideRoundUp(uncreditedWork, powerGrid.CapacityPerGenerator);
				fuelConsumed = generatorSeconds * powerGrid.FuelPerGeneratorSecond.Amount;
				nextFuelCreditUnits = fuelCreditUnits + generatorSeconds * powerGrid.CapacityPerGenerator - suppliedCapacitySeconds;
			}

			spend[(int)powerGrid.FuelPerGeneratorSecond.Resource] += fuelConsumed;

			var resourceSpend = new long[resourceTypes.Length];
			var resourceOutput = new long[resourceTypes.Length];
			foreach (var resource in resourceTypes)
			{
				var index = (int)resource;
				var available = resources.Get(resource);
				if (spend[index] > available)
				{
					return null;
				}

				var finalAmount = (BigInteger)available - spend[index] + output[index];
				if (finalAmount > long.MaxValue)
				{
					return null;
				}

				resourceSpend[index] = (long)spend[index];
				resourceOutput[index] = (long)output[index];
			}

			var maximumGeneratorSeconds = (BigInteger)machines.Get(powerGrid.GeneratorMachine) * elapsedSeconds;
			if (generatorSeconds > maximumGeneratorSeconds)
			{
				return null;
			}

			return new PowerPlan(
				remainders,
				allocationPlan.Phase,
				allocationPlan.Weights,
				resourceSpend,
				resourceOutput,
				ToLongChecked(fuelConsumed, "Generator fuel consumption exceeds the supported range."),
				ToLongChecked(nextFuelCreditUnits, "Generator fuel credit exceeds the supported range."),
				suppliedCapacitySeconds
			);
		}

		private PoweredAllocationPlan AllocatePoweredWork(MachineFleet machines, long elapsedSeconds, long suppliedCapacitySeconds)
		{
			var allocations = new List<PoweredRecipeAllocation>();
			var weights = new long[poweredAllocationWeights.Length];
			var totalWeight = 0L;
			foreach (var recipe in recipes)
			{
				if (recipe.PowerDemand == 0)
				{
					continue;
				}

				var machineCount = machines.Get(recipe.Machine);
				if (machineCount == 0)
				{
					continue;
				}

				var weight = checked((long)machineCount * recipe.PowerDemand);
				var maximumWork = (BigInteger)weight * elapsedSeconds;
				weights[(int)recipe.Machine] = weight;
				totalWeight = checked(totalWeight + weight);
				allocations.Add(new PoweredRecipeAllocation(recipe, weight, maximumWork));
			}

			var totalPoweredWork = (BigInteger)totalWeight * elapsedSeconds;
			if (suppliedCapacitySeconds < 0 || suppliedCapacitySeconds > totalPoweredWork)
			{
				throw new InvalidOperationException("Powered work exceeds the demand available in this simulation interval.");
			}

			var previousPhase = 0L;
			if (HasSameAllocationWeights(weights))
			{
				previousPhase = poweredAllocationPhase;
			}

			if (suppliedCapacitySeconds == 0)
			{
				return new PoweredAllocationPlan(allocations, previousPhase, weights);
			}

			var combinedWork = (BigInteger)previousPhase + suppliedCapacitySeconds;
			var fullCycles = combinedWork / totalWeight;
			var nextPhase = (long)(combinedWork % totalWeight);
			var previousCycleWork = ApportionPoweredWork(allocations, previousPhase, totalWeight);
			var nextCycleWork = ApportionPoweredWork(allocations, nextPhase, totalWeight);

			foreach (var allocation in allocations)
			{
				var machineIndex = (int)allocation.Recipe.Machine;
				allocation.Work = fullCycles * allocation.Weight + nextCycleWork[machineIndex] - previousCycleWork[machineIndex];
				if (allocation.Work < 0 || allocation.Work > allocation.MaximumWork)
				{
					throw new InvalidOperationException("Proportional powered work allocation crossed a machine work boundary.");
				}
			}

			return new PoweredAllocationPlan(allocations, nextPhase, weights);
		}

		private static long[] ApportionPoweredWork(IReadOnlyList<PoweredRecipeAllocation> allocations, long suppliedCapacitySeconds, long totalWeight)
		{
			var work = new long[Enum.GetValues(typeof(MachineType)).Length];
			var allocatedWork = 0L;
			foreach (var allocation in allocations)
			{
				var machineIndex = (int)allocation.Recipe.Machine;
				var baseWork = ToLongChecked(
					(BigInteger)suppliedCapacitySeconds * allocation.Weight / totalWeight,
					"Powered allocation exceeds the supported range."
				);

				work[machineIndex] = baseWork;
				allocatedWork = checked(allocatedWork + baseWork);
			}

			var remainingWork = suppliedCapacitySeconds - allocatedWork;
			while (remainingWork > 0)
			{
				PoweredRecipeAllocation best = null;
				foreach (var allocation in allocations)
				{
					if (best == null || HasHigherNextAllocationPriority(allocation, best, work))
					{
						best = allocation;
					}
				}

				if (best == null)
				{
					throw new InvalidOperationException("Powered work could not be apportioned across powered recipes.");
				}

				work[(int)best.Recipe.Machine] = checked(work[(int)best.Recipe.Machine] + 1);
				remainingWork--;
			}

			return work;
		}

		private static bool HasHigherNextAllocationPriority(PoweredRecipeAllocation candidate, PoweredRecipeAllocation current, long[] work)
		{
			var candidateWork = work[(int)candidate.Recipe.Machine];
			var currentWork = work[(int)current.Recipe.Machine];
			var candidatePriority = (BigInteger)candidate.Weight * (currentWork + 1L);
			var currentPriority = (BigInteger)current.Weight * (candidateWork + 1L);
			var priorityComparison = candidatePriority.CompareTo(currentPriority);
			if (priorityComparison != 0)
			{
				return priorityComparison > 0;
			}

			return candidate.Recipe.Machine.CompareTo(current.Recipe.Machine) < 0;
		}

		private bool HasSameAllocationWeights(long[] weights)
		{
			for (var index = 0; index < weights.Length; index++)
			{
				if (weights[index] != poweredAllocationWeights[index])
				{
					return false;
				}
			}

			return true;
		}

		private void ApplyPowerPlan(ResourceLedger resources, PowerPlan plan)
		{
			var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
			foreach (var resource in resourceTypes)
			{
				var amount = plan.ResourceSpend[(int)resource];
				if (amount == 0)
				{
					continue;
				}

				var spent = resources.TrySpend(resource, amount);
				if (!spent)
				{
					throw new InvalidOperationException("Resource balances changed while applying a powered production plan.");
				}
			}

			foreach (var resource in resourceTypes)
			{
				var amount = plan.ResourceOutput[(int)resource];
				if (amount > 0)
				{
					resources.Add(resource, amount);
				}
			}

			Array.Copy(plan.Remainders, poweredRunRemainders, poweredRunRemainders.Length);
			Array.Copy(plan.AllocationWeights, poweredAllocationWeights, poweredAllocationWeights.Length);
			poweredAllocationPhase = plan.AllocationPhase;
			fuelCreditUnits = plan.FuelCreditUnits;
		}

		private int GetRunRemainderLimit(MachineType machine)
		{
			var powerDemand = GetPowerDemand(machine);
			if (powerDemand > 0)
			{
				return powerDemand;
			}

			return 1;
		}

		private int GetPowerDemand(MachineType machine)
		{
			foreach (var recipe in recipes)
			{
				if (recipe.Machine == machine && recipe.PowerDemand > 0)
				{
					return recipe.PowerDemand;
				}
			}

			return 0;
		}

		private static BigInteger DivideRoundUp(BigInteger numerator, BigInteger denominator)
		{
			if (numerator == 0)
			{
				return BigInteger.Zero;
			}

			return (numerator + denominator - BigInteger.One) / denominator;
		}

		private static long ToLongChecked(BigInteger value, string message)
		{
			if (value < 0 || value > long.MaxValue)
			{
				throw new OverflowException(message);
			}

			return (long)value;
		}

		private static void ProcessRecipe(ResourceLedger resources, MachineFleet machines, ProductionRecipe recipe, long elapsedSeconds)
		{
			var machineCount = machines.Get(recipe.Machine);
			if (machineCount == 0 || elapsedSeconds == 0)
			{
				return;
			}

			var desiredRuns = checked((long)machineCount * elapsedSeconds);
			ProcessRecipe(resources, recipe, desiredRuns);
		}

		private static void ProcessRecipe(ResourceLedger resources, ProductionRecipe recipe, long desiredRuns)
		{
			if (desiredRuns == 0)
			{
				return;
			}

			var actualRuns = GetAffordableRuns(resources, recipe, desiredRuns);
			if (actualRuns == 0)
			{
				return;
			}

			ConsumeInputs(resources, recipe, actualRuns);
			resources.Add(recipe.Output.Resource, checked(recipe.Output.Amount * actualRuns));
		}

		private static long GetAffordableRuns(ResourceLedger resources, ProductionRecipe recipe, long desiredRuns)
		{
			var affordableRuns = desiredRuns;

			foreach (var input in recipe.Inputs)
			{
				var availableRuns = resources.Get(input.Resource) / input.Amount;
				affordableRuns = Math.Min(affordableRuns, availableRuns);
			}

			return affordableRuns;
		}

		private static void ConsumeInputs(ResourceLedger resources, ProductionRecipe recipe, long runs)
		{
			foreach (var input in recipe.Inputs)
			{
				var spent = resources.TrySpend(input.Resource, checked(input.Amount * runs));
				if (!spent)
				{
					throw new InvalidOperationException("Production inputs changed while applying a simulation step.");
				}
			}
		}

		private sealed class PoweredRecipeAllocation
		{
			public ProductionRecipe Recipe { get; }

			public long Weight { get; }

			public BigInteger MaximumWork { get; }

			public BigInteger Work { get; set; }

			public PoweredRecipeAllocation(ProductionRecipe recipe, long weight, BigInteger maximumWork)
			{
				Recipe = recipe;
				Weight = weight;
				MaximumWork = maximumWork;
			}
		}

		private sealed class PoweredAllocationPlan
		{
			public List<PoweredRecipeAllocation> Allocations { get; }

			public long Phase { get; }

			public long[] Weights { get; }

			public PoweredAllocationPlan(List<PoweredRecipeAllocation> allocations, long phase, long[] weights)
			{
				Allocations = allocations;
				Phase = phase;
				Weights = weights;
			}
		}

		private sealed class PowerPlan
		{
			public long[] Remainders { get; }

			public long AllocationPhase { get; }

			public long[] AllocationWeights { get; }

			public long[] ResourceSpend { get; }

			public long[] ResourceOutput { get; }

			public long FuelConsumed { get; }

			public long FuelCreditUnits { get; }

			public long SuppliedCapacitySeconds { get; }

			public PowerPlan(long[] remainders, long allocationPhase, long[] allocationWeights, long[] resourceSpend, long[] resourceOutput, long fuelConsumed, long fuelCreditUnits, long suppliedCapacitySeconds)
			{
				Remainders = remainders;
				AllocationPhase = allocationPhase;
				AllocationWeights = allocationWeights;
				ResourceSpend = resourceSpend;
				ResourceOutput = resourceOutput;
				FuelConsumed = fuelConsumed;
				FuelCreditUnits = fuelCreditUnits;
				SuppliedCapacitySeconds = suppliedCapacitySeconds;
			}
		}
	}
}
