import Utils;

public static var ConnectedInstrument : boolean = false;
public static var RequestInput : boolean = false;

public var totalKeys : int = 5;
public var totalStrokeUps : int = 4;
public var buttons : String[] = new String[totalKeys];
public var keys : KeyCode[,] = new KeyCode[2,totalKeys];
public var strokeKeyUps : KeyCode[] = new KeyCode[totalStrokeUps];
public var strokeKeyDown : KeyCode = KeyCode.Alpha9;
public var StrokeType : String = "";
public var skipped : boolean = false;

private var CurrentConnectedJoysticks : int = 0;
private var LastCurrentValue : int = -1;
private var msgNotSent : boolean = true;

function Awake()
{
	GameObject.DontDestroyOnLoad(gameObject);
	
	buttons[0] = "1";
	buttons[1] = "2";
	buttons[2] = "3";
	buttons[3] = "0";
	buttons[4] = "4";
	StrokeType = "X";
	
	keys[0,0] = KeyCode.Q;
	keys[0,1] = KeyCode.S;
	keys[0,2] = KeyCode.C;
	keys[0,3] = KeyCode.G;
	keys[0,4] = KeyCode.N;
	
	keys[1,0] = KeyCode.LeftShift;
	keys[1,1] = KeyCode.RightControl;
	keys[1,2] = KeyCode.LeftAlt;
	keys[1,3] = KeyCode.RightAlt;
	keys[1,4] = KeyCode.RightShift;
	
	strokeKeyUps[0] = KeyCode.Alpha8;
	strokeKeyUps[1] = KeyCode.Equals;
	strokeKeyUps[2] = KeyCode.Period;
	strokeKeyUps[3] = KeyCode.Comma;
}

function Update()
{
	CurrentConnectedJoysticks = Input.GetJoystickNames().length;
	
	if (LastCurrentValue != CurrentConnectedJoysticks)
	{
		LastCurrentValue = CurrentConnectedJoysticks;
		ConnectedInstrument = CurrentConnectedJoysticks > 0;
		RequestInput = true;
		msgNotSent = true;
		
		if (ConnectedInstrument)
			SetDeviceType(Input.GetJoystickNames()[0]);
	}
	
	// Se estiver na cena do jogo.
	if (Application.loadedLevelName.Equals(Common.SCENE_GUITAR) || Application.loadedLevelName.Equals(Common.SCENE_DRUMS))
	{
		if (RequestInput)
		{
			if (msgNotSent)
			{
				msgNotSent = false;
				Camera.main.SendMessage("ConnectReceiver", ConnectedInstrument, SendMessageOptions.DontRequireReceiver);
			}
			
			if (ConnectedInstrument)
			{
				if(Input.GetButtonDown(buttons[2]))
					Exit();
				
				if(Input.GetButtonDown(buttons[3]))
				{
					var temp : String = buttons[2];
					buttons[2]=buttons[3];
					buttons[3]=temp;
					
					Exit();
				}
			}
			
			if(Input.GetKeyDown(KeyCode.Equals) || Input.GetMouseButton(0))
			{
				skipped = true;
				Exit();
			}
		}
		else msgNotSent = true;
	}
	else
	{
		if (skipped)
		{
			skipped = false;
			RequestInput = true;
		}
		
		msgNotSent = true;
	}
}

function Exit()
{
	RequestInput = false;
	Camera.main.SendMessage("HideConfigReceiver", SendMessageOptions.DontRequireReceiver);
}

function SetDeviceType( type : String )
{
	disconnected = false;
	
	if (type.Contains("Harmonix"))
	{
		buttons[2] = "3";
		buttons[3] = "0";
		StrokeType = "X";
	}
	else if (type.Contains("X-plorer"))
	{
		buttons[0] = "0";
		buttons[1] = "1";
		buttons[2] = "3";
		buttons[3] = "2";
		StrokeType = "Y";
	}
	else if (type.Contains("Hero") || type == "")
	{
		buttons[2] = "0";
		buttons[3] = "3";
		StrokeType = "X";
	}
}