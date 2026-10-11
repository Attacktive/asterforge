using System;

namespace Asterforge.Game.Simulation.Resources
{
	public sealed class ResourceLedger
	{
		private readonly long[] amounts = new long[Enum.GetValues(typeof(ResourceType)).Length];

		public long Get(ResourceType resource)
		{
			return amounts[(int)resource];
		}

		public void Add(ResourceType resource, long amount)
		{
			if (amount < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(amount), amount, "Resource additions cannot be negative.");
			}

			checked
			{
				amounts[(int)resource] += amount;
			}
		}

		public bool TrySpend(ResourceType resource, long amount)
		{
			if (amount < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(amount), amount, "Resource spending cannot be negative.");
			}

			var index = (int)resource;
			if (amounts[index] < amount)
			{
				return false;
			}

			amounts[index] -= amount;

			return true;
		}
	}
}
