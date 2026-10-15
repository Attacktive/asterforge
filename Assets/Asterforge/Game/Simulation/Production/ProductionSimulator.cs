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

		public long CaptureFuelCreditUnits()
		{
			return fuelCreditUnits;
		}

		public void RestorePoweredState(long[] runRemainders, long restoredFuelCreditUnits)
		{
			if (runRemainders == null)
			{
				throw new ArgumentNullException(nameof(runRemainders));
			}

			if (runRemainders.Length != poweredRunRemainders.Length)
			{
				throw new ArgumentException("Powered production remainder data does not match this game version.", nameof(runRemainders));
			}

			var machineTypes = (MachineType[])Enum.GetValues(typeof(MachineType));
			foreach (var machine in machineTypes)
			{
				var remainder = runRemainders[(int)machine];
				var limit = GetRunRemainderLimit(machine);
				if (remainder < 0 || remainder >= limit)
				{
					throw new ArgumentOutOfRangeException(nameof(runRemainders), remainder, "Powered production remainder is outside its machine's work boundary.");
				}
			}

			if (restoredFuelCreditUnits < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(restoredFuelCreditUnits), restoredFuelCreditUnits, "Fuel credit cannot be negative.");
			}

			if (powerGrid == null)
			{
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
			var remainingWork = (BigInteger)suppliedCapacitySeconds;

			foreach (var recipe in recipes)
			{
				if (recipe.PowerDemand == 0)
				{
					continue;
				}

				var machineCount = machines.Get(recipe.Machine);
				var maximumRecipeWork = (BigInteger)machineCount * elapsedSeconds * recipe.PowerDemand;
				var allocatedWork = BigInteger.Min(remainingWork, maximumRecipeWork);
				var totalWork = allocatedWork + poweredRunRemainders[(int)recipe.Machine];
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
				remainingWork -= allocatedWork;
			}

			if (remainingWork != 0)
			{
				throw new InvalidOperationException("Powered work exceeds the demand available in this simulation interval.");
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
				resourceSpend,
				resourceOutput,
				ToLongChecked(fuelConsumed, "Generator fuel consumption exceeds the supported range."),
				ToLongChecked(nextFuelCreditUnits, "Generator fuel credit exceeds the supported range."),
				suppliedCapacitySeconds
			);
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
			fuelCreditUnits = plan.FuelCreditUnits;
		}

		private int GetRunRemainderLimit(MachineType machine)
		{
			foreach (var recipe in recipes)
			{
				if (recipe.Machine == machine && recipe.PowerDemand > 0)
				{
					return recipe.PowerDemand;
				}
			}

			return 1;
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

		private sealed class PowerPlan
		{
			public long[] Remainders { get; }

			public long[] ResourceSpend { get; }

			public long[] ResourceOutput { get; }

			public long FuelConsumed { get; }

			public long FuelCreditUnits { get; }

			public long SuppliedCapacitySeconds { get; }

			public PowerPlan(long[] remainders, long[] resourceSpend, long[] resourceOutput, long fuelConsumed, long fuelCreditUnits, long suppliedCapacitySeconds)
			{
				Remainders = remainders;
				ResourceSpend = resourceSpend;
				ResourceOutput = resourceOutput;
				FuelConsumed = fuelConsumed;
				FuelCreditUnits = fuelCreditUnits;
				SuppliedCapacitySeconds = suppliedCapacitySeconds;
			}
		}
	}
}
