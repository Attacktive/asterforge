using System;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;
using NUnit.Framework;

namespace Asterforge.Game.Tests.Production
{
	public sealed class PoweredProductionPartitionTests
	{
		[Test]
		public void Advance_WhenGeneratedCoalMeetsExactRunBoundary_IsBatchInvariant()
		{
			var result = CompareIndustryAdvancement(2, 1, 1, 1, 12);

			Assert.That(result.BatchedResources.Get(ResourceType.Metal), Is.EqualTo(8));
			Assert.That(result.BatchedResources.Get(ResourceType.Coal), Is.Zero);
			Assert.That(result.BatchedResources.Get(ResourceType.Steel), Is.EqualTo(4));
			Assert.That(result.BatchedFuelConsumed, Is.EqualTo(4));
		}

		[Test]
		public void Advance_AcrossIndustryConfigurations_IsBatchInvariant()
		{
			for (var smelterCount = 0; smelterCount <= 4; smelterCount++)
			{
				for (var coalMineCount = 0; coalMineCount <= 3; coalMineCount++)
				{
					for (var generatorCount = 1; generatorCount <= 2; generatorCount++)
					{
						for (var steelworksCount = 1; steelworksCount <= 3; steelworksCount++)
						{
							CompareIndustryAdvancement(smelterCount, coalMineCount, generatorCount, steelworksCount, 12);
						}
					}
				}
			}
		}

		private static IndustryAdvancementComparison CompareIndustryAdvancement(int smelterCount, int coalMineCount, int generatorCount, int steelworksCount, int elapsedSeconds)
		{
			var batchedResources = new ResourceLedger();
			var batchedMachines = new MachineFleet();
			var batchedSimulator = CreatePoweredSimulator();
			ConfigureMachines(batchedMachines, smelterCount, coalMineCount, generatorCount, steelworksCount);
			var batchedStatus = batchedSimulator.Advance(batchedResources, batchedMachines, elapsedSeconds);

			var steppedResources = new ResourceLedger();
			var steppedMachines = new MachineFleet();
			var steppedSimulator = CreatePoweredSimulator();
			ConfigureMachines(steppedMachines, smelterCount, coalMineCount, generatorCount, steelworksCount);
			var steppedFuelConsumed = 0L;
			var steppedSuppliedCapacitySeconds = 0L;
			for (var second = 0; second < elapsedSeconds; second++)
			{
				var status = steppedSimulator.Advance(steppedResources, steppedMachines, 1);
				steppedFuelConsumed += status.FuelConsumed;
				steppedSuppliedCapacitySeconds += status.SuppliedCapacitySeconds;
			}

			var context = $"Smelters={smelterCount}, CoalMines={coalMineCount}, Generators={generatorCount}, Steelworks={steelworksCount}, Seconds={elapsedSeconds}";
			var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
			foreach (var resource in resourceTypes)
			{
				Assert.That(batchedResources.Get(resource), Is.EqualTo(steppedResources.Get(resource)), context);
			}

			Assert.That(batchedStatus.FuelConsumed, Is.EqualTo(steppedFuelConsumed), context);
			Assert.That(batchedStatus.SuppliedCapacitySeconds, Is.EqualTo(steppedSuppliedCapacitySeconds), context);
			Assert.That(batchedSimulator.CapturePoweredRunRemainders(), Is.EqualTo(steppedSimulator.CapturePoweredRunRemainders()), context);
			Assert.That(batchedSimulator.CaptureFuelCreditUnits(), Is.EqualTo(steppedSimulator.CaptureFuelCreditUnits()), context);

			return new IndustryAdvancementComparison(batchedResources, batchedStatus.FuelConsumed);
		}

		private static void ConfigureMachines(MachineFleet machines, int smelterCount, int coalMineCount, int generatorCount, int steelworksCount)
		{
			machines.Add(MachineType.Smelter, smelterCount);
			machines.Add(MachineType.CoalMine, coalMineCount);
			machines.Add(MachineType.CoalGenerator, generatorCount);
			machines.Add(MachineType.Steelworks, steelworksCount);
		}

		private static ProductionSimulator CreatePoweredSimulator()
		{
			var powerGrid = new PowerGrid(
				MachineType.CoalGenerator,
				4,
				new ResourceQuantity(ResourceType.Coal, 1)
			);

			return new ProductionSimulator(OutpostRecipes.All, powerGrid);
		}

		private readonly struct IndustryAdvancementComparison
		{
			public ResourceLedger BatchedResources { get; }

			public long BatchedFuelConsumed { get; }

			public IndustryAdvancementComparison(ResourceLedger batchedResources, long batchedFuelConsumed)
			{
				BatchedResources = batchedResources;
				BatchedFuelConsumed = batchedFuelConsumed;
			}
		}
	}
}
