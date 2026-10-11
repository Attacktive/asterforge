using Asterforge.Game.Simulation.Progression;

namespace Asterforge.Game.Persistence
{
	public sealed class OutpostLoadResult
	{
		public OutpostLoadResult(OutpostWorld world, long offlineSeconds)
		{
			World = world;
			OfflineSeconds = offlineSeconds;
		}

		public OutpostWorld World { get; }

		public long OfflineSeconds { get; }
	}
}
