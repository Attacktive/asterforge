using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Production
{
	public sealed class ProductionSimulator
	{
		private readonly IReadOnlyList<ProductionRecipe> recipes;

		public ProductionSimulator(IReadOnlyList<ProductionRecipe> recipes)
		{
			this.recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
		}

		public void Advance(ResourceLedger resources, MachineFleet machines, long elapsedSeconds)
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

			foreach (var recipe in recipes)
			{
				var machineCount = machines.GetActive(recipe.Machine);
				if (machineCount == 0 || elapsedSeconds == 0)
				{
					continue;
				}

				var desiredRuns = checked((long)machineCount * elapsedSeconds);
				var actualRuns = GetAffordableRuns(resources, recipe, desiredRuns);
				if (actualRuns == 0)
				{
					continue;
				}

				ConsumeInputs(resources, recipe, actualRuns);
				resources.Add(recipe.Output.Resource, checked(recipe.Output.Amount * actualRuns));
			}
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
