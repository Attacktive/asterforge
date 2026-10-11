using System;

namespace Asterforge.Game.Simulation.Production
{
	public sealed class MachineFleet
	{
		private readonly int[] counts = new int[Enum.GetValues(typeof(MachineType)).Length];

		public int Get(MachineType machine)
		{
			return counts[(int)machine];
		}

		public void Add(MachineType machine, int count = 1)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count, "Machine additions cannot be negative.");
			}

			checked
			{
				counts[(int)machine] += count;
			}
		}
	}
}
