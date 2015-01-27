using System.Collections.Generic;
using System.Linq;
using Assets.Helpers;
using UnityEngine;

namespace Assets
{
    public class World : MonoBehaviour
    {
        public bool DrawPolygons;
        public List<Polygon> Polygons { get; set; }
        void Start () 
        {
            Polygons = new List<Polygon>();
            foreach (var poly in GetComponentsInChildren<Transform>().Skip(1).Where(x => x.transform.parent == this.transform))
            {
                var verts = poly.GetComponentsInChildren<Transform>().Where(x => x.name == "Cube").Select(v => v.transform.position.ToVector2XY()).ToList();
                Polygons.Add(new Polygon(verts));
                var col = poly.gameObject.AddComponent<MeshCollider>();
                var vertexList = new List<Vector3>();
                var triList = new List<int>();

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

                var mesh = new Mesh();
                mesh.vertices = vertexList.ToArray();
                mesh.SetTriangles(triList.ToArray(), 0);
                col.sharedMesh = mesh;
            }
        }

        public void OnDrawGizmos()
        {
            if (DrawPolygons)
            {
                var polygons = new List<Polygon>();
                foreach (var poly in GetComponentsInChildren<Transform>().Skip(1))
                {
                    var verts =
                        poly.GetComponentsInChildren<Transform>()
                            .Where(x => x.name == "Cube")
                            .Select(v => v.transform.position.ToVector2XY())
                            .ToList();
                    polygons.Add(new Polygon(verts));
                }

                foreach (var polygon in polygons)
                {
                    polygon.Draw();
                }
            }
        }
    }
}
