using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Assets;
using Assets.Helpers;
using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;


public class World : MonoBehaviour 
{
    public List<Polygon> Polygons { get; set; }
	void Start () 
    {
	    Polygons = new List<Polygon>();
        GameObject root = GameObject.Find("Polygons");
        foreach (var poly in root.GetComponentsInChildren<Transform>().Skip(1).Where(x => x.transform.parent == root.transform))
	    {
            var verts = poly.GetComponentsInChildren<Transform>().Where(x => x.name == "Cube").Select(v => v.transform.position.ToVector2XY()).ToList();
	        Polygons.Add(new Polygon(verts));

            var col = poly.gameObject.AddComponent<MeshCollider>() as MeshCollider;

            List<Vector3> vertexList = new List<Vector3>();
            List<int> triList = new List<int>();

            foreach(var v in verts)
            {
                vertexList.Add(v.ToVector3XY() + Vector3.forward - poly.transform.position);
                vertexList.Add(v.ToVector3XY() - Vector3.forward - poly.transform.position);
            }

            for(int i = 0; i < verts.Count; i++)
            {
                var i0 = i * 2;
                var i1 = i0 + 1;
                var i2 = (i0 + 2) % vertexList.Count;
                var i3 = (i0 + 3) % vertexList.Count;
                triList.AddRange(new [] {i0, i1, i2});
                triList.AddRange(new [] {i1, i3, i2});
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertexList.ToArray();
            mesh.SetTriangles(triList.ToArray(), 0);

            col.sharedMesh = mesh;
	    }
    }

    public void OnDrawGizmos()
    {
        var polygons = new List<Polygon>();

        foreach (var poly in GameObject.Find("Polygons").GetComponentsInChildren<Transform>().Skip(1))
        {
            var verts = poly.GetComponentsInChildren<Transform>().Where(x => x.name == "Cube").Select(v => v.transform.position.ToVector2XY()).ToList();
            polygons.Add(new Polygon(verts));
        }

        foreach (var polygon in polygons)
        {
            polygon.Draw();
        }
    }
}
