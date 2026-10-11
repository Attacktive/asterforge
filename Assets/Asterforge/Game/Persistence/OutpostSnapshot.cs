using System;

namespace Asterforge.Game.Persistence
{
	[Serializable]
	public sealed class OutpostSnapshot
	{
		public const int CurrentVersion = 1;

		public int Version = CurrentVersion;
		public long SavedAtUnixSeconds;
		public long[] ResourceAmounts = Array.Empty<long>();
		public int[] MachineCounts = Array.Empty<int>();
		public bool IsIndustrialized;
	}
}
