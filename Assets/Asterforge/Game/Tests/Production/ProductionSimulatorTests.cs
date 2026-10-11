using System;
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
	}
}
