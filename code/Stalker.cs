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
		[Net]
		public Entity TrackedEntity { get; set; }
		public Stalker()
		{

		}

		public override void Spawn()
		{
			TrackedEntity = (Entity)ConsoleSystem.Caller.Pawn;
			base.Spawn();
			Log.Info( "Spawning" );
			SetModel( "models/pyramid.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			Position = SetSpawnPosition();
			Rotation = Rotation.LookAt( GetClosestPlayer( this ).Position );

		}

		public override void ClientSpawn()
		{
			base.Spawn();
			Log.Info( "Spawning" );
			SetModel( "models/pyramid.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		}

		[GameEvent.Tick.Server]
		public void Update()
		{
			Rotation = Rotation.LookAt(TrackedEntity.Position - Position);
			Position = ChangePositionBasedOnTrackedEntity();	
		}


		public Vector3 ChangePositionBasedOnTrackedEntity()
		{
			
			if ( IsPlayerLookingAtStalker() )
			{
				Log.Info( "Looking at" );
				return Position;
			}

			return (TrackedEntity.Position) + TrackedEntity.Rotation.Backward * 500;
		}

		public bool IsPlayerLookingAtStalker()
		{

			Vector3 directionToObject = Position - TrackedEntity.Position;
			Vector3 forwardDirection = TrackedEntity.Rotation.Forward;
			float angle = Vector3.Dot( directionToObject, forwardDirection);

			Log.Info( angle );
			return angle >= 0.1 ? true : false;

		}

		public Entity GetClosestPlayer( Entity e )
		{

			foreach ( var player in Game.Clients )
			{
				if ( Vector3.DistanceBetween( e.Position, player.Pawn.Position ) < Vector3.DistanceBetween( e.Position, TrackedEntity.Position))
				{
					TrackedEntity = (Entity)player.Pawn;
				}
			}
			TrackedEntity = TrackedEntity;

			return TrackedEntity;
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
