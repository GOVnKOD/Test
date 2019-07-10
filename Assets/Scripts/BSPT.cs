using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BSPT : MonoBehaviour
{
    public class plane
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double D { get; set; }
        public plane()
        {

        }
        //РАСЧЕТ ПЛОСКОСТИ
        public plane(Vector3 vec0, Vector3 vec1, Vector3 vec2)
        {
            X = (vec1[1] - vec0[1]) * (vec2[2] - vec0[2]) - (vec2[1] - vec0[1]) * (vec1[2] - vec0[2]);
            Y = -((vec1[0] - vec0[0]) * (vec2[2] - vec0[2]) - (vec2[0] - vec0[0]) * (vec1[2] - vec0[2]));
            
            Z = (vec1[0] - vec0[0]) * (vec2[1] - vec0[1]) - (vec2[0] - vec0[0]) * (vec1[1] - vec0[1]);
            D = (-vec0[0]) * X + (-(vec0[1]) * Y) + ((-vec0[2]) * Z);
            print("X*(-vec0[0])=" + X*(-vec0[0]));
            print("Y*(-vec0[1])=" + ((-vec0[1])*Y));
            print("Z*(-vec0[2])=" + Z*(-vec0[2]));
            print("D:" + D.ToString());
        }

    }

    public class leave
    {
        public
        uint planeIndex;
        uint frontChild;
        uint backChild;
        Vector3[][] polygon;
        public leave(uint pInd, uint fChild, uint bChild, Vector3[][] poly)
        {
            planeIndex = pInd;
            frontChild = fChild;
            backChild = bChild;
            polygon = poly;
        }
    }
    //ПРОВЕРКА, НАХОДИТСЯ ЛИ ТОЧКА НА ПЛОСКОСТИ
    bool isPointOnPlane(Vector3 vec, plane pl)
    {
        if (pl.X * vec[0] + pl.Y * vec[1] + pl.Z * vec[2] + pl.D == 0)
            return true;
        else
            return false;
    }
    //ПРОВЕРКА, НАХОДИТСЯ ЛИ ТРЕУГОЛЬНИК НА ПЛОСКОСТИ
    bool isTriangleOnPlane(Vector3[] vec, plane pl)
    {
        if ((pl.X * vec[0].x + pl.Y * vec[0].y + pl.Z * vec[0].z + pl.D == 0) &&
            (pl.X * vec[1].x + pl.Y * vec[1].y + pl.Z * vec[1].z + pl.D == 0) &&
            (pl.X * vec[2].x + pl.Y * vec[2].y + pl.Z * vec[2].z + pl.D == 0)
            )
            return true;
        else
            return false;
    }
    Vector3[][] getPoligon(plane pl)
    {
        Vector3[][] vec= { };

        return vec;
    }
    public plane[] planes;
    public leave[] leaves = { };

    // Start is called before the first frame update
    void Start()
    {
        List <plane> planes =new List<plane>{};
        GameObject cubic = GameObject.Find("Cube");
        int[] triangles = cubic.GetComponent<MeshFilter>().mesh.triangles;
        Vector3[] vector3 = cubic.GetComponent<MeshFilter>().mesh.vertices;
        //ОБХОД ПЛОСКОСТЕЙ
        for(int i=0; i<triangles.Length;i+=3)
        {
            print("tr1:" + vector3[triangles[i]].ToString() + "tr2:" + vector3[triangles[i + 1]].ToString() + "tr3:" + vector3[triangles[i + 2]].ToString());
            bool havePlane = false;
            for (int j=0;j<planes.Count;j++)
            {

                havePlane = havePlane || isTriangleOnPlane(new Vector3[] {vector3[triangles[i]],vector3[triangles[i+1]],vector3[triangles[i+2]] }, planes[j]);
                
            }
            if (!havePlane)
            {
                planes.Add(new plane(vector3[triangles[i]], vector3[triangles[i + 1]], vector3[triangles[i + 2]]));
            }
            print(planes.Count);
        }
        ////ВВЫВОД ПЛОСКОСТЕЙ
        //for (int i = 0; i < planes.Count; i++)
        //{
        //    print("x:" + planes[i].X.ToString() + "y:" + planes[i].Y.ToString() + " z:" + planes[i].Z.ToString() + " D:" + planes[i].D.ToString());
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
