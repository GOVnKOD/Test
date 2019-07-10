using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class generate_plate : MonoBehaviour
{
    private Camera camera;
    // Use this for initialization
    void Start()
    {
        camera = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public int count = 0;
    void OnGUI()
    {
        int size = 50;
        float posX = camera.pixelWidth / 2 - size / 4;
        float posY = camera.pixelHeight / 2 - size / 2;
        GUI.contentColor = Color.black;
        GUI.Label(new Rect(posX, posY, size, size), "+");

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Vector3 point = new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2, 0);
            Ray ray = camera.ScreenPointToRay(point);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                StartCoroutine(SphereIndicator(hit.point));
            }
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            int tcount = 0;
            Vector3[] norm = GameObject.Find("Sphere_0").GetComponent<MeshFilter>().mesh.normals;
            print(GameObject.Find("Sphere_0"));
            print(norm.Length);
            foreach (Vector3 bIbIbI in norm)
            {
                print("count of normal:" + tcount);
                tcount += 1;
                print(bIbIbI);
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Vector3 point = new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2, 0);
            Ray ray = camera.ScreenPointToRay(point);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                StartCoroutine(SphereIndicator2(hit.point));
            }
        }
    }
    private IEnumerator SphereIndicator(Vector3 pos)
    {
        GameObject sphere = new GameObject();
        sphere.name = "Sphere_" + count.ToString();
        count += 1;
        Mesh mekka = new Mesh();
        Vector3[] vertices = {
            /*0*/ new Vector3 ( 0.5f,  0.5f, -0.5f),/*1*/ new Vector3 ( 0.5f, -0.5f, -0.5f),/*2*/ new Vector3 (-0.5f, -0.5f, -0.5f),/*3*/ new Vector3 (-0.5f,  0.5f, -0.5f),
            /*4*/ new Vector3 (-0.5f,  0.5f,  0.5f),/*5*/ new Vector3 (-0.5f, -0.5f,  0.5f),/*6*/ new Vector3 ( 0.5f, -0.5f,  0.5f),/*7*/ new Vector3 ( 0.5f,  0.5f,  0.5f),
            /*8*/ new Vector3 ( 0.5f,  0.5f, -0.5f),/*9*/ new Vector3 (-0.5f,  0.5f, -0.5f),/*10*/new Vector3 (-0.5f,  0.5f,  0.5f),/*11*/new Vector3 ( 0.5f,  0.5f,  0.5f),
            /*12*/new Vector3 ( 0.5f, -0.5f,  0.5f),/*13*/new Vector3 (-0.5f, -0.5f,  0.5f),/*14*/new Vector3 (-0.5f, -0.5f, -0.5f),/*15*/new Vector3 ( 0.5f, -0.5f, -0.5f),
            /*16*/new Vector3 ( 0.5f,  0.5f,  0.5f),/*17*/new Vector3 ( 0.5f, -0.5f,  0.5f),/*18*/new Vector3 ( 0.5f, -0.5f, -0.5f),/*19*/new Vector3 ( 0.5f,  0.5f, -0.5f),
            /*20*/new Vector3 (-0.5f,  0.5f, -0.5f),/*21*/new Vector3 (-0.5f, -0.5f, -0.5f),/*22*/new Vector3 (-0.5f, -0.5f,  0.5f),/*23*/new Vector3 (-0.5f,  0.5f,  0.5f)
        };
        int[] triangel = {
             0,  1,  2,
             0,  2,  3,
             4,  5,  6,
             4,  6,  7,
             8,  9, 10,
             8, 10, 11,
            12, 13, 14,
            12, 14, 15,
            16, 17, 18,
            16, 18, 19,
            20, 21, 22,
            20, 22, 23
        };
        mekka.vertices = vertices;
        mekka.SetTriangles(triangel, 0);
        sphere.AddComponent<MeshFilter>();
        sphere.AddComponent<MeshRenderer>();
        UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        sphere.transform.localScale = new Vector3(2, 2, 2);
        sphere.GetComponent<MeshRenderer>().shadowCastingMode = shadowCastingMode;
        sphere.GetComponent<MeshRenderer>().materials = GameObject.Find("Cube1").GetComponent<MeshRenderer>().materials;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = GameObject.Find("Cube1").GetComponent<MeshRenderer>().sharedMaterial;
        sphere.AddComponent<BoxCollider>().size = new Vector3(1, 1, 1);
        sphere.GetComponent<MeshFilter>().mesh = mekka;
		sphere.GetComponent<MeshFilter>().mesh.uv =sphere.GetComponent<MeshFilter>().mesh.GetUVs(0,GameObject.Find("Cube1").GetComponent<MeshFilter>().mesh.uv);
		//GameObject.Find("Cube1").GetComponent<MeshFilter>().mesh.uv;
        sphere.transform.position = pos;
        yield return new WaitForSeconds(1);
        //Destroy(sphere);
    }
    private IEnumerator SphereIndicator2(Vector3 pos)
    {
        GameObject sphere = new GameObject();
        sphere.name = "Sphere_" + count.ToString();
        count += 1;
        Mesh mekka = new Mesh();
        Vector3[] vertices = {
            /*0*/ new Vector3 ( 0.5f,  0.5f, -0.5f),/*1*/ new Vector3 ( 0.5f, -0.5f, -0.5f),/*2*/ new Vector3 (-0.5f, -0.5f, -0.5f),/*3*/ new Vector3 (-0.5f,  0.5f, -0.5f),
            /*4*/ new Vector3 (-0.5f,  0.5f,  0.5f),/*5*/ new Vector3 (-0.5f, -0.5f,  0.5f),/*6*/ new Vector3 ( 0.5f, -0.5f,  0.5f),/*7*/ new Vector3 ( 0.5f,  0.5f,  0.5f),
            /*8*/ new Vector3 ( 0.5f,  0.5f, -0.5f),/*9*/ new Vector3 (-0.5f,  0.5f, -0.5f),/*10*/new Vector3 (-0.5f,  0.5f,  0.5f),/*11*/new Vector3 ( 0.5f,  0.5f,  0.5f),
            /*12*/new Vector3 ( 0.5f, -0.5f,  0.5f),/*13*/new Vector3 (-0.5f, -0.5f,  0.5f),/*14*/new Vector3 (-0.5f, -0.5f, -0.5f),/*15*/new Vector3 ( 0.5f, -0.5f, -0.5f),
            /*16*/new Vector3 ( 0.5f,  0.5f,  0.5f),/*17*/new Vector3 ( 0.5f, -0.5f,  0.5f),/*18*/new Vector3 ( 0.5f, -0.5f, -0.5f),/*19*/new Vector3 ( 0.5f,  0.5f, -0.5f),
            /*20*/new Vector3 (-0.5f,  0.5f, -0.5f),/*21*/new Vector3 (-0.5f, -0.5f, -0.5f),/*22*/new Vector3 (-0.5f, -0.5f,  0.5f),/*23*/new Vector3 (-0.5f,  0.5f,  0.5f)
        };
        int[] triangel = {
             2,  1,  0,
             3,  2,  0,
             6,  5,  4,
             7,  6,  4,
            10,  9,  8,
            11, 10,  8,
            14, 13, 12,
            15, 14, 12,
            18, 17, 16,
            19, 18, 16,
            22, 21, 20,
            23, 22, 20
        };
        mekka.vertices = vertices;
        mekka.SetTriangles(triangel, 0);
        sphere.AddComponent<MeshFilter>();
        sphere.AddComponent<MeshRenderer>();
        UnityEngine.Rendering.ShadowCastingMode shadowCastingMode;
        shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        sphere.transform.localScale = new Vector3(2, 2, 2);
        sphere.GetComponent<MeshRenderer>().shadowCastingMode = shadowCastingMode;
        sphere.GetComponent<MeshRenderer>().materials = GameObject.Find("Cube1").GetComponent<MeshRenderer>().materials;
        sphere.GetComponent<MeshRenderer>().sharedMaterial = GameObject.Find("Cube1").GetComponent<MeshRenderer>().sharedMaterial;
        sphere.AddComponent<BoxCollider>().size = new Vector3(1, 1, 1);
        sphere.GetComponent<MeshFilter>().mesh = mekka;
        sphere.GetComponent<MeshFilter>().mesh.uv =GameObject.Find("Cube1").GetComponent<MeshFilter>().mesh.uv;
        sphere.transform.position = pos;
        yield return new WaitForSeconds(1);
        //Destroy(sphere);
    }
}
