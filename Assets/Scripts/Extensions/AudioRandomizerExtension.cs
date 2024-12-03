using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AudioRandomizerExtension
{
    public static AudioClip GetRandomClip(this IEnumerable<AudioClip> clips)
    {
        if (clips == null || !clips.Any())
            return null;

        return clips.ElementAt(Random.Range(0, clips.Count()));
    }

    public static AudioSource SetRandomVolume(this AudioSource source, float minValue = 0.75f, float maxValue = 1f)
    {
        float randomVolume = Random.Range(minValue, maxValue);
        source.volume = randomVolume;

        return source;
    }

    public static AudioSource SetRandomPitch(this AudioSource source, float minValue = 0.9f, float maxValue = 1.1f)
    {
        float randomPitch = Random.Range(minValue, maxValue);
        source.pitch = randomPitch;

        return source;
    }
}
