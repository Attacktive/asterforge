using System;

namespace Asterforge.Game.Persistence
{
	[Serializable]
	public sealed class OutpostSnapshot
	{
		public const int CurrentVersion = 6;

		public int Version = CurrentVersion;
		public long SavedAtUnixSeconds;
		public long[] ResourceAmounts = Array.Empty<long>();
		public int[] MachineCounts = Array.Empty<int>();
		public long[] PoweredRunRemainders = Array.Empty<long>();
		public long[] PoweredAllocationWeights = Array.Empty<long>();
		public long PoweredAllocationPhase;
		public long PowerFuelCreditUnits;
		public bool IsIndustrialized;
	}
}
