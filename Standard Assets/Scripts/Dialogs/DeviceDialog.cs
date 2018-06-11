using UnityEngine;
using System.Collections;

public class DeviceDialog : MonoBehaviour
{
	public Texture2D menuQuit;
	
	public DeviceDialogType Type { get; set; }
	
	GUIStyle msgStyle;
	
	void Awake()
	{
		msgStyle = new GUIStyle();
		msgStyle.alignment = TextAnchor.MiddleCenter;
		msgStyle.normal.textColor = Color.white;
	}
	
	void OnGUI()
	{
		GUI.depth = 0;
		
		GUILayout.BeginHorizontal(GUILayout.Width(Screen.width));
		GUILayout.FlexibleSpace();
		
		GUILayout.BeginVertical(GUILayout.Height(Screen.height));
		GUILayout.FlexibleSpace();
		
		GUILayout.Space(60);
		
		/*switch (Type)
		{
		case DeviceDialogType.Plug:
			GUILayout.Label("ACIONE A PRIMEIRA PALHETA \n NA GUITARRA DE MADEIRA \n OU CONECTE A \n GUITARA DE PLASTICO", msgStyle);
			break;
			
		case DeviceDialogType.Config:
			GUILayout.Label("PRESSIONE A TECLA AMARELA", msgStyle);
			break;
		}*/
		
		GUILayout.Space(30);
		
		//if (GUILayout.Button(menuQuit, GUIStyle.none, GUILayout.Width(200)))
		//	Game.Instance.Quit();
		
		GUILayout.FlexibleSpace();
		GUILayout.EndVertical();
		
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}
}

public enum DeviceDialogType
{
	Plug,
	Config
}
