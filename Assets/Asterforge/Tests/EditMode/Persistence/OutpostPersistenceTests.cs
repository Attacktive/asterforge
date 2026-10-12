using System;
using Asterforge.Game.Persistence;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Progression;
using Asterforge.Game.Simulation.Resources;
using NUnit.Framework;

namespace Asterforge.Game.Tests.Persistence
{
	public sealed class OutpostPersistenceTests
	{
		[Test]
		public void CaptureSnapshot_ThenRestore_PreservesWorldState()
		{
			var world = new OutpostWorld();
			world.Resources.Add(ResourceType.Ore, 123);
			world.Resources.Add(ResourceType.Research, 125);
			world.Machines.Add(MachineType.Extractor, 3);
			world.Machines.Add(MachineType.Smelter, 2);
			world.Machines.Add(MachineType.CoalGenerator);
			world.TryIndustrialize();

			var snapshot = world.CaptureSnapshot(1_000);
			var restored = OutpostWorld.Restore(snapshot);

			Assert.That(restored.Resources.Get(ResourceType.Ore), Is.EqualTo(123));
			Assert.That(restored.Resources.Get(ResourceType.Research), Is.EqualTo(25));
			Assert.That(restored.Machines.Get(MachineType.Extractor), Is.EqualTo(3));
			Assert.That(restored.Machines.Get(MachineType.Smelter), Is.EqualTo(2));
			Assert.That(restored.Machines.Get(MachineType.CoalGenerator), Is.EqualTo(1));
			Assert.That(restored.IsIndustrialized, Is.True);
		}

		[Test]
		public void RestoreWithOfflineProgress_AdvancesWorldByElapsedSeconds()
		{
			var world = new OutpostWorld();
			world.Machines.Add(MachineType.Extractor);
			var snapshot = world.CaptureSnapshot(100);

			var result = OutpostWorld.RestoreWithOfflineProgress(snapshot, 112);

			Assert.That(result.OfflineSeconds, Is.EqualTo(12));
			Assert.That(result.World.Resources.Get(ResourceType.Ore), Is.EqualTo(12));
		}

		[Test]
		public void RestoreWithOfflineProgress_WithFutureSaveTimestamp_DoesNotReverseSimulation()
		{
			var world = new OutpostWorld();
			world.Machines.Add(MachineType.Extractor);
			var snapshot = world.CaptureSnapshot(200);

			var result = OutpostWorld.RestoreWithOfflineProgress(snapshot, 150);

			Assert.That(result.OfflineSeconds, Is.Zero);
			Assert.That(result.World.Resources.Get(ResourceType.Ore), Is.Zero);
		}

		[Test]
		public void Restore_WithVersionOneSnapshot_DefaultsCoalGeneratorsToZero()
		{
			var machineCounts = new int[Enum.GetValues(typeof(MachineType)).Length - 1];
			machineCounts[(int)MachineType.Extractor] = 2;
			var snapshot = new OutpostSnapshot
			{
				Version = OutpostSnapshot.VersionWithoutPower,
				SavedAtUnixSeconds = 100,
				ResourceAmounts = new long[Enum.GetValues(typeof(ResourceType)).Length],
				MachineCounts = machineCounts,
				IsIndustrialized = true
			};

			var restored = OutpostWorld.Restore(snapshot);

			Assert.That(restored.Machines.Get(MachineType.Extractor), Is.EqualTo(2));
			Assert.That(restored.Machines.Get(MachineType.CoalGenerator), Is.Zero);
			Assert.That(restored.IsIndustrialized, Is.True);
		}

		[Test]
		public void Restore_WithUnsupportedSnapshotVersion_Throws()
		{
			var snapshot = new OutpostSnapshot
			{
				Version = OutpostSnapshot.CurrentVersion + 1,
				SavedAtUnixSeconds = 100,
				ResourceAmounts = new long[Enum.GetValues(typeof(ResourceType)).Length],
				MachineCounts = new int[Enum.GetValues(typeof(MachineType)).Length]
			};

			Assert.Throws<InvalidOperationException>(() => OutpostWorld.Restore(snapshot));
		}
	}
}
