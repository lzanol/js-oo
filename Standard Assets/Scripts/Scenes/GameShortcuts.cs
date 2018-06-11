using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameShortcuts : MonoBehaviour
{
	Event evt;
	Dictionary<KeyCode, bool> pressedKeys = new Dictionary<KeyCode, bool>();
	
	void OnGUI()
	{
		evt = Event.current;
		
		if (evt.isKey)
		{
			KeyCode keyCode = evt.keyCode;
			
			switch (evt.type)
			{
			case EventType.KeyDown:
				if (!(pressedKeys.ContainsKey(keyCode) && pressedKeys[keyCode]))
				{
					pressedKeys[keyCode] = true;
					
					switch (evt.keyCode)
					{
					case KeyCode.Equals:
						if(!SoundEngine.CurrentInstance.IsPlaying)
							Game.Instance.PlayPause();
					break;
					
					case KeyCode.Space:
						/*if(Network.isServer)
							BandNetworkController.CurrentInstance.Replay();
						else*/
						Game.Instance.PlayPause();
						break;
					
					/*case KeyCode.Backspace:
						Game.Instance.Quit();
						break;*/
						
					case KeyCode.O:
						//Game.Instance.OpenMusic(new DataSet.MusicData("Musics/423201322954PM.tmd"), 2);
						break;
					}
				}
				break;
				
			case EventType.KeyUp:
				pressedKeys[keyCode] = false;
				break;
			}
		}
		
		if (!Network.isClient && evt.type == EventType.ScrollWheel)
			GamePlay.Instance.Scroll(evt.delta.y*0.5f);
	}
}
