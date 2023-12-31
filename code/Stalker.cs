﻿using System;
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

			int RandomPlayer = RNG.Next( 0, Game.Clients.Count );
			TrackedEntity = (Entity)Game.Clients.ElementAt(RandomPlayer).Pawn;
			base.Spawn();
			SetModel( "models/gnome_stalker/stalker01.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			Rotation = Rotation.LookAt( GetClosestPlayer( this ).Position );

		}

		public override void ClientSpawn()
		{
			base.Spawn();
			SetModel( "models/gnome_stalker/stalker01.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Dynamic);

		}

		[GameEvent.Tick.Server]
		public void Update()
		{
			if (TrackedEntity.Health > 0)
				Position = ChangePositionBasedOnTrackedEntity();

			SendStalkerMessage();
			PlayStalkerSounds();
			KillEntityIfLastObservedTimeTooGreat( TrackedEntity );
		}

		private void PlayStalkerSounds()
		{
			if (LastStalkerSound > RNG.Next(50, 100))
			{
				if (ListOfStalkerSounds.Count > 0)
				{
					PlaySound( ListOfStalkerSounds[RNG.Next( 0, ListOfStalkerSounds.Count)].ResourcePath );
					LastStalkerSound = 0;
				}
			}
		}

		private void SendStalkerMessage()
		{
			if ( LastStalkerMessage > RNG.Next( 50, 100 ) )
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
					if ( !EnsureEntityIsGrounded( newPosition ) && NavMesh.IsLoaded && CheckMagnitudeOfClosestPoint(newPosition))
					{
						newPosition = (Vector3)NavMesh.GetClosestPoint( newPosition );
					}
				}
				else
				{
					newPosition = Position;
				}
				Rotation = Rotation.LookAt( TrackedEntity.Position - Position );
				return newPosition;
			}

			return Position;
		}

		/// <summary>
		/// Used to determine if the magnitude of the stalker is too far away from a navmesh, if too far away, spawn behind using static distance (Won't work on slopes however)
		/// </summary>
		/// <param name="newPosition"></param>
		/// <returns></returns>
		private bool CheckMagnitudeOfClosestPoint(Vector3 newPosition)
		{
			if ( Vector3.DistanceBetween((Vector3)NavMesh.GetClosestPoint(newPosition), newPosition ) < 50) {
				return true;
			}
			return false;
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

		public void KillEntityIfLastObservedTimeTooGreat(Entity e)
		{
			if ( StalkerLastObserved > 15 && e.Health > 0) 
			{ 
				e.Health = 0;
				e.OnKilled();
				Position = (TrackedEntity.Position) + TrackedEntity.Rotation.Backward * 15;
				StalkerLastObserved = 0;
				PlaySound( ListOfStalkerSounds[RNG.Next( 0, ListOfStalkerSounds.Count )].ResourcePath );
				LastStalkerSound = 0;
			}
		}

	}
}
