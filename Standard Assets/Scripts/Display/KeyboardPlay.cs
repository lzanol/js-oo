using UnityEngine;
using System.Collections;
using Events;

public class KeyboardPlay : MonoBehaviour
{
	private Animation myAnimation;
	
	void Start()
	{
		StartCoroutine(StartDelayed());
		
	}
	
	private IEnumerator StartDelayed()
	{
		yield return new WaitForSeconds(0.1f);
		myAnimation = animation;
		myAnimation["KeyUp"].wrapMode = WrapMode.Loop;
		myAnimation.Play("KeyUp");
		
		for(var i=60; i<96 ; i++)
		{
			//myAnimation[i.ToString()].blendMode = AnimationBlendMode.Additive;
			myAnimation[i.ToString()].wrapMode = WrapMode.ClampForever;
			myAnimation[i.ToString()].speed = 6f;
		}
		
		Game.Instance.Device.OnKeyDown += this.ReceiveNoteDown;
		Game.Instance.Device.OnKeyUp += this.ReceiveNoteUp;
	}
	
	void ReceiveNoteDown(object sender, DeviceEventArgs e)
	{
		StartAnimation(e.KeyIndex);
	}
	
	void ReceiveNoteUp(object sender, DeviceEventArgs e)
	{
		StopAnimation(e.KeyIndex);
	}
	
	public void StartAnimation(int animationName, float duration = 0f)
	{
		animation.Blend((animationName + 60).ToString(),1f,0.1f);
		if(duration > 0f)
		{
			StartCoroutine(StopAnimationWithTime(animationName,duration));
		}
	}
	
	private void StopAnimation(int animationName)
	{
		animation.Stop((animationName + 60).ToString());
	}
	
	private IEnumerator StopAnimationWithTime(int animationName, float duration)
	{
		yield return new WaitForSeconds(duration);
		StopAnimation(animationName);
	}
}
