using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stalker
{
	[GameResource("Stalker Message", "stkmsg","A string which contains a unique message for the stalker to say occassionally.")]
	public class StalkerMessage : GameResource
	{
		public string Message { get; set; }

		public static IReadOnlyList<StalkerMessage> All => _all;
		internal static List<StalkerMessage> _all = new List<StalkerMessage>();

		protected override void PostLoad()
		{
			base.PostLoad();
			if (!_all.Contains(this)) 
			{
				_all.Add(this);
			}
		}

	}
}
