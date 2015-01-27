using UnityEngine;

namespace Assets
{
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
            go = new GameObject();
            go.transform.position = this.position;
            var c = this.go.AddComponent<SphereCollider>();
            c.radius = .5f;
        }

        public void Destroy()
        {
            GameObject.Destroy(go);
        }
    }
}
