#pragma strict

var CamAge : CameraMirror;
var CamWuWu : CameraMirror;

private var GoToRot : float = 270;
var Ready : GamePlayGuitar;

function Start()
{
	Ready=CamAge.GetComponent("GamePlayGuitar");
}

function SwitchOrientation ()
{
	GoToRot*=-1;
}

function OnMouseDown ()
{
	if(GoToRot == 90)
		GoToRot = 270;
	else
		GoToRot = 90;
		
	CamAge.SwitchView=true;
	CamWuWu.SwitchView=true;
}

function Update()
{
	if(Ready.enabled)
	{
		collider.enabled=true;
		
		for(var t : Transform in transform)
		{
			t.renderer.enabled=true;
		}
	}
	
	transform.eulerAngles.z=Mathf.Lerp(transform.eulerAngles.z,GoToRot,Time.deltaTime*10);
}