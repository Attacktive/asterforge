using System;
using Asterforge.Game.Simulation.Resources;
using NUnit.Framework;

namespace Asterforge.Game.Tests
{
	public sealed class ResourceLedgerTests
	{
		[Test]
		public void Add_StoresResourceAmount()
		{
			var ledger = new ResourceLedger();
			ledger.Add(ResourceType.Ore, 25);

			Assert.That(ledger.Get(ResourceType.Ore), Is.EqualTo(25));
		}

		[Test]
		public void TrySpend_WhenEnoughResources_DeductsAmount()
		{
			var ledger = new ResourceLedger();
			ledger.Add(ResourceType.Metal, 40);

			var spent = ledger.TrySpend(ResourceType.Metal, 15);

			Assert.That(spent, Is.True);
			Assert.That(ledger.Get(ResourceType.Metal), Is.EqualTo(25));
		}

		[Test]
		public void TrySpend_WhenResourcesAreInsufficient_DoesNotChangeBalance()
		{
			var ledger = new ResourceLedger();
			ledger.Add(ResourceType.Components, 3);

			var spent = ledger.TrySpend(ResourceType.Components, 4);

			Assert.That(spent, Is.False);
			Assert.That(ledger.Get(ResourceType.Components), Is.EqualTo(3));
		}

		[Test]
		public void Add_WhenAmountIsNegative_Throws()
		{
			var ledger = new ResourceLedger();

			Assert.Throws<ArgumentOutOfRangeException>(() => ledger.Add(ResourceType.Research, -1));
		}
	}
}
