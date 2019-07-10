using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class prostr_spheri : MonoBehaviour
{
    Vector3[] mvertices;
    int press;
    Vector2[] muv;
    // Start is called before the first frame update
    void Start()
    {
        Mesh cubek = GameObject.Find("Cube").GetComponent<MeshFilter>().mesh;
        muv = cubek.uv;
        mvertices = cubek.vertices;
        press = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.GetComponent<generate_plate>().count > 0)
        {
            Mesh cubik = GameObject.Find("Cube").GetComponent<MeshFilter>().mesh;
            int[] triangles = cubik.triangles;
            Vector2[] uvs = muv;
            //Собрал куб по частям
            if (Input.GetKeyDown(KeyCode.E))
            {
                press++;
                Debug.Log(press);
                Vector3[] vertices1 = new Vector3[mvertices.Length];
                if (press > mvertices.Length + 1)
                {
                    press = 0;
                }
                for (int i = 0; i < press; i++)
                {
                    vertices1[i] = mvertices[i];
                }
                Debug.Log("vse");
                cubik.vertices = vertices1;
            }
            //Верни куб на место!
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.ClearDeveloperConsole();
                press = 0;
                cubik.vertices = mvertices;
            }
            //BSP - деревце(возможно)
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.ClearDeveloperConsole();
                //все херня давай по новой
            }
        }

    }
}