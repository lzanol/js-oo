using UnityEngine;
using System.Collections;
using System;

public class GameMenu : MonoBehaviour
{
	public Texture2D menuResume;
	public Texture2D menuReset;
	public Texture2D menuConfig;
	public Texture2D menuQuit;
	public Texture2D background;
	public ConfigDialog configDialogTmpl;
	public DeviceDialog deviceDialogTmpl;
	public MessageUI messageUITmpl;
	
	ConfigDialog configDialog;
	DeviceDialog deviceDialog;
	
	Rect bgRect = new Rect(0,0,0,0);
	Rect btRect = new Rect(0,0,0,0);
	float factor;
	float factorY = 0.8f;
	float offsetX = -23f;
	GUIStyle messageStyle;
	Rect messageRect;
	
	//public string GameOverMessage { get; set; }
	
	public bool SubmenuOpened { get; set; }
	
	protected virtual void Awake()
	{
		messageStyle = new GUIStyle();
		messageStyle.fontSize = 20;
		messageStyle.normal.textColor = Color.white;
		messageStyle.alignment = TextAnchor.MiddleCenter;
		
		messageRect = new Rect(0, 0, Screen.width, Screen.height);
		
		//GameOverMessage = "FIM DA MÚSICA!";
	}
	
	protected virtual void OnGUI()
	{
		GUI.depth = 1;
		
		if (SubmenuOpened)
			return;
		
		bgRect.height = Screen.height;
		
		factor = bgRect.height/background.height;
		
		bgRect.width = background.width*factor;
		bgRect.x = Screen.width - bgRect.width;
		
		GUI.DrawTexture(bgRect, background);
		
		btRect.width = 300f*factor;
		btRect.height = menuResume.height*btRect.width/menuResume.width;
		btRect.x = Screen.width - 298f*factor;
		btRect.y = 300f*factor;
		
		if (!Network.isClient)
		{
			if (Game.Instance.IsGameOver || Network.isServer)
			{
				if (GUI.Button(btRect, menuReset, GUIStyle.none))
					Game.Instance.Reset();
				
				//GUI.Label(messageRect, GameOverMessage, messageStyle);
			}
			else
			{
				if (GUI.Button(btRect, menuResume, GUIStyle.none))
					Game.Instance.Play();
				
				btRect.x += offsetX*factor;
				btRect.y += btRect.height*factorY;
				
				if (GUI.Button(btRect, menuReset, GUIStyle.none))
					Game.Instance.Reset();
				
				btRect.x += offsetX*factor;
				btRect.y += btRect.height*factorY;
				
				if (GUI.Button(btRect, menuConfig, GUIStyle.none) && configDialog == null)
				{
					configDialog = (ConfigDialog)Instantiate(configDialogTmpl);
					configDialog.SetMusicData(Game.Instance.MusicData, Game.Instance.CurrentTrackIndex);
					configDialog.OnClose += DialogCloseHandler;
					
					SubmenuOpened = true;
				}
			}
			
			btRect.x += offsetX*factor;
			btRect.y += btRect.height*factorY;
		}
		
		if (GUI.Button(btRect, menuQuit, GUIStyle.none))
			if (GameView.Instance.HasMessage)
				Game.Instance.Quit();
			else
			{
				MessageUI msgUI = (MessageUI)Instantiate(messageUITmpl);
				msgUI.Text = "SAIR\ntem certeza que deseja sair?";
				msgUI.ButtonConfigs = new MessageUI.ButtonConfig[]{ new MessageUI.ButtonConfig(() => Game.Instance.Quit(), "ok"),
					new MessageUI.ButtonConfig(() => Destroy(msgUI.gameObject), "cancelar") };
			}
	}
	
	// :: CUSTOM ::
	public void ShowDeviceDialog(DeviceDialogType type)
	{
		if (deviceDialog == null)
		{
			SubmenuOpened = true;
			deviceDialog = (DeviceDialog)Instantiate(deviceDialogTmpl);
		}
		
		deviceDialog.Type = type;
	}
	
	public void HideDeviceDialog()
	{
		Destroy(deviceDialog);
		deviceDialog = null;
		SubmenuOpened = false;
	}
	
	// :: HANDLERS ::
	void DialogCloseHandler(object sender, EventArgs e)
	{
		if (Application.loadedLevelName != Utils.Common.SCENE_REAL)
		{
			Game.Instance.InstrumentInternal = SoundEngine.CurrentInstance.GetSampleSetIndex(Game.Instance.CurrentTrack.hash);
			
			if (Application.loadedLevelName == Utils.Common.SCENE_GUITAR)
				((GamePlayGuitar)GamePlayGuitar.Instance).ConfigInstrument();
		}
		
		Destroy(configDialog);
		configDialog = null;
		SubmenuOpened = false;
	}
}
