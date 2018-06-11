using UnityEngine;
using System;
using System.Collections;

namespace Events
{
	public class DeviceEventArgs : EventArgs
	{
		public int KeyIndex { get; set; }
		
		public DeviceEventArgs(int keyIndex)
		{
			KeyIndex = keyIndex;
		}
	}
	
	public class OpenDialogEventArgs : EventArgs
	{
		public bool Confirmed { get; set; }
		public string Path { get; set; }
		public string Name { get; set; }
		
		public OpenDialogEventArgs(bool confirmed, string path, string name)
		{
			Confirmed = confirmed;
			Path = path;
			Name = name;
		}
	}
}