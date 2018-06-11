using UnityEngine;
using System.Collections;

public class ShowExpiredTime : MonoBehaviour 
{
	public GUIStyle labelStyle;

	void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	void OnGUI()
	{
		GUI.Label(new Rect(Screen.width - 210,Screen.height - 20, 200,20),"Versão de teste: " + Validator.DaysLeft + " dia(s) restante(s)", labelStyle);
	}
}
