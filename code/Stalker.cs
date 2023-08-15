using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;


namespace Stalker
{
	[Spawnable]
	[Library("Stalkers"), Title("Stalker")]
	public partial class Stalker : AnimatedEntity
	{
		public Stalker()
		{

		}

		public override void Spawn()
		{
			base.Spawn();
			Log.Info( "Spawning" );
			SetModel( "models/pyramid.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			Rotation = Rotation.LookAt( GetClosestPlayer( this ).Position );
			Position = SetSpawnPosition();

			
		}

		public Entity GetClosestPlayer( Entity e )
		{
			Entity ClosestPlayer = new Entity();


			foreach ( var player in Game.Clients )
			{
				if ( Vector3.DistanceBetween( e.Position, player.Pawn.Position ) < Vector3.DistanceBetween( e.Position, ClosestPlayer.Position ) )
				{
					ClosestPlayer = (Entity)player.Pawn;
				}
			}

			return ClosestPlayer;
		}

		public Vector3 SetSpawnPosition()
		{
			var owner = ConsoleSystem.Caller?.Pawn as Player;
			var tr = Trace.Ray( owner.EyePosition, owner.EyePosition + owner.EyeRotation.Forward * 500 )
				.UseHitboxes()
				.Ignore( owner )
				.Run();
			var newPosition = tr.EndPosition + Vector3.Down * this.Model.PhysicsBounds.Mins.z;
			return newPosition;
		}
	}
}
