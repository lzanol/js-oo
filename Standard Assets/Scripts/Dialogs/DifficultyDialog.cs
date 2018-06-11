using UnityEngine;
using System;
using System.Collections;

public class DifficultyDialog : MonoBehaviour 
{
	public event EventHandler<EventArgs> OnClose;
	
	public Texture2D blackBackgroundImage;
	public Texture2D backgroundImage;
	public GUIStyle stringStyle;
	public GUIStyle[] buttonStarStyles;
	public GUIStyle buttonCancel;
	
	void OnGUI ()
	{
		GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), blackBackgroundImage);
		GUI.DrawTexture(new Rect(Screen.width /2 - 270, Screen.height/2 -100,540,200), backgroundImage);
		GUI.Label(new Rect((Screen.width/2) - 400, (Screen.height/2) -50,800,100), "Classificar \'" + Menu.musicSelected.title + "':",stringStyle);
		
		float screenHalfWidth =  Screen.width/2f;
		float screenHalfHeight = Screen.height/2f;
		
		if(GUI.Button (new Rect(screenHalfWidth - 235f, screenHalfHeight - 15f, 90f, 50), "", buttonStarStyles[0]))
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), blackBackgroundImage);
			SaveStar(0);
		}
		
		if(GUI.Button (new Rect(screenHalfWidth - 140f, screenHalfHeight - 15f, 90f, 50), "", buttonStarStyles[1]))
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), blackBackgroundImage);
			SaveStar(1);
		}
		
		if(GUI.Button (new Rect(screenHalfWidth - 45f, screenHalfHeight - 15f, 90f, 50), "", buttonStarStyles[2]))
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), blackBackgroundImage);
			SaveStar(2);
		}
		
		if(GUI.Button (new Rect(screenHalfWidth + 50f, screenHalfHeight - 15f, 90f, 50), "", buttonStarStyles[3]))
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), blackBackgroundImage);
			SaveStar(3);
		}
		
		if(GUI.Button (new Rect(screenHalfWidth + 145f, screenHalfHeight - 15f, 90f, 50), "", buttonStarStyles[4]))
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), blackBackgroundImage);
			SaveStar(4);
		}
		
		if(GUI.Button (new Rect(screenHalfWidth -50f, screenHalfHeight + 35f, 100f, 40f), "", buttonCancel))
		{
			Close();
		}
	}
	
	public void Close ()
	{
		EventHandler<EventArgs> e = OnClose;
		if(e != null)
			e(this, new EventArgs());
		Destroy(gameObject);
		
	}
	
	public void SaveStar (int starsNumber)
	{
		DataSet.MusicData md = Menu.musicSelected;
		md.attrib = "" + starsNumber;
		md.SaveFile(Utils.Common.MUSICS_DIR + System.IO.Path.GetFileName(md.path));
		Close ();
	}
}
