using UnityEngine;
using System;
using System.Collections;

public class TrackEditDialog : MonoBehaviour 
{
	public event EventHandler<EventArgs> OnClose;
	
	private Rect dialogBox;
	
	public Texture2D blackBackground;
	public GUIStyle dialogBackground;
	public GUIStyle soloButton;
	public GUIStyle accompButton;
	public GUIStyle rythmicButton;
	public GUIStyle cancelButton;
	
	private void OnGUI()
	{
		GUI.depth = 0;
		GUI.DrawTexture(new Rect(0,0, Screen.width, Screen.height), blackBackground);
		dialogBox = new Rect(Screen.width/2f - 240f, Screen.height/2f - 100f, 480f, 200f);
		GUI.Box( dialogBox, "\nSelecione", dialogBackground);
		
		if(GUI.Button(new Rect(dialogBox.x + 40f, dialogBox.y + 70f, 120f, 60f), "", soloButton))
			SaveChange(DataSet.DevicePlayType.SOLO);
		if(GUI.Button(new Rect(dialogBox.x + 180f, dialogBox.y + 70f, 120f, 60f), "", accompButton))
			SaveChange(DataSet.DevicePlayType.ACCOMPANIMENT);
		if(GUI.Button(new Rect(dialogBox.x + 320f, dialogBox.y + 70f, 120f, 60f), "", rythmicButton))
			SaveChange(DataSet.DevicePlayType.DRUMS);
		if(GUI.Button(new Rect(dialogBox.x + 190, dialogBox.y + 140f, 100f, 40f), "", cancelButton))
			Close();
	}
	
	public void Close ()
	{
		EventHandler<EventArgs> e = OnClose;
		if(e != null)
			e(this, new EventArgs());
		Destroy(gameObject);
	}
	
	public void SaveChange(DataSet.DevicePlayType devicePlay)
	{
		DataSet.MusicData md = Menu.musicSelected;
		md.Instruments[Menu.trackSelected].playable = devicePlay;
		md.SaveFile(Utils.Common.MUSICS_DIR + System.IO.Path.GetFileName(md.path));
		Close ();
	}
}
