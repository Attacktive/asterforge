using System;
using Asterforge.Game.Persistence;
using Asterforge.Game.Simulation.Progression;
using UnityEngine;

namespace Asterforge.Unity.Presentation
{
	public sealed class OutpostPrototype : MonoBehaviour
	{
		private float elapsedSimulationSeconds;

		[SerializeField] private OutpostSnapshot reloadSnapshot;
		private OutpostSession session;

		public OutpostSession Session
		{
			get
			{
				if (session == null)
				{
					session = new OutpostSession();
				}

				return session;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (FindAnyObjectByType<OutpostPrototype>() != null)
			{
				return;
			}

			var prototypeObject = new GameObject("Asterforge Outpost");
			DontDestroyOnLoad(prototypeObject);
			prototypeObject.AddComponent<OutpostPrototype>();
		}

		private void Awake()
		{
			if (GetComponent<OutpostDashboard>() == null)
			{
				gameObject.AddComponent<OutpostDashboard>();
			}
		}

		private void OnEnable()
		{
			if (session != null || reloadSnapshot == null)
			{
				// A surviving session may have accepted commands while this component was disabled.
				reloadSnapshot = null;

				return;
			}

			try
			{
				Restore(reloadSnapshot, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"Asterforge could not restore its script-reload snapshot. {exception.Message}");
			}

			reloadSnapshot = null;
		}

		private void OnDisable()
		{
			if (session != null)
			{
				reloadSnapshot = session.CaptureSnapshot(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
			}
		}

		private void Update()
		{
			elapsedSimulationSeconds += Time.unscaledDeltaTime;
			var wholeSeconds = (long)elapsedSimulationSeconds;
			if (wholeSeconds <= 0)
			{
				return;
			}

			elapsedSimulationSeconds -= wholeSeconds;
			Session.Advance(wholeSeconds);
		}

		public long Restore(OutpostSnapshot snapshot, long nowUnixSeconds)
		{
			var offlineSeconds = Session.Restore(snapshot, nowUnixSeconds);
			elapsedSimulationSeconds = 0f;

			return offlineSeconds;
		}
	}
}
