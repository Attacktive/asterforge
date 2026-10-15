using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Progression;
using Asterforge.Game.Simulation.Resources;
using NUnit.Framework;

namespace Asterforge.Game.Tests.Progression
{
	public sealed class OutpostWorldTests
	{
		private OutpostWorld world;

		[SetUp]
		public void SetUp()
		{
			world = new OutpostWorld();
		}

		[Test]
		public void MineOre_AddsOneOre()
		{
			world.MineOre();

			Assert.That(world.Resources.Get(ResourceType.Ore), Is.EqualTo(1));
		}

		[Test]
		public void TryBuyMachine_WhenAffordable_SpendsResourcesAndAddsMachine()
		{
			world.Resources.Add(ResourceType.Ore, 25);

			var firstPurchase = world.TryBuyMachine(MachineType.Extractor);
			var secondPurchase = world.TryBuyMachine(MachineType.Extractor);

			Assert.That(firstPurchase, Is.True);
			Assert.That(secondPurchase, Is.True);
			Assert.That(world.Machines.Get(MachineType.Extractor), Is.EqualTo(2));
			Assert.That(world.Resources.Get(ResourceType.Ore), Is.Zero);
		}

		[Test]
		public void TryBuyMachine_WhenResourcesAreInsufficient_DoesNotChangeWorld()
		{
			world.Resources.Add(ResourceType.Ore, 9);

			var purchased = world.TryBuyMachine(MachineType.Extractor);

			Assert.That(purchased, Is.False);
			Assert.That(world.Machines.Get(MachineType.Extractor), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Ore), Is.EqualTo(9));
		}

		[Test]
		public void TryBuyMachine_RequiresEarlierProductionStage()
		{
			world.Resources.Add(ResourceType.Ore, 35);

			var smelterBeforeExtractor = world.TryBuyMachine(MachineType.Smelter);
			var extractor = world.TryBuyMachine(MachineType.Extractor);
			var smelterAfterExtractor = world.TryBuyMachine(MachineType.Smelter);

			Assert.That(smelterBeforeExtractor, Is.False);
			Assert.That(extractor, Is.True);
			Assert.That(smelterAfterExtractor, Is.True);
			Assert.That(world.Machines.Get(MachineType.Smelter), Is.EqualTo(1));
		}

		[Test]
		public void Advance_WithResearchStation_ProducesResearchWithoutConsumingComponents()
		{
			world.Resources.Add(ResourceType.Components, 7);
			world.Machines.Add(MachineType.ResearchStation, 2);

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Research), Is.EqualTo(20));
			Assert.That(world.Resources.Get(ResourceType.Components), Is.EqualTo(7));
			Assert.That(world.GetLastNetRate(ResourceType.Research), Is.EqualTo(2d));
		}

		[Test]
		public void RedesignedEconomy_LeavesLegacyComponentsAndAssemblerUnavailable()
		{
			Assert.That(world.IsResourceUnlocked(ResourceType.Components), Is.False);
			Assert.That(world.IsMachineUnlocked(MachineType.Assembler), Is.False);
		}

		[Test]
		public void TryIndustrialize_WhenResearchIsSufficient_RevealsIndustryAndUnlocksCoalMine()
		{
			world.Resources.Add(ResourceType.Research, 125);

			var industrialized = world.TryIndustrialize();

			Assert.That(industrialized, Is.True);
			Assert.That(world.IsIndustrialized, Is.True);
			Assert.That(world.IsResourceUnlocked(ResourceType.Coal), Is.True);
			Assert.That(world.IsResourceUnlocked(ResourceType.Steel), Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalMine), Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalGenerator), Is.False);
			Assert.That(world.IsMachineUnlocked(MachineType.Steelworks), Is.False);
			Assert.That(world.Resources.Get(ResourceType.Research), Is.EqualTo(25));
		}

		[Test]
		public void IndustryMachines_UnlockInDependencyOrder()
		{
			world.Resources.Add(ResourceType.Research, 100);
			world.TryIndustrialize();

			world.Machines.Add(MachineType.CoalMine);
			var generatorUnlocked = world.IsMachineUnlocked(MachineType.CoalGenerator);
			var steelworksBeforeGenerator = world.IsMachineUnlocked(MachineType.Steelworks);
			world.Machines.Add(MachineType.CoalGenerator);

			Assert.That(generatorUnlocked, Is.True);
			Assert.That(steelworksBeforeGenerator, Is.False);
			Assert.That(world.IsMachineUnlocked(MachineType.Steelworks), Is.True);
		}

		[Test]
		public void Advance_WithFueledIndustry_UsesPowerGridAndReportsActualSteelRate()
		{
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 40);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks);

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Coal), Is.EqualTo(10));
			Assert.That(world.Resources.Get(ResourceType.Steel), Is.EqualTo(10));
			Assert.That(world.PowerCapacity, Is.EqualTo(4));
			Assert.That(world.PowerDemand, Is.EqualTo(4));
			Assert.That(world.LastPowerStatus.IsBrownout, Is.False);
			Assert.That(world.GetLastNetRate(ResourceType.Steel), Is.EqualTo(1d));
		}

		[Test]
		public void SteelCapability_WithBalancedZeroBuffers_ReportsSustainableTarget()
		{
			world.Machines.Add(MachineType.Smelter, 4);
			world.Machines.Add(MachineType.CoalMine, 3);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks);

			world.Advance(1);
			var capability = world.GetSteelCapability();

			Assert.That(world.Resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Coal), Is.Zero);
			Assert.That(world.GetLastNetRate(ResourceType.Metal), Is.Zero);
			Assert.That(world.GetLastNetRate(ResourceType.Coal), Is.Zero);
			Assert.That(world.GetLastNetRate(ResourceType.Steel), Is.EqualTo(1d));
			Assert.That(capability.SustainableRate, Is.EqualTo(1d));
		}

		[Test]
		public void SteelCapability_WithStockpileBackedOutput_DoesNotReportSustainableTarget()
		{
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 40);
			world.Machines.Add(MachineType.Smelter);
			world.Machines.Add(MachineType.CoalMine);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks);

			world.Advance(1);
			var capability = world.GetSteelCapability();

			Assert.That(world.GetLastNetRate(ResourceType.Steel), Is.EqualTo(1d));
			Assert.That(world.GetLastNetRate(ResourceType.Metal), Is.EqualTo(-3d));
			Assert.That(world.GetLastNetRate(ResourceType.Coal), Is.EqualTo(-2d));
			Assert.That(capability.SustainableRate, Is.EqualTo(0.25d));
		}

		[Test]
		public void Advance_WithoutGenerator_DoesNotRunSteelworks()
		{
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 40);
			world.Machines.Add(MachineType.Steelworks);

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Steel), Is.Zero);
			Assert.That(world.LastPowerStatus.IsBrownout, Is.True);
		}

		[Test]
		public void SteelMetalShortage_CanBeRecoveredByOreFundedSmelterExpansion()
		{
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 40);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks);
			world.Advance(10);
			world.Machines.Add(MachineType.Extractor);
			world.Resources.Add(ResourceType.Ore, 25);

			var purchased = world.TryBuyMachine(MachineType.Smelter);

			Assert.That(world.Resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(purchased, Is.True);
			Assert.That(world.Machines.Get(MachineType.Smelter), Is.EqualTo(1));
		}

		[Test]
		public void TryIndustrialize_WhenResearchIsInsufficient_DoesNotUnlockIndustry()
		{
			world.Resources.Add(ResourceType.Research, 99);

			var industrialized = world.TryIndustrialize();

			Assert.That(industrialized, Is.False);
			Assert.That(world.IsIndustrialized, Is.False);
			Assert.That(world.IsResourceUnlocked(ResourceType.Coal), Is.False);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalMine), Is.False);
			Assert.That(world.Resources.Get(ResourceType.Research), Is.EqualTo(99));
		}

		[Test]
		public void Advance_WithPurchasedExtractor_ProducesOre()
		{
			world.Resources.Add(ResourceType.Ore, 10);
			world.TryBuyMachine(MachineType.Extractor);

			world.Advance(12);

			Assert.That(world.Resources.Get(ResourceType.Ore), Is.EqualTo(12));
			Assert.That(world.GetLastNetRate(ResourceType.Ore), Is.EqualTo(1d));
		}
	}
}
