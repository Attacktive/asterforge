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
		public void TryIndustrialize_WhenRequirementsAreMet_SpendsCostAndUnlocksIndustryMachines()
		{
			world.Resources.Add(ResourceType.Metal, 600);
			world.Resources.Add(ResourceType.Components, 125);
			world.Resources.Add(ResourceType.Research, 50);

			var industrialized = world.TryIndustrialize();
			var coalMine = world.TryBuyMachine(MachineType.CoalMine);

			Assert.That(industrialized, Is.True);
			Assert.That(coalMine, Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.Steelworks), Is.True);
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Components), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Research), Is.Zero);
		}

		[Test]
		public void TryIndustrialize_WhenRequirementsAreMissing_DoesNotUnlockIndustry()
		{
			world.Resources.Add(ResourceType.Metal, 499);
			world.Resources.Add(ResourceType.Components, 100);
			world.Resources.Add(ResourceType.Research, 50);

			var industrialized = world.TryIndustrialize();

			Assert.That(industrialized, Is.False);
			Assert.That(world.IsIndustrialized, Is.False);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalMine), Is.False);
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
