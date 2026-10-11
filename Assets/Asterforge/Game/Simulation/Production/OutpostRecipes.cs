using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Resources;

namespace Asterforge.Game.Simulation.Production
{
	public static class OutpostRecipes
	{
		private static readonly IReadOnlyList<ProductionRecipe> recipes = Array.AsReadOnly(
			new[]
			{
				new ProductionRecipe(
					MachineType.Extractor,
					Array.Empty<ResourceQuantity>(),
					new ResourceQuantity(ResourceType.Ore, 1)
				),
				new ProductionRecipe(
					MachineType.Smelter,
					new[]
					{
						new ResourceQuantity(ResourceType.Ore, 2)
					},
					new ResourceQuantity(ResourceType.Metal, 1)
				),
				new ProductionRecipe(
					MachineType.Assembler,
					new[]
					{
						new ResourceQuantity(ResourceType.Metal, 3)
					},
					new ResourceQuantity(ResourceType.Components, 1)
				),
				new ProductionRecipe(
					MachineType.ResearchStation,
					new[]
					{
						new ResourceQuantity(ResourceType.Components, 2)
					},
					new ResourceQuantity(ResourceType.Research, 1)
				),
				new ProductionRecipe(
					MachineType.CoalMine,
					Array.Empty<ResourceQuantity>(),
					new ResourceQuantity(ResourceType.Coal, 1)
				),
				new ProductionRecipe(
					MachineType.Steelworks,
					new[]
					{
						new ResourceQuantity(ResourceType.Metal, 4),
						new ResourceQuantity(ResourceType.Coal, 2)
					},
					new ResourceQuantity(ResourceType.Steel, 1)
				)
			}
		);

		public static IReadOnlyList<ProductionRecipe> All => recipes;
	}
}
