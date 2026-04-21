using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex
{
    public float3 position;
    public Color32 color;
}

[StructLayout(LayoutKind.Sequential)]
public struct TriangleUInt16
{
    public ushort a, b, c;
    public static implicit operator TriangleUInt16(int3 t) => new TriangleUInt16 
    {
        a = (ushort)t.x,
        b = (ushort)t.y,
        c = (ushort)t.z
    };
    // implicit operator to turn the supplied int3 struct into 3 short values as a struct to save memory and improve performance for triangles 
}