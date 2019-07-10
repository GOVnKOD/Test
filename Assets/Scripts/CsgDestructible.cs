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

    private void buildBspTree()
    {
        //int[] triangles = cubic.GetComponent<MeshFilter>().mesh.triangles;
        //Vector3[] vector3 = cubic.GetComponent<MeshFilter>().mesh.vertices;
    }
}

class BspTree
{
    struct Node
    {
        uint planeIndex;
        uint frontChild;
        uint backChild;
        ArrayList tmp;
    }

    Node[]  nodes = new Node[] {};
    Plane[] planes = new Plane[] { };
}