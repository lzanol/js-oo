using UnityEngine;
using System.Collections;

public class GameMenuReal : MonoBehaviour
{
	const int MARGIN = 20;
	
	public Texture2D buttonTex16_9;
	public Texture2D buttonTex4_3;
	
	int w;
	int h;
	Vector3 pos;
	Quaternion rot;
	Rect buttonRect4_3;
	Rect buttonRect16_9;
	
	void Awake()
	{
		w = 60;
		h = 60;
		
		buttonRect4_3 = new Rect(Screen.width/2 - w, Screen.height - h - MARGIN, w, h);
		buttonRect16_9 = new Rect(Screen.width/2 + w, buttonRect4_3.y, w, h);
	}
	
	void OnGUI()
	{
		if (!Game.Instance.IsPlaying)
		{
			buttonRect4_3.x = Screen.width/2 - w;
			buttonRect4_3.y = Screen.height - h - MARGIN;
			
			buttonRect16_9.x = Screen.width/2 + w;
			buttonRect16_9.y = buttonRect4_3.y;
			
			if (GUI.Button(buttonRect4_3, buttonTex4_3, GUIStyle.none))
			{
				pos = camera.transform.position;
				pos.y = -16;
				pos.z = -32;
				rot = camera.transform.rotation;
				rot.eulerAngles = new Vector3(317,0,0);
				
				camera.transform.position = pos;
				camera.transform.rotation = rot;
			}
			
			if (GUI.Button(buttonRect16_9, buttonTex16_9, GUIStyle.none))
			{
				pos = camera.transform.position;
				pos.y = -12;
				pos.z = -22;
				rot = camera.transform.rotation;
				rot.eulerAngles = new Vector3(317,0,0);
				
				camera.transform.position = pos;
				camera.transform.rotation = rot;
			}
		}
	}
}