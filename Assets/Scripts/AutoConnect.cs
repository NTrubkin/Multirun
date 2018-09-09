using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Trubkin.Util
{
	public class AutoConnect : MonoBehaviour
	{
		// todo check args before applying
		private void Start()
		{
			if (ArgumentHandler.Singleton == null)
			{
				Debug.LogWarning("ArgumentHandler not found.");
				return;
			}
			
			if (NetworkManager.singleton == null)
			{
				Debug.LogWarning("ArgumentHandler not found.");
				return;
			}

			if (ArgumentHandler.Singleton.GetValue("connect") == "server")
			{
				var port = Convert.ToInt32(ArgumentHandler.Singleton.GetValue("port"));
				
				NetworkManager.singleton.networkPort = port;
				NetworkManager.singleton.StartHost();
				
			}
			
			if (ArgumentHandler.Singleton.GetValue("connect") == "client")
			{
				var ip = ArgumentHandler.Singleton.GetValue("ip");
				var port = Convert.ToInt32(ArgumentHandler.Singleton.GetValue("port"));
				
				NetworkManager.singleton.networkAddress = ip;
				NetworkManager.singleton.networkPort = port;
				NetworkManager.singleton.StartClient();

			}
		}
	}
}