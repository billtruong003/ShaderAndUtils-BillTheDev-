#region Namespaces

using System.Collections;
using UnityEngine;

#endregion

namespace MVC
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(AudioSource))]
	public class VehicleAudioSource : ToolkitBehaviour
	{
		#region Variables

		public AudioSource source;

		#endregion

		#region Utilities

		public void PlayOnceAndDisable()
		{
			StopAllCoroutines();
			StartCoroutine(PlayOnceAndDisableCoroutine(source));
		}
		public void PlayOnceDelayedAndDisable(float delay)
		{
			StopAllCoroutines();
			StartCoroutine(PlayOnceAndDisableCoroutine(source, delay));
		}
		public void StopAndDisable()
		{
			source.Stop();

			source.enabled = false;
		}
		public void PauseAndDisable()
		{
			source.Pause();

			source.enabled = false;
		}
		public void PlayAndEnable()
		{
			source.enabled = true;

			source.Play();
		}
		public void UnPauseAndEnable()
		{
			source.enabled = true;

			source.UnPause();
		}

		private IEnumerator PlayOnceAndDisableCoroutine(AudioSource source, float delay = 0f)
		{
			if (!source.clip)
				yield break;

			source.enabled = true;

			if (delay > 0f)
				yield return new WaitForSeconds(delay);

			source.Play();

			yield return null;

			bool orgLoop = source.loop;

			source.loop = false;

			while (source.isPlaying)
				yield return null;

			source.loop = orgLoop;
			source.enabled = false;
		}

		#endregion

		#region Methods

		private void Awake()
		{
			source = GetComponent<AudioSource>();
		}

		#endregion
	}
}
