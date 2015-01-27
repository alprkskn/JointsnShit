using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets;
using Assets.Helpers;

public class Anchor
{
    public Vector3 position;
    public Vector3 dir;
    public GameObject go;
    public float length;

    public Anchor(Vector3 position, Vector3 dir)
    {
        this.position = position;
        this.dir = dir;
        this.go = new GameObject();
        this.go.transform.position = this.position;
        var c = this.go.AddComponent<SphereCollider>();
        c.radius = .5f;
    }

    public void Destroy()
    {
        GameObject.Destroy(this.go);
    }
}

public class MouseAnchor : MonoBehaviour
{
    public Pendulum _pendulum;
    public GameObject _pivot;
    //ConfigurableJoint hj;
    Plane plane;
    List<Anchor> anchors;
    World world;
    float threshold = 0.2f;
    float length;
	// Use this for initialization
	void Start ()
    {
        _pendulum = this.gameObject.GetComponent<Pendulum>();
	    _pivot = GameObject.Find("Anchor");
        world = GameObject.Find("World").GetComponent<World>();
        anchors = new List<Anchor>();
        //hj = this.GetComponent<ConfigurableJoint>();
        plane = new Plane(Vector3.forward, Vector3.zero);
        CreateNewAnchor(Vector3.zero);
	}
	

    private void PushAnchor(Vector3 pos)
    {
        if(anchors.Count > 0)
        {
            var a = this.anchors.Last();
            this.anchors.Add(new Anchor(pos, pos - a.position));
        }
        else
        {
            this.anchors.Add(new Anchor(pos, Vector3.zero));
        }
    }

    private void CreateNewAnchor(Vector3 pos)
    {
        PushAnchor(pos);
        _pivot.transform.position = pos;
        _pendulum.Pivot = _pivot;
        _pendulum.ResetRopeLength();
    }

    private void PopAnchor()
    {
        var a = this.anchors.Last();
        a.Destroy();
        this.anchors.RemoveAt(this.anchors.Count - 1);
        _pivot.transform.position = this.anchors.Last().position;
        _pendulum.Pivot = _pivot;
        _pendulum.ResetRopeLength();
    }

	void Update ()
    {
        foreach (var v in world.Polygons.SelectMany(x => x.Vertices))
        {
            if (_pendulum.Pivot != null && v == _pendulum.Pivot.transform.position.ToVector2XY())
                continue;
            if (_pendulum.Pivot != null && v.FindDistanceToSegment(this.transform.position.ToVector2XY(), _pendulum.Pivot.transform.position.ToVector2XY()) < threshold)
            {
                CreateNewAnchor(v);
                break;
            }
        }

        if (this.anchors.Count > 1)
        {
            var prevAnchor = this.anchors[this.anchors.Count - 2];
            RaycastHit hitInfo;
            if (Physics.Raycast(this.transform.position, prevAnchor.position - this.transform.position, out hitInfo))
            {
                if (hitInfo.collider == prevAnchor.go.collider)
                {
                    PopAnchor();
                }
            }
        }

        foreach (var l in world.Polygons.SelectMany(x => x.Edges))
        {
            Vector2 c;
            var pos = transform.position.ToVector2XY();
            if (pos.FindDistanceToSegment(l.Start, l.End, out c) < 1 &&
                MyExtensions.FasterLineSegmentIntersection(pos,pos+_pendulum.currentVelocity.ToVector2XY() * 5, l.Start, l.End))
            {
                var vel = _pendulum.currentVelocity * -0.3f;
                if (vel.magnitude > 10)
                    vel = vel.normalized * 10;
                else if (vel.magnitude < 1f)
                {
                    vel = Vector3.zero;
                }
                _pendulum.currentVelocity = vel;
            }
        }

	    if (Input.GetMouseButtonDown(1))
	    {
            anchors.Clear();
	        _pendulum.Pivot = null;
	    } 
        else if (Input.GetMouseButtonDown(0))
        {
            float dist;
            Vector3 mosPos = Vector3.zero;
            Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
            if (plane.Raycast(ray, out dist))
            {
                mosPos = ray.GetPoint(dist);
            }

            RaycastHit hitInfo;
            if (Physics.Raycast(this.transform.position, mosPos - this.transform.position, out hitInfo))
            {
                CreateNewAnchor(hitInfo.point);
                _pivot.transform.position = hitInfo.point;
                _pendulum.Pivot = _pivot;
                _pendulum.ResetRopeLength();
                
            }
        }
	}

    void OnDrawGizmos()
    {
        if (Application.isPlaying && _pendulum.Pivot != null)
        {
            Gizmos.DrawWireSphere(_pendulum.Pivot.transform.position, .3f);
            for(int i = 0; i < anchors.Count - 1; i++)
            {
                Debug.DrawLine(this.anchors[i].position, this.anchors[i + 1].position);
            }
            Gizmos.DrawLine(_pendulum.Pivot.transform.position, this.transform.position);
        }
    }
}
