using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSet;

public class GamePlayPiano : GamePlayReal
{
	public Transform trailTmpl;
	public GameObject[] sparksParents;
	public GameObject[] keysParents;
	
	List<ParticleSystem> sparks = new List<ParticleSystem>();
	List<Transform> keys = new List<Transform>();
	
	// :: AUX ::
	int i;
	NoteData nd;
	Transform trail;
	Vector3 pos;
	
	public bool IsStringGuitar { get; set; }
	
	// :: AUTO ::
	protected override void Awake()
	{
		base.Awake();
		
		IsStringGuitar = false;
		
		IgnoreTransposeVisual = true;
		SoundEngine.CurrentInstance.KeysEnabled = !IsStringGuitar;
		
		foreach (GameObject sparkParent in sparksParents)
			sparks.AddRange(sparkParent.GetComponentsInChildren<ParticleSystem>());
		
		foreach (GameObject keyParent in keysParents)
			keys.AddRange(keyParent.GetComponentsInChildren<Transform>().Where(a => a.gameObject.GetInstanceID() != keyParent.gameObject.GetInstanceID()));
	}
	
	protected override void Start()
	{
		base.Start();
		
		if (IsStringGuitar)
		{
			GuitarInputStrings g = gameObject.AddComponent<GuitarInputStrings>();
			g.OnKeyDown += DeviceKeyDownHandler;
			g.OnKeyUp += DeviceKeyUpHandler;
		}
		else
		{
			Game.Instance.Device.OnKeyDown += DeviceKeyDownHandler;
			Game.Instance.Device.OnKeyUp += DeviceKeyUpHandler;
		}
	}
	
	const float TRAIL_TIME = 3f;
	const float TRAIL_DIST = 36f;
	
	int lastTrail;
	float trailTime = TRAIL_TIME;
	float noteTime;
	NoteData noteData;
	
	protected override void Update()
	{
		base.Update();
		
		for (int i = lastTrail; i < sortedNotes.Length; ++i)
		{
			noteData = sortedNotes[i];
			noteTime = Game.Instance.MusicData.BeatsToTime(noteData.deltaTimeAbs + HIT_AREA_CORRECTION);
			
			if (time >= noteTime - trailTime)
			{
				lastTrail = i + 1;
				
				if (time < noteTime)
				{
					trail = (Transform)Instantiate(trailTmpl);
					trail.parent = notesContainer;
					
					pos = noteMap[noteData].position;
					pos.y += 0.4f;
					pos.z += 1f;
					trail.position = pos;
					
					trail.localRotation = Quaternion.Euler(new Vector3(270f,0f,0f));
					trail.localScale = new Vector3(1f,1f,noteMap[noteData].localPosition.y);
					
					int noteIndex = (noteData.midiCode - baseNote)%12;
					Material mat = trail.GetComponentInChildren<Renderer>().material;
					
					mat.mainTextureOffset = new Vector2(naturalIndexes[noteIndex]/8f, mat.mainTextureOffset.y);
					
					Destroy(trail.gameObject, trailTime);
				}
			}
			else break;
		}
	}
	
	void OnApplicationFocus(bool focus)
	{
		if (!IsStringGuitar)
			SoundEngine.CurrentInstance.KeysEnabled = focus;
	}
	
	// :: CUSTOM ::
	public override void OpenTrack(InstrumentData track)
	{
		base.OpenTrack(track);
		
		trailTime = Game.Instance.MusicData.BeatsToTime(Game.Instance.MusicData.MeasureToBeats(TRAIL_DIST));
		
		if (trailTime > TRAIL_TIME)
			trailTime = TRAIL_TIME;
	}
	
	public override void Scroll(float amount)
	{
		base.Scroll(amount);
		
		if (!Game.Instance.IsPlaying)
			lastTrail = 0;
	}
	
	Vector3 posKey = Vector3.zero;
	//Vector3 rtKey = Vector3.zero;
	
	// :: HANDLERS ::
	void DeviceKeyDownHandler(object sender, Events.DeviceEventArgs e)
	{
		if (IsStringGuitar)
			SoundEngine.CurrentInstance.PlayNoteByIndex(e.KeyIndex, Game.Instance.Device.currentDeviceID);
		Debug.Log(e.KeyIndex);
		posKey = keys[e.KeyIndex].localPosition;
		//rtKey = keys[e.KeyIndex].localEulerAngles;
		posKey.y = -0.16f;
		//rtKey.x = 276f;
		//keys[e.KeyIndex].localRotation = Quaternion.Euler(rtKey);
		keys[e.KeyIndex].localPosition = posKey;
		
		for (i = lastNoteIndex; i < sortedNotes.Length; ++i)
		{
			nd = sortedNotes[i];
			float noteTime = Game.Instance.MusicData.BeatsToTime(nd.deltaTimeAbs + HIT_AREA_CORRECTION);
			
			if (noteTime + timeHalfLimit >= time)
			{
				if (noteTime - timeHalfLimit < time)
				{
					lastNoteIndex = i;
					
					if (e.KeyIndex == nd.midiCode - baseNote)
					{
						sparks[e.KeyIndex].Play();
						++Score;
					}
				}
				else break;
			}
		}
	}
	
	void DeviceKeyUpHandler(object sender, Events.DeviceEventArgs e)
	{
		posKey = keys[e.KeyIndex].localPosition;
		//rtKey = keys[e.KeyIndex].localEulerAngles;
		posKey.y = 0f;
		//rtKey.x = 270f;
		//keys[e.KeyIndex].localRotation = Quaternion.Euler(rtKey);
		keys[e.KeyIndex].localPosition = posKey;
	}
}
