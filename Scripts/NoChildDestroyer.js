#pragma strict

function Update ()
{
	if(transform.childCount==0)
		Destroy(gameObject);
}