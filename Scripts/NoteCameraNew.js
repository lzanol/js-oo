#pragma strict

function OnPreRender ()
{
	GL.SetRevertBackfacing (true);
}
 
function OnPostRender ()
{
	GL.SetRevertBackfacing (false);
}