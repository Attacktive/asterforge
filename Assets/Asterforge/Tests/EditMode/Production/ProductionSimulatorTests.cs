using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;
using NUnit.Framework;

namespace Asterforge.Game.Tests.Production
{
	public sealed class ProductionSimulatorTests
	{
		private ProductionSimulator simulator;
		private ResourceLedger resources;
		private MachineFleet machines;

		[SetUp]
		public void SetUp()
		{
			simulator = new ProductionSimulator(OutpostRecipes.All);
			resources = new ResourceLedger();
			machines = new MachineFleet();
		}

		[Test]
		public void Advance_WithExtractor_ProducesOreForElapsedTime()
		{
			machines.Add(MachineType.Extractor);

			simulator.Advance(resources, machines, 10);

			Assert.That(resources.Get(ResourceType.Ore), Is.EqualTo(10));
		}

		[Test]
		public void Advance_WithProductionChain_UsesUpstreamOutputInSameInterval()
		{
			machines.Add(MachineType.Extractor, 2);
			machines.Add(MachineType.Smelter);

			simulator.Advance(resources, machines, 10);

			Assert.That(resources.Get(ResourceType.Ore), Is.Zero);
			Assert.That(resources.Get(ResourceType.Metal), Is.EqualTo(10));
		}

		[Test]
		public void Advance_WhenInputIsInsufficient_ClampsProduction()
		{
			resources.Add(ResourceType.Ore, 7);
			machines.Add(MachineType.Smelter, 10);

			simulator.Advance(resources, machines, 1);

			Assert.That(resources.Get(ResourceType.Ore), Is.EqualTo(1));
			Assert.That(resources.Get(ResourceType.Metal), Is.EqualTo(3));
		}

		[Test]
		public void Advance_WithMultipleMachines_ScalesThroughput()
		{
			machines.Add(MachineType.Extractor, 4);

			simulator.Advance(resources, machines, 30);

			Assert.That(resources.Get(ResourceType.Ore), Is.EqualTo(120));
		}

		[Test]
		public void Advance_WhenElapsedTimeIsNegative_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => simulator.Advance(resources, machines, -1));
		}

		[Test]
		public void PowerGrid_WithDefaultFuelQuantity_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(
				() => new PowerGrid(
					MachineType.CoalGenerator,
					4,
					default
				)
			);
		}

		[Test]
		public void Advance_WithFueledGenerator_PowersSteelProduction()
		{
			var poweredSimulator = CreatePoweredSimulator(OutpostRecipes.All);
			resources.Add(ResourceType.Metal, 40);
			resources.Add(ResourceType.Coal, 40);
			machines.Add(MachineType.CoalGenerator);
			machines.Add(MachineType.Steelworks);

			var status = poweredSimulator.Advance(resources, machines, 10);

			Assert.That(resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(resources.Get(ResourceType.Coal), Is.EqualTo(10));
			Assert.That(resources.Get(ResourceType.Steel), Is.EqualTo(10));
			Assert.That(status.Capacity, Is.EqualTo(4));
			Assert.That(status.Demand, Is.EqualTo(4));
			Assert.That(status.FuelConsumed, Is.EqualTo(10));
			Assert.That(status.IsBrownout, Is.False);
		}

		[Test]
		public void Advance_WhenPowerDemandExceedsSupply_ScalesPoweredProductionProportionally()
		{
			var poweredSimulator = CreatePoweredSimulator(OutpostRecipes.All);
			resources.Add(ResourceType.Metal, 80);
			resources.Add(ResourceType.Coal, 60);
			machines.Add(MachineType.CoalGenerator);
			machines.Add(MachineType.Steelworks, 2);

			var status = poweredSimulator.Advance(resources, machines, 10);

			Assert.That(resources.Get(ResourceType.Metal), Is.EqualTo(40));
			Assert.That(resources.Get(ResourceType.Coal), Is.EqualTo(30));
			Assert.That(resources.Get(ResourceType.Steel), Is.EqualTo(10));
			Assert.That(status.Capacity, Is.EqualTo(4));
			Assert.That(status.Demand, Is.EqualTo(8));
			Assert.That(status.IsBrownout, Is.True);
		}

		[Test]
		public void Advance_WhenGeneratorHasNoFuel_DoesNotRunPoweredProduction()
		{
			var poweredSimulator = CreatePoweredSimulator(OutpostRecipes.All);
			resources.Add(ResourceType.Metal, 40);
			machines.Add(MachineType.CoalGenerator);
			machines.Add(MachineType.Steelworks);

			var status = poweredSimulator.Advance(resources, machines, 10);

			Assert.That(resources.Get(ResourceType.Metal), Is.EqualTo(40));
			Assert.That(resources.Get(ResourceType.Steel), Is.Zero);
			Assert.That(status.FuelConsumed, Is.Zero);
			Assert.That(status.IsBrownout, Is.True);
		}

		[Test]
		public void Advance_WithCoalGeneration_IsIndependentOfRecipeIterationOrder()
		{
			var reversedRecipes = new List<ProductionRecipe>();
			for (var index = OutpostRecipes.All.Count - 1; index >= 0; index--)
			{
				reversedRecipes.Add(OutpostRecipes.All[index]);
			}

			var poweredSimulator = CreatePoweredSimulator(reversedRecipes);
			resources.Add(ResourceType.Metal, 40);
			machines.Add(MachineType.CoalMine, 3);
			machines.Add(MachineType.CoalGenerator);
			machines.Add(MachineType.Steelworks);

			poweredSimulator.Advance(resources, machines, 10);

			Assert.That(resources.Get(ResourceType.Coal), Is.Zero);
			Assert.That(resources.Get(ResourceType.Steel), Is.EqualTo(10));
		}

		[Test]
		public void Advance_WhenFuelIsSharedWithPoweredInput_IsBatchInvariant()
		{
			var batchedResources = new ResourceLedger();
			var batchedMachines = new MachineFleet();
			var batchedSimulator = CreatePoweredSimulator(OutpostRecipes.All);
			batchedResources.Add(ResourceType.Metal, 40);
			batchedResources.Add(ResourceType.Coal, 20);
			batchedMachines.Add(MachineType.CoalGenerator);
			batchedMachines.Add(MachineType.Steelworks);

			var batchedStatus = batchedSimulator.Advance(batchedResources, batchedMachines, 10);

			var steppedResources = new ResourceLedger();
			var steppedMachines = new MachineFleet();
			var steppedSimulator = CreatePoweredSimulator(OutpostRecipes.All);
			steppedResources.Add(ResourceType.Metal, 40);
			steppedResources.Add(ResourceType.Coal, 20);
			steppedMachines.Add(MachineType.CoalGenerator);
			steppedMachines.Add(MachineType.Steelworks);
			var steppedFuelConsumed = 0L;
			for (var second = 0; second < 10; second++)
			{
				steppedFuelConsumed += steppedSimulator.Advance(steppedResources, steppedMachines, 1).FuelConsumed;
			}

			Assert.That(batchedResources.Get(ResourceType.Metal), Is.EqualTo(steppedResources.Get(ResourceType.Metal)));
			Assert.That(batchedResources.Get(ResourceType.Coal), Is.EqualTo(steppedResources.Get(ResourceType.Coal)));
			Assert.That(batchedResources.Get(ResourceType.Steel), Is.EqualTo(steppedResources.Get(ResourceType.Steel)));
			Assert.That(batchedStatus.FuelConsumed, Is.EqualTo(steppedFuelConsumed));
			Assert.That(batchedResources.Get(ResourceType.Steel), Is.EqualTo(6));
			Assert.That(batchedStatus.FuelConsumed, Is.EqualTo(7));
		}

		[Test]
		public void Advance_WithFractionalBrownout_CarriesProductionAcrossAdvances()
		{
			var poweredSimulator = CreatePoweredSimulator(OutpostRecipes.All, 2);
			resources.Add(ResourceType.Metal, 40);
			resources.Add(ResourceType.Coal, 40);
			machines.Add(MachineType.CoalGenerator);
			machines.Add(MachineType.Steelworks);

			var fuelConsumed = 0L;
			for (var second = 0; second < 10; second++)
			{
				fuelConsumed += poweredSimulator.Advance(resources, machines, 1).FuelConsumed;
			}

			Assert.That(resources.Get(ResourceType.Steel), Is.EqualTo(5));
			Assert.That(resources.Get(ResourceType.Metal), Is.EqualTo(20));
			Assert.That(resources.Get(ResourceType.Coal), Is.EqualTo(20));
			Assert.That(fuelConsumed, Is.EqualTo(10));
		}

		[Test]
		public void Advance_WithLargeRepresentableInterval_DoesNotOverflowPowerRatio()
		{
			var poweredSimulator = CreatePoweredSimulator(OutpostRecipes.All);
			resources.Add(ResourceType.Metal, 8_000_000_000L);
			resources.Add(ResourceType.Coal, 6_000_000_000L);
			machines.Add(MachineType.CoalGenerator, 1_000);
			machines.Add(MachineType.Steelworks, 1_000);

			PowerStatus status = default;
			Assert.DoesNotThrow(() => status = poweredSimulator.Advance(resources, machines, 2_000_000));

			Assert.That(resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(resources.Get(ResourceType.Coal), Is.Zero);
			Assert.That(resources.Get(ResourceType.Steel), Is.EqualTo(2_000_000_000L));
			Assert.That(status.FuelConsumed, Is.EqualTo(2_000_000_000L));
			Assert.That(status.IsBrownout, Is.False);
		}

		private static ProductionSimulator CreatePoweredSimulator(IReadOnlyList<ProductionRecipe> recipes, int capacityPerGenerator = 4)
		{
			var powerGrid = new PowerGrid(
				MachineType.CoalGenerator,
				capacityPerGenerator,
				new ResourceQuantity(ResourceType.Coal, 1)
			);

			return new ProductionSimulator(recipes, powerGrid);
		}
	}
}
