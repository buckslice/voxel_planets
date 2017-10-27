
//#define SMOOTH_SHADING

#define GEOMORPHING


using UnityEngine;
using System.Threading.Tasks;

public class Octree {

    // front right - front left - back left - back right, then same order but for bottom layer
    // start game and pause right at beginning to see layout better
    public Octree[] children;   // would be cool to have all octree stored in same array somehow, or custom memory management like in dtb pools
    // front left back right up down
    //public Octree[] neighbors = new Octree[6];
    public Octree parent;

    public bool hasChildren = false;
    public int depth;  // 0 is root, MAX_LEVEL is depth limit
    int branch;  // which child of your parent are you

    Array3<Voxel> voxels; // need to save for vertex modification (should prob be called density)

    Vector3 localPos;    // position of voxel grid (denotes the corner so it gets offset to remain centered)

    public CelestialBody body;

    public ChunkObject obj = null;
    public ColliderObject col = null;

    public const int SIZE = 16;        // number of voxel cells in octree
    public const int MAX_DEPTH = 10;     // max depth meshes can split to
    public readonly float voxelSize;   // size of each voxel for this tree (in meters)
    public const float BASE_VOXEL_SIZE = 2.0f;  // highest depth has 2x2x2 meter voxels

    public Bounds localArea; // bounding box for area this tree represents
    // gonna be bad.. once planets can rotate lol
    // will prob need to make own thing or have some extension that takes into account planet rotation

    public bool splitting = false; // set when a tree is waiting on list/currently being split
    bool dying = false;    // gets set when a child is merged into parent

    public const float colliderGenDistance = 50.0f;
    public const float fadeRate = 1.0f; // 0.5f would be half of normal time, so 2 seconds

    float timeSinceCreation = 0.0f;
    const float timeFullyCreated = 0.5f;
    const float blendRange = 0.05f; // percent of each split level that is geoblended

    public Octree(CelestialBody body, Octree parent, Vector3 center, int depth, int branch) {
        this.body = body;
        this.parent = parent;
        this.depth = depth;
        this.branch = branch;

        voxelSize = Mathf.Pow(2, (MAX_DEPTH - depth)) * BASE_VOXEL_SIZE;
        // *2 at end makes so highest depth has 2x2x2 meter voxels
        // so voxels are 2m^3 at max depth
        // then 4, 8, 16, etc

#if (SMOOTH_SHADING)
        pos = center - new Vector3(2, 2, 2) * voxelSize - Vector3.one * (SIZE / 2f) * voxelSize;
#else
        localPos = center - Vector3.one * (SIZE / 2f) * voxelSize;
#endif

        localArea = new Bounds(center, Vector3.one * voxelSize * SIZE);
    }

    public MeshData GenerateMesh(bool createVoxels) {
        // so while SIZE is 16, which means theres 16 cells/blocks in grid 
        // you need 17 values to be able to construct those blocks
        // (think of 17 points in a grid and the blocks are the 16 spaces in between)
        // if smoothing then need a buffer of 2 around (front and back so +4) for smoothing and normal calculation
        // (so mesh goes from 2-19 basically (0, 1, 20, 21) are not visible in final result)

#if (SMOOTH_SHADING)
        if (createVoxels) {
            voxels = WorldGenerator.CreateVoxels(SIZE + 5, depth, voxelSize, pos);
        }
        MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize, 2, 2);
        data.CalculateVertexSharing();
        //Simplification simp = new Simplification(data.vertices, data.triangles);
        //data.normals = VoxelUtils.CalculateSmoothNormals(voxels, voxelSize, data.vertices);
        //data.SplitEdgesCalcSmoothness();
        data.CalculateSharedNormals();  // todo figure out why this doesnt make it smoothed...
#else
        if (createVoxels) {
            voxels = WorldGenerator.CreateVoxels(SIZE + 1, depth, voxelSize, localPos);
        }

        //if (!needsMesh) {
        //    return null;
        //}

        //MeshData data = MarchingTetrahedra.CalculateMeshData(voxels, voxelSize);

        MeshData data = MarchingCubes.CalculateMeshData(voxels, voxelSize);
        data.CalculateNormals();
#endif

        //data.CalculateColorsByDepth(depth);
        return data;
    }

    // all the stuff that requires the main thread for Unity functions is done here
    // most other work is offloaded to helper threads. builds mesh and game object
    // dont actually need to build a gameobject if theres no mesh...
    // or at least have an empty object for the parent transform and dont attach components
    public void BuildGameObject(MeshData data) {
        Mesh mesh = null;

        if (data != null) {
            mesh = data.CreateMesh();
        } else {
            //Debug.Log(++meshlessboys);
        }
        obj = SplitManager.GetObject();

        //StringBuilder builder = new StringBuilder();
        //builder.Append("Tree ");
        //builder.Append(depth);
        //builder.Append(" ");
        //builder.Append(center.x);
        //builder.Append(" ");
        //builder.Append(center.y);
        //builder.Append(" ");
        //builder.Append(center.z);
        //obj.go.name = builder.ToString();


        obj.go.transform.parent = body.transform;
        obj.go.transform.localPosition = localPos;
        obj.go.transform.localRotation = Quaternion.identity;
        //obj.go.transform.position = pos;

        obj.mr.material = body.terrainMat; // incase terrain is edited after
        obj.mpb.SetVector(ShaderProps.localOffset, localPos);
        obj.UpdatePropBlock();

        if (mesh == null) {
            obj.ov.shouldDraw = false;
        } else {
            obj.ov.shouldDraw = true;
            obj.mf.mesh = mesh;
            obj.ov.init(depth, branch, localArea, body, depth == 0 ? Color.blue : Color.red);
        }

    }

    public void Update() {
        if (hasChildren) {
            //GeoMorphInternal();
            if (ShouldMerge()) {
                Merge();
            } else {
                for (int i = 0; i < 8; i++) {
                    children[i].Update();
                }
            }
        } else if (!splitting && ShouldSplit()) {
            splitting = true;
            SplitManager.AddToSplitList(this);
        } else {
            //GeoMorphLeaf();
            // if at max depth, have valid mesh, and close to cam then should have a collider
            if (depth == MAX_DEPTH && obj.mf.sharedMesh && GetSqrDistToCamFromCenter() < colliderGenDistance * colliderGenDistance) {
                if (col == null) {     // if collider is null then spawn one
                    col = SplitManager.GetCollider();
                    col.go.transform.SetParent(obj.go.transform, false);
                    col.go.transform.localPosition = Vector3.zero;
                    col.go.transform.localRotation = Quaternion.identity;
                    col.mc.sharedMesh = obj.mf.sharedMesh;
                }
            } else if (col != null) {   // otherwise if have collider then return it
                SplitManager.ReturnCollider(col);
                col = null;
            }

        }

        SetGeomorph();
    }

    // area is in localSpace so transform it to be in world space
    // this is transform point basically... (except that didnt work????)
    public static Vector3 LocalToWorld(Transform body, Vector3 localPos) {
        return body.rotation * localPos + body.position;
    }
    public static Vector3 WorldToLocal(Transform body, Vector3 worldPos) {
        return Quaternion.Inverse(body.rotation) * (worldPos - body.position);
    }

    // one function now
    // could split back into two like before for #efficiency but probly not worth
    // when morphing, first fade in children then fade out parent
    // if timeFullyCreated is 1 then children fade in from 0-0.5, parent fades out from 0.5-1

    // maybe just dont do the halfway morph. either start morphing or unmorphing
    // removes the distance calculations
    //void SetGeomorph() {
    //    timeSinceCreation += Time.deltaTime;
    //    float t = timeSinceCreation / timeFullyCreated;

    //    if (hasChildren) {  // internal node
    //        float dist = (body.player - LocalToWorld(body.transform, localArea.center)).magnitude;
    //        float level = body.splitLevels[depth];    // my depth
    //        float absDist = Mathf.Abs(dist - level);
    //        float blendBand = level * blendRange;

    //        t = Mathf.Min(t, Mathf.Min(absDist / blendBand, 1.0f));
    //        t = Mathf.Clamp01(2.0f - 2.0f * t);
    //        obj.mr.enabled = t > 0.0f;

    //    } else {    // leaf node
    //        if (parent == null) {
    //            Debug.Assert(depth == 0);
    //            obj.SetTransparency(1.0f);
    //            return;
    //        }
    //        float dist = (body.player - LocalToWorld(body.transform, parent.localArea.center)).magnitude;
    //        float level = body.splitLevels[depth - 1];    // level at parents depth
    //        float absDist = Mathf.Abs(dist - level);
    //        float blendBand = level * blendRange;

    //        t = Mathf.Min(t, Mathf.Min(absDist / blendBand, 1.0f));
    //        t = Mathf.Clamp01(2.0f * t);
    //    }

    //    obj.SetTransparency(t);

    //}

    // faster version of set geomorph that only uses timeSinceCreation to determine blend amount
    // saves on a lot of distance calls
    // only problem is when merging it pops back (need to have timer before merge to reblend?)
    void SetGeomorph() {
        timeSinceCreation += Time.deltaTime;
        float t = timeSinceCreation / timeFullyCreated;

        if (hasChildren) {
            t = Mathf.Clamp01(2.0f - 2.0f * t);
            obj.mr.enabled = t > 0.0f;
        } else {
            if (parent == null) {
                Debug.Assert(depth == 0);
                t = 1.0f;
            } else {
                t = Mathf.Clamp01(2.0f * t);
            }
        }
        obj.SetTransparency(t);
    }

    public Task<SplitData> SplitAsync() {
        return Task<SplitData>.Factory.StartNew(() => {
            SplitData data = new SplitData(this);
            children = new Octree[8];
            for (int i = 0; i < 8; i++) {
                Vector3 coff = childOffsets[i];
                Octree child = new Octree(body, this, localArea.center + coff * SIZE * voxelSize * .25f, depth + 1, i);

                data.Add(child.GenerateMesh(true));
                children[i] = child;
            }

            return data;
        }, TaskCreationOptions.None);
    }

    public void SplitResolve(MeshData[] data) {
        splitting = false;
        if (!ShouldSplit()) {
            return;
        }

        for (int i = 0; i < 8; ++i) {
            Octree c = children[i];
            c.BuildGameObject(data[i]);
            c.obj.go.transform.parent = obj.go.transform;
            c.SetGeomorph();
        }
        obj.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (depth > 0) {
            obj.ov.shouldDraw = false;
        }
        hasChildren = true;

        timeSinceCreation = 0.0f;   // now used to fade yourself out
        SetGeomorph();
    }

    //public float GetSqrDistToCamFromArea() {
    //    //return area.SqrDistance(body.cam.position);
    //    return area.SqrDistance(body.player);
    //}

    // this could be made into a lookup
    // just calculate distance to planet once then should be able to figure out 
    // distance to any node based on branch and leaf and stuff i think...
    // just need to calculate new local up and forward vectors i think...
    public float GetSqrDistToCamFromCenter() {
        //return (body.cam.position - area.center).sqrMagnitude;
        return (body.player - LocalToWorld(body.transform, localArea.center)).sqrMagnitude;
    }

    public bool ShouldSplit() {
        float level = body.splitLevels[depth];
        return CanSplit() && GetSqrDistToCamFromCenter() < level * level;
    }

    bool CanSplit() {
        return depth < MAX_DEPTH && !dying && timeSinceCreation >= timeFullyCreated;
    }

    bool ShouldMerge() {
        float level = body.splitLevels[depth];
        return CanMerge() && GetSqrDistToCamFromCenter() > level * level;
    }

    bool CanMerge() {
        return !children[0].hasChildren
            && !children[1].hasChildren
            && !children[2].hasChildren
            && !children[3].hasChildren
            && !children[4].hasChildren
            && !children[5].hasChildren
            && !children[6].hasChildren
            && !children[7].hasChildren;
    }

    // called when parent merges 8 children
    void Merge() {
        for (int i = 0; i < 8; i++) {
            children[i].OnGettingMerged();
            children[i] = null;
        }
        hasChildren = false;
        obj.mr.enabled = true;
        timeSinceCreation = timeFullyCreated;
        SetGeomorph();
        obj.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        if (obj.mf.mesh.vertexCount > 0) {  // only draw gizmo if this chunk has a mesh
            obj.ov.shouldDraw = true;
        }
    }

    // called on children getting merged by their parent
    void OnGettingMerged() {
        Object.Destroy(obj.mf.mesh);
        SplitManager.ReturnObject(obj);
        if (col != null) {
            SplitManager.ReturnCollider(col);
            col = null;
        }
        dying = true;
    }

    // given point in LOCALSPACE find the smallest octree node that contains this point
    // returns null if outside the tree

    public Octree FindOctree(Vector3 worldPos) {
        Vector3 point = WorldToLocal(body.transform, worldPos);
        if (!localArea.Contains(point)) {
            Debug.LogWarning("FindOctree called outside of initial tree bounds");
            return null;
        }

        Octree cur = this;
        while (cur.hasChildren) {
            if (point.y > cur.localArea.center.y) {
                if (point.z > cur.localArea.center.z) {
                    if (point.x > cur.localArea.center.x) {
                        cur = cur.children[0];
                    } else {
                        cur = cur.children[1];
                    }
                } else {
                    if (point.x > cur.localArea.center.x) {
                        cur = cur.children[3];
                    } else {
                        cur = cur.children[2];
                    }
                }
            } else {
                if (point.z > cur.localArea.center.z) {
                    if (point.x > cur.localArea.center.x) {
                        cur = cur.children[4];
                    } else {
                        cur = cur.children[5];
                    }
                } else {
                    if (point.x > cur.localArea.center.x) {
                        cur = cur.children[7];
                    } else {
                        cur = cur.children[6];
                    }
                }
            }
        }
        return cur;
    }

    public bool IsMaxDepth() {
        return depth == MAX_DEPTH;
    }

    // should only be called from root (maybe make RootOctree subclass of octree with these methods in it)
    public void EditVoxels(Bounds b, int delta) {
        if (depth != 0 || !localArea.Intersects(b)) {
            Debug.LogWarning("EditVoxels error i dunno");
            return;
        }

        if (hasChildren) {
            for (int i = 0; i < 8; ++i) {
                children[i].EditVoxels(b, delta);
            }
        } else {
            Vector3 c = (b.center - (localArea.center - localArea.extents)) / voxelSize;   // local center
            Vector3 e = b.extents / voxelSize;

            int x0 = Mathf.Clamp((int)(c.x - (e.x - 1.0f)), 0, SIZE);
            int y0 = Mathf.Clamp((int)(c.y - (e.y - 1.0f)), 0, SIZE);
            int z0 = Mathf.Clamp((int)(c.z - (e.z - 1.0f)), 0, SIZE);
            int x1 = Mathf.Clamp((int)(c.x + e.x), 0, SIZE);
            int y1 = Mathf.Clamp((int)(c.y + e.y), 0, SIZE);
            int z1 = Mathf.Clamp((int)(c.z + e.z), 0, SIZE);

            for (int z = z0; z <= z1; ++z) {
                for (int y = y0; y <= y1; ++y) {
                    for (int x = x0; x <= x1; ++x) {
                        Voxel v = voxels[x, y, z];
                        v.density = (sbyte)Mathf.Clamp(v.density + delta, -128, 127);
                        voxels[x, y, z] = v;
                    }
                }
            }
            // rebuild mesh immediately
            obj.mf.mesh = GenerateMesh(false).CreateMesh();
            if (col != null) {
                col.mc.sharedMesh = obj.mf.sharedMesh;
            }
        }
    }

    public static Vector3[] childOffsets = {
        new Vector3(1, 1, 1),
        new Vector3(-1, 1, 1),
        new Vector3(-1, 1, -1),
        new Vector3(1, 1, -1),
        new Vector3(1, -1, 1),
        new Vector3(-1, -1, 1),
        new Vector3(-1, -1, -1),
        new Vector3(1, -1, -1)
    };

    // checks some things to make sure octree state is valid
    public bool IsTreeValid() {
        if (hasChildren) {
            if (children.Length != 8) {
                return false;
            }
            if (obj.go.transform.childCount != 8) {
                return false;
            }
            for (int i = 0; i < children.Length; ++i) {
                if (!children[i].IsTreeValid()) {
                    return false;
                }
            }
        }
        return true;
    }

    public int GetNumGameObjects(bool leafsOnly) {
        if (hasChildren) {
            int c = leafsOnly ? 0 : 1;
            for (int i = 0; i < 8; ++i) {
                c += children[i].GetNumGameObjects(leafsOnly);
            }
            return c;
        }
        return 1;
    }


    //// child order ufr,ufl,ubl,ubr,ffr,ffl,fbl,fbr
    //// neighbor order f,l,b,r,u,d
    //// was gonna make a lookup table but FOKCIT
    //// this cant happen async btw
    //void SetChildNeighbors() {
    //    // basic cases, each new child's neighbors is 3 of children
    //    children[0].neighbors[1] = children[1];
    //    children[0].neighbors[2] = children[3];
    //    children[0].neighbors[5] = children[4];
    //    children[1].neighbors[2] = children[2];
    //    children[1].neighbors[3] = children[0];
    //    children[1].neighbors[5] = children[5];
    //    children[2].neighbors[0] = children[1];
    //    children[2].neighbors[3] = children[3];
    //    children[2].neighbors[5] = children[6];
    //    children[3].neighbors[0] = children[0];
    //    children[3].neighbors[1] = children[2];
    //    children[3].neighbors[5] = children[7];
    //    children[4].neighbors[1] = children[5];
    //    children[4].neighbors[2] = children[7];
    //    children[4].neighbors[4] = children[0];
    //    children[5].neighbors[2] = children[6];
    //    children[5].neighbors[3] = children[4];
    //    children[5].neighbors[4] = children[1];
    //    children[6].neighbors[0] = children[5];
    //    children[6].neighbors[3] = children[7];
    //    children[6].neighbors[4] = children[2];
    //    children[7].neighbors[0] = children[4];
    //    children[7].neighbors[1] = children[6];
    //    children[7].neighbors[4] = children[3];

    //    // for each of these need to check if neighbor has kids
    //    // if so then make the right kid your neighbor and update their neighbor to be u
    //    // this seems like a lot of work just to have no big gaps between splits...
    //    // f this for now, not even stitching anyways
    //    children[0].neighbors[0] = neighbors[0].hasChildren ? neighbors[0].kid ?;
    //    children[0].neighbors[3] = neighbors[3];
    //    children[0].neighbors[4] = neighbors[4];
    //    children[1].neighbors[0] = neighbors[0];
    //    children[1].neighbors[1] = neighbors[1];
    //    children[1].neighbors[4] = neighbors[4];
    //    children[2].neighbors[1] = neighbors[1];
    //    children[2].neighbors[2] = neighbors[2];
    //    children[2].neighbors[4] = neighbors[4];
    //    children[3].neighbors[2] = neighbors[2];
    //    children[3].neighbors[3] = neighbors[3];
    //    children[3].neighbors[4] = neighbors[4];
    //    children[4].neighbors[0] = neighbors[0];
    //    children[4].neighbors[3] = neighbors[3];
    //    children[4].neighbors[5] = neighbors[5];
    //    children[5].neighbors[0] = neighbors[0];
    //    children[5].neighbors[1] = neighbors[1];
    //    children[5].neighbors[5] = neighbors[5];
    //    children[6].neighbors[1] = neighbors[1];
    //    children[6].neighbors[2] = neighbors[2];
    //    children[6].neighbors[5] = neighbors[5];
    //    children[7].neighbors[2] = neighbors[2];
    //    children[7].neighbors[3] = neighbors[3];
    //    children[7].neighbors[5] = neighbors[5];
    //}
}
