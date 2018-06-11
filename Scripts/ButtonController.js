#pragma strict

var DebugMode : boolean = false;
var Buttons : GameObject[];
private var InitPos : float[];
private var i : int = 0;
private var j : int = 0;
private var ButtonsColor : Color[];
private var msgNotSent : boolean = true;
private var game : Game;
private var gamePlay : GamePlayGuitar;
private var currentStroke : float;
private var lastStroke : float = 0;
private var n : int;
private var guitarDetector : GuitarInputDetector;
private var isKeyPressed : boolean = false;

function Start()
{
	ButtonsColor = new Color[6];
	InitPos = new float[Buttons.length];
	
	for(i = 0; i < Buttons.Length; ++i)
	{
		InitPos[i]=Buttons[i].transform.localPosition.z;
		ButtonsColor[i]=Buttons[i].renderer.material.color;
	}
	
	guitarDetector = GameObject.Find("InputManager").GetComponent(typeof(GuitarInputDetector));
	game = Camera.main.GetComponent("Game") as Game;
	gamePlay = Camera.main.GetComponent("GamePlayGuitar") as GamePlayGuitar;
}

function Update()
{	
	if (!guitarDetector.skipped && !GuitarInputDetector.ConnectedInstrument)
		return;
	
	currentStroke = Input.GetAxis(guitarDetector.StrokeType);
	
	if (guitarDetector.skipped) {
		if (Input.GetKey(guitarDetector.strokeKeyDown))
			currentStroke = -1;
		else for (i = guitarDetector.totalStrokeUps; i--;)
			if (Input.GetKey(guitarDetector.strokeKeyUps[i]))
				currentStroke = i + 1;
	}
	else currentStroke = currentStroke > 0 ? 1 : (currentStroke < 0 ? -1 : 0);
	
	n = 0;
	
	for (i = 0; i < guitarDetector.totalKeys; ++i) {
		isKeyPressed = false;
		
		if (guitarDetector.skipped)
			for (j = 2; j--;)
				if (Input.GetKey(guitarDetector.keys[j,i]))
					isKeyPressed = true;
		
		if (Input.GetButton(guitarDetector.buttons[i]) || isKeyPressed)
			n |= 1 << i + 1;
	}
	
	n |= currentStroke != 0;
	
	if (currentStroke != lastStroke)
	{
		lastStroke = currentStroke;
		
		if (n & 1)
			gamePlay.KeyDownReceiver(n, true);
	}
	
	for (i = 1; i < 4; ++i)
	{
		// Se a barra for tocada sem nenhuma tecla pressionada.
		if (n == 1)
		{
			Buttons[i].renderer.material.color=Color.Lerp(Buttons[i].renderer.material.color,Color.white,Time.deltaTime*40);
			Buttons[i].transform.localPosition.z=Mathf.Lerp(Buttons[i].transform.localPosition.z,InitPos[i]-.12,Time.deltaTime*40);
		}
		else
		{
			if (Buttons[i].renderer.material.color!=ButtonsColor[i])
				Buttons[i].renderer.material.color=Color.Lerp(Buttons[i].renderer.material.color,ButtonsColor[i],Time.deltaTime*40);
			
			if (Buttons[i].transform.localPosition.z<InitPos[i]-.01)
				Buttons[i].transform.localPosition.z=Mathf.Lerp(Buttons[i].transform.localPosition.z,InitPos[i],Time.deltaTime*5);
		}
	}
	
	for (i = 0; i < guitarDetector.totalKeys; ++i)
	{
		if (n & 1 << i + 1)
			Buttons[i].transform.localPosition.z=Mathf.Lerp(Buttons[i].transform.localPosition.z,InitPos[i]-.1,Time.deltaTime*20);
		else if (Buttons[i].transform.localPosition.z<InitPos[i]-.01)
			Buttons[i].transform.localPosition.z=Mathf.Lerp(Buttons[i].transform.localPosition.z,InitPos[i],Time.deltaTime*20);
	}
	
	if (!GuitarInputDetector.RequestInput && Input.GetButtonDown("9"))
		game.PlayPause();
}