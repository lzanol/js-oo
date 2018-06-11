using UnityEngine;
using System.Collections;
using System;

public class MessageUI : MonoBehaviour
{
	public class ButtonConfig
	{
		public Action Func { get; set; }
		public string Text { get; set; }
		
		public ButtonConfig(Action func, string text)
		{
			this.Func = func;
			this.Text = text;
		}
	}
	
	public GUIStyle messageStyle;
	public Texture2D messageBg;
	public Texture2D messageBtBg;
	public Texture2D buttonBg;
	public GUIStyle buttonInvisible;
	
	const float BUTTON_MARGIN_FACTOR_X = 1.2f;
	const float BUTTON_MARGIN_FACTOR_Y = 1.3f;
	
	GUIStyle buttonStyle;
	Rect messageRect;
	Rect buttonRect;
	
	// :: aux ::
	ButtonConfig[] btConfigs;
	ButtonConfig btConfig;
	int i;
	int totalButtons;
	float groupWidth;
	
	public ButtonConfig[] ButtonConfigs
	{
		get { return btConfigs; }
		set
		{
			btConfigs = value;
			messageStyle.normal.background = TotalButtons > 0 ? messageBtBg : messageBg;
		}
	}
	
	public string Text { get; set; }
	
	int TotalButtons
	{
		get { return ButtonConfigs == null ? 0 : ButtonConfigs.Length; }
	}
	
	void Awake()
	{
		Text = "";
		messageRect = new Rect(0,0,0,0);
		buttonRect = new Rect(0,0,0,0);
		buttonStyle = new GUIStyle(messageStyle);
		buttonStyle.normal.background = buttonBg;
		ButtonConfigs = null;
	}
	
	void OnGUI()
	{
		GUI.depth = 0;
		
		totalButtons = TotalButtons;
		
		messageRect.width = Math.Max(Screen.width/3f, 300f);
		messageRect.height = messageRect.width*messageStyle.normal.background.height/messageStyle.normal.background.width;
		messageRect.x = (Screen.width - messageRect.width)/2;
		messageRect.y = (Screen.height - messageRect.height)/2;
		
		messageStyle.fontSize = Mathf.RoundToInt(messageRect.width/460f*30f);
		buttonStyle.fontSize = messageStyle.fontSize - 2;
		
		GUI.Label(messageRect, Text + (totalButtons > 0 ? "\n\n" : ""), messageStyle);

		if (totalButtons > 0)
		{
			buttonRect.width = messageRect.width/3.8f;
			buttonRect.height = buttonStyle.normal.background.height*buttonRect.width/buttonStyle.normal.background.width;
			buttonRect.y = messageRect.yMax - buttonRect.height*BUTTON_MARGIN_FACTOR_Y;
			groupWidth = buttonRect.width*totalButtons;
			
			for (i = 0; i < totalButtons; ++i)
			{
				btConfig = ButtonConfigs[i];
				buttonRect.x = messageRect.x + (messageRect.width - groupWidth)/2f + buttonRect.width*i;
				
				if (GUI.Button(buttonRect, btConfig.Text, buttonStyle))
					btConfig.Func();
			}
		}
		else
		{
			if(GUI.Button ( messageRect,"", buttonInvisible))
			{
				Game.Instance.Play();
			}
		}
	}
}
