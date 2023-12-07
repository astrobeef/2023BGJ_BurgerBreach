using Godot;

namespace Utility
{
    public struct Hit3D
    {
        public bool didHit;
        public StaticBody3D collider;
        public long collider_id;
        public Vector3 normal;
        public Vector3 position;
        public Rid rid;
        public int shape;

        public Hit3D(Godot.Collections.Dictionary hitDictionary)
        {
            didHit = (hitDictionary.Count > 0);

            if (didHit)
            {
                collider = (StaticBody3D)hitDictionary["collider"];
                collider_id = (long)hitDictionary["collider_id"];
                normal = (Vector3)hitDictionary["normal"];
                position = (Vector3)hitDictionary["position"];
                rid = (Rid)hitDictionary["rid"];
                shape = (int)hitDictionary["shape"];
            }
            else
            {
                collider = null;
                collider_id = -1;
                normal = Vector3.Zero;
                position = Vector3.Zero;
                rid = new Rid(null);
                shape = -1;
            }
        }

        public override string ToString()
        {
            string name = (collider.GetParent()?.GetParent()?.Name != null) ? collider.GetParent().GetParent().Name : "NO_NAME";
            return $"(name:{name} collider:{collider}, position:{position})";
        }
    }
}