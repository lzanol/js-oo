using UnityEngine;
using System.Collections;

public class GameViewReal : GameView
{
	protected override void Awake()
	{
		base.Awake();
		
		scoreEnabled = false;
	}
	
	public override void OpenMusic()
	{
		base.OpenMusic();
		
		ShowMessage("INICIAR\n\naperte espaço");
	}
}
