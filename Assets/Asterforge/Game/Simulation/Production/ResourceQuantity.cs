using System;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Production
{
	public readonly struct ResourceQuantity
	{
		public ResourceType Resource { get; }

		public long Amount { get; }

		public ResourceQuantity(ResourceType resource, long amount)
		{
			if (amount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(amount), amount, "Resource quantities must be positive.");
			}

			Resource = resource;
			Amount = amount;
		}
	}
}
