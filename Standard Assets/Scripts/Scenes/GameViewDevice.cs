using UnityEngine;
using System.Collections;

public class GameViewDevice : GameView
{
	public override void OpenMusic()
	{
		base.OpenMusic();
		
		if (Application.loadedLevelName.Contains("Piano"))
			ShowMessage("INICIAR\n\nPressione espaço");
		else
			ShowMessage("INICIAR\n\nToque a primeira corda");
	}
	
	public override void Pause()
	{
		base.Pause();
		
		if (Game.Instance.IsGameOver)
			ShowMessage("FIM DA MÚSICA\n\nvocê acertou " + Mathf.RoundToInt(100f*GamePlay.Instance.Score/GamePlay.Instance.TotalScore) + "% das notas");
	}
}
