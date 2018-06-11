using UnityEngine;
using System;
using System.Collections;
using DataSet;

public class RemoveDialog : MonoBehaviour 
{
	public event EventHandler<EventArgs> OnClose;
	public event EventHandler<EventArgs> OnRemove;
	
	public Texture2D blackBackgroundImage;
	public Texture2D backgroundImage;
	public GUIStyle stringStyle;
	public GUIStyle yesButtonStyle;
	public GUIStyle noButtonStyle;
	
	public void OnGUI ()
	{
		GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), blackBackgroundImage);
		GUI.DrawTexture(new Rect(Screen.width /2 - 300, Screen.height/2 -100,600,200), backgroundImage);
		GUI.Label(new Rect((Screen.width/2) - 300, (Screen.height/2) - 50,600,100), "Deseja remover \'" + Menu.musicSelected.title + "' da lista?", stringStyle);
		
		if(GUI.Button (new Rect(Screen.width/2 - 200, (Screen.height/2) + 20, 100, 40), "", yesButtonStyle))
			RemoveFromList();
		
		if(GUI.Button (new Rect(Screen.width/2 + 100, (Screen.height/2) + 20, 100, 40), "", noButtonStyle))
			Close ();
	}
	
	public void Close ()
	{
		EventHandler<EventArgs> e = OnClose;
		if (e != null)
			e (this, new EventArgs ());
		Destroy (gameObject);
	}
	
	private void RemoveFromList()
	{	
		string sourceFile = Utils.Common.MUSICS_DIR + System.IO.Path.GetFileName (Menu.musicSelected.path);
		System.IO.File.Move (sourceFile, sourceFile + ".delete");
		EventHandler<EventArgs> e = OnRemove;
		if (e != null)
			e (this, new EventArgs ());
		Close ();
	}
}
