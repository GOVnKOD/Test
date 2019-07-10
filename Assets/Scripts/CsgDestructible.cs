using System.Collections;
using System.Collections.Generic;
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

        bspTree.buildFromMesh(mesh);
    }
}

class BspTree
{
    class Node
    {
        public int planeIndex;
        public int frontChild;
        public int backChild;
        public ArrayList tmp;
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

    public void buildFromMesh(Mesh mesh)
    {
        var triangleList = buildTriangleListFromMesh(mesh);

        var rootNodeIndex = createNodeFromTriangleList_Recursive(triangleList);
        Debug.Assert(rootNodeIndex == 0, "The root node must have zero index!");
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

    private int createNodeFromTriangleList_Recursive(List<Triangle> triangleList)
    {
        int splittingPlaneIndex = pickSplittingPlane(triangleList);

        Triangle splittingTriangle = triangleList[splittingPlaneIndex];
        Plane splittingPlane = createPlaneFromTriangle(splittingTriangle);

        var trianglesInFront = new List<Triangle>();
        var trianglesBehind = new List<Triangle>();
        var splitTriangles = new List<Triangle>();
        classifyTriangles(triangleList, splittingPlane,
            trianglesInFront, trianglesBehind, splitTriangles);
        //
        int newNodeIndex = createNewNode();
        
        nodes[newNodeIndex].planeIndex = splittingPlaneIndex;

        if(trianglesInFront.Count == 0)
        {
            nodes[newNodeIndex].frontChild = EMPTY_LEAF_INDEX;
        }
        else
        {
            nodes[newNodeIndex].frontChild = createNodeFromTriangleList_Recursive(trianglesInFront);
        }

        if(trianglesBehind.Count == 0)
        {
            nodes[newNodeIndex].frontChild = SOLID_LEAF_INDEX;
        }
        else
        {
            nodes[newNodeIndex].frontChild = createNodeFromTriangleList_Recursive(trianglesInFront);
        }

        return newNodeIndex;
    }

    /// <summary>
    ///  returns the (unique) index of the splitting plane
    /// </summary>
    /// <param name="triangleList"></param>
    /// <returns></returns>
    private int pickSplittingPlane(List<Triangle> triangleList)
    {
        //
        return 0;
    }

    private Plane createPlaneFromTriangle(Triangle splittingTriangle)
    {
        //
        return new Plane(new Vector3(0,1,0), 0);
    }


    private int createNewNode()
    {
        var newNodeIndex = nodes.Count;

        nodes.Add(new Node());

        return newNodeIndex;
    }
}
