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

		private static ProductionSimulator CreatePoweredSimulator(IReadOnlyList<ProductionRecipe> recipes)
		{
			var powerGrid = new PowerGrid(
				MachineType.CoalGenerator,
				4,
				new ResourceQuantity(ResourceType.Coal, 1)
			);

			return new ProductionSimulator(recipes, powerGrid);
		}
	}
}
