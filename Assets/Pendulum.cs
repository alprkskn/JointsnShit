using System.Collections.Generic;
using System.Linq;
using Assets.Helpers;
using UnityEngine;

// Author: Eric Eastwood (ericeastwood.com)
//
// Description:
// Written for this gd.se question: http://gamedev.stackexchange.com/a/75748/16587
// Simulates/Emulates pendulum motion in code
// Works in any 3D direction and with any force/direciton of gravity
//
// Demonstration: https://i.imgur.com/vOQgFMe.gif
//
// Usage: https://i.imgur.com/BM52dbT.png
namespace Assets
{
    public class Pendulum : MonoBehaviour
    {
        public GameObject Pivot;
        public GameObject Bob;

        private GameObject _fakePivot;
        private World _world;
        private List<Anchor> Anchors { get; set; }
        private Plane _plane;

        private float Mass = 1f;
        private const float PullForce = 80f;
        private const float ReleaseSpeed = 0.3f;

        private float _time = 0f;
        private const float Dt = 0.01f;
        private float _currentTime = 0f;
        private float _accumulator = 0f;

        private float _distanceCheckThreshold = 0.2f;
        private float _ropeLength = 2f;
        private bool _bobStartingPositionSet = false;
        private float _tensionForce = 0f;
        private float _gravityForce = 0f;
        private Vector3 _bobStartingPosition;
        private Vector3 _gravityDirection;
        private Vector3 _tensionDirection;
        private Vector3 _tangentDirection;
        private Vector3 _pendulumSideDirection;
        private Vector3 _currentStatePosition;
        private Vector3 _previousStatePosition;

        public Vector3 CurrentVelocity { get; set; }
        void Start()
        {
            Anchors = new List<Anchor>();
            _fakePivot = new GameObject();
            _world = GameObject.Find("World").GetComponent<World>();
            _plane = new Plane(Vector3.forward, Vector3.zero);
            _bobStartingPosition = this.Bob.transform.position;
            _bobStartingPositionSet = true;
            CreateNewAnchor(Vector3.zero);
            this.PendulumInit();
        }
    
        void Update()
        {
            foreach (var v in _world.Polygons.SelectMany(x => x.Vertices))
            {
                if (Pivot != null && v == Pivot.transform.position.ToVector2XY())
                    continue;
                if (Pivot != null && v.FindDistanceToSegment(transform.position.ToVector2XY(), Pivot.transform.position.ToVector2XY()) < _distanceCheckThreshold)
                {
                    CreateNewAnchor(v);
                    break;
                }
            }

            // check and delete unnecessary anchors, this can/should be improved using angle checks
            if (Anchors.Count > 1)
            {
                var prevAnchor = Anchors[Anchors.Count - 2];
                RaycastHit hitInfo;
                if (Physics.Raycast(this.transform.position, prevAnchor.position - this.transform.position, out hitInfo))
                {
                    if (hitInfo.collider == prevAnchor.go.collider)
                    {
                        PopAnchor();
                    }
                }
            } 

            //ball&wall collision, should be rewritten properly
            foreach (var l in _world.Polygons.SelectMany(x => x.Edges))
            {
                Vector2 c;
                var pos = transform.position.ToVector2XY();
                if (pos.FindDistanceToSegment(l.Start, l.End, out c) < 1 &&
                    MyExtensions.FasterLineSegmentIntersection(pos, pos + CurrentVelocity.ToVector2XY() * 5, l.Start, l.End))
                {
                    var vel = CurrentVelocity * -0.3f;
                    if (vel.magnitude > 10)
                        vel = vel.normalized * 10;
                    else if (vel.magnitude < 1f)
                    {
                        vel = Vector3.zero;
                    }
                    CurrentVelocity = vel;
                }
            }

            HandleInput();

            //physics, beware
            float frameTime = Time.time - _currentTime;
            _currentTime = Time.time;
            _accumulator += frameTime;
            while (_accumulator >= Dt)
            {
                _previousStatePosition = _currentStatePosition;
                _currentStatePosition = (Pivot != null)
                    ? PendulumUpdate(_currentStatePosition, Dt, Input.GetKey(KeyCode.Space))
                    : FreefallUpdate(_currentStatePosition, Dt);
                _accumulator -= Dt;
                _time += Dt;
            }
            float alpha = _accumulator / Dt;
            Vector3 newPosition = _currentStatePosition * alpha + _previousStatePosition * (1f - alpha);
            Bob.transform.position = newPosition;
        }

        private void HandleInput()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                _ropeLength += ReleaseSpeed;
            }

            if (Input.GetMouseButtonDown(1))
            {
                Anchors.Clear();
                Pivot = null;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Anchors.Clear();
                Pivot = null;
                float dist;
                Vector3 mosPos = Vector3.zero;
                Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
                if (_plane.Raycast(ray, out dist))
                {
                    mosPos = ray.GetPoint(dist);
                }

                RaycastHit hitInfo;
                if (Physics.Raycast(this.transform.position, mosPos - this.transform.position, out hitInfo))
                {
                    CreateNewAnchor(hitInfo.point);
                    Pivot.transform.position = hitInfo.point;
                    ResetRopeLength();
                }
            }
        }

        private void PushAnchor(Vector3 pos)
        {
            if (Anchors.Count > 0)
            {
                var a = Anchors.Last();
                Anchors.Add(new Anchor(pos, pos - a.position));
            }
            else
            {
                Anchors.Add(new Anchor(pos, Vector3.zero));
            }
        }

        private void CreateNewAnchor(Vector3 pos)
        {
            PushAnchor(pos);
            _fakePivot.transform.position = pos;
            Pivot = _fakePivot;
            ResetRopeLength();
        }

        private void PopAnchor()
        {
            var a = Anchors.Last();
            a.Destroy();
            Anchors.RemoveAt(Anchors.Count - 1);
            _fakePivot.transform.position = Anchors.Last().position;
            Pivot = _fakePivot;
            ResetRopeLength();
        }

        private void ResetPendulumForces()
        {
            this.CurrentVelocity = Vector3.zero;
            _currentStatePosition = this.Bob.transform.position;
        }

        public void ResetRopeLength()
        {
            _ropeLength = Vector3.Distance(Pivot.transform.position, Bob.transform.position);
        }

        private void PendulumInit()
        {
            this.ResetRopeLength();
            this.ResetPendulumForces();
        }

        private void MoveBob(Vector3 resetBobPosition)
        {
            this.Bob.transform.position = resetBobPosition;
            _currentStatePosition = resetBobPosition;
        }

        private Vector3 FreefallUpdate(Vector3 currentStatePosition, float deltaTime)
        {
            _gravityForce = Mass * Physics.gravity.magnitude;
            _gravityDirection = Physics.gravity.normalized;
            CurrentVelocity += _gravityDirection * _gravityForce * deltaTime;

            return currentStatePosition + this.CurrentVelocity * deltaTime;
        }

        private Vector3 PendulumUpdate(Vector3 currentStatePosition, float deltaTime, bool pull)
        {
            _gravityForce = Mass * Physics.gravity.magnitude;
            _gravityDirection = Physics.gravity.normalized;
            CurrentVelocity += _gravityDirection * _gravityForce * deltaTime ;
            var pivotP = Pivot.transform.position;
            var bobP = _currentStatePosition;
            var auxiliaryMovementDelta = CurrentVelocity * deltaTime;
            float distanceAfterGravity = Vector3.Distance(pivotP, bobP + auxiliaryMovementDelta);
        
            if (distanceAfterGravity > _ropeLength || Mathf.Approximately(distanceAfterGravity, _ropeLength))
            {
                _tensionDirection = (pivotP - bobP).normalized;
                _pendulumSideDirection = (Quaternion.Euler(0f, 90f, 0f) * _tensionDirection);
                _pendulumSideDirection.Scale(new Vector3(1f, 0f, 1f));
                _pendulumSideDirection.Normalize();
                _tangentDirection = (-1f * Vector3.Cross(_tensionDirection, _pendulumSideDirection)).normalized;
                var inclinationAngle = Vector3.Angle(bobP - pivotP, _gravityDirection);
                _tensionForce = Mass * Physics.gravity.magnitude * Mathf.Cos(Mathf.Deg2Rad * inclinationAngle);
                var centripetalForce = ((Mass * Mathf.Pow(CurrentVelocity.magnitude, 2)) / _ropeLength);
                _tensionForce += centripetalForce;
                CurrentVelocity += _tensionDirection * _tensionForce * deltaTime;
            }
            if (pull)
            {
                CurrentVelocity += _tensionDirection * PullForce * deltaTime;
            }
        
            var movementDelta = Vector3.zero;
            movementDelta += CurrentVelocity * deltaTime;
            float distance = Vector3.Distance(pivotP, currentStatePosition + movementDelta);
            if (!pull)
            {
                return GetPointOnLine(pivotP, currentStatePosition + movementDelta, distance <= _ropeLength ? distance : _ropeLength);
            }
            else
            {
                return _currentStatePosition + movementDelta;
            }
        }

        Vector3 GetPointOnLine(Vector3 start, Vector3 end, float distanceFromStart)
        {
            return start + (distanceFromStart * Vector3.Normalize(end - start));
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying && Pivot != null)
            {
                Gizmos.DrawWireSphere(Pivot.transform.position, .3f);
                for (int i = 0; i < Anchors.Count - 1; i++)
                {
                    Debug.DrawLine(Anchors[i].position, Anchors[i + 1].position);
                }
                Gizmos.DrawLine(Pivot.transform.position, this.transform.position);
            }
        }

        //void OnDrawGizmos()
        //{
        //    if (this.Pivot == null)
        //        return;

        //    Gizmos.color = new Color(.5f, 0f, .5f);
        //    Gizmos.DrawWireSphere(this.Pivot.transform.position, _ropeLength);
        //    Gizmos.DrawWireCube(_bobStartingPosition, new Vector3(.5f, .5f, .5f));
        //    // Blue: Auxilary
        //    Gizmos.color = new Color(.3f, .3f, 1f); // blue
        //    Vector3 auxVel = .3f * this.CurrentVelocity;
        //    Gizmos.DrawRay(this.Bob.transform.position, auxVel);
        //    Gizmos.DrawSphere(this.Bob.transform.position + auxVel, .2f);
        //    // Yellow: Gravity
        //    Gizmos.color = new Color(1f, 1f, .2f);
        //    Vector3 gravity = .3f * _gravityForce * _gravityDirection;
        //    Gizmos.DrawRay(this.Bob.transform.position, gravity);
        //    Gizmos.DrawSphere(this.Bob.transform.position + gravity, .2f);
        //    // Orange: Tension
        //    Gizmos.color = new Color(1f, .5f, .2f); // Orange
        //    Vector3 tension = .3f * _tensionForce * _tensionDirection;
        //    Gizmos.DrawRay(this.Bob.transform.position, tension);
        //    Gizmos.DrawSphere(this.Bob.transform.position + tension, .2f);
        //    // Red: Resultant
        //    Gizmos.color = new Color(1f, .3f, .3f); // red
        //    Vector3 resultant = gravity + tension;
        //    Gizmos.DrawRay(this.Bob.transform.position, resultant);
        //    Gizmos.DrawSphere(this.Bob.transform.position + resultant, .2f);
        //    /* * /
        //    // Green: Pendulum side direction
        //    Gizmos.color = new Color(.3f, 1f, .3f);
        //    Gizmos.DrawRay(this.Bob.transform.position, 3f*_pendulumSideDirection);
        //    Gizmos.DrawSphere(this.Bob.transform.position + 3f*_pendulumSideDirection, .2f);
        //    /* */
        //    /* * /
        //    // Cyan: tangent direction
        //    Gizmos.color = new Color(.2f, 1f, 1f); // cyan
        //    Gizmos.DrawRay(this.Bob.transform.position, 3f*_tangentDirection);
        //    Gizmos.DrawSphere(this.Bob.transform.position + 3f*_tangentDirection, .2f);
        //    /* */
        //}
    }
}