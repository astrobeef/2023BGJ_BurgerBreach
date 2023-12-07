using Godot;

namespace Utility
{
	public static class Raycasting
	{
		/// <summary>
		/// Doc ref: <see cref="https://docs.godotengine.org/en/stable/tutorials/physics/physics_introduction.html#doc-physics-introduction-collision-layer-code-example"/>
		/// </summary>
		public static uint COLLISION_MASK_ALL = 0xffffffff;


		public static bool RayCast(Node3D anyNode3D, Vector3 from, Vector3 to, out Hit3D hit)
		{
			return RayCast(anyNode3D, from, to, COLLISION_MASK_ALL, out hit);
		}

		public static bool RayCast(Node3D anyNode3D, Vector3 from, Vector3 to, uint hitLayers, out Hit3D hit)
		{
			PhysicsRayQueryParameters3D ray = new()
			{
				From = from,
				To = to,
				CollideWithAreas = false,
				CollideWithBodies = true,
				CollisionMask = COLLISION_MASK_ALL
			};

			hit = new Hit3D(anyNode3D.GetWorld3D().DirectSpaceState.IntersectRay(ray));

			return hit.didHit;
		}

		public static bool RayCast(Camera3D cam, uint camHitLayers, out Hit3D hit)
		{
			Vector3 from = cam.GlobalPosition;
			Vector3 to = cam.ProjectPosition(cam.GetViewport().GetMousePosition(), 1000);

			return RayCast(cam, from, to, camHitLayers, out hit);
		}
	}
}
