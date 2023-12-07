using Godot;

namespace Utility
{
    public static class Raycasting
    {
        /// <summary>
        /// Doc ref: <see cref="https://docs.godotengine.org/en/stable/tutorials/physics/physics_introduction.html#doc-physics-introduction-collision-layer-code-example"/>
        /// </summary>
        private static uint _collisionMask_All = 0xffffffff;


        public static bool RayCast(Node3D anyNode3D, Vector3 from, Vector3 to, out Hit3D hit)
        {
            return RayCast(anyNode3D, from, to, _collisionMask_All, out hit);
        }

        public static bool RayCast(Node3D anyNode3D, Vector3 from, Vector3 to, uint hitLayers, out Hit3D hit)
        {
            PhysicsRayQueryParameters3D ray = new()
            {
                From = from,
                To = to,
                CollideWithAreas = false,
                CollideWithBodies = true,
                CollisionMask = _collisionMask_All
            };

            hit = new Hit3D(anyNode3D.GetWorld3D().DirectSpaceState.IntersectRay(ray));

            return hit.didHit;
        }

        public static bool RayCast(Node3D anyNode3D, Camera3D cam, uint camHitLayers, out Hit3D hit)
        {
            Vector3 from = cam.GlobalPosition;
            Vector3 to = cam.ProjectPosition(anyNode3D.GetViewport().GetMousePosition(), 1000);

            return RayCast(anyNode3D, from, to, camHitLayers, out hit);
        }
    }
}