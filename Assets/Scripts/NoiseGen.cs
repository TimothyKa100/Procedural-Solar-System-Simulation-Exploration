using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Unity.Mathematics.noise;

public struct NoiseGen
{
    public static float CalculateNoiseWithDerivatives(float3 position, NoiseSettingsData settings)
    {
        float noiseHeight = 0f;
        float amplitude = 1f;
        float frequency = settings.frequency;

        for (int i = 0; i < settings.octaveCount; i++)
        {
            float3 samplePos = (position/settings.scale + settings.octaveOffsets[i]) * frequency; // control which bit of noise to sample from and how detailed it should be 

            float noise = snoise(samplePos);
            noiseHeight += noise * amplitude;

            amplitude *= settings.persistance;
            frequency *= settings.lacunarity;
        }
        // classic fBm

        return noiseHeight;
    }
}
