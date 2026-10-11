using System;

namespace Asterforge.Game.Simulation.Production
{
	public sealed class MachineFleet
	{
		private readonly int[] counts = new int[Enum.GetValues(typeof(MachineType)).Length];
		private readonly bool[] paused = new bool[Enum.GetValues(typeof(MachineType)).Length];

		public int Get(MachineType machine)
		{
			return counts[(int)machine];
		}

		public int GetActive(MachineType machine)
		{
			if (IsPaused(machine))
			{
				return 0;
			}

			return Get(machine);
		}

		public bool IsPaused(MachineType machine)
		{
			return paused[(int)machine];
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

		public void SetPaused(MachineType machine, bool isPaused)
		{
			paused[(int)machine] = isPaused;
		}
	}
}
