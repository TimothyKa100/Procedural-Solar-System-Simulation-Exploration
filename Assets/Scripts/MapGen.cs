using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Unity.Burst;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class MapGen
{
    public static (float, Vector3[]) GenSeed(int seed, int octave, Vector3 offset, float persistance)
    {
        float maxPossibleHeight = 0;
        float testAmplitude = 1f;

        System.Random prng = new System.Random(seed);
        Vector3[] octaveOffsets = new Vector3[octave];

        for (int i = 0; i < octave; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            float offsetZ = prng.Next(-100000, 100000) + offset.z;
            octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);

            maxPossibleHeight += testAmplitude;
            testAmplitude *= persistance;

        }
        return (maxPossibleHeight, octaveOffsets);
    }

    // FINALLY IT WORKS PROPERLY!!!
    public static float[,] GenHeightMap(float res, int seed, Vector3[] octaveOffsets, float scale, int octave, float persistance, float lacunarity, Vector2 bottomLeftCorner, Vector3 rotation, float frequency = 1f, float noiseHeight = 0f)
    {
        int length = 256;
        float amplitude = 1f;
        float[,] heightMap = new float[length, length];
        // Centre the map
        float halfLength = (length-1) /2f;
        Quaternion rotated = Quaternion.Euler(rotation*90);
        //Debug.Log("Rotation: " + rotated.eulerAngles);

        for (float x = 0; x < length; x++)
        {
            for (float z = 0; z < length; z++)
            {
                float localAmplitude = amplitude;
                float localFrequency = frequency;
                float localNoiseHeight = noiseHeight;

                for (int i = 0; i < octave; i++)
                {
                    float sampleX = x/res + bottomLeftCorner.x;
                    //float sampleX = ((x / res - threeQuarterLength + centre.x * (length - 1)) / scale) * localFrequency;
                    float sampleZ = z / res + bottomLeftCorner.y;
                    //float sampleZ = ((z / res - threeQuarterLength + centre.y * (length - 1)) / scale) * localFrequency;
                    Vector3 vertex = new Vector3(sampleX, halfLength, sampleZ);
                    if (x == 0 && z == 0) Debug.Log("Vertex: " + vertex);
                    vertex = vertex.normalized * halfLength;
                    
                    
                    Vector3 rotatedVertex = rotated * vertex/scale * localFrequency;
                    if (x == 0 && z == 0) Debug.Log("Rotated Vertex: " + rotatedVertex);

                    //Flat floor level
                    float simplexValue = noise.snoise((float3)(rotatedVertex + octaveOffsets[i]*localFrequency)) * 2 - 1;
                    localNoiseHeight += simplexValue * localAmplitude;
                    localAmplitude *= persistance;
                    localFrequency *= lacunarity;
                }
                heightMap[(int)x, (int)z] = localNoiseHeight;
                //Debug.Log("Height: " + localNoiseHeight);
            }
        }
        return heightMap;
    }

    public static float[] GenInverseMap(float maxPossibleHeight, float[,] heightMap, AnimationCurve heightCurve)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float[] inverseMap = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float inverseHeight = Mathf.InverseLerp(-maxPossibleHeight*0.7f, maxPossibleHeight*0.7f, heightMap[x, y]);
                inverseMap[y*height+x] = heightCurve.Evaluate(inverseHeight);
            }
        }

        return inverseMap;
    }

    public static Color[] GenColorMap(float[] inverseMap, Gradient gradient)
    {

        Color[] colorMapColors = new Color[inverseMap.Length];
        for (int i = 0; i < inverseMap.Length; i++)
        {
            Color heightValue = gradient.Evaluate(inverseMap[i]);
            colorMapColors[i] = heightValue;
        }
        return colorMapColors;
    }
}