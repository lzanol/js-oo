#pragma strict

private var guitarDetector : GuitarInputDetector;
private var menu : Menu;
private var currentStroke : float;
private var lastStroke : float = 0;

function Start()
{
	guitarDetector = GameObject.Find("InputManager").GetComponent(typeof(GuitarInputDetector));
	menu = gameObject.GetComponent(typeof(Menu));
}

function Update()
{
	if(Input.GetKeyDown(KeyCode.UpArrow))
		menu.ReceiveStroke(1);
	else if(Input.GetKeyDown(KeyCode.DownArrow))
		menu.ReceiveStroke(-1);
	
	if(!GuitarInputDetector.ConnectedInstrument)
		return;
		
	currentStroke = Input.GetAxis(guitarDetector.StrokeType);
	currentStroke = currentStroke > 0 ? 1 :
		(currentStroke < 0 ? -1 : 0);
		
	if (currentStroke != lastStroke)
	{
		lastStroke = currentStroke;
		menu.ReceiveStroke(currentStroke);
	}
}