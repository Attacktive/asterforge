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
		public void CaptureSnapshot_ThenRestore_PreservesPoweredAllocationCycleState()
		{
			var world = new OutpostWorld();
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 40);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks, 2);
			world.Advance(1);

			var snapshot = world.CaptureSnapshot(100);
			var restoredSnapshot = OutpostWorld.Restore(snapshot).CaptureSnapshot(100);

			Assert.That(snapshot.PoweredAllocationWeights[(int)MachineType.Steelworks], Is.EqualTo(8));
			Assert.That(snapshot.PoweredAllocationPhase, Is.EqualTo(4));
			Assert.That(restoredSnapshot.PoweredRunRemainders, Is.EqualTo(snapshot.PoweredRunRemainders));
			Assert.That(restoredSnapshot.PoweredAllocationWeights, Is.EqualTo(snapshot.PoweredAllocationWeights));
			Assert.That(restoredSnapshot.PoweredAllocationPhase, Is.EqualTo(snapshot.PoweredAllocationPhase));
			Assert.That(restoredSnapshot.PowerFuelCreditUnits, Is.EqualTo(snapshot.PowerFuelCreditUnits));
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
		public void RestoreWithOfflineProgress_PreservesFractionalPoweredProductionState()
		{
			var world = new OutpostWorld();
			world.Resources.Add(ResourceType.Metal, 40);
			world.Resources.Add(ResourceType.Coal, 40);
			world.Machines.Add(MachineType.Smelter);
			world.Machines.Add(MachineType.CoalMine);
			world.Machines.Add(MachineType.CoalGenerator);
			world.Machines.Add(MachineType.Steelworks, 3);
			world.Advance(1);
			var snapshot = world.CaptureSnapshot(100);

			world.Advance(1);
			var restored = OutpostWorld.RestoreWithOfflineProgress(snapshot, 101).World;

			Assert.That(restored.Resources.Get(ResourceType.Metal), Is.EqualTo(world.Resources.Get(ResourceType.Metal)));
			Assert.That(restored.Resources.Get(ResourceType.Coal), Is.EqualTo(world.Resources.Get(ResourceType.Coal)));
			Assert.That(restored.Resources.Get(ResourceType.Steel), Is.EqualTo(world.Resources.Get(ResourceType.Steel)));
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
		public void Restore_WithLegacySnapshotVersion_Throws()
		{
			var snapshot = new OutpostSnapshot
			{
				Version = OutpostSnapshot.CurrentVersion - 1,
				SavedAtUnixSeconds = 100,
				ResourceAmounts = new long[Enum.GetValues(typeof(ResourceType)).Length],
				MachineCounts = new int[Enum.GetValues(typeof(MachineType)).Length]
			};

			Assert.Throws<InvalidOperationException>(() => OutpostWorld.Restore(snapshot));
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
