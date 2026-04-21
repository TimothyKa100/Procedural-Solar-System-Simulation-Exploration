using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using Unity.Jobs;
using System.Collections.Generic;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Quadtree: MonoBehaviour
{
    public const float minSize = 255f;

    public static Transform viewer;
    Mesh mesh;
    Bounds bounds;
    Quadtree[] children = new Quadtree[4];

    Vector3 centre;
    float sideLength;
    float thresholdSqrdDistance;
    float quarterLength;
    float sqrDistance;

    int res;
    bool active;

    MeshRenderer meshRenderer;
    MeshCollider meshCollider;

    string ID;
    Quaternion rotation;

    Material material;

    // both of below are static as they need to be accessed by all quadtrees to determine scheduling of mesh generation
    private static readonly object jobHandlesLock = new object(); // placeholder object for thread locking for thread safety in multithreading 
    private static Queue<JobHandle> jobHandles = new Queue<JobHandle>();

    JobHandle[] childHandle = new JobHandle[4];
    Mesh.MeshDataArray[] childArray = new Mesh.MeshDataArray[4];
    NoiseSettingsData[] childSettings = new NoiseSettingsData[4];
    string[] childID = new string[4];
    int childRes;

    public NoiseSettings settings;

    public static PhysicsMaterial noBounce;

    public bool genLock;
    public Quadtree parent;

    public void Initialise(int res, Quaternion rotation, string ID, Vector3 centre, Material material, Mesh mesh, NoiseSettings settings, Quadtree parent)
    {
        GetComponent<MeshFilter>().sharedMesh = mesh;
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.material = noBounce;
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = material;
        bounds = meshRenderer.bounds; //world space coordinates 
        //Debug.Log($"bounds {bounds.size}"); -- doesn't work anymore with rotation
        //Debug.Log("Centre: " + centre);
        this.centre = centre;
        //Debug.Log("Position: " + position);

        sideLength =  settings.meshScale * 255 / res; // meshScale is the max scale + res is the current resolution (it goes in powers of 2 with 1 being the lowest res - ie the directional quadtree generated from Main)
        //Debug.Log($"sidelength {sideLength}");
        thresholdSqrdDistance = sideLength * sideLength *1.4f /4f ;
        quarterLength = sideLength / 4;
        
        this.res = res;
        this.ID = ID;
        active = true;
        this.rotation = rotation;
        this.material = material;
        childRes = res * 2;
        this.settings = settings;
        this.parent = parent;
    }

    private void FixedUpdate()
    {
        bounds =  meshRenderer.bounds;
        //Debug.Log($"mesh {mesh}"); 
        //Debug.Log($"bounds {bounds}");
        sqrDistance = bounds.SqrDistance(viewer.position); // sqrDistance is much more efficient than dist as sqaure roots are very demanding
        //Debug.Log($"sqaure dist {sqrDistance}");
        if (active)
        {
            splitQuad();
        }
        else
        {
            regenerate();
        }
    }


    // Update called to check for when to regenerate itself if distance is too far
    // called for disabling the children / setactieve(false) to save on performance
    void regenerate()
    {
        if (sqrDistance > thresholdSqrdDistance*UniversalGravitation.scaleForLanding * UniversalGravitation.scaleForLanding/2550/2550*4 && genLock is false)
        {
            //Debug.Log("regen");
            meshRenderer.enabled = true;
            meshCollider.enabled = true;
            //Debug.Log($"mesh {mesh}");
            active = true;
            for (int i = 0; i < 4; i++)
            {
                children[i].gameObject.SetActive(false);
            }
        } 
    }

    void splitQuad()
    {
        if (sqrDistance < (thresholdSqrdDistance * UniversalGravitation.scaleForLanding * UniversalGravitation.scaleForLanding /2550/2550 ) && sideLength > minSize)
        {
            //Debug.Log($"sqaure dist split {sqrDistance}");
            //Debug.Log("split");
            //Debug.Log($"Before split mesh {mesh}");
            enabled = false;
            //Debug.Log($"Disabled mesh {mesh}");
            if (children[0] is null)
            {
                CreateChildren();
            }
            else
            {
                GetChildren();
            }
            enabled = true;
            active = false;
            //Debug.Log($"Splitting mesh {mesh}");
            meshRenderer.enabled = false;
            meshCollider.enabled = false;
            //Debug.Log($"After split mesh {mesh}");
        }
    }

    void CreateChildren()
    {
        // using standard quadrant labelling to avoid confusion
        Vector3[] centres = new Vector3[]
        {
            centre + rotation * new Vector3(quarterLength, 0, quarterLength),
            centre + rotation * new Vector3(-quarterLength, 0, quarterLength),
            centre + rotation * new Vector3(-quarterLength, 0, -quarterLength),
            centre + rotation * new Vector3(quarterLength, 0, -quarterLength),
        };

        centres = centres.OrderBy(c => (c - viewer.position).magnitude).ToArray();

        for (int i = 0; i < centres.Length; i++)
        {
            GameObject childObj = new GameObject("Quadtree" + res.ToString() + i.ToString());
            Quadtree child = childObj.AddComponent<Quadtree>();
            child.enabled = false;
            child.transform.parent = transform.parent;
            childObj.transform.localPosition = Vector3.zero;
            childObj.transform.localScale = Vector3.one / 2550;
            children[i] = child;
            childID[i] = ID + i.ToString();
            lock (jobHandlesLock)
            {
                JobHandle previousHandle = jobHandles.LastOrDefault();
                (childHandle[i], childArray[i], childSettings[i]) = AdvancedMeshGen.GenerateHandle(childID[i], rotation, childRes, settings, previousHandle);
                jobHandles.Enqueue(childHandle[i]);
            }
        }

        for (int i = 0; i < children.Length; i++)
        {
            Mesh thisMesh = AdvancedMeshGen.CompleteMeshGeneration(childHandle[i], childArray[i], childSettings[i]);
            // this calls handle.Complete() -> this function would signify that it is possible to generate and once the previous jobs are finished or there are spare threads this could start
            children[i].Initialise(childRes, rotation, childID[i], centres[i], material, thisMesh, settings, this);
            jobHandles.Dequeue();
            children[i].enabled = true; 
        }
    }

    void GetChildren()
    {
        for (int i = 0; i < 4; i++)
        {
            children[i].gameObject.SetActive(true);
        }
    }
}