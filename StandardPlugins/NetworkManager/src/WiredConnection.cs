
using System;

namespace NetworkManagerDocklet
{
	
	
	public class WiredConnection : NetworkConnection
	{
		
		public WiredConnection (string busName, string objectPath, ConnectionOwner owner) : base (busName, objectPath, owner)
		{
		}
	}
}
