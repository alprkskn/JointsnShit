using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Helpers;
using UnityEngine;

namespace Assets
{
    public class Line
    {
        public Vector2 Key { get; set; }
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }

        public Line(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
            Key = (start + end)/2;
        }

        public bool Contains(Vector2 ch)
        {
            return Start == ch || End == ch;
        }

        public bool CutsPoly(Polygon pol)
        {
            foreach (var edge in pol.Edges.Where(x => !(x.Contains(Start) || x.Contains(End))))
            {
                if (this.LineSegmentsCross(edge))
                    return true;
            }
            return false;
        }
    }

    public class Polygon
    {
        public List<Vector2> Vertices { get; set; }
        public List<Line> Edges { get; set; } 

        public Polygon(List<Vector2> verts)
        {
            Vertices = verts;
            Edges = new List<Line>();

            for (int i = 0; i < Vertices.Count; i++)
            {
                var v1 = Vertices[i];
                var v2 = Vertices[(i + 1) % Vertices.Count];
                Edges.Add(new Line(v1, v2));
            }
        }

        public void Draw()
        {
            
            for (int i = 0; i < Vertices.Count; i++)
            {
                var v1 = Vertices[i];
                var v2 = Vertices[(i + 1) % Vertices.Count];
                Debug.DrawLine(v1, v2);
            }
        }
    }
}
