#pragma strict

/*var Buttons : GameObject[];
private var InitPos : float[];
private var i : int = 0;
private var ButtonsColor : Color[];
private var msgNotSent : boolean = true;
private var game : Game;
private var gamePlay : GamePlayDrums;
private var n : int;
private var nLast : int = 0;
private var deviceDetector : DeviceDetector;

function Start()
{
	ButtonsColor = new Color[6];
	InitPos = new float[Buttons.length];
	
	for(i = 0; i < Buttons.Length; ++i)
	{
		InitPos[i]=Buttons[i].transform.localPosition.z;
		ButtonsColor[i]=Buttons[i].renderer.material.color;
	}
	
	deviceDetector = GameObject.Find("InputManager").GetComponent(typeof(DeviceDetector));
	game = Camera.main.GetComponent("Game") as Game;
	gamePlay = Camera.main.GetComponent("GamePlayDrums") as GamePlayDrums;
}

function Update()
{
	if (!deviceDetector.skipped && !DeviceDetector.ConnectedInstrument)
		return;
	
	n = 0;
	
	for (i = 0; i < deviceDetector.totalKeys; ++i)
		if (Input.GetButtonDown(deviceDetector.buttons[i]) || deviceDetector.skipped && Input.GetKeyDown(deviceDetector.keys[i]))
			n |= 1 << i;
	
	if (n > 0)
	{
		gamePlay.KeyDownReceiver(n);
	}
	
	for (i = 0; i < deviceDetector.totalKeys; ++i)
		if (n & 1 << i)
			Buttons[i].transform.localPosition.z = Mathf.Lerp(Buttons[i].transform.localPosition.z,
					InitPos[i] - .1, Time.deltaTime*20);
		else if (Buttons[i].transform.localPosition.z < InitPos[i])
			Buttons[i].transform.localPosition.z = Mathf.Lerp(Buttons[i].transform.localPosition.z,
					InitPos[i], Time.deltaTime*20);
		else Buttons[i].transform.localPosition.z = InitPos[i];
	
	if (!DeviceDetector.RequestInput && Input.GetButtonDown("9"))
		game.PlayPause();
}*/