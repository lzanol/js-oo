using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSet;
using Readers;
using Utils;

public class Game : MonoBehaviour
{
	static Game instance;
	
	bool refreshInstrument = false;
	bool isPlaying;
	
	// :: STATIC ::
	public static Game Instance
	{
		get { return instance; }
	}
	
	// :: AUTO ::
	void Awake()
	{
		instance = this;
		
		// Ativo até a primeira pausa.
		isPlaying = true;//!Network.isClient;
		
		SoundEngine = SoundEngine.CurrentInstance;
		Device = (new GameObject("Device")).AddComponent<Device>();
	}
	
	// (...) teste
	void Start()
	{
		//OpenMusic(new MusicData("../Commons/Musics/85201371604PM.tmd"), 0);
	}
	
	const float VOLUME_UP_AMOUNT = .05f;
	const float VOLUME_DOWN_AMOUNT = .1f;

	float volume;

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			volume = SoundEngine.CurrentInstance.MainVolume;
			SoundEngine.CurrentInstance.MainVolume = volume < 1f - VOLUME_UP_AMOUNT ? volume + VOLUME_UP_AMOUNT : 1f;
		}
		
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			volume = SoundEngine.CurrentInstance.MainVolume;
			SoundEngine.CurrentInstance.MainVolume = volume > VOLUME_DOWN_AMOUNT ? volume - VOLUME_DOWN_AMOUNT : 0f;
		}
	}

	void OnLevelWasLoaded(int level)
	{
		if (Menu.musicSelected != null && Menu.trackSelected != -1)
			OpenMusic(Menu.musicSelected, Menu.trackSelected, BandNetworkController.tracksToMute.ToArray());
			
		else Debug.Log("Game_.OnLevelWasLoaded(): No song selected!");
	}
	
	// :: CUSTOM ::
	public SoundEngine SoundEngine { get; set; }
	public Device Device { get; set; }
	public MusicData MusicData { get; set; }
	
	// Trilha a ser tocada.
	public InstrumentData CurrentTrack { get; set; }
	public int CurrentTrackIndex { get; set; }
	public int[] TracksToMute { get; set; }
	
	public bool IsPlaying
	{
		get { return SoundEngine.CurrentInstance.IsPlaying || isPlaying; }
	}
	
	public bool IsGameOver { get; set; }
	
	public int Instrument
	{
		get { return InstrumentInternal; }
		set
		{
			SampleSet sampleSet = SoundEngine.GetSampleSetAt(value);
			
			if (sampleSet != null)
			{
				CurrentTrack.id = value;
				Debug.Log(CurrentTrack.hash = sampleSet.Hash);
				CurrentTrack.octave = 0;
				CurrentTrack.transpose = 0;
				
				InstrumentInternal = value;
			}
		}
	}
	
	public int InstrumentInternal
	{
		get { return SoundEngine.GetSampleSetIndex(CurrentTrack.hash); }
		set
		{
			if (!SoundEngine.IsPlaying)
				SoundEngine.SetInstrument(Device.currentDeviceID, value, CurrentTrack, true);
			else refreshInstrument = true;
		}
	}
	
	// Valores entre -n..n (depende do instrumento).
	public int Octave
	{
		get { return CurrentTrack.octave; }
		set
		{
			SampleSet sampleSet = CurrentSampleSet;
			int midiBase = (value + sampleSet.Octave)*12;
			
			if (midiBase < sampleSet.Lowest)
				value = sampleSet.Lowest/12 - sampleSet.Octave;
			else
			{
				int maxMidiBase = sampleSet.Span + sampleSet.Lowest - Device.TotalKeysAll[Device.currentDeviceID] + 11;
				
				if (midiBase > maxMidiBase)
					value = maxMidiBase/12 - sampleSet.Octave;
			}
			
			CurrentTrack.octave = value;
			InstrumentInternal = InstrumentInternal;
		}
	}
	
	// Valores entre -11..11.
	public int Transpose
	{
		get { return CurrentTrack.transpose; }
		set
		{
			CurrentTrack.transpose = value;
			InstrumentInternal = InstrumentInternal;
		}
	}
	
	public SampleSet CurrentSampleSet
	{
		get { return SoundEngine.GetSampleSet(Device.currentDeviceID); }
	}
	
	/**
	 * Executa ou pausa a música carregada.
	 * Retorna TRUE se der play, senão FALSE se der pause.
	 */
	public void Play()
	{
		if (MusicData == null)
			return;
		
		if (!SoundEngine.IsPlaying)
		{
			if (IsGameOver)
			{
				Reset();
				IsGameOver = false;
			}
			
			if(Network.isServer)
			{
				if(BandNetworkController.CurrentInstance.play)
				{
					GamePlay.Instance.Play();
				
					/*float scrollTime = MusicData.BeatsToTime(MusicData.MeasureToBeats(GamePlay.Instance.CurrentPosition));
					
					if (scrollTime > 0f)
						SoundEngine.SetTime(scrollTime);*/
					
					SoundEngine.Play(MusicData);
				}
				else
					BandNetworkController.CurrentInstance.Replay();
			}
			else
			{
				GamePlay.Instance.Play();
				
				/*float scrollTime = MusicData.BeatsToTime(MusicData.MeasureToBeats(GamePlay.Instance.CurrentPosition));
				
				if (scrollTime > 0f)
					SoundEngine.SetTime(scrollTime);*/
				
				SoundEngine.Play(MusicData);
			}
		}
		else if(Network.isServer || Network.isClient)
			GameView.Instance.Play();
	}
	
	public void Pause()
	{
		isPlaying = false;
		
		if (Network.isServer || Network.isClient)
		{
			GameView.Instance.Pause();
			return;
		}
		
		GamePlay.Instance.Pause();
		SoundEngine.Pause();
		
		if (refreshInstrument)
		{
			refreshInstrument = false;
			InstrumentInternal = InstrumentInternal;
		}
	}
	
	public void PlayPause()
	{
		if (SoundEngine.IsPlaying && (!Network.isServer /*&& !Network.isClient*/ || !GameView.Instance.IsMenuOpened))
			Pause();
		else Play();
	}
	
	public void Stop()
	{
		SoundEngine.Stop();
		GamePlay.Instance.Stop();
		
		if (refreshInstrument)
		{
			refreshInstrument = false;
			InstrumentInternal = InstrumentInternal;
		}
	}
	
	public void Reset()
	{
		IsGameOver = false;
		
		Stop();
		GamePlay.Instance.Reset();
		
		isPlaying = true;
		
		// (...) verif. se é necessário
		OpenMusic(MusicData, CurrentTrackIndex, TracksToMute);
	}
	
	public void GameOver()
	{
		if(Network.isServer)
			BandNetworkController.CurrentInstance.GameOverClients();
		IsGameOver = true;
		Stop();
	}
	
	public void Quit()
	{
		Clear();
		SoundEngine.KeysEnabled = false;
		
		Application.LoadLevel(Common.SCENE_MENU);
	}
	
	public void Lock()
	{
		GetComponent<GameShortcuts>().enabled = false;
	}
	
	public void Unlock()
	{
		GetComponent<GameShortcuts>().enabled = true;
	}
	
	public MusicData OpenMidi(string path)
	{
		MusicData musicData = (new MidiReader(path)).GetMusicData();
		List<InstrumentData> tracksToExclude = new List<InstrumentData>();
		int totalVisualOctaves = Device.TotalKeysAll[Device.currentDeviceID]/12;
		
		for (int i = 0; i < musicData.instruments.Count; ++i)
		{
			InstrumentData instrData = (InstrumentData)musicData.instruments[i];
			
			if (instrData.notes.Count > 0)
			{
				int octDefault = SoundEngine.GetSampleSet(instrData.hash).Octave;
				int octMin = instrData.Notes.Min(noteData => noteData.midiCode)/12;
				int octMax = instrData.Notes.Max(noteData => noteData.midiCode)/12;
				
				instrData.octave = octMin - octDefault;
				
				// Se houver notas excedentes, cria trilhas extras
				// para as comportar.
				if (octMax - octMin > totalVisualOctaves)
				{
					int totalExtraTracks = Mathf.CeilToInt((octMax - (octMin + totalVisualOctaves - 1))/(float)totalVisualOctaves);
					
					for (int j = 0; j < totalExtraTracks; ++j)
					{
						InstrumentData instrDataNew = new InstrumentData(instrData, false);
						NoteData noteData;
						float deltaTimeAbs = 0;
						
						instrDataNew.octave += totalVisualOctaves;
						
						int midiCodeBase = (instrDataNew.octave + octDefault)*12;
						
						for (int k = 0; k < instrData.notes.Count; ++k)
						{
							noteData = instrData.Notes[k];
							
							// Se houver uma nota fora das oitavas visuais disponí­veis,
							// a exclui da trilha atual e a adiciona em uma nova trilha.
							if (noteData.midiCode >= midiCodeBase && noteData.midiCode < midiCodeBase + 12*totalVisualOctaves)
							{
								noteData.deltaTimeRel = noteData.deltaTimeAbs - deltaTimeAbs;
								deltaTimeAbs = noteData.deltaTimeAbs;
								instrDataNew.notes.Add(noteData);
								instrData.notes.Remove(noteData);
							}
						}
						
						musicData.instruments.Insert(i + 1, instrDataNew);
					}
				}
			}
			else
			{
				Debug.Log("Trilha vazia!");
				tracksToExclude.Add(instrData);
			}
		}
		
		foreach (InstrumentData track in tracksToExclude)
			musicData.instruments.Remove(track);
		
		return musicData;
	}
	
	public void OpenMusic(MusicData md, int trackIndex, int[] tracksToMute)
	{
		if (md == null)
			throw new Exception("Game_.OpenMusic(): \"MusicData md\" parameter is null!");
		
		if (md.instruments.Count <= 0)
			throw new Exception("Game_.OpenMusic(): No tracks found!");
		
		if (trackIndex < 0 && trackIndex >= md.instruments.Count)
			throw new Exception("Game_.OpenMusic(): Invalid \"int trackIndex\" parameter!");
		
		Clear();
		
		MusicData = md;
		CurrentTrackIndex = trackIndex;
		TracksToMute = tracksToMute;
		CurrentTrack = MusicData.instruments[trackIndex];
		
		// Define o som do PLS.
		InstrumentInternal = SoundEngine.GetSampleSetIndex(CurrentTrack.hash);
		
		int i = 0;
		
		// Habilita todas as trilhas.
		foreach(InstrumentData track in md.instruments)
		{
			track.muted = Network.isClient || TracksToMute.Any(n => n == i);
			++i;
		};
		
		// Desabilita o som da trilha atual.
		CurrentTrack.muted = true;
		
		GamePlay.Instance.OpenTrack(CurrentTrack);
	}
	
	void Clear()
	{
		SoundEngine.CurrentInstance.MainVolume = volume = 1f;
		
		Stop();
		SoundEngine.Clear();
		System.GC.Collect();
	}
}
