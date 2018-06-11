var Particle : GameObject;

function OnTriggerEnter( other : Collider )
{
	other.gameObject.SendMessage("Collided");
	
	if(transform.parent)
		Instantiate(Particle,Vector3(transform.position.x,transform.position.y+.5,transform.position.z-2),transform.rotation);
	else
		Instantiate(Particle,Vector3(transform.position.x,transform.position.y+.1,transform.position.z-1),transform.rotation);
}