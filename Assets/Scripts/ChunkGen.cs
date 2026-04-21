using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct ChunkGen : IJobFor
{
    [WriteOnly] AdvancedMeshGenSetup meshGen;
    [ReadOnly] NoiseSettingsData settings;
    // Adding these WriteOnly and ReadOnly tags improve performance by allowing the Burst compiler to optimize the code (also safety)

    public float2 origin;
    public quaternion meshRotation;
    public float res;

    public int resolution;
    //as resolution is the number of gaps between vertices on each row/column, the number of vertices on each row/column is resolution + 1
    public int vertexCount => (resolution + 1) * (resolution + 1);
    public int indexCount => resolution * resolution * 6;
    //need 6 indices per square (2 triangles per square, 3 indices per triangle) - this is counted from the bottom left vertex of each square so no need for the final row/column
    public int jobLength => resolution + 1;

    public void Execute(int z)
    {
        // z is incremented by the job system - similar to a for loop but each loop is a job and a seperate thread - it represents the row number (hence used z for depth)
        int vertexIndex = (resolution + 1) * z;
        int triangleIndex = 2 * resolution * z;
        float halfLength = resolution * settings.meshScale /2f;

        //Debug.Log($"z : {z}"); 

        float sampleZ = ((float)z / resolution - 0.5f) * resolution * settings.meshScale/res + origin.y;
        // this places the sample point at the centre of the row of squares

        //Debug.Log($"sampleZ : {sampleZ}");

        for (float x = 0; x <= resolution; x++)
        {
            // the 

            float sampleX = (x / resolution - 0.5f) * resolution * settings.meshScale / res + origin.x; 
            // this places the sample point to the centre of each individual sqaure
            
            float3 basePos = float3(sampleX, halfLength, sampleZ);
            // this represent the point coordinate of the top side of the cube

            //Debug.Log($"bassePos {basePos}");
            float3 normalizedPos = normalize(basePos) * halfLength;
            // gets its sphere coordinates 

            float3 rotatedPos = mul(meshRotation, normalizedPos);
            
            float noiseHeight = NoiseGen.CalculateNoiseWithDerivatives(rotatedPos, settings);
            //Debug.Log($"noiseHeight : {noiseHeight}");
            //Debug.Log($"settings maxHeight : {settings.maxHeight}");    
            float inverseHeight = clamp((noiseHeight + settings.maxHeight*0.7f)/(2f*settings.maxHeight*0.7f),0f,1f);
            //Debug.Log($"Inverse height: {inverseHeight}");
            float3 finalPos = normalize(rotatedPos) * (127.5f*settings.meshScale + settings.amplitude * noiseHeight);
            
            // Create vertex
            var vertex = new Vertex
            {
                position = finalPos,
                color = settings.SampleGradient(inverseHeight) // Store slope information in color
            };

            meshGen.SetVertex(vertexIndex, vertex);

            // Generate triangles
            if (z < resolution && x < resolution)
            {
                int3 triangle1 = int3(
                    vertexIndex,
                    vertexIndex + resolution + 1,
                    vertexIndex + 1
                );

                int3 triangle2 = int3(
                    vertexIndex + 1,
                    vertexIndex + resolution + 1,
                    vertexIndex + resolution + 2
                );
                //Debug.Log($"triangle index 1 : {triangleIndex}");
                meshGen.SetTriangle(triangleIndex++, triangle1);
                //Debug.Log($"triangle index 2 : {triangleIndex}");
                meshGen.SetTriangle(triangleIndex++, triangle2);
            }

            vertexIndex++;
        }
    }

    public static JobHandle ScheduleParallel(
        Mesh.MeshData meshData,
        NoiseSettingsData settings,
        float2 origin,
        quaternion meshRotation,
        float res,
        JobHandle dependency,
        int resolution = 255)
    {
        var job = new ChunkGen
        {
            meshGen = new AdvancedMeshGenSetup(),
            settings = settings,
            origin = origin,
            meshRotation = meshRotation,
            res = res,
            resolution = resolution
        };

        job.meshGen.Setup(meshData, job.vertexCount, job.indexCount);

        var handle = job.ScheduleParallel(job.jobLength, 1, dependency);

        return handle;
    }
}