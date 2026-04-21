using Unity.Jobs;
using UnityEngine;

public class Main : MonoBehaviour
{
    public Material material;
    public NoiseSettings settings;
    private void Awake()
    {
        Vector3[] rotations = new Vector3[6]
        {
            Vector3.zero,
            Vector3.forward,
            Vector3.forward*2, // the opposite side of the initial face of cube
            Vector3.back,
            Vector3.right,
            Vector3.left
        };

        Vector3[] origins = new Vector3[6]
        { 
            Vector3.up,
            Vector3.left,
            Vector3.down,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        for (int i = 0; i < 6; i++)
        {
            GameObject childObj = new GameObject("Quadtree" + 0 + i.ToString()); // This creates a new gameobject in the scene 
            childObj.transform.parent = transform;
            childObj.transform.localScale = Vector3.one/2550; // so when landed it is at localScale 1 to avoid artefacst
            childObj.transform.localPosition = Vector3.zero;
            Quadtree quadtree = childObj.AddComponent<Quadtree>();
            quadtree.enabled = false;
            
            // setting the correct position for each directional quadtree 
            Vector3 origin = origins[i] * 127.5f * settings.meshScale; // 127.5 is half of 255, the number of gaps between vertices on row of each mesh
            Quaternion rotation = Quaternion.Euler(rotations[i] * 90);

            // generate their mesh, the reason why I don't generate mesh within the quadtree class that belows to the chunk is for scheduling
            (JobHandle jobHandle, Mesh.MeshDataArray array, NoiseSettingsData noiseSettings) = AdvancedMeshGen.GenerateHandle(i.ToString(), rotation, 1, settings, default);
            Mesh mesh = AdvancedMeshGen.CompleteMeshGeneration(jobHandle, array, noiseSettings);
            quadtree.Initialise(1, rotation, i.ToString(), origin, material, mesh, settings, null);
            quadtree.enabled = true;
        }
    }
}