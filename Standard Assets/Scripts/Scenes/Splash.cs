using UnityEngine;
using System.Collections;

public class Splash : MonoBehaviour
{
	public Texture2D TeamTexture;
	public Texture2D splashTexture;
	public Texture2D clientTexture;
	
	public float delayTime;
	
	float initialTime;
	Rect splashRect;
	
	// :: Constructs ::
	static Splash()
	{
		Validator.Initialize();
	}
	
	void Awake()
	{
		if(Menu.IS_CLIENT)
			splashTexture = clientTexture;
		initialTime = Time.time;
		splashRect = new Rect(0, 0, 0, 0);
	}
	
	void OnGUI()
	{
		if (splashTexture != null)
		{
			splashRect.width = Screen.width;
			splashRect.height = splashTexture.height*Screen.width/splashTexture.width;
			
			if (splashRect.height < Screen.height)
			{
				splashRect.width = splashTexture.width*Screen.height/splashTexture.height;
				splashRect.height = Screen.height;
			}
			
			splashRect.x = (Screen.width - splashRect.width)/2;
			splashRect.y = (Screen.height - splashRect.height)/2;
			
			
			GUI.DrawTexture(splashRect, splashTexture);
			
			if(TeamTexture)
				GUI.DrawTexture(new Rect(Screen.width*.6f,Screen.height*.55f,Screen.height*.3f,Screen.height*.3f), TeamTexture);
			
		}
	}
	
	void Update()
	{
		if(Time.time - initialTime >= delayTime)
		{
			Application.LoadLevel(Utils.Common.SCENE_MENU);
		}
	}
}