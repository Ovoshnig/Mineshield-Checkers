using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AudioRandomizerExtension
{
    public static void PlayRandomly(this AudioSource audioSource, IEnumerable<AudioClip> clips,
        (float, float) randomVolumeRange, (float, float) randomPitchRange)
    {
        int randomIndex = Random.Range(0, clips.Count());
        AudioClip randomClip = clips.ToArray()[randomIndex];

        var (minVolume, maxVolume) = randomVolumeRange;
        float randomVolume = Random.Range(minVolume, maxVolume);

        var (minPitch, maxPitch) = randomPitchRange;
        float randomPitch = Random.Range(minPitch, maxPitch);

        audioSource.volume = randomVolume;
        audioSource.pitch = randomPitch;
        audioSource.clip = randomClip;

        audioSource.Play();
    }

    public static void PlayOneShotRandomly(this AudioSource audioSource, IEnumerable<AudioClip> clips,
        (float, float) randomVolumeRange, (float, float) randomPitchRange)
    {
        int randomIndex = Random.Range(0, clips.Count());
        AudioClip randomClip = clips.ToArray()[randomIndex];

        var (minVolume, maxVolume) = randomVolumeRange;
        float randomVolume = Random.Range(minVolume, maxVolume);

        var (minPitch, maxPitch) = randomPitchRange;
        float randomPitch = Random.Range(minPitch, maxPitch);

        audioSource.volume = randomVolume;
        audioSource.pitch = randomPitch;

        audioSource.PlayOneShot(randomClip);
    }
}
