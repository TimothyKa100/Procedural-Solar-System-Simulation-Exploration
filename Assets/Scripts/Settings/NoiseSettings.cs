using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "NoiseSettings", menuName = "Scriptable Objects/NoiseSettings")]
public class NoiseSettings : ScriptableObject
{
    public int seed;
    public float scale = 50f;
    public int octave = 6;
    public float persistance = 0.6f;
    public float lacunarity = 2f;
    public float frequency = 1f;
    public float amplitude = 50f;
    public int meshScale = 1000;
    public Vector3 offset;
    public AnimationCurve heightCurve;
    public Gradient gradient;

    public NoiseSettingsData CreateJobFriendlySettings(int samplePoints = 256)
    {
        // Generate octave offsets
        var prng = new System.Random(seed);
        var octaveOffsets = new NativeArray<float3>(octave, Allocator.Persistent);

        float maxPossibleHeight = 0;
        float testAmplitude = 1f;

        for (int i = 0; i < octave; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            float offsetZ = prng.Next(-100000, 100000) + offset.z;
            offsetX = offsetY = offsetZ = 0;
            octaveOffsets[i] = new float3(offsetX, offsetY, offsetZ);

            maxPossibleHeight += testAmplitude;
            testAmplitude *= persistance;
        }
             
        // Sample height curve into array
        var heightCurvePoints = new NativeArray<float>(samplePoints, Allocator.Persistent);
        for (int i = 0; i < samplePoints; i++)
        {
            float vertex = i / (float)(samplePoints - 1);
            heightCurvePoints[i] = heightCurve.Evaluate(vertex);
        }

        // Convert gradient to color array
        var gradientColors = new NativeArray<Color32>(samplePoints, Allocator.Persistent);
        for (int i = 0; i < samplePoints; i++)
        {
            float vertex = i / (float)(samplePoints - 1);
            gradientColors[i] = gradient.Evaluate(vertex);
            //Debug.Log($"Color: {color}");
        }

        return new NoiseSettingsData
        {
            scale = scale,
            octaveCount = octave,
            persistance = persistance,
            lacunarity = lacunarity,
            frequency = frequency,
            amplitude = amplitude,
            maxHeight = maxPossibleHeight,
            meshScale = meshScale,
            octaveOffsets = octaveOffsets,
            heightCurvePoints = heightCurvePoints,
            gradientColors = gradientColors,
            heightCurveSampleCount = samplePoints
        };
    }
}

// Job-compatible struct
public struct NoiseSettingsData : IDisposable
{
    public float scale;
    public int octaveCount;
    public float persistance;
    public float lacunarity;
    public float frequency;
    public float amplitude;
    public float maxHeight;
    public float meshScale;
    public int heightCurveSampleCount;

    [ReadOnly] public NativeArray<float3> octaveOffsets; // improves clarify + performance
    [ReadOnly] public NativeArray<float> heightCurvePoints;
    [ReadOnly] public NativeArray<Color32> gradientColors;

    // Helper method to sample height curve
    public float SampleHeightCurve(float vertexNo)
    {
        float index = vertexNo * (heightCurveSampleCount - 1);
        int lowIndex = (int)math.floor(index);
        int highIndex = (int)math.ceil(index);
        float t = index - lowIndex;

        // Clamp indices
        lowIndex = math.clamp(lowIndex, 0, heightCurveSampleCount - 1);
        highIndex = math.clamp(highIndex, 0, heightCurveSampleCount - 1);

        return math.lerp(heightCurvePoints[lowIndex], heightCurvePoints[highIndex], t);
    }

    public Color32 SampleGradient(float inverseHeight)
    {
        float index = inverseHeight * (heightCurveSampleCount - 1);
        int lowIndex = (int)math.floor(index);
        int highIndex = (int)math.ceil(index);
        float t = index - lowIndex;

        // Clamp indices
        lowIndex = math.clamp(lowIndex, 0, heightCurveSampleCount - 1);
        highIndex = math.clamp(highIndex, 0, heightCurveSampleCount - 1);

        Color32 lerpedColor = Color32.Lerp(gradientColors[lowIndex], gradientColors[highIndex], t);
        //Debug.Log(lerpedColor);

        return Color32.Lerp(gradientColors[lowIndex], gradientColors[highIndex], t);
    }

    public void Dispose()
    {
        try
        {
            if (octaveOffsets.IsCreated) octaveOffsets.Dispose();
            if (heightCurvePoints.IsCreated) heightCurvePoints.Dispose();
            if (gradientColors.IsCreated) gradientColors.Dispose();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error disposing NoiseSettingsData: {e}");
        }
    }
}