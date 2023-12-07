using System.Reflection.Emit;
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

        public static Hit3D EMPTY = new Hit3D(null);

        public Hit3D(Godot.Collections.Dictionary hitDictionary)
        {
            didHit = (hitDictionary?.Count > 0);

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

        static float _NEW_POS_SQR_THRESH = 0.0005f;

        /// <summary>
        /// Evalute if the hit position of 'a' is not the hit position of 'b', with some margin of error
        /// </summary>
        public static bool IsNewHitPosition(Hit3D a, Hit3D b)
        {
            float distance = (a.position - b.position).LengthSquared();

            if (distance > _NEW_POS_SQR_THRESH)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            if (collider != null)
            {
                string name = (collider.GetParent()?.GetParent()?.Name != null) ? collider.GetParent().GetParent().Name : "NO_NAME";
                return $"(name:{name} collider:{collider}, position:{position})";
            }
            else
                return "(NULL)";
        }

        public static bool operator ==(Hit3D a, Hit3D b)
        {
            return a.collider == b.collider && !IsNewHitPosition(a,b);
        }

        public static bool operator !=(Hit3D a, Hit3D b)
        {
            return !(a == b);
        }

        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object obj)
        {
            if (obj is Hit3D obj_unit)
            {
                return this == obj_unit;
            }

            return false;
        }
    }
}