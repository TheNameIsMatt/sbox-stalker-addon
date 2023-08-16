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

		[Net]
		TimeSince TimeSinceStalkerLastObserved { get; set; }

		[Net]
		TimeSince TimeSinceStalkerLastTeleported { get; set; }
		public Stalker()
		{

		}

		public override void Spawn()
		{
			TimeSinceStalkerLastObserved = 0;
			TimeSinceStalkerLastTeleported = 0;

			TrackedEntity = (Entity)ConsoleSystem.Caller.Pawn;
			base.Spawn();
			SetModel( "models/pyramid.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			Position = SetSpawnPosition();
			Rotation = Rotation.LookAt( GetClosestPlayer( this ).Position );

		}

		public override void ClientSpawn()
		{
			base.Spawn();
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
			Vector3 newPosition;

			if ( IsPlayerLookingAtStalker() )
			{
				Log.Info( "Looking at" );
				return Position;
			}

			if ( TimeSinceStalkerLastObserved > 2)
			{
				if ( TimeSinceStalkerLastTeleported > 5 )
				{
					newPosition = (TrackedEntity.Position) + TrackedEntity.Rotation.Backward * 500;
					TimeSinceStalkerLastTeleported = 0;
				}
				else
				{
					newPosition = Position;
				}
				return newPosition;
			}

			return Position;
		}

		public bool IsPlayerLookingAtStalker()
		{
			

			Vector3 directionToObject = Position - TrackedEntity.Position;
			Vector3 forwardDirection = TrackedEntity.Rotation.Forward.Normal;
			float angle = Vector3.Dot( directionToObject, forwardDirection);

			if ( angle >= 0.1 )
			{
				TimeSinceStalkerLastObserved = 0;
			}

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
