using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Events;

public class GuitarInputStrings : MonoBehaviour
{
	class KeyPressed
	{
		public int id;
		public float time;
		
		public KeyPressed(int id, float time)
		{
			this.id = id;
			this.time = time;
		}
	}
	
	public event EventHandler<DeviceEventArgs> OnKeyDown;
	public event EventHandler<DeviceEventArgs> OnKeyUp;
	
	KeyCode[] keys = {KeyCode.Alpha9,KeyCode.Alpha8,KeyCode.Alpha7,KeyCode.Q,KeyCode.S,KeyCode.C,KeyCode.G};
	int[] ids = {10,26,18,50,34,66,98,42,58,82,114,2,9,25,17,49,33,65,97,41,57,81,113,1,12,28,20,52,36,68,100,44,60,84,116,4};
	
	Dictionary<int, int> keyMap = new Dictionary<int, int>();
	bool[] keysPressed = new bool[3];
	int n;
	int i;
	
	void Awake()
	{
		i = 0;
		
		foreach (int id in ids)
			keyMap[id] = i++;
	}
	
	void Update()
	{
		n = 0;
		
		for (i = 0; i < keys.Length; ++i)
			if (Input.GetKey(keys[i]))
				n |= 1 << i;
		
		for (i = 0; i < 3; ++i)
		{
			if ((n & 1 << i) > 0)
			{
				if (!keysPressed[i])
				{
					keysPressed[i] = true;
					DispatchEvent(OnKeyDown, keyMap[n]);
				}
			}
			else if (keysPressed[i])
			{
				keysPressed[i] = false;
				//DispatchEvent(OnKeyUp, keyMap[n]);
				//DispatchEvent(OnKeyUp, keyMap[1 << i]);
			}
		}
	}
	
	void DispatchEvent(EventHandler<DeviceEventArgs> e, int keyIndex)
	{
		if (e != null)
			e(this, new DeviceEventArgs(keyIndex));
	}
}
