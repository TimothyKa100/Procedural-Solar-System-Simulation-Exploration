using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;

public struct AdvancedMeshGenSetup
{
    // disbaled safety checks for performance
    [NativeDisableContainerSafetyRestriction]
    private NativeArray<Vertex> vertices;
    [NativeDisableContainerSafetyRestriction]
    private NativeArray<TriangleUInt16> triangles;

    public void Setup(Mesh.MeshData meshData, int vertexCount, int indexCount)
    {
        // Create vertex buffer that ChunkGen can write to
        NativeArray<VertexAttributeDescriptor> meshDataDescriptor = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory); // temporary array to hold vertex attributes for mesh data creation - disposed after use pretty much immediately so no point in making it permenant (safe memory and is quicker) 
        meshDataDescriptor[0] = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3); // Vector3, hence dimension:3 
        meshDataDescriptor[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8,  dimension: 4); // UNorm8 is 0-1 range - uses Color32 struct hence dimension 4 and 8 bits per channel

        meshData.SetVertexBufferParams(vertexCount, meshDataDescriptor);
        meshDataDescriptor.Dispose(); // dispose of the temporary array after setting up meshData

        meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount) // means one submesh (submeshes in Unity are used usually for different textures - since I only use colours there's no point for having multiple submeshes)
        {
            vertexCount = vertexCount
        },
        MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices); // these MeshUpdateFlags disable checks to improve performance

        vertices = meshData.GetVertexData<Vertex>();
        triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2); // short is 2 bytes instead of 4 to save memory, there's no need for 4 bytes for indices

        //Debug.Log($"Vertex buffer created with size: {vertices.Length}");
        //Debug.Log($"Vertex struct size: {System.Runtime.InteropServices.Marshal.SizeOf<Vertex>()}");

        //// Verify first few vertices have correct layout
        //unsafe
        //{
        //    var ptr = vertices.GetUnsafePtr();
        //    Debug.Log($"First vertex color offset: {(int)(&((Vertex*)ptr)->color) - (int)ptr}");
        //}
    }

    public void SetVertex(int index, Vertex vertex)
    {
        vertices[index] = new Vertex
        {
            position = vertex.position,
            color = vertex.color
        };
        //Debug.Log($"vertex color: {vertices[index].color}");
    }

    public void SetTriangle(int index, int3 triangle)
    {
        triangles[index] = triangle;
    }
}