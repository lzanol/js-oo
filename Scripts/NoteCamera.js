#pragma strict

function OnPreRender ()
{
	GL.SetRevertBackfacing (false);
}
 
function OnPostRender ()
{
	GL.SetRevertBackfacing (true);
}