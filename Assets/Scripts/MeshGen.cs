using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Mathematics; 
using static Unity.Mathematics.math;
using static UnityEngine.UI.Image;
using Unity.VisualScripting;

public static class MeshGen
{
    public static NoiseSettings settings;
    public static Vector3[] origins;
    public static Vector3[] rotations;

    public static Mesh GenerateMesh(int res, string ID, Vector3 position)
    {
        Mesh mesh = new Mesh();
        (float maxPossibleHeight, Vector3[] octaveOffsets) = MapGen.GenSeed(settings.seed, settings.octave, settings.offset, settings.persistance);
        //Debug.Log("max possible height: " + maxPossibleHeight);
        //Debug.Log("Octave Offsets: " + octaveOffsets[0] + octaveOffsets[1]);
        Debug.Log("Position" + position);
        // the line below is faulty! (no longer -- it's removed)
        Vector3 rotation = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            if (position.normalized == origins[i])
            {
                rotation = rotations[i];
                break;
            }
        }

        string verticesID = ID.Substring(1);
        float scale = 0.5f;
        Vector2 origin = new Vector2(0, 0);
        int quarterLength = 255*25;
        for (int i = 0; i < verticesID.Length; i++)
        {
            scale = exp2(i);
            char c = verticesID[i];
            switch (c)
            {
                case '0':
                    origin += Vector2.one * quarterLength / scale;
                    break;
                case '1':
                    origin += new Vector2(-1, 1) * quarterLength / scale;
                    break;
                case '2':
                    origin -= Vector2.one * quarterLength / scale;
                    break;
                case '3':
                    origin += new Vector2(1, -1) * quarterLength / scale;
                    break;
            }
        }
        Debug.Log("scale: " + scale);
        Debug.Log("Origin: " + origin);
        Vector2 bottomLeftCorner = origin - Vector2.one * quarterLength / scale;
        Debug.Log("bottomLeftCorner : "  + bottomLeftCorner);

        //Debug.Log(bottomLeftCorner);
        ///Debug.Log("Octave Offsets: " + string.Join(", ", octaveOffsets.Select(o => o.ToString()).ToArray()));
        float[,] heightMap = MapGen.GenHeightMap(res, settings.seed, octaveOffsets, settings.scale, settings.octave, settings.persistance, settings.lacunarity, bottomLeftCorner/100, rotation, settings.frequency);
        float[] inverseMap = MapGen.GenInverseMap(maxPossibleHeight, heightMap, settings.heightCurve);
        Color[] colors = MapGen.GenColorMap(inverseMap, settings.gradient);
        Vector3[] vertices = CreateVertices(res, settings.amplitude, inverseMap, settings.meshScale, origin);
        int[] triangles = CreateTriangles();
        UpdateMesh(mesh, vertices, triangles, colors);
        return mesh;
    }

    // issue 1 the create vertices function for normalisation gives changing normalisation origin instead of matching the origin of the mesh
    // issue is because I have gen the vertices assuming centre before shifting the position, I need to gen directly with its position of mesh!
    // fixed issue 1, but the positions of the meshes are out of place (shifted away from origin all by same scale factor)
    public static Vector3[] CreateVertices(int res, float amplitude, float[] inverseMap, int meshScale, Vector2 origin)
    {
        Vector3[] vertices = new Vector3[256 * 256];
        // changing the origin to match the sub quadtree mesh position relative to its max parent (disregarding orirentation)
        

        for (int i = 0; i < vertices.Length; i++)
        {
            float x = (i % 256 - 127.5f) * meshScale / res + origin.x;
            float y = (i / 256 - 127.5f) * meshScale / res + origin.y;
            Vector3 cubeVertex = new Vector3(x, 0, y);
            Vector3 worldCubeVertex = cubeVertex + Vector3.up*12750;
            //Debug.Log(cubeVertex.normalized);
            vertices[i] = worldCubeVertex.normalized*(12750+ amplitude * inverseMap[i]) - Vector3.up*12750;
            //vertices[i] = cubeVertex;
        }
        return vertices;
    }

    public static int[] CreateTriangles()
    {
        int[] triangles = new int[255 * 255 * 6];

        int vert = 0;
        int tris = 0;

        for (int y = 0; y < 255; y++)
        {
            for (int x = 0; x < 255; x++)
            {
                triangles[tris] = vert;
                triangles[tris + 1] = vert + 256;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + 256;
                triangles[tris + 5] = vert + 256 + 1;
                vert++;
                tris += 6;
            }
            //if(y == 254)
            //{
            //    Debug.Log("Vert: " + vert);
            //}
            vert++;
        }
        return triangles;
    }

    public static void UpdateMesh(Mesh mesh, Vector3[] vertices, int[] triangles, Color[] colors)
    {
        mesh.Clear(); // I accidentally use 257*257 vertices instead of 256*256, could optimise afterwards but this would suit for now!
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }
}