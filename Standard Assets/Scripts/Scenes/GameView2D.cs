using UnityEngine;
using System.Collections;

public class GameView2D : GameView
{
	protected override void Awake()
	{
		base.Awake();
		
		LyricsEnabled = false;
		scoreEnabled = false;
		Hud = GameView.HudStyle.Right;
		style.normal.textColor = Color.gray;
	}
	
	public override void OpenMusic()
	{
		base.OpenMusic();
		
		ShowMessage("INICIAR\n\ntoque para começar");
	}
}
