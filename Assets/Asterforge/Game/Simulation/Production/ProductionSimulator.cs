using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Production
{
	public sealed class ProductionSimulator
	{
		private readonly IReadOnlyList<ProductionRecipe> recipes;
		private readonly PowerGrid powerGrid;

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

			foreach (var recipe in recipes)
			{
				if (recipe.PowerDemand == 0)
				{
					ProcessRecipe(resources, machines, recipe, elapsedSeconds);
				}
			}

			var powerStatus = powerGrid.Supply(resources, machines, recipes, elapsedSeconds);
			if (powerStatus.DemandedCapacitySeconds == 0)
			{
				return powerStatus;
			}

			foreach (var recipe in recipes)
			{
				if (recipe.PowerDemand == 0)
				{
					continue;
				}

				var machineCount = machines.Get(recipe.Machine);
				var desiredRuns = checked((long)machineCount * elapsedSeconds);
				var powerLimitedRuns = checked(desiredRuns * powerStatus.SuppliedCapacitySeconds) / powerStatus.DemandedCapacitySeconds;
				ProcessRecipe(resources, recipe, powerLimitedRuns);
			}

			return powerStatus;
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
	}
}
