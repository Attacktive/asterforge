using System;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Progression
{
	public readonly struct ResourceCost
	{
		public ResourceType Resource { get; }

		public long Amount { get; }

		public ResourceCost(ResourceType resource, long amount)
		{
			if (amount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(amount), amount, "Resource costs must be positive.");
			}

			Resource = resource;
			Amount = amount;
		}
	}
}
