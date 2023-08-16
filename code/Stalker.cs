using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;


namespace Stalker
{
	[Spawnable]
	[Library( "Stalkers" ), Title( "Stalker" )]
	public partial class Stalker : AnimatedEntity
	{

		public Stalker()
		{

		}

		[Net]
		public Entity TrackedEntity { get; set; }

		[Net]
		public bool IsGrounded { get; set; }

		[Net]
		TimeSince StalkerLastObserved { get; set; }

		[Net]
		TimeSince StalkerLastTeleported { get; set; }

		TimeSince LastStalkerMessage { get; set; }

		TimeSince LastStalkerSound { get; set; }

		[Net]
		public IList<SoundEvent> ListOfStalkerSounds { get; set; }


		private Random RNG = new Random();
		public override void Spawn()
		{
			StalkerLastObserved = 0;
			StalkerLastTeleported = 0;
			LastStalkerMessage = 0;

			//In order to use the limited IEnumerable from ResourceLibrary, I have to iterate over it and store in a new IList variable, that way I can use all the functions I need
			//The reason it is IList is so it can be networked.
			foreach ( var item in ResourceLibrary.GetAll<SoundEvent>().Where( x => x.ResourcePath.Contains( "stalksounds/" ) ))
			{
				ListOfStalkerSounds.Add( item );
			}

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
			Rotation = Rotation.LookAt( TrackedEntity.Position - Position );
			Position = ChangePositionBasedOnTrackedEntity();
			SendStalkerMessage();
			PlayStalkerSounds();
		}

		private void PlayStalkerSounds()
		{
			if (LastStalkerSound > RNG.Next(30, 100 ))
			{
				if (ListOfStalkerSounds.Count > 0)
				{
					PlaySound( ListOfStalkerSounds[RNG.Next( 0, ListOfStalkerSounds.Count - 1 )].ResourcePath );
					LastStalkerSound = 0;
				}
			}
		}

		private void SendStalkerMessage()
		{
			if ( LastStalkerMessage > RNG.Next( 25, 100 ) )
			{
				var message = StalkerMessage.All[RNG.Next( 0, StalkerMessage.All.Count - 1 )].Message;

				//The reason I am able to pass a To.Single() in the parameters for this, even though the actual method doesn't contain it a param for it, is because of some magic in the backend, by adding the [ClientRpc] field to the method,
				// this means I am able to call it on the entity that we pass.
				SendMessage( To.Single( TrackedEntity ), "Stalker", message );
				LastStalkerMessage = 0;
			}
		}

		[ClientRpc]
		public static void SendMessage( string name, string message)
		{
			Chat.Current?.AddEntry( name, message );
		}

		public Vector3 ChangePositionBasedOnTrackedEntity()
		{
			Vector3 newPosition;

			if ( IsPlayerLookingAtStalker() )
			{
				return Position;
			}

			if ( StalkerLastObserved > RNG.Next	( 1, 3 ) )
			{
				if ( StalkerLastTeleported > RNG.Next(2,5) )
				{
					newPosition = (TrackedEntity.Position) + TrackedEntity.Rotation.Backward * RNG.Next(50,500);
					StalkerLastTeleported = 0;
					if ( !EnsureEntityIsGrounded( newPosition ) )
					{
						newPosition = (Vector3)NavMesh.GetClosestPoint( newPosition );
					}
				}
				else
				{
					newPosition = Position;
				}
				return newPosition;
			}

			return Position;
		}

		private bool EnsureEntityIsGrounded( Vector3 currentPos )
		{
			string[] tags = new string[2];
			tags[0] = "Ground";
			tags[1] = "ground";
			var tr = Trace.Ray( this.Position, this.Rotation.Down * 10 )
				.UseHitboxes()
				.WithAnyTags(tags)
				.Run();

			if ( tr.Entity is not null )
			{
				IsGrounded = true;
				return true;
			}
			else
			{
				IsGrounded = false;
				return false;
			}

		}

		public bool IsPlayerLookingAtStalker()
		{
			Vector3 directionToObject = Position - TrackedEntity.Position;
			Vector3 forwardDirection = TrackedEntity.Rotation.Forward.Normal;
			float angle = Vector3.Dot( directionToObject, forwardDirection );

			if ( angle >= 0.1 )
			{
				StalkerLastObserved = 0;
				return true;
			}
			return false;
		}

		public Entity GetClosestPlayer( Entity e )
		{
			foreach ( var player in Game.Clients )
			{
				if ( Vector3.DistanceBetween( e.Position, player.Pawn.Position ) < Vector3.DistanceBetween( e.Position, TrackedEntity.Position ) )
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
