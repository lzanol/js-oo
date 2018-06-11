#pragma strict

var SwitchView : boolean = false;
var MirroredCamera : boolean = false;
var isRotating : boolean = false;
var ValueX : float = 1;

function Start()
{
	if(name=="Camera")
		isRotating = true;
}

function OnPreCull ()
{
	camera.ResetWorldToCameraMatrix ();
	camera.ResetProjectionMatrix ();
	camera.projectionMatrix = camera.projectionMatrix * Matrix4x4.Scale(Vector3 (ValueX, 1, 1));
}
 
function OnPreRender ()
{
	GL.SetRevertBackfacing (MirroredCamera);
}
 
function OnPostRender ()
{
	GL.SetRevertBackfacing (!MirroredCamera);
}

function Update ()
{
	if(SwitchView)
	{
		MirroredCamera= !MirroredCamera;
		ValueX*=-1;
		SwitchView=false;
	}
	
	/*if(isRotating)
	{
		if(MirroredCamera)
			transform.eulerAngles.z=.6;
		else
			transform.eulerAngles.z=.2;
	}*/
	
}