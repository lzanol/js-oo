using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Events;

public class Device : MonoBehaviour
{
	[System.Serializable]
	public enum DevicesNames
	{
		KEYBOARD = 0,
		DRUM
	}
	
	[System.Serializable]
	public class DeviceInfo
	{
		public DevicesNames deviceName = DevicesNames.KEYBOARD;
		public GameObject devicePrefab;
	}
	
	public event EventHandler<DeviceEventArgs> OnKeyDown;
	public event EventHandler<DeviceEventArgs> OnKeyUp;
	
	public DeviceInfo[] devices = {};
	public int currentDeviceID = Utils.Common.PLS.IndexOf("bigmem") == -1 ? 0 : 1;
	
	private static int[] totalKeysAll = new int[2];
	
	private Dictionary<DevicesNames, KeyCode[]> deviceKeys = new Dictionary<DevicesNames, KeyCode[]>()
	{
		{DevicesNames.KEYBOARD, new KeyCode[]
			{KeyCode.Z, KeyCode.Alpha1, KeyCode.X, KeyCode.Alpha2, KeyCode.C, KeyCode.V, KeyCode.Alpha3, KeyCode.B, KeyCode.Alpha4, KeyCode.N, KeyCode.Alpha5, KeyCode.M, KeyCode.A, KeyCode.Alpha6, KeyCode.S, KeyCode.Alpha7, KeyCode.D, KeyCode.F, KeyCode.Alpha8, KeyCode.G, KeyCode.Alpha9, KeyCode.H, KeyCode.Alpha0, KeyCode.J, KeyCode.Q, KeyCode.I, KeyCode.W, KeyCode.O, KeyCode.E, KeyCode.R, KeyCode.P, KeyCode.T, KeyCode.K, KeyCode.Y, KeyCode.L, KeyCode.U,
				KeyCode.Equals, KeyCode.Period, KeyCode.Comma}
		},
		{DevicesNames.DRUM, new KeyCode[]
			{KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9}
		}
	};

	private KeyCode[] specialKeys = new KeyCode[] {KeyCode.LeftShift, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.RightShift};

	private Dictionary<KeyCode, int> keyMap = new Dictionary<KeyCode, int>();
	private Dictionary<KeyCode, bool> pressedKeys = new Dictionary<KeyCode, bool>();
	private Transform myTransform;
	private int type = 0;
	private GameObject currentDevice;
	private int keyIndex;
	
	// :: Getters/Setters ::
	public static int[] TotalKeysAll
	{
		get { return totalKeysAll; }
	}
	
	public static int TotalDevices
	{
		get { return totalKeysAll.Length; }
	}
	
	public bool IsVisible
	{
		get { return (currentDevice == null); }
	}
	
	public int/*InstrumentType*/ Type
	{
		set
		{
			//checa instrument type
			switch(this.type = value)
			{
			case 0:
				this.ChangeDevice(DevicesNames.KEYBOARD);
				break;
			case 1:
				this.ChangeDevice(DevicesNames.DRUM);
				break;
			}
		}
	}
	
	public int TotalKeys
	{
		get { return TotalKeysAll[this.type]; }
	}
	
	// :: Auto ::
	static Device()
	{
		totalKeysAll[0] = 36;
		totalKeysAll[1] = Utils.Common.PLS.IndexOf("bigmem") == -1 ? 8 : 196;
	}
	
	void Awake()
	{
		myTransform = transform;
		
		foreach (KeyValuePair<DevicesNames, KeyCode[]> pair in this.deviceKeys)
			for (int i = 0; i < pair.Value.Length; ++i)
				this.keyMap[pair.Value[i]] = i;
	}
	
	void Start()
	{
		if (devices.Length > 0)
			ChangeDevice(devices[currentDeviceID].deviceName);
	}
	
	void OnGUI()
	{
		if (enabled)
		{
			Event evt = Event.current;
			
			if (evt.isKey && this.keyMap.ContainsKey(evt.keyCode))
			{
				bool hasKeyCode = this.pressedKeys.ContainsKey(evt.keyCode) && this.pressedKeys[evt.keyCode];
				
				switch (evt.type)
				{
				case EventType.KeyDown:
					if (!hasKeyCode)
					{
						this.pressedKeys[evt.keyCode] = true;
						this.keyIndex = this.keyMap[evt.keyCode];

						this.TryAltKeys(ref this.keyIndex, evt);

						if (this.keyIndex > -1)
							this.DispatchEvent(OnKeyDown, this.keyIndex);
					}
					break;
				
				case EventType.KeyUp:
					if (hasKeyCode)
					{
						this.pressedKeys[evt.keyCode] = false;
						this.keyIndex = this.keyMap[evt.keyCode];

						this.TryAltKeys(ref this.keyIndex, evt);

						if (this.keyIndex > -1)
							this.DispatchEvent(OnKeyUp, this.keyIndex);
					}
					break;
				}
			}
		}
	}
	
	private void TryAltKeys(ref int keyIndex, Event evt)
	{
		if (keyIndex > 35)
		{
			keyIndex = (keyIndex - 36)*12;
			// Shift_L, Control_R, Alt_L, Alt_R, Shift_R
			if (Input.GetKey(specialKeys[1]))
			{
				if (Input.GetKey(specialKeys[3]))
				    keyIndex += 0;
				else if (Input.GetKey(specialKeys[4]))
					keyIndex += 1;
				else if (Input.GetKey(specialKeys[0]))
					keyIndex += 2;
				else if (!evt.alt && !evt.shift)
					keyIndex += 5;
			}
			else if (Input.GetKey(specialKeys[2]))
			{
				if (Input.GetKey(specialKeys[4]))
					keyIndex += 3;
				else if (Input.GetKey(specialKeys[0]))
					keyIndex += 9;
				else if (Input.GetKey(specialKeys[3])) {
					if (keyIndex < 24)
						keyIndex += 12;
					else keyIndex = -1;
				}
				else if (!evt.control && !evt.shift)
					keyIndex += 6;
			}
			else if (Input.GetKey(specialKeys[0]))
			{
				if (Input.GetKey(specialKeys[3]))
					keyIndex += 11;
				else if (!evt.control && !evt.alt)
					keyIndex += 4;
			}
			else if (Input.GetKey(specialKeys[3]))
			{
				if (Input.GetKey(specialKeys[4]))
					keyIndex += 10;
				//else if (!evt.shift && !evt.control)
				else keyIndex += 7;
			}
			else if (Input.GetKey(specialKeys[4]))
				keyIndex += 8;
			else keyIndex = -1;

			/*int n = 0;
			
			for (int i = this.specialKeys.Length; i--;)
			{
				bool isKeyPressed = false;

				if (Input.GetKey(this.specialKeys[i]))
					isKeyPressed = true;
				
				if (isKeyPressed)
					n |= 1 << i + 1;
			}

			if (specialKeysMap.Contains(n))
				this.keyIndex = (this.keyIndex - 35)*specialKeysMap[n];*/
		}
	}

	private void DispatchEvent(EventHandler<DeviceEventArgs> e, int keyIndex)
	{
		if (e != null)
			e(this, new DeviceEventArgs(keyIndex));
	}
	
	private void ChangeDevice(DevicesNames newDeviceName)
	{
		for(int i = 0; i < devices.Length;i++)
		{
			if(devices[i].deviceName == newDeviceName)
			{
				currentDeviceID = i;
				ChangeVisible(true);
				return;
			}
		}
	}
	
	private void ChangeVisible(bool showDevice)
	{
		if(!showDevice)
		{
			if(currentDevice)
			{
				Destroy(currentDevice);
			}
		}
		else
		{
			if(currentDevice)
			{
				Destroy(currentDevice);
				currentDevice = null;
			}
			if(devices[currentDeviceID].devicePrefab)
			{
				currentDevice = Instantiate(devices[currentDeviceID].devicePrefab,myTransform.position,Quaternion.identity) as GameObject;
				currentDevice.transform.parent = myTransform;
			}
		}
	}
}