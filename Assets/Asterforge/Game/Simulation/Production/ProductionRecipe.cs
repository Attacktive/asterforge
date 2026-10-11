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

		public ProductionRecipe(MachineType machine, ResourceQuantity[] inputs, ResourceQuantity output)
		{
			Machine = machine;
			this.inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
			Output = output;
		}
	}
}
