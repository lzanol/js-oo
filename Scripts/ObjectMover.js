#pragma strict
function Start () {
	collider.enabled=true;
}

function Collided()
{
	if(transform.childCount>0)
		transform.Find("Note5").renderer.enabled=false;
	else
		renderer.enabled=false;
}