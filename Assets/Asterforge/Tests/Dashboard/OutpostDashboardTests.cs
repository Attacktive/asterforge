using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Progression;
using Asterforge.Game.Simulation.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Asterforge.Tests.Dashboard
{
	public sealed class OutpostDashboardTests
	{
		[TestCase(MachineType.Extractor)]
		[TestCase(MachineType.Smelter)]
		[TestCase(MachineType.ResearchStation)]
		[TestCase(MachineType.CoalMine)]
		[TestCase(MachineType.CoalGenerator)]
		[TestCase(MachineType.Steelworks)]
		public void BatchCostMatchesSequentialPurchases(MachineType machine)
		{
			for (var owned = 0; owned < 20; owned++)
			{
				foreach (var quantity in new[] { 1, 2, 10, 100 })
				{
					var expected = 0L;
					for (var index = 0; index < quantity; index++)
					{
						expected += OutpostRules.GetMachineCost(machine, owned + index)[0].Amount;
					}

					Assert.That(OutpostRules.GetMachineCost(machine, owned, quantity)[0].Amount, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void OddPricesKeepPerUnitRounding()
		{
			Assert.That(OutpostRules.GetMachineCost(MachineType.Smelter, 0, 10)[0].Amount, Is.EqualTo(815));
		}

		[Test]
		public void InsufficientFixedBatchDoesNotPartiallySpendOrBuild()
		{
			var session = WithOre(324);
			Assert.That(session.TryBuyMachines(MachineType.Extractor, 10), Is.False);
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(324));
			Assert.That(session.GetMachineCount(MachineType.Extractor), Is.Zero);
		}

		[Test]
		public void ExactFixedBatchSpendsTheWholeEscalatingCost()
		{
			var session = WithOre(325);
			Assert.That(session.TryBuyMachines(MachineType.Extractor, 10), Is.True);
			Assert.That(session.GetMachineCount(MachineType.Extractor), Is.EqualTo(10));
			Assert.That(session.GetResource(ResourceType.Ore), Is.Zero);
		}

		[TestCase(0, 0, 0)]
		[TestCase(9, 0, 9)]
		[TestCase(10, 1, 0)]
		[TestCase(324, 9, 54)]
		[TestCase(325, 10, 0)]
		public void MaxBuysOnlyAffordableUnits(long ore, int expectedCount, long remaining)
		{
			var session = WithOre(ore);
			Assert.That(session.QuotePurchase(MachineType.Extractor, OutpostSession.BuyMax).Quantity, Is.EqualTo(expectedCount));
			session.TryBuyMachines(MachineType.Extractor, OutpostSession.BuyMax);
			Assert.That(session.GetMachineCount(MachineType.Extractor), Is.EqualTo(expectedCount));
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(remaining));
		}

		[Test]
		public void MaxHandlesLedgerSizedBalancesWithoutOverflow()
		{
			var session = WithOre(long.MaxValue);
			var quote = session.QuotePurchase(MachineType.Extractor, OutpostSession.BuyMax);
			Assert.That(quote.CanPurchase, Is.True);
			Assert.That(quote.Quantity, Is.GreaterThan(1000000));
			Assert.That(quote.Costs[0].Amount, Is.LessThanOrEqualTo(long.MaxValue));
			Assert.Throws<OverflowException>(() => OutpostRules.GetMachineCost(MachineType.Extractor, 0, quote.Quantity + 1));
			Assert.That(session.TryBuyMachines(MachineType.Extractor, OutpostSession.BuyMax), Is.True);
			Assert.That(session.GetResource(ResourceType.Ore), Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void LockedMachinesCannotBeBatchPurchased()
		{
			var session = WithOre(10000);
			Assert.That(session.QuotePurchase(MachineType.Smelter, 10).Block, Is.EqualTo(PurchaseBlock.Locked));
			Assert.That(session.TryBuyMachines(MachineType.Smelter, OutpostSession.BuyMax), Is.False);
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(10000));
		}

		[Test]
		public void ExecutionRechecksBalancesInsteadOfTrustingAnOldQuote()
		{
			var session = WithOre(325);
			Assert.That(session.QuotePurchase(MachineType.Extractor, 10).CanPurchase, Is.True);
			Assert.That(session.TryBuyMachines(MachineType.Extractor, 1), Is.True);
			Assert.That(session.TryBuyMachines(MachineType.Extractor, 10), Is.False);
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(315));
			Assert.That(session.GetMachineCount(MachineType.Extractor), Is.EqualTo(1));
		}

		[Test]
		public void MachineCountLimitCannotWrapOrSpendResources()
		{
			var world = new OutpostWorld();
			world.Machines.Add(MachineType.Extractor, int.MaxValue);
			world.Resources.Add(ResourceType.Ore, 100);
			var session = FromWorld(world);
			Assert.That(session.QuotePurchase(MachineType.Extractor, OutpostSession.BuyMax).Block, Is.EqualTo(PurchaseBlock.MachineLimit));
			Assert.That(session.TryBuyMachines(MachineType.Extractor, 1), Is.False);
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(100));
		}

		[Test]
		public void NegativeBatchIsRejected()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new OutpostSession().QuotePurchase(MachineType.Extractor, -1));
		}

		[Test]
		public void QuotesDoNotMutateStateAndCostsAreReadOnly()
		{
			var session = WithOre(325);
			var revision = session.Revision;
			var quote = session.QuotePurchase(MachineType.Extractor, 10);
			Assert.That(session.Revision, Is.EqualTo(revision));
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(325));
			Assert.That(((ICollection<ResourceCost>)quote.Costs).IsReadOnly, Is.True);
		}

		[Test]
		public void EstimatesWaitOnlyWhenMissingResourcesHavePositiveNetProduction()
		{
			var session = new OutpostSession();
			Assert.That(session.QuotePurchase(MachineType.Extractor, 1).SecondsUntilAffordable, Is.Null);
			var world = new OutpostWorld();
			world.Machines.Add(MachineType.Smelter);
			session = FromWorld(world);
			session.Advance(1);
			Assert.That(session.QuotePurchase(MachineType.ResearchStation, 1).SecondsUntilAffordable, Is.EqualTo(19));
		}

		[TestCase(3, 3, 1, 1, SteelBottleneck.Metal)]
		[TestCase(4, 2, 1, 1, SteelBottleneck.Coal)]
		[TestCase(4, 3, 0, 1, SteelBottleneck.Power)]
		[TestCase(4, 3, 1, 0, SteelBottleneck.Steelworks)]
		[TestCase(4, 3, 1, 1, SteelBottleneck.None)]
		public void BottlenecksComeFromTheActualEconomy(int smelters, int mines, int generators, int steelworks, SteelBottleneck expected)
		{
			var world = Industry(smelters, mines, generators, steelworks);
			Assert.That(FromWorld(world).GetSteelBottlenecks(1), Is.EqualTo(expected));
		}

		[Test]
		public void StockpilesDoNotHideUnsustainableProduction()
		{
			var world = Industry(1, 1, 1, 1);
			world.Resources.Add(ResourceType.Metal, 1000);
			world.Resources.Add(ResourceType.Coal, 1000);
			var session = FromWorld(world);
			session.Advance(1);
			Assert.That(session.GetNetRate(ResourceType.Steel), Is.EqualTo(1));
			Assert.That(session.SteelCapability.SustainableRate, Is.EqualTo(0.25));
			Assert.That(session.GetSteelBottlenecks(1), Is.EqualTo(SteelBottleneck.Metal | SteelBottleneck.Coal));
		}

		[Test]
		public void ExistingSnapshotsRestoreOfflineProgressWithoutChangingTheirFormat()
		{
			var world = new OutpostWorld();
			world.Machines.Add(MachineType.Extractor, 2);
			var saved = world.CaptureSnapshot(100);
			var session = new OutpostSession();
			Assert.That(session.Restore(saved, 110), Is.EqualTo(10));
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(20));
			Assert.That(session.CaptureSnapshot(110).Version, Is.EqualTo(saved.Version));
		}

		[Test]
		public void FailedRestoreLeavesTheCurrentSessionIntact()
		{
			var session = WithOre(10);
			var saved = session.CaptureSnapshot(100);
			saved.Version++;
			Assert.Throws<InvalidOperationException>(() => session.Restore(saved, 110));
			Assert.That(session.GetResource(ResourceType.Ore), Is.EqualTo(10));
		}

		[Test]
		public void DashboardStyleAssetsAreIncludedAndImportable()
		{
			Assert.That(Resources.Load<ThemeStyleSheet>("OutpostTheme"), Is.Not.Null);
			Assert.That(Resources.Load<StyleSheet>("OutpostDashboard"), Is.Not.Null);
		}

		private static OutpostSession WithOre(long ore)
		{
			var world = new OutpostWorld();
			world.Resources.Add(ResourceType.Ore, ore);

			return FromWorld(world);
		}

		private static OutpostSession FromWorld(OutpostWorld world)
		{
			var session = new OutpostSession();
			session.Restore(world.CaptureSnapshot(0), 0);

			return session;
		}

		private static OutpostWorld Industry(int smelters, int mines, int generators, int steelworks)
		{
			var world = new OutpostWorld();
			world.Resources.Add(ResourceType.Research, 100);
			world.TryIndustrialize();
			world.Machines.Add(MachineType.Smelter, smelters);
			world.Machines.Add(MachineType.CoalMine, mines);
			world.Machines.Add(MachineType.CoalGenerator, generators);
			world.Machines.Add(MachineType.Steelworks, steelworks);

			return world;
		}
	}
}
