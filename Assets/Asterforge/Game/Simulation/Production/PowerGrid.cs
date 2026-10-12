using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Production
{
	public readonly struct PowerStatus
	{
		public long Capacity { get; }

		public long Demand { get; }

		public long SuppliedCapacitySeconds { get; }

		public long DemandedCapacitySeconds { get; }

		public long FuelConsumed { get; }

		public bool IsBrownout => SuppliedCapacitySeconds < DemandedCapacitySeconds;

		public PowerStatus(long capacity, long demand, long suppliedCapacitySeconds, long demandedCapacitySeconds, long fuelConsumed)
		{
			Capacity = capacity;
			Demand = demand;
			SuppliedCapacitySeconds = suppliedCapacitySeconds;
			DemandedCapacitySeconds = demandedCapacitySeconds;
			FuelConsumed = fuelConsumed;
		}
	}

	public sealed class PowerGrid
	{
		public MachineType GeneratorMachine { get; }

		public int CapacityPerGenerator { get; }

		public ResourceQuantity FuelPerGeneratorSecond { get; }

		public PowerGrid(MachineType generatorMachine, int capacityPerGenerator, ResourceQuantity fuelPerGeneratorSecond)
		{
			if (capacityPerGenerator <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(capacityPerGenerator), capacityPerGenerator, "Generator capacity must be positive.");
			}

			if (fuelPerGeneratorSecond.Amount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(fuelPerGeneratorSecond), fuelPerGeneratorSecond.Amount, "Generator fuel quantity must be positive.");
			}

			GeneratorMachine = generatorMachine;
			CapacityPerGenerator = capacityPerGenerator;
			FuelPerGeneratorSecond = fuelPerGeneratorSecond;
		}

		public long GetCapacity(MachineFleet machines)
		{
			if (machines == null)
			{
				throw new ArgumentNullException(nameof(machines));
			}

			return checked((long)machines.Get(GeneratorMachine) * CapacityPerGenerator);
		}

		public long GetDemand(MachineFleet machines, IReadOnlyList<ProductionRecipe> recipes)
		{
			if (machines == null)
			{
				throw new ArgumentNullException(nameof(machines));
			}

			if (recipes == null)
			{
				throw new ArgumentNullException(nameof(recipes));
			}

			var demand = 0L;
			foreach (var recipe in recipes)
			{
				demand = checked(demand + (long)machines.Get(recipe.Machine) * recipe.PowerDemand);
			}

			return demand;
		}

		internal PowerStatus Supply(ResourceLedger resources, MachineFleet machines, IReadOnlyList<ProductionRecipe> recipes, long elapsedSeconds)
		{
			var capacity = GetCapacity(machines);
			var demand = GetDemand(machines, recipes);
			var demandedCapacitySeconds = checked(demand * elapsedSeconds);
			if (elapsedSeconds == 0 || demand == 0 || capacity == 0)
			{
				return new PowerStatus(capacity, demand, 0, demandedCapacitySeconds, 0);
			}

			var generatorCount = machines.Get(GeneratorMachine);
			var maximumGeneratorSeconds = checked((long)generatorCount * elapsedSeconds);
			var requiredGeneratorSeconds = DivideRoundUp(demandedCapacitySeconds, CapacityPerGenerator);
			var affordableGeneratorSeconds = resources.Get(FuelPerGeneratorSecond.Resource) / FuelPerGeneratorSecond.Amount;
			var activeGeneratorSeconds = Math.Min(maximumGeneratorSeconds, Math.Min(requiredGeneratorSeconds, affordableGeneratorSeconds));
			var fuelConsumed = checked(activeGeneratorSeconds * FuelPerGeneratorSecond.Amount);
			if (fuelConsumed > 0)
			{
				var spent = resources.TrySpend(FuelPerGeneratorSecond.Resource, fuelConsumed);
				if (!spent)
				{
					throw new InvalidOperationException("Generator fuel changed while applying a simulation step.");
				}
			}

			var suppliedCapacitySeconds = Math.Min(demandedCapacitySeconds, checked(activeGeneratorSeconds * CapacityPerGenerator));

			return new PowerStatus(capacity, demand, suppliedCapacitySeconds, demandedCapacitySeconds, fuelConsumed);
		}

		private static long DivideRoundUp(long numerator, long denominator)
		{
			return checked((numerator + denominator - 1L) / denominator);
		}
	}
}
