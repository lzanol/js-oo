var GuitarPlayPos : Transform;
var GuitarShowPos : Transform;
var Trail : Transform;

private var TrailInitPos : float;
private var Game;

function Start ()
{
	Game = Camera.main.GetComponent("Game");
	TrailInitPos = Trail.position.y;
}

function Update ()
{
	if(!Game.IsPlaying)
	{
		if(transform.position != GuitarShowPos.position)
		{
			transform.position = Vector3.MoveTowards(transform.position,GuitarShowPos.position,Time.deltaTime*300);
			transform.rotation = Quaternion.Lerp(transform.rotation,GuitarShowPos.rotation,Time.deltaTime*10);
		}
		else
		{
			transform.Rotate(Vector3.up * Time.deltaTime * 100);
		}
		
		Trail.position.y = Mathf.Lerp(Trail.position.y,TrailInitPos+40,Time.deltaTime*10);
	}
	else
	{
		Trail.position.y = Mathf.Lerp(Trail.position.y,TrailInitPos,Time.deltaTime*10);
		
		transform.position = Vector3.Lerp(transform.position,GuitarPlayPos.position,Time.deltaTime*10);
		transform.rotation = Quaternion.Lerp(transform.rotation,GuitarPlayPos.rotation,Time.deltaTime*10);
	}
	
}