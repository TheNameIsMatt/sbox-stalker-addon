using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stalker
{
	public class Utilities
	{

		public Utilities()
		{
		}

		public static int GenerateRandomInt( int min, int max )
		{
			Random rng = new Random();
			return rng.Int(min, max);
		}
	}
}
