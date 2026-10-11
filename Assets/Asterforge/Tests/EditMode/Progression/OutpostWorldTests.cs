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
		public void ContributeAvailableToIndustrialization_BanksPartialProgress()
		{
			world.Resources.Add(ResourceType.Metal, 200);
			world.Resources.Add(ResourceType.Components, 25);

			var contributed = world.ContributeAvailableToIndustrialization();

			Assert.That(contributed, Is.EqualTo(225));
			Assert.That(world.GetIndustrializationContribution(ResourceType.Metal), Is.EqualTo(200));
			Assert.That(world.GetIndustrializationContribution(ResourceType.Components), Is.EqualTo(25));
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Components), Is.Zero);
			Assert.That(world.IsIndustrialized, Is.False);
		}

		[Test]
		public void ContributeAvailableToIndustrialization_WhenFullyFunded_UnlocksIndustryAndLeavesSurplus()
		{
			world.Resources.Add(ResourceType.Metal, 600);
			world.Resources.Add(ResourceType.Components, 125);
			world.Resources.Add(ResourceType.Research, 75);

			var contributed = world.ContributeAvailableToIndustrialization();

			Assert.That(contributed, Is.EqualTo(650));
			Assert.That(world.IsIndustrialized, Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalMine), Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.Steelworks), Is.True);
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.EqualTo(100));
			Assert.That(world.Resources.Get(ResourceType.Components), Is.EqualTo(25));
			Assert.That(world.Resources.Get(ResourceType.Research), Is.EqualTo(25));
		}

		[Test]
		public void ContributeAvailableToIndustrialization_AcrossSeparateStockpiles_PreservesEarlierFunding()
		{
			world.Resources.Add(ResourceType.Research, 50);
			world.ContributeAvailableToIndustrialization();
			world.Resources.Add(ResourceType.Components, 100);
			world.ContributeAvailableToIndustrialization();
			world.Resources.Add(ResourceType.Metal, 500);

			world.ContributeAvailableToIndustrialization();

			Assert.That(world.IsIndustrialized, Is.True);
			Assert.That(world.GetIndustrializationContribution(ResourceType.Research), Is.EqualTo(50));
			Assert.That(world.GetIndustrializationContribution(ResourceType.Components), Is.EqualTo(100));
			Assert.That(world.GetIndustrializationContribution(ResourceType.Metal), Is.EqualTo(500));
		}

		[Test]
		public void SetMachinePaused_StopsAndResumesProduction()
		{
			world.Resources.Add(ResourceType.Ore, 35);
			world.TryBuyMachine(MachineType.Extractor);
			world.TryBuyMachine(MachineType.Smelter);
			world.SetMachinePaused(MachineType.Smelter, true);

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Ore), Is.EqualTo(10));
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.Zero);

			world.SetMachinePaused(MachineType.Smelter, false);
			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Ore), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.EqualTo(10));
		}

		[Test]
		public void Advance_WithPurchasedExtractor_ProducesOre()
		{
			world.Resources.Add(ResourceType.Ore, 10);
			world.TryBuyMachine(MachineType.Extractor);

			world.Advance(12);

			Assert.That(world.Resources.Get(ResourceType.Ore), Is.EqualTo(12));
		}
	}
}
