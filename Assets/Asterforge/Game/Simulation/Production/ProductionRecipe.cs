using System;
using System.Collections.Generic;

namespace Asterforge.Game.Simulation.Production
{
	public sealed class ProductionRecipe
	{
		private readonly ResourceQuantity[] inputs;

		public MachineType Machine { get; }

		public IReadOnlyList<ResourceQuantity> Inputs => inputs;

		public ResourceQuantity Output { get; }

		public int PowerDemand { get; }

		public ProductionRecipe(MachineType machine, ResourceQuantity[] inputs, ResourceQuantity output, int powerDemand = 0)
		{
			if (powerDemand < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(powerDemand), powerDemand, "Power demand cannot be negative.");
			}

			Machine = machine;
			this.inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
			Output = output;
			PowerDemand = powerDemand;
		}
	}
}
