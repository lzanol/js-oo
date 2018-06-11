using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DataSet;

public abstract class GamePlay : MonoBehaviour
{
	protected const float HIT_AREA_CORRECTION = 0.15f;
	
	const float VIEWPORT_HEIGHT = 32f;
	const float HIT_TIME = 0.35f;
	
	static GamePlay instance;
	
	protected Transform notesContainer;
	protected InstrumentData currentTrack;
	protected float time;
	protected float timeHalfLimit = HIT_TIME/2f;
	protected NoteData[] sortedNotes = {};
	protected int lastNoteIndex = 0;
	
	float length = 0f;
	int score = 0;
	
	// :: AUX ::
	Vector3 pos = Vector3.zero;
	float y;
	
	// :: STATIC ::
	public static GamePlay Instance
	{
		get { return instance; }
	}

	public Vector3 ScrollPosition
	{
		get { return notesContainer.localPosition; }
	}

	// :: GET/SET ::
	protected float MusicScale { get; set; }

	protected float Length
	{
		get { return length*MusicScale; }
		set { length = value; }
	}
	
	// :: AUTO ::
	protected virtual void Awake()
	{
		instance = this;
		
		enabled = false;
		MusicScale = 1f;
		
		CreateContainer();
	}
	
	protected virtual void Start() {}
	
	protected virtual void Update()
	{
		if (Game.Instance.SoundEngine.IsPlaying)
		{
			time = Game.Instance.SoundEngine.GetTime();
			
			if (time > Game.Instance.SoundEngine.GetLength() || time.Equals(Game.Instance.SoundEngine.GetLength()))
			{
				Game.Instance.GameOver();
				return;
			}
			
			Move(-Game.Instance.MusicData.BeatsToMeasure(Game.Instance.MusicData.TimeToBeats(time))*MusicScale);
		}
		else Game.Instance.GameOver();
	}
	
	protected virtual void OnGUI() {}
	
	// :: CUSTOM ::
	/*public float CurrentPosition
	{
		get { return -notesContainer.localPosition.y; }
	}
	
	// Valores entre 0..100.
	public float ScrollPosition
	{
		get { return CurrentPosition*100f/(Length - VIEWPORT_HEIGHT); }
		set
		{
			if (!Game.Instance.SoundEngine.IsPlaying)
				Move(-(value < 0 ? 0f : (value > 100 ? 100f : value))*(Length - VIEWPORT_HEIGHT)/100f);
		}
	}
	
	// Posição do TrackContainer (medidas da Unity).
	public float ScrollMeasure
	{
		get { return CurrentPosition; }
		set
		{
			if (!Game.Instance.SoundEngine.IsPlaying)
				Move(-(value < 0 ? 0f : (value > Length - VIEWPORT_HEIGHT ? Length - VIEWPORT_HEIGHT : value)));
		}
	}*/
	
	public int Score
	{
		get { return score; }
		set { GameView.Instance.Score = (score = value).ToString(); }
	}
	
	public int TotalScore { get; set; }
	
	public void Play()
	{
		if (Game.Instance.MusicData != null)
			enabled = true;
		
		GameView.Instance.Play();
	}
	
	public void Pause()
	{
		enabled = false;
		GameView.Instance.Pause();
	}
	
	public void Stop()
	{
		Pause();
		Move(0);
		
		lastNoteIndex = 0;
	}
	
	public void Reset()
	{
		Score = 0;
		
		Destroy(notesContainer.gameObject);
		CreateContainer();
		
		/*foreach (GameObject note in notesContainer.GetComponentsInChildren<GameObject>())
			Destroy(note);*/
		
		GameView.Instance.Reset();
	}
	
	void Move(float y)
	{
		pos = notesContainer.localPosition;
		pos.y = y;
		notesContainer.localPosition = pos;
	}
	
	public virtual void Scroll(float amount)
	{
		if (!Game.Instance.IsPlaying)
		{
			y = amount + notesContainer.localPosition.y;
			
			if (y > 0)
				y = 0f;
			else if (y < VIEWPORT_HEIGHT - Length)
				y = VIEWPORT_HEIGHT - Length;
			
			Move(y);
			Game.Instance.SoundEngine.SetTime(Game.Instance.MusicData.BeatsToTime(Game.Instance.MusicData.MeasureToBeats(-y/MusicScale)));
			
			GameView.Instance.LastLyricsIndex = 0;
			lastNoteIndex = 0;
			
			// Zera os pontos se usar scroll.
			Score = 0;
		}
	}
	
	public virtual void OpenTrack(InstrumentData track)
	{
		enabled = false;
		Move(0);
		
		// Armazena as notas ordenadas por tempo.
		sortedNotes = track.notes.ToArray();
		Length = Game.Instance.MusicData.BeatsToMeasure(Game.Instance.MusicData.GetMusicDuration());
		currentTrack = track;
		TotalScore = sortedNotes.Length;
		
		GameView.Instance.OpenMusic();
	}
	
	void CreateContainer()
	{
		notesContainer = (new GameObject("NotesContainer")).transform;
		notesContainer.parent = GameObject.Find("Panel").transform;
		notesContainer.localPosition = new Vector3(0,0,0);
		notesContainer.localRotation = Quaternion.identity;
		notesContainer.localScale = Vector3.one; // (!) remover
	}
}
