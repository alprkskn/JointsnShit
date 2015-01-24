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

    HingeJoint hj;
    Plane plane;
    List<Anchor> anchors;
    World world;
    float threshold = 0.2f;
    float length;
	// Use this for initialization
	void Start ()
    {
        world = GameObject.Find("World").GetComponent<World>();
        anchors = new List<Anchor>();
        hj = this.GetComponent<HingeJoint>();
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
        hj.connectedAnchor = pos;
        hj.anchor = pos - this.transform.position;
        this.length = hj.anchor.magnitude;
    }

    private void PopAnchor()
    {
        var a = this.anchors.Last();
        a.Destroy();
        this.anchors.RemoveAt(this.anchors.Count - 1);
        hj.connectedAnchor = this.anchors.Last().position;
        hj.anchor = hj.connectedAnchor - this.transform.position;
        this.length = hj.anchor.magnitude;
    }

	// Update is called once per frame
	void Update ()
    {
        //if(Input.GetMouseButtonDown(0))
        //{
        //    float dist;
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //    plane.Raycast(ray, out dist);

        //    Vector3 anchorPos = ray.GetPoint(dist);
        //    Debug.Log(anchorPos);
        //    SetAnchor(anchorPos);
        //    //hj.connectedAnchor = anchorPos;
        //}

        foreach (var v in world.Polygons.SelectMany(x => x.Vertices))
        {
            if (v == hj.connectedAnchor.ToVector2XY())
                continue;
            //hj.connectedAnchor
            if (v.FindDistanceToSegment(this.transform.position.ToVector2XY(), hj.connectedAnchor.ToVector2XY()) < threshold)
            {
                //Debug.DrawLine(transform.position, hj.connectedAnchor, Color.red, 1f);
                //Debug.DrawLine(transform.position, v.ToVector3XY(), Color.green, 1f);
                CreateNewAnchor(v);
                break;
            }
        }

        if (this.anchors.Count > 1)
        {
            var prevAnchor = this.anchors[this.anchors.Count - 2];
            RaycastHit hitInfo;
            //Debug.DrawLine(this.transform.position, prevAnchor.position);
            if (Physics.Raycast(this.transform.position, prevAnchor.position - this.transform.position, out hitInfo))
            {
                //Debug.Log("Hit: " + hitInfo.collider.name);
                if (hitInfo.collider == prevAnchor.go.collider)
                {
                    PopAnchor();
                }
            }
        }

        //foreach(var l in world.Polygons.SelectMany(x => x.Edges))
        //{
        //    Vector2 c;
        //    if (transform.position.ToVector2XY().FindDistanceToSegment(l.Start, l.End, out c) < threshold)
        //    {
                
        //        var vel = hj.gameObject.GetComponent<Rigidbody>().velocity * -0.3f;
        //        if (vel.magnitude > 10)
        //            vel = vel.normalized * 10;
        //        else if (vel.magnitude < 1f)
        //        {
        //            vel = Vector3.zero;
        //        }
        //        else
        //        {
        //            hj.gameObject.GetComponent<Rigidbody>().velocity = vel;
        //            this.transform.position = c.ToVector3XY() + vel.normalized;
        //        }
        //    }
        //}
	}

    void OnDrawGizmos()
    {
        if(Application.isPlaying)
        {
            Gizmos.DrawWireSphere(hj.connectedAnchor, .3f);
            for(int i = 0; i < anchors.Count - 1; i++)
            {
                Debug.DrawLine(this.anchors[i].position, this.anchors[i + 1].position);
            }
            Gizmos.DrawLine(hj.connectedAnchor, this.transform.position);
        }
    }
}
