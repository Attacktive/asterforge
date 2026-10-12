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
		public void TryIndustrialize_WhenResearchIsSufficient_SpendsResearchAndUnlocksIndustry()
		{
			world.Resources.Add(ResourceType.Research, 125);

			var industrialized = world.TryIndustrialize();

			Assert.That(industrialized, Is.True);
			Assert.That(world.IsIndustrialized, Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalMine), Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.Steelworks), Is.True);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalGenerator), Is.True);
			Assert.That(world.Resources.Get(ResourceType.Research), Is.EqualTo(25));
		}

		[Test]
		public void TryIndustrialize_WhenResearchIsInsufficient_DoesNotUnlockIndustry()
		{
			world.Resources.Add(ResourceType.Research, 99);

			var industrialized = world.TryIndustrialize();

			Assert.That(industrialized, Is.False);
			Assert.That(world.IsIndustrialized, Is.False);
			Assert.That(world.IsMachineUnlocked(MachineType.CoalMine), Is.False);
			Assert.That(world.Resources.Get(ResourceType.Research), Is.EqualTo(99));
		}

		[Test]
		public void PowerCapacity_WithUnfueledGenerator_DistinguishesInstalledAndFuelBackedCapacity()
		{
			world.Machines.Add(MachineType.CoalGenerator);

			Assert.That(world.PowerCapacity, Is.EqualTo(4));
			Assert.That(world.FuelBackedPowerCapacity, Is.Zero);

			world.Resources.Add(ResourceType.Coal, 1);

			Assert.That(world.FuelBackedPowerCapacity, Is.EqualTo(4));
		}

		[Test]
		public void Advance_WithPurchasedExtractor_ProducesOre()
		{
			world.Resources.Add(ResourceType.Ore, 10);
			world.TryBuyMachine(MachineType.Extractor);

			world.Advance(12);

			Assert.That(world.Resources.Get(ResourceType.Ore), Is.EqualTo(12));
		}

		[Test]
		public void Advance_WithSteelworksButNoGenerator_DoesNotProduceSteel()
		{
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 20);
			world.Machines.Add(MachineType.Steelworks);

			Assert.That(world.PowerCapacity, Is.Zero);
			Assert.That(world.FuelBackedPowerCapacity, Is.Zero);
			Assert.That(world.PowerDemand, Is.EqualTo(4));

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Steel), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.EqualTo(40));
			Assert.That(world.Resources.Get(ResourceType.Coal), Is.EqualTo(20));
			Assert.That(world.LastPowerStatus.FuelBackedCapacitySeconds, Is.Zero);
			Assert.That(world.LastPowerStatus.IsBrownout, Is.True);
		}

		[Test]
		public void Advance_WithCoalGenerator_PowersSteelworksAndReportsGridStatus()
		{
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 30);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks);

			Assert.That(world.PowerCapacity, Is.EqualTo(4));
			Assert.That(world.FuelBackedPowerCapacity, Is.EqualTo(4));
			Assert.That(world.PowerDemand, Is.EqualTo(4));

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Steel), Is.EqualTo(10));
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Coal), Is.Zero);
			Assert.That(world.FuelBackedPowerCapacity, Is.EqualTo(4));
			Assert.That(world.LastPowerStatus.FuelBackedCapacitySeconds, Is.EqualTo(40));
			Assert.That(world.LastPowerStatus.SuppliedCapacitySeconds, Is.EqualTo(40));
			Assert.That(world.LastPowerStatus.DemandedCapacitySeconds, Is.EqualTo(40));
			Assert.That(world.LastPowerStatus.FuelConsumed, Is.EqualTo(10));
			Assert.That(world.LastPowerStatus.IsBrownout, Is.False);
		}

		[Test]
		public void Advance_WhenPowerDemandExceedsCapacity_ReportsBrownoutAndScalesSteelProduction()
		{
			world.Resources.Add(ResourceType.Metal, 80);
			world.Resources.Add(ResourceType.Coal, 60);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks, 2);

			Assert.That(world.PowerCapacity, Is.EqualTo(4));
			Assert.That(world.FuelBackedPowerCapacity, Is.EqualTo(4));
			Assert.That(world.PowerDemand, Is.EqualTo(8));

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Steel), Is.EqualTo(10));
			Assert.That(world.Resources.Get(ResourceType.Metal), Is.EqualTo(40));
			Assert.That(world.Resources.Get(ResourceType.Coal), Is.EqualTo(30));
			Assert.That(world.LastPowerStatus.FuelBackedCapacitySeconds, Is.EqualTo(40));
			Assert.That(world.LastPowerStatus.SuppliedCapacitySeconds, Is.EqualTo(40));
			Assert.That(world.LastPowerStatus.DemandedCapacitySeconds, Is.EqualTo(80));
			Assert.That(world.LastPowerStatus.IsBrownout, Is.True);
		}

		[Test]
		public void Advance_WhenCoalMineOutputIsFullyConsumed_StillReportsFueledGrid()
		{
			world.Resources.Add(ResourceType.Metal, 40);
			world.Machines.Add(MachineType.CoalMine, 3);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks);

			world.Advance(10);

			Assert.That(world.Resources.Get(ResourceType.Coal), Is.Zero);
			Assert.That(world.Resources.Get(ResourceType.Steel), Is.EqualTo(10));
			Assert.That(world.FuelBackedPowerCapacity, Is.EqualTo(4));
			Assert.That(world.LastPowerStatus.FuelBackedCapacitySeconds, Is.EqualTo(40));
		}
	}
}
