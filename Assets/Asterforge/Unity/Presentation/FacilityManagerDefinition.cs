using Asterforge.Game.Simulation.Production;

namespace Asterforge.Unity.Presentation
{
	public sealed class FacilityManagerDefinition
	{
		public FacilityManagerDefinition(MachineType machine, string name, string role, string tagline, string initials, string portraitResourcePath)
		{
			Machine = machine;
			Name = name;
			Role = role;
			Tagline = tagline;
			Initials = initials;
			PortraitResourcePath = portraitResourcePath;
		}

		public MachineType Machine { get; }

		public string Name { get; }

		public string Role { get; }

		public string Tagline { get; }

		public string Initials { get; }

		public string PortraitResourcePath { get; }
	}
}
