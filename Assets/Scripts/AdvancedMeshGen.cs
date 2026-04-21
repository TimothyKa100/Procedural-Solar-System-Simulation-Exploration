using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;

public static class AdvancedMeshGen
{
    public static (JobHandle, Mesh.MeshDataArray, NoiseSettingsData) GenerateHandle(string ID, Quaternion rotation, float res, NoiseSettings settings, JobHandle lastHandle)
    {
        var meshDataArray = Mesh.AllocateWritableMeshData(1); // using advanced mesh API to access and directly write mesh data to improve processing overheads
        var jobSettings = settings.CreateJobFriendlySettings();

        var meshData = meshDataArray[0]; // this is where we write our mesh data to

        string verticesID = ID.Substring(1);
        float scale = 0.5f;
        float2 origin = float2.zero;
        float quarterLength = 255 * settings.meshScale/4;

        for (int i = 0; i < verticesID.Length; i++)
        {
            scale = math.exp2(i);
            switch (verticesID[i])
            {
                case '0':
                    origin += new float2(1,1) * quarterLength / scale;
                    break;
                case '1':
                    origin += new float2(-1, 1) * quarterLength / scale;
                    break;
                case '2':
                    origin += new float2(-1,-1) * quarterLength / scale;
                    break;
                case '3':
                    origin += new float2(1, -1) * quarterLength / scale;
                    break;
            }
        }
        // calculation for getting the un-normalised origin of the mesh (position it would've been on the cube) for mesh generation

        JobHandle handle = ChunkGen.ScheduleParallel(
            meshData,
            jobSettings,
            origin,
            rotation,
            res,
            lastHandle
        );
        // this allows for multithreading (on CPU with jobs)
        return (handle, meshDataArray, jobSettings);   
    }

    public static Mesh CompleteMeshGeneration(JobHandle handle, Mesh.MeshDataArray meshDataArray, NoiseSettingsData jobSettings)
    {
        Mesh mesh = new Mesh();
        try
        {
            handle.Complete();

            // Only apply the mesh data if we haven't hit an exception
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

            // Use the default Unity calculations - could be improved with my own algorithm but it is not too expensive
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            return mesh;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error completing mesh generation: {e}");

            // If we created a mesh but failed, destroy it
            if (mesh != null)
            {
                if (Application.isEditor)
                {
                    Object.DestroyImmediate(mesh);
                }
                else
                {
                    Object.Destroy(mesh);
                }
            }

            // Dispose the mesh data without trying to apply it
            meshDataArray.Dispose();
            throw;
        }
        finally
        {
            // Safety to avoid memory leakds
            jobSettings.Dispose();
        }
    }
}