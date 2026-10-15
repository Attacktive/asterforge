using System.Collections.Generic;
using Asterforge.Game.Simulation.Production;
using Asterforge.Game.Simulation.Resources;
using NUnit.Framework;

namespace Asterforge.Game.Tests.Production
{
	public sealed class PoweredBrownoutAllocationTests
	{
		[Test]
		public void Advance_WithTwoPoweredRecipes_IsProportionalOrderAndBatchInvariant()
		{
			var smelterRecipe = new ProductionRecipe(
				MachineType.Smelter,
				new ResourceQuantity[0],
				new ResourceQuantity(ResourceType.Metal, 1),
				4
			);
			var researchRecipe = new ProductionRecipe(
				MachineType.ResearchStation,
				new ResourceQuantity[0],
				new ResourceQuantity(ResourceType.Research, 1),
				4
			);
			var orderedRecipes = new[]
			{
				smelterRecipe,
				researchRecipe
			};
			var reversedRecipes = new[]
			{
				researchRecipe,
				smelterRecipe
			};
			var powerGrid = CreatePowerGrid(4);

			var batched = RunScenario(orderedRecipes, powerGrid, 10, false);
			var stepped = RunScenario(orderedRecipes, powerGrid, 10, true);
			var reversedBatched = RunScenario(reversedRecipes, powerGrid, 10, false);
			var reversedStepped = RunScenario(reversedRecipes, powerGrid, 10, true);

			AssertEquivalent(batched, stepped);
			AssertEquivalent(batched, reversedBatched);
			AssertEquivalent(batched, reversedStepped);
			Assert.That(batched.Metal, Is.EqualTo(5));
			Assert.That(batched.Research, Is.EqualTo(5));
			Assert.That(batched.FuelConsumed, Is.EqualTo(10));
			Assert.That(batched.SuppliedCapacitySeconds, Is.EqualTo(40));
		}

		[Test]
		public void Advance_WithUnequalPoweredRecipes_PreservesFractionalSharesAcrossPartitions()
		{
			var recipes = CreateUnequalRecipes();
			var powerGrid = CreatePowerGrid(1);

			var batched = RunScenario(recipes, powerGrid, 3, false);
			var stepped = RunScenario(recipes, powerGrid, 3, true);

			AssertEquivalent(batched, stepped);
			Assert.That(batched.Metal, Is.EqualTo(1));
			Assert.That(batched.Research, Is.EqualTo(1));
			Assert.That(batched.FuelConsumed, Is.EqualTo(3));
			Assert.That(batched.SuppliedCapacitySeconds, Is.EqualTo(3));
		}

		[Test]
		public void RestorePoweredState_WithUnequalPoweredRecipes_ContinuesAllocationCycle()
		{
			var recipes = CreateUnequalRecipes();
			var powerGrid = CreatePowerGrid(1);
			var resources = new ResourceLedger();
			var machines = CreateMachines();
			var simulator = new ProductionSimulator(recipes, powerGrid);
			resources.Add(ResourceType.Coal, 20);
			simulator.Advance(resources, machines, 1);

			var restoredResources = new ResourceLedger();
			restoredResources.Add(ResourceType.Coal, resources.Get(ResourceType.Coal));
			restoredResources.Add(ResourceType.Metal, resources.Get(ResourceType.Metal));
			restoredResources.Add(ResourceType.Research, resources.Get(ResourceType.Research));
			var restoredMachines = CreateMachines();
			var restoredSimulator = new ProductionSimulator(recipes, powerGrid);
			restoredSimulator.RestorePoweredState(
				simulator.CapturePoweredRunRemainders(),
				simulator.CapturePoweredAllocationWeights(),
				simulator.CapturePoweredAllocationPhase(),
				simulator.CaptureFuelCreditUnits()
			);

			var uninterruptedStatus = simulator.Advance(resources, machines, 1);
			var restoredStatus = restoredSimulator.Advance(restoredResources, restoredMachines, 1);

			Assert.That(restoredResources.Get(ResourceType.Coal), Is.EqualTo(resources.Get(ResourceType.Coal)));
			Assert.That(restoredResources.Get(ResourceType.Metal), Is.EqualTo(resources.Get(ResourceType.Metal)));
			Assert.That(restoredResources.Get(ResourceType.Research), Is.EqualTo(resources.Get(ResourceType.Research)));
			Assert.That(restoredStatus.FuelConsumed, Is.EqualTo(uninterruptedStatus.FuelConsumed));
			Assert.That(restoredStatus.SuppliedCapacitySeconds, Is.EqualTo(uninterruptedStatus.SuppliedCapacitySeconds));
			Assert.That(restoredSimulator.CapturePoweredRunRemainders(), Is.EqualTo(simulator.CapturePoweredRunRemainders()));
			Assert.That(restoredSimulator.CapturePoweredAllocationWeights(), Is.EqualTo(simulator.CapturePoweredAllocationWeights()));
			Assert.That(restoredSimulator.CapturePoweredAllocationPhase(), Is.EqualTo(simulator.CapturePoweredAllocationPhase()));
			Assert.That(restoredSimulator.CaptureFuelCreditUnits(), Is.EqualTo(simulator.CaptureFuelCreditUnits()));
			Assert.That(resources.Get(ResourceType.Coal), Is.EqualTo(18));
			Assert.That(resources.Get(ResourceType.Metal), Is.EqualTo(1));
			Assert.That(resources.Get(ResourceType.Research), Is.Zero);
		}

		private static IReadOnlyList<ProductionRecipe> CreateUnequalRecipes()
		{
			return new[]
			{
				new ProductionRecipe(
					MachineType.Smelter,
					new ResourceQuantity[0],
					new ResourceQuantity(ResourceType.Metal, 1),
					1
				),
				new ProductionRecipe(
					MachineType.ResearchStation,
					new ResourceQuantity[0],
					new ResourceQuantity(ResourceType.Research, 1),
					2
				)
			};
		}

		private static PowerGrid CreatePowerGrid(int capacityPerGenerator)
		{
			return new PowerGrid(
				MachineType.CoalGenerator,
				capacityPerGenerator,
				new ResourceQuantity(ResourceType.Coal, 1)
			);
		}

		private static MachineFleet CreateMachines()
		{
			var machines = new MachineFleet();
			machines.Add(MachineType.CoalGenerator);
			machines.Add(MachineType.Smelter);
			machines.Add(MachineType.ResearchStation);

			return machines;
		}

		private static BrownoutResult RunScenario(IReadOnlyList<ProductionRecipe> recipes, PowerGrid powerGrid, long elapsedSeconds, bool stepped)
		{
			var resources = new ResourceLedger();
			var machines = CreateMachines();
			var simulator = new ProductionSimulator(recipes, powerGrid);
			resources.Add(ResourceType.Coal, 20);
			var fuelConsumed = 0L;
			var suppliedCapacitySeconds = 0L;
			if (stepped)
			{
				for (var second = 0L; second < elapsedSeconds; second++)
				{
					var status = simulator.Advance(resources, machines, 1);
					fuelConsumed += status.FuelConsumed;
					suppliedCapacitySeconds += status.SuppliedCapacitySeconds;
				}
			}
			else
			{
				var status = simulator.Advance(resources, machines, elapsedSeconds);
				fuelConsumed = status.FuelConsumed;
				suppliedCapacitySeconds = status.SuppliedCapacitySeconds;
			}

			return new BrownoutResult(
				resources.Get(ResourceType.Coal),
				resources.Get(ResourceType.Metal),
				resources.Get(ResourceType.Research),
				fuelConsumed,
				suppliedCapacitySeconds,
				simulator.CapturePoweredRunRemainders(),
				simulator.CapturePoweredAllocationWeights(),
				simulator.CapturePoweredAllocationPhase(),
				simulator.CaptureFuelCreditUnits()
			);
		}

		private static void AssertEquivalent(BrownoutResult expected, BrownoutResult actual)
		{
			Assert.That(actual.Coal, Is.EqualTo(expected.Coal));
			Assert.That(actual.Metal, Is.EqualTo(expected.Metal));
			Assert.That(actual.Research, Is.EqualTo(expected.Research));
			Assert.That(actual.FuelConsumed, Is.EqualTo(expected.FuelConsumed));
			Assert.That(actual.SuppliedCapacitySeconds, Is.EqualTo(expected.SuppliedCapacitySeconds));
			Assert.That(actual.PoweredRunRemainders, Is.EqualTo(expected.PoweredRunRemainders));
			Assert.That(actual.PoweredAllocationWeights, Is.EqualTo(expected.PoweredAllocationWeights));
			Assert.That(actual.PoweredAllocationPhase, Is.EqualTo(expected.PoweredAllocationPhase));
			Assert.That(actual.FuelCreditUnits, Is.EqualTo(expected.FuelCreditUnits));
		}

		private sealed class BrownoutResult
		{
			public long Coal { get; }

			public long Metal { get; }

			public long Research { get; }

			public long FuelConsumed { get; }

			public long SuppliedCapacitySeconds { get; }

			public long[] PoweredRunRemainders { get; }

			public long[] PoweredAllocationWeights { get; }

			public long PoweredAllocationPhase { get; }

			public long FuelCreditUnits { get; }

			public BrownoutResult(long coal, long metal, long research, long fuelConsumed, long suppliedCapacitySeconds, long[] poweredRunRemainders, long[] poweredAllocationWeights, long poweredAllocationPhase, long fuelCreditUnits)
			{
				Coal = coal;
				Metal = metal;
				Research = research;
				FuelConsumed = fuelConsumed;
				SuppliedCapacitySeconds = suppliedCapacitySeconds;
				PoweredRunRemainders = poweredRunRemainders;
				PoweredAllocationWeights = poweredAllocationWeights;
				PoweredAllocationPhase = poweredAllocationPhase;
				FuelCreditUnits = fuelCreditUnits;
			}
		}
	}
}
