using System;
using System.Collections.Generic;
using Asterforge.Game.Simulation.Production;

namespace Asterforge.Unity.Presentation
{
	public static class FacilityManagers
	{
		private static readonly FacilityManagerDefinition rheaVale = new FacilityManagerDefinition(
			MachineType.Extractor,
			"Rhea Vale",
			"Extraction Foreman",
			"Finds the resources that keep us moving.",
			"RV",
			"Managers/rhea_vale"
		);

		private static readonly FacilityManagerDefinition tomaSato = new FacilityManagerDefinition(
			MachineType.Smelter,
			"Toma Sato",
			"Furnace Chief",
			"Turns raw materials into opportunity.",
			"TS",
			"Managers/toma_sato"
		);

		private static readonly FacilityManagerDefinition maraQuinn = new FacilityManagerDefinition(
			MachineType.Assembler,
			"Mara Quinn",
			"Assembly Lead",
			"Keeps production precise and efficient.",
			"MQ",
			"Managers/mara_quinn"
		);

		private static readonly FacilityManagerDefinition ivoMercer = new FacilityManagerDefinition(
			MachineType.ResearchStation,
			"Dr. Ivo Mercer",
			"Research Director",
			"Uncovers a brighter and stronger tomorrow.",
			"IM",
			"Managers/ivo_mercer"
		);

		private static readonly FacilityManagerDefinition nikaDrazic = new FacilityManagerDefinition(
			MachineType.CoalMine,
			"Nika Drazic",
			"Mining Supervisor",
			"Keeps our fuel, people and equipment running.",
			"ND",
			"Managers/nika_drazic"
		);

		private static readonly FacilityManagerDefinition eliasVoss = new FacilityManagerDefinition(
			MachineType.Steelworks,
			"Elias Voss",
			"Steelworks Superintendent",
			"Builds the foundation for a stronger colony.",
			"EV",
			"Managers/elias_voss"
		);

		private static readonly IReadOnlyList<FacilityManagerDefinition> all = Array.AsReadOnly(
			new[]
			{
				rheaVale,
				tomaSato,
				maraQuinn,
				ivoMercer,
				nikaDrazic,
				eliasVoss
			}
		);

		public static IReadOnlyList<FacilityManagerDefinition> All => all;

		public static FacilityManagerDefinition Get(MachineType machine)
		{
			return machine switch
			{
				MachineType.Extractor => rheaVale,
				MachineType.Smelter => tomaSato,
				MachineType.Assembler => maraQuinn,
				MachineType.ResearchStation => ivoMercer,
				MachineType.CoalMine => nikaDrazic,
				MachineType.Steelworks => eliasVoss,
				_ => throw new ArgumentOutOfRangeException(nameof(machine), machine, "Unknown facility manager assignment.")
			};
		}
	}
}
