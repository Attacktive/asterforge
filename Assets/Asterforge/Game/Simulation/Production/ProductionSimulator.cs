using System;
using System.Collections.Generic;
using System.Numerics;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Production
{
	public sealed class ProductionSimulator
	{
		private const long PowerRatioScale = 1_000_000_000L;

		private readonly IReadOnlyList<ProductionRecipe> recipes;
		private readonly PowerGrid powerGrid;
		private readonly long[] poweredRunRemainders = new long[Enum.GetValues(typeof(MachineType)).Length];

		public ProductionSimulator(IReadOnlyList<ProductionRecipe> recipes)
			: this(recipes, null)
		{
		}

		public ProductionSimulator(IReadOnlyList<ProductionRecipe> recipes, PowerGrid powerGrid)
		{
			this.recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
			this.powerGrid = powerGrid;
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

			var maximumRatioUnits = GetMaximumPowerRatioUnits(capacity, demand);
			var plan = FindBestPowerPlan(resources, machines, elapsedSeconds, demand, demandedCapacitySeconds, maximumRatioUnits);
			ApplyPowerPlan(resources, plan);

			return new PowerStatus(capacity, demand, plan.SuppliedCapacitySeconds, demandedCapacitySeconds, plan.FuelConsumed);
		}

		private PowerPlan FindBestPowerPlan(ResourceLedger resources, MachineFleet machines, long elapsedSeconds, long demand, long demandedCapacitySeconds, long maximumRatioUnits)
		{
			var low = 0L;
			var high = maximumRatioUnits;
			while (low < high)
			{
				var candidate = low + (high - low + 1L) / 2L;
				if (BuildPowerPlan(resources, machines, elapsedSeconds, demand, demandedCapacitySeconds, candidate) != null)
				{
					low = candidate;
				}
				else
				{
					high = candidate - 1L;
				}
			}

			var plan = BuildPowerPlan(resources, machines, elapsedSeconds, demand, demandedCapacitySeconds, low);
			if (plan == null)
			{
				throw new InvalidOperationException("Unable to construct a feasible powered production plan.");
			}

			return plan;
		}

		private PowerPlan BuildPowerPlan(ResourceLedger resources, MachineFleet machines, long elapsedSeconds, long demand, long demandedCapacitySeconds, long ratioUnits)
		{
			var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
			var spend = new BigInteger[resourceTypes.Length];
			var output = new BigInteger[resourceTypes.Length];
			var completedRuns = new long[poweredRunRemainders.Length];
			var remainders = (long[])poweredRunRemainders.Clone();

			var suppliedPowerUnits = (BigInteger)demandedCapacitySeconds * ratioUnits;
			var generatorSecondUnits = (BigInteger)PowerRatioScale * powerGrid.CapacityPerGenerator;
			var generatorSeconds = DivideRoundUp(suppliedPowerUnits, generatorSecondUnits);
			var fuelConsumed = generatorSeconds * powerGrid.FuelPerGeneratorSecond.Amount;
			spend[(int)powerGrid.FuelPerGeneratorSecond.Resource] += fuelConsumed;

			foreach (var recipe in recipes)
			{
				if (recipe.PowerDemand == 0)
				{
					continue;
				}

				var machineCount = machines.Get(recipe.Machine);
				var desiredRuns = (BigInteger)machineCount * elapsedSeconds;
				var totalRunUnits = desiredRuns * ratioUnits + poweredRunRemainders[(int)recipe.Machine];
				var runs = totalRunUnits / PowerRatioScale;
				if (runs > long.MaxValue)
				{
					return null;
				}

				var runCount = (long)runs;
				completedRuns[(int)recipe.Machine] = runCount;
				remainders[(int)recipe.Machine] = (long)(totalRunUnits % PowerRatioScale);

				foreach (var input in recipe.Inputs)
				{
					spend[(int)input.Resource] += runs * input.Amount;
				}

				output[(int)recipe.Output.Resource] += runs * recipe.Output.Amount;
			}

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

			var suppliedCapacitySeconds = BigInteger.Min(
				demandedCapacitySeconds,
				generatorSeconds * powerGrid.CapacityPerGenerator
			);

			return new PowerPlan(
				completedRuns,
				remainders,
				resourceSpend,
				resourceOutput,
				ToLongChecked(fuelConsumed, "Generator fuel consumption exceeds the supported range."),
				ToLongChecked(suppliedCapacitySeconds, "Supplied power exceeds the supported range.")
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
		}

		private static long GetMaximumPowerRatioUnits(long capacity, long demand)
		{
			if (capacity >= demand)
			{
				return PowerRatioScale;
			}

			return (long)((BigInteger)capacity * PowerRatioScale / demand);
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
			public long[] CompletedRuns { get; }

			public long[] Remainders { get; }

			public long[] ResourceSpend { get; }

			public long[] ResourceOutput { get; }

			public long FuelConsumed { get; }

			public long SuppliedCapacitySeconds { get; }

			public PowerPlan(long[] completedRuns, long[] remainders, long[] resourceSpend, long[] resourceOutput, long fuelConsumed, long suppliedCapacitySeconds)
			{
				CompletedRuns = completedRuns;
				Remainders = remainders;
				ResourceSpend = resourceSpend;
				ResourceOutput = resourceOutput;
				FuelConsumed = fuelConsumed;
				SuppliedCapacitySeconds = suppliedCapacitySeconds;
			}
		}
	}
}
