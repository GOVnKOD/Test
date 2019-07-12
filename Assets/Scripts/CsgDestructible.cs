using System;
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
            bspTree.debugPrintNodes();
            //Vector3 point = new Vector3(GetComponent<Camera>().pixelWidth / 2, GetComponent<Camera>().pixelHeight / 2, 0);
            //Ray ray = GetComponent<Camera>().ScreenPointToRay(point);
            //RaycastHit hit;
            //if (Physics.Raycast(ray, out hit))
            //{
            //    StartCoroutine(SphereIndicator(hit.point));
            //}
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            buildBspTreeIfNeeded();

            performCsgOperation();

            updateTriangleMesh();
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
        var mesh = GameObject.Find("Cube").GetComponent<MeshFilter>().mesh;

        //
        var subtractedMesh = mesh;

        var subtractedBspTree = new BspTree();

        var uniformScalingFactor = 4.0f;
        var subtractedMeshTransform = Matrix4x4.Scale(new Vector3(uniformScalingFactor, uniformScalingFactor, uniformScalingFactor));
 
        subtractedBspTree.buildFromMesh(subtractedMesh, subtractedMeshTransform, true);
        subtractedBspTree.debugPrintNodes();
        //
        bspTree.mergeSubtract(subtractedBspTree);
    }

    private void updateTriangleMesh()
    {
        Mesh mesh = generateMeshFromBSPTree(bspTree);
        GetComponent<MeshFilter>().mesh = mesh;
        transform.localScale = new Vector3(1, 1, 1);
    }

    private Mesh generateMeshFromBSPTree(BspTree bspTree)
    {
        Mesh mesh = new Mesh();
        mesh = bspTree.generateMesh();
        return mesh;
    }

    private void buildBspTree()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        
        bspTree = new BspTree();

        bspTree.buildFromMesh(mesh, transform.localToWorldMatrix, false);
    }
}

class BspTree
{
    class Node
    {
        public Plane plane;
        public int frontChild;
        public int backChild;
        public List<Face> faces;    /// convex polygons lying on this node's plane
    }

    List<Node>  nodes = new List<Node>();

    int rootNodeIndex = 0;

    Bounds bounds;

    static readonly int EMPTY_LEAF_INDEX = -1;  // denotes empty space
    static readonly int SOLID_LEAF_INDEX = -2;

    static bool isInternalNodeIndex(int nodeIndex)
    {
        return nodeIndex >= 0;
    }

    /// <summary>
    /// Represents a convex polygon.
    /// </summary>
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
    public void buildFromMesh(Mesh mesh, Matrix4x4 vertexTransform, bool reverseFaces)
    {
        //debugPrintMesh(mesh);

        var faceList = buildFaceListFromMesh(mesh, vertexTransform, reverseFaces);

        rootNodeIndex = createNodeFromFaceList_Recursive(faceList);

        Debug.Assert(rootNodeIndex == 0);

        debugPrint();
    }


    #region Debugging

    public void debugPrintNodes()
    {
        var count = 0;
        foreach(Node node in nodes)
        {

            Debug.LogFormat("Node[{0}]",count);
            Debug.LogFormat("Вивод FACE-ов");
            var i = 0;
            foreach(Face face in node.faces)
            {
                Debug.LogFormat("Вивод FACE[{0}]:{1},{2},{3}",i,face.vertices[0], face.vertices[1],face.vertices[2]);
                i += 1;
            }
            Debug.LogFormat("Вивод плоскости",node.plane);
            count++;
        }
    }

    private static void debugPrintMesh(Mesh mesh)
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
        Debug.LogFormat("{0} nodes", nodes.Count);
    }

    #endregion

    #region BSP tree building
    private List<Face> buildFaceListFromMesh(Mesh mesh, Matrix4x4 vertexTransform, bool reverseFaces)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        //
        var numVertices = vertices.Length;
        Vector3[] transformedVertices = new Vector3[numVertices];

        //
        var LARGE_NUMBER = 1e10f;
        var LARGE_VECTOR = new Vector3(LARGE_NUMBER, LARGE_NUMBER, LARGE_NUMBER);
        bounds.SetMinMax(LARGE_VECTOR, -LARGE_VECTOR);

        for (int i = 0; i < numVertices; i++)
        {
            transformedVertices[i] = vertexTransform.MultiplyPoint(vertices[i]);

            bounds.min = Vector3.Min(bounds.min, transformedVertices[i]);
            bounds.max = Vector3.Max(bounds.max, transformedVertices[i]);
        }

        Debug.LogFormat("bounds:{0}", bounds);

        //
        var faceList = new List<Face>();

        var numTriangles = triangles.Length;
        for (int triangleIndex = 0; triangleIndex < numTriangles; triangleIndex+=3)
        {
            var i0 = reverseFaces ? triangles[triangleIndex + 2] : triangles[triangleIndex + 0];
            var i1 = triangles[triangleIndex + 1];
            var i2 = reverseFaces ? triangles[triangleIndex + 0] : triangles[triangleIndex + 2];

            var v0 = transformedVertices[i0];
            var v1 = transformedVertices[i1];
            var v2 = transformedVertices[i2];

            //
            var newFace = new Face();

            //var reservedCount = 8;  // to reduce (re-)allocations
            newFace.vertices = new List<Vector3>();
            newFace.vertices.Add(v0);
            newFace.vertices.Add(v1);
            newFace.vertices.Add(v2);

            faceList.Add(newFace);
        }
        //Debug.Log("ОБход созданного листа face");
        //var temp_count = 0;
        //foreach (Face tempFace in faceList)
        //{
        //    Debug.Log(temp_count);
        //    Debug.LogFormat("Текущий FACE имеет точки: {0} {1} {2}", tempFace.vertices[0], tempFace.vertices[1], tempFace.vertices[2]);
        //    temp_count += 1;
        //}

        //Debug.LogFormat("Created {0} faces.", faceList.Count);

        return faceList;
    }

    private int createNodeFromFaceList_Recursive(List<Face> faceList)
    {
        Plane splittingPlane = pickSplittingPlane(faceList);
        Face splittingFace = faceList[0];
        //Debug.LogFormat("createNode: plane:{0} = {1}", splittingPlane, splittingPlane);

        List<Face> facesOnPlane;
        List<Face> facesInFront;
        List<Face> facesBehind;
        
        splitFacesByPlane(faceList, splittingPlane,
            out facesOnPlane, out facesInFront, out facesBehind);

        //
        int newNodeIndex = createNewNode();
        
        nodes[newNodeIndex].plane = splittingPlane;
        nodes[newNodeIndex].faces = facesOnPlane;
        //Debug.LogFormat("Count facesInFront:{0}", facesInFront.Count);
        //Debug.LogFormat("Count facesBehind:{0}", facesBehind.Count);
        //
        if (facesInFront.Count == 0)
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
    private Plane pickSplittingPlane(List<Face> faceList)
    {
        // pick the first one. assume that the object is convex.
        var newPlane = createPlaneFromFace(faceList.First());
        return newPlane;
    }

    private Plane createPlaneFromFace(Face face)
    {
        var numVertices = face.vertices.Count;
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
            var relV0 = previousVertex - faceCenter;
            var relV1 = currentVertex - faceCenter;
            planeNormal += Vector3.Cross(relV0, relV1);
            previousVertex = currentVertex;
        }

        planeNormal = planeNormal.normalized;

        var plane = new Plane(planeNormal, faceCenter);

        for (int i = 0; i < numVertices; i++)
        {
            var currentVertex = face.vertices[i];
            var pointRelation = classifyPoint(currentVertex, plane, CLIP_EPSILON);
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
        frontFace_.vertices = frontVerts.Take(numFront).ToList();

        backFace_ = new Face();
        backFace_.vertices = backVerts.Take(numBack).ToList();

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

    static EPlaneSide ClassifyPoint(Vector3 point, Plane plane, float epsilon)
    {
        float d = plane.GetDistanceToPoint(point);
        return (d > +epsilon) ? EPlaneSide.Front : (d < -epsilon) ? EPlaneSide.Back : EPlaneSide.On;
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

#endregion

    #region BSP tree merging and CSG operations

    public void mergeSubtract(BspTree subtractedBspTree)
    {
        mergeSubtractRecursive(ref rootNodeIndex, subtractedBspTree);
    }

    public void mergeSubtractRecursive(
        ref int ourNodeIndex
        , BspTree subtractedBspTree//, int theirNodeIndex
        )
    {
#if false//AAA
        foreach(Node node in nodes)
        {
            node.faces = clipFacesWithBspTree(node.faces, subtractedBspTree);
        }
#else
        if(isInternalNodeIndex(ourNodeIndex))
        {
            Node node = nodes[ourNodeIndex];

            node.faces = clipFacesWithBspTree(node.faces, subtractedBspTree);

            mergeSubtractRecursive(ref node.frontChild, subtractedBspTree);
            mergeSubtractRecursive(ref node.backChild, subtractedBspTree);
        }
        else
        {
            // This is a leaf node.
            if (ourNodeIndex == SOLID_LEAF_INDEX)
            {
                // This leaf node represents a solid/filled space.
                ourNodeIndex = copyNodesFromBspTree(subtractedBspTree);
            }
            else
            {
                // This leaf node represents an empty space.

                // Do nothing.
            }
        }
#endif
        int copyNodesFromBspTree(BspTree other)
        {
            var newNodeIndex = nodes.Count;

            for( int nodeIndex = 0; nodeIndex < other.nodes.Count; nodeIndex++)
            {
                var otherNode = other.nodes[nodeIndex];

                var newNode = new Node();
                newNode.plane = otherNode.plane;

                //
                newNode.frontChild = otherNode.frontChild;
                if (isInternalNodeIndex(otherNode.frontChild))
                {
                    newNode.frontChild += newNodeIndex;
                }

                //
                newNode.backChild = otherNode.backChild;
                if (isInternalNodeIndex(otherNode.backChild))
                {
                    newNode.backChild += newNodeIndex;
                }

                newNode.faces = otherNode.faces;

                //
                nodes.Add(newNode);
            }

            return newNodeIndex;
        }
    }

    private List<Face> clipFacesWithBspTree(List<Face> faces, BspTree subtractedBspTree)
    {
        var clippedFaces = new List<Face>();

        clipFacesWithBspTreeRecursive(faces, clippedFaces, subtractedBspTree, 0 /*root node index*/);

        return clippedFaces;
    }

    private void clipFacesWithBspTreeRecursive(
        List<Face> originalFaces
        , List<Face> clippedFaces   // inout
        , BspTree subtractedBspTree, int theirNodeIndex
        )
    {
        if (isInternalNodeIndex(theirNodeIndex))
        {
            Node splittingNode = subtractedBspTree.nodes[theirNodeIndex];
            Plane splittingPlane = splittingNode.plane;

            List<Face> facesOnPlane;
            List<Face> facesInFront;
            List<Face> facesBehind;

            splitFacesByPlane(originalFaces, splittingPlane,
                out facesOnPlane, out facesInFront, out facesBehind);

            Debug.LogFormat("theirNodeIndex={0}, plane={1}, front={2}, back={3}, originalFaces={4}, clippedFaces={5}"
                , theirNodeIndex, splittingPlane, facesInFront.Count, facesBehind.Count, originalFaces.Count, clippedFaces.Count);

            //
            clipFacesWithBspTreeRecursive(facesInFront, clippedFaces, subtractedBspTree, splittingNode.frontChild);

            clipFacesWithBspTreeRecursive(facesBehind, clippedFaces, subtractedBspTree, splittingNode.backChild);

            clippedFaces.AddRange(facesOnPlane);
        }
        else
        {
            if(theirNodeIndex == SOLID_LEAF_INDEX)
            {
                clippedFaces.AddRange(originalFaces);
            }
        }
    }

#endregion





    public Mesh generateMesh()
    {
        Mesh mesh = new Mesh();

        List<Vector3> tempVerticesList = new List<Vector3>();
        List<int> tempTrianglesList = new List<int>();

        foreach (Node node in nodes)
        {
            foreach (Face face in node.faces)
            {
                var numVertices = face.vertices.Count;

                var baseVertex = face.vertices[0];

#if false

                var baseVertexIndex = tempVerticesList.Count;
                tempVerticesList.Add(baseVertex);

                for (int i = 1; i < numVertices - 1; i++)
                {
                    var v1 = face.vertices[i + 0];
                    var v2 = face.vertices[i + 1];

                    tempVerticesList.Add(v1);
                    tempVerticesList.Add(v2);

                    //
                    tempTrianglesList.Add(baseVertexIndex);
                    tempTrianglesList.Add(baseVertexIndex + i);
                    tempTrianglesList.Add(baseVertexIndex + i + 1);
                }
#else
            
                for (int i = 1; i < numVertices - 1; i++)
                {
                    var v1 = face.vertices[i + 0];
                    var v2 = face.vertices[i + 1];

                    var baseVertexIndex = tempVerticesList.Count;
                    tempVerticesList.Add(baseVertex);
                    tempVerticesList.Add(v1);
                    tempVerticesList.Add(v2);

                    tempTrianglesList.Add(baseVertexIndex + 0);
                    tempTrianglesList.Add(baseVertexIndex + 1);
                    tempTrianglesList.Add(baseVertexIndex + 2);
                }
#endif
            }
        }

        mesh.vertices = tempVerticesList.ToArray<Vector3>() ;
        mesh.triangles = tempTrianglesList.ToArray();
        return mesh;
    }
}
