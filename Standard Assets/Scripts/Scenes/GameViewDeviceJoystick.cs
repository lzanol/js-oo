using UnityEngine;
using System.Collections;

public class GameViewDeviceJoystick : GameViewDevice
{
	public override void OpenMusic()
	{
		base.OpenMusic();
		
		ShowMessage("INICIAR\n\nAperte espaço ou clique aqui");
		
		GameObject.Find("DeviceCamera").camera.enabled = false;
	}
	
	public override void Play()
	{
		base.Play();
		
		GameObject.Find("DeviceCamera").camera.enabled = false;
	}
	
	public override void Pause()
	{
		base.Pause();
		
		GameObject.Find("DeviceCamera").camera.enabled = true;
	}
	
	// :: RECEIVERS ::
	public void ConnectReceiver(bool connected)
	{
		Game.Instance.Pause();
		Game.Instance.Lock();
		ShowMessage("INICIAR\n\nAperte espaço ou clique aqui");
		//gameMenu.ShowDeviceDialog(connected ? DeviceDialogType.Config : DeviceDialogType.Plug);
	}
	
	public void HideConfigReceiver()
	{
		if (gameMenu != null)
		{
			gameMenu.HideDeviceDialog();
			HideMenu();
			Game.Instance.Unlock();
			Game.Instance.Play();
		}
	}
}
