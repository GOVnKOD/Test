using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CsgDestructible : MonoBehaviour
{
    BspTree bspTree = null;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            buildBspTreeIfNeeded();

            performCsgOperation();

            updateTriangleMesh();

            //Vector3 point = new Vector3(GetComponent<Camera>().pixelWidth / 2, GetComponent<Camera>().pixelHeight / 2, 0);
            //Ray ray = GetComponent<Camera>().ScreenPointToRay(point);
            //RaycastHit hit;
            //if (Physics.Raycast(ray, out hit))
            //{
            //    StartCoroutine(SphereIndicator(hit.point));
            //}
        }
    }

    private void buildBspTreeIfNeeded()
    {
        if (null == bspTree)
        {
            buildBspTree();
        }
    }

    private void performCsgOperation()
    {
    }

    private void updateTriangleMesh()
    {
    }

    private void buildBspTree()
    {
        var mesh = GetComponent<MeshFilter>().mesh;

        bspTree = new BspTree();

        bspTree.buildFromMesh(mesh, transform);
    }
}

class BspTree
{
    class Node
    {
        public int planeIndex;
        public int frontChild;
        public int backChild;
        public List<Face> faces;    /// convex polygons lying on this node's plane
    }

    List<Node>  nodes = new List<Node>();
    List<Plane> planes = new List<Plane>();

    static readonly int EMPTY_LEAF_INDEX = -1;  // denotes empty space
    static readonly int SOLID_LEAF_INDEX = -2;

    struct Triangle
    {
        //public Vector3[] points = new Vector3[]{};
        public Vector3 pointA;
        public Vector3 pointB;
        public Vector3 pointC;
    }

    class Face
    {
        public List<Vector3> vertices;
    }

    /// Denotes a relation of a polygon/surface to some splitting plane;
    /// used when classifying polygons/surfaces when building BSP trees.
    /// Don't change the values - they are used as bit-masks / 'OR' ops.
    enum EPlaneSide
    {
        On = 0x0,	//!< The polygon is lying on the plane.
        Back = 0x1,	//!< The polygon is lying in back of the plane ('below', 'behind').
        Front = 0x2,	//!< The polygon is lying in front of the plane ('above', 'over').
        Split = 0x3,	//!< The polygon intersects with the plane ('spanning' the plane).
    };

    static float CLIP_EPSILON = 0.001f;

    ///
    static EPlaneSide ClassifyPoint(Vector3 point, Plane plane, float epsilon)
    {
        float d = plane.GetDistanceToPoint(point);
        return (d > +epsilon) ? EPlaneSide.Front : (d < -epsilon) ? EPlaneSide.Back : EPlaneSide.On;
    }

    public void buildFromMesh(Mesh mesh, Transform localToWorldTransform)
    {
        //debugPrintMesh(mesh);

        var faceList = buildFaceListFromMesh(mesh, localToWorldTransform);

        var rootNodeIndex = createNodeFromFaceList_Recursive(faceList);
        Debug.Assert(rootNodeIndex == 0, "The root node must have zero index!");

        debugPrint();
    }

    private void debugPrintMesh(Mesh mesh)
    {
        var numVertices= mesh.vertices.Length;
        Debug.LogFormat("Mesh has {0} vertices", numVertices);

        for(int i = 0; i < numVertices; i++)
        {
            Debug.LogFormat("[{0}]: {1}", i, mesh.vertices[i]);
        }
    }

    public void debugPrint()
    {
        Debug.LogFormat("{0} nodes, {0} planes", nodes.Count, planes.Count);
    }

    private List<Triangle> buildTriangleListFromMesh(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        var triangleList = new List<Triangle>();

        var numTriangles =  triangles.Length / 3;
        for(int triangleIndex = 0; triangleIndex < numTriangles; triangleIndex++)
        {
            var i0 = triangles[triangleIndex+0];
            var i1 = triangles[triangleIndex+1];
            var i2 = triangles[triangleIndex+2];

            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            var newTriangle = new Triangle();

            newTriangle.pointA = v0;
            newTriangle.pointB = v1;
            newTriangle.pointC = v2;

            triangleList.Add(newTriangle);
        }

        return triangleList;
    }

    private List<Face> buildFaceListFromMesh(Mesh mesh, Transform localToWorldTransform)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        //
        var numVertices = triangles.Length / 3;
        Vector3[] transformedVertices = new Vector3[numVertices];

        for (int i = 0; i < numVertices; i++)
        {
            transformedVertices[i] = localToWorldTransform.TransformPoint(vertices[i]);
            Debug.LogFormat("Vertex[{0}]: {1}", i, transformedVertices[i]);
        }

        //
        var faceList = new List<Face>();

        var numTriangles = triangles.Length / 3;
        for (int triangleIndex = 0; triangleIndex < numTriangles; triangleIndex++)
        {
            var i0 = triangles[triangleIndex + 0];
            var i1 = triangles[triangleIndex + 1];
            var i2 = triangles[triangleIndex + 2];
            //AAA
            var v0 = vertices[i0];
            var v1 = vertices[i1];
            var v2 = vertices[i2];

            //
            var newFace = new Face();

            var reservedCount = 8;  // to reduce (re-)allocations
            newFace.vertices = new List<Vector3>(reservedCount);
            newFace.vertices.Add(v0);
            newFace.vertices.Add(v1);
            newFace.vertices.Add(v2);

            faceList.Add(newFace);
        }

        Debug.LogFormat("Created {0} faces.", numTriangles);

        return faceList;
    }

    private int createNodeFromFaceList_Recursive(List<Face> faceList)
    {
        int splittingPlaneIndex = pickSplittingPlane(faceList);

        Face splittingFace = faceList[splittingPlaneIndex];
        Plane splittingPlane = createPlaneFromFace(splittingFace);
        Debug.LogFormat("createNode: plane:{0} = {1}", splittingPlaneIndex, splittingPlane);

        List<Face> facesOnPlane;
        List<Face> facesInFront;
        List<Face> facesBehind;
        
        splitFacesByPlane(faceList, splittingPlane,
            out facesOnPlane, out facesInFront, out facesBehind);

        //
        int newNodeIndex = createNewNode();
        
        nodes[newNodeIndex].planeIndex = splittingPlaneIndex;
        nodes[newNodeIndex].faces = facesOnPlane;

        //
        if(facesInFront.Count == 0)
        {
            nodes[newNodeIndex].frontChild = EMPTY_LEAF_INDEX;
        }
        else
        {
            nodes[newNodeIndex].frontChild = createNodeFromFaceList_Recursive(facesInFront);
        }

        //
        if(facesBehind.Count == 0)
        {
            nodes[newNodeIndex].backChild = SOLID_LEAF_INDEX;
        }
        else
        {
            nodes[newNodeIndex].backChild = createNodeFromFaceList_Recursive(facesBehind);
        }

        return newNodeIndex;
    }

    /// <summary>
    ///  returns the (unique) index of the splitting plane
    /// </summary>
    /// <param name="faceList"></param>
    /// <returns></returns>
    private int pickSplittingPlane(List<Face> faceList)
    {
        // pick the first one. assume that the object is convex.
        var newPlaneIndex = planes.Count;

        var newPlane = createPlaneFromFace(faceList.First());
        planes.Add(newPlane);

        return newPlaneIndex;
    }

    private Plane createPlaneFromFace(Face face)
    {
        var numVertices = face.vertices.Count;
        //
        var faceCenter = Vector3.zero;
        foreach (Vector3 vertex in face.vertices)
        {
            faceCenter += vertex;
        }
        faceCenter /= numVertices;

        //
        var planeNormal = Vector3.zero;

        var previousVertex = face.vertices[numVertices - 1];
        for (int i = 0; i < numVertices; i++)
        {
            var currentVertex = face.vertices[i];
 Debug.LogWarningFormat("Vertx[{0}]: {1}", i, currentVertex);
            var relV0 = previousVertex - faceCenter;
            var relV1 = currentVertex - faceCenter;

            planeNormal += Vector3.Cross(relV0, relV1);
        }

        planeNormal = planeNormal.normalized;

        var plane = new Plane(planeNormal, faceCenter);
        Debug.LogWarningFormat("Plane:{0}", plane);

        for (int i = 0; i < numVertices; i++)
        {
            var currentVertex = face.vertices[i];
            var pointRelation = classifyPoint(currentVertex, plane, CLIP_EPSILON);
            Debug.Assert(pointRelation == EPlaneSide.On);
        }

        //
        return plane;
    }

    /// <summary>
    /// partitions the given set of planes by the plane
    /// </summary>
    /// <param name="faceList"></param>
    /// <param name="splittingPlane"></param>
    /// <param name="facesOnPlane"></param>
    /// <param name="facesInFront"></param>
    /// <param name="facesBehind"></param>
    /// <param name="splitFaces"></param>
    void splitFacesByPlane(List<Face> faceList, Plane splittingPlane,
           out List<Face> facesOnPlane, out List<Face> facesInFront, out List<Face> facesBehind)
    {
        facesOnPlane = new List<Face>();
        facesInFront = new List<Face>();
        facesBehind = new List<Face>();

        foreach (Face face in faceList)
        {
            Face backFace, frontFace;
            var planeSide = splitFaceByPlane(face, splittingPlane, out backFace, out frontFace);

            Debug.LogFormat("Split face by plane:{0} -> {1}, front:{2}, back:{3}", splittingPlane, planeSide, frontFace != null, backFace != null);

            switch (planeSide)
            {
                case EPlaneSide.On:
                    facesOnPlane.Add(face);
                    break;

                case EPlaneSide.Back:
                    facesBehind.Add(backFace);
                    break;

                case EPlaneSide.Front:
                    facesInFront.Add(frontFace);
                    break;

                case EPlaneSide.Split:
                    facesBehind.Add(backFace);
                    facesInFront.Add(frontFace);
                    break;
            }
        }
    }

    /// <summary>
    /// Splits the face in a back and front part.
    /// </summary>
    /// <param name="face"></param>
    /// <param name="splittingPlane"></param>
    /// <returns></returns>
    private EPlaneSide splitFaceByPlane(
        Face face
        , Plane splittingPlane
        , out Face frontFace_, out Face backFace_
        , float epsilon = 1e-3f//CLIP_EPSILON
        )
    {
        var numVertices = face.vertices.Count;

        var dists = new float[numVertices + 1];
        var sides = new EPlaneSide[numVertices + 1];

        // First, classify all points. This allows us to avoid any bisection if possible
        var counts = new int[3]; // 'on', 'back' and 'front'
        var faceStatus = (int)EPlaneSide.On;

        // determine sides for each point
        for (int i = 0; i < numVertices; i++)
        {
            var vertex = face.vertices[i];
            var d = splittingPlane.GetDistanceToPoint(vertex);
            dists[i] = d;
            var side = (d > +epsilon)
                                    ? EPlaneSide.Front
                                    : (d < -epsilon) ? EPlaneSide.Back : EPlaneSide.On;
            sides[i] = side;
            faceStatus |= (int)side;
            counts[(int)side]++;
        }
        sides[numVertices] = sides[0];
        dists[numVertices] = dists[0];

        //
        if (faceStatus != (int)EPlaneSide.Split)
        {
            backFace_ = face;
            frontFace_ = face;
            return (EPlaneSide)faceStatus;
        }
        Debug.DebugBreak();//AAA
        // Straddles the splitting plane - we must clip.

        //mxBIBREF("'float-Time Collision Detection' by Christer Ericson (2005), 8.3.4 Splitting Polygons Against a Plane, PP.369-373");

        var MAX_VERTS = 32;

        var backVerts = new Vector3[MAX_VERTS];
        var frontVerts = new Vector3[MAX_VERTS];

        var numFront = 0;
        var numBack = 0;


        // Test all edges (a, b) starting with edge from last to first Vector3
        Vector3 vA = face.vertices[numVertices - 1];
        float distA = dists[numVertices - 1];
        EPlaneSide sideA = sides[numVertices - 1];

        // Loop over all edges given by Vector3 pair (n - 1, n)
        for (int i = 0; i < numVertices; i++)
        {
            Vector3 vB = face.vertices[i];
            float distB = dists[i];
            EPlaneSide sideB = sides[i];
            if (sideB == EPlaneSide.Front)
            {
                if (sideA == EPlaneSide.Back)
                {
                    // Edge (a, b) straddles, output intersection point to both sides
                    // always calculate the split going from the same side or minor epsilon issues can happen
                    Vector3 v = getIntersectionPoint(vB, vA, distB, distA);
                    Debug.Assert(classifyPoint(v, splittingPlane, epsilon) == EPlaneSide.On);
                    frontVerts[numFront++] = backVerts[numBack++] = v;
                }
                // In all three cases, output b to the front side
                frontVerts[numFront++] = vB;
            }
            else if (sideB == EPlaneSide.Back)
            {
                if (sideA == EPlaneSide.Front)
                {
                    // Edge (a, b) straddles plane, output intersection point
                    Vector3 v = getIntersectionPoint(vA, vB, distA, distB);
                    Debug.Assert(classifyPoint(v, splittingPlane, epsilon) == EPlaneSide.On);
                    frontVerts[numFront++] = backVerts[numBack++] = v;
                }
                else if (sideA == EPlaneSide.On)
                {
                    // Output a when edge (a, b) goes from ‘on’ to ‘behind’ plane
                    backVerts[numBack++] = vA;
                }
                // In all three cases, output b to the back side
                backVerts[numBack++] = vB;
            }
            else
            {
                // b is on the plane. In all three cases output b to the front side
                frontVerts[numFront++] = vB;
                // In one case, also output b to back side
                if (sideA == EPlaneSide.Back)
                    backVerts[numBack++] = vB;
            }
            // Keep b as the starting point of the next edge
            vA = vB;
            distA = distB;
            sideA = sideB;
        }

        //
        frontFace_ = new Face();
        frontFace_.vertices = frontVerts.ToList();

        backFace_ = new Face();
        backFace_.vertices = backVerts.ToList();

        Debug.Assert(faceStatus == (int)EPlaneSide.Split);
        return EPlaneSide.Split;
    }

    static
        EPlaneSide classifyPoint(Vector3 point, Plane plane, float epsilon)
    {
        var d = plane.GetDistanceToPoint(point);
        return (d > +epsilon)
                                ? EPlaneSide.Front
                                : (d < -epsilon) ? EPlaneSide.Back : EPlaneSide.On;
    }


    /// NOTE: for consistency (to prevent minor epsilon issues)
    /// the point 'a' must always be in front of the plane, the point 'a' - behind the plane,
    /// i.e. 'distA' must always be > 0, and 'distB' must always be < 0.
    static
    Vector3 getIntersectionPoint(
                          Vector3 a, Vector3 b,
                          float distA, float distB
                           )
    {
        // always calculate the split going from the same side or minor epsilon issues can happen
        //mxASSERT(distA > 0 && distB < 0);

        float fraction = distA / (distA - distB);
        return Vector3.Lerp(a, b, fraction);
        //interpolatedVertex.xyz = a.xyz + (b.xyz - a.xyz) * fraction;
    }


    private int createNewNode()
    {
        var newNodeIndex = nodes.Count;

        nodes.Add(new Node());

        return newNodeIndex;
    }
}
