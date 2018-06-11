using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DataSet;
using System;

public class ConfigDialog : MonoBehaviour 
{
	public event EventHandler<EventArgs> OnClose;
	
	private float[] speedPercents = {0.625f,0.8125f,1f,1.375f,1.75f};
	private string[] speedNames = {"Super Lento","Lento","Normal","Rápido","Super Rápido"};
	
	public GUIStyle dialogBackground;
	
	public Texture2D blackBackground;
	
	public GUIStyle comboBoxLabel;
	public GUIStyle comboBoxButton;
	
	public Texture2D[] speedImages;
	public GUIStyle[] ArrowButtons;
	public GUIStyle bpmLabel;
	
	public GUIStyle playButton;
	public GUIStyle stopButton;
	public GUIStyle trackLabel;
	
	public GUIStyle itemButton;
	
	public GUISkin skinToUse;
	
	private int currentSpeedSelected;
	
	private DataSet.MusicData musicData;
	private int trackSelected;
	private string filePath;
	
	private int maxMoves = 0;
	private int currentMoves = 0;
	
	private float initialBPM;
	
	private bool comboBoxOpened = false;
	private Vector2 comboBoxScrollPosition = new Vector2(0f,0f);
	private SampleSet currentSampleSet;
	private List<SampleSet> sampleSets;
		
	public void Awake()
	{
		SoundEngine.CurrentInstance.Stop();
	}
	
	public void OnGUI()
	{
		GUI.depth = 0;
		GUI.skin = skinToUse;
		GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),blackBackground);
		Rect dialogRect = new Rect(Screen.width/2 - 250, Screen.height/2 - 250, 500, 500);
		GUI.BeginGroup(dialogRect);
		GUI.Box(new Rect(0, 0, dialogRect.width, dialogRect.height),"",dialogBackground);
		
		//Volume
		if(!SoundEngine.CurrentInstance.IsPlaying)
		{
			if(GUI.Button(new Rect(100,225,40,40),"",  playButton))
				SoundEngine.CurrentInstance.Play(musicData);
		}
		else
		{
			if(GUI.Button(new Rect(100,225,40,40),"", stopButton))
				SoundEngine.CurrentInstance.Stop();
		}
		
		//Controle de volume por trilha.
		if(currentMoves > 0)
		{
			if(GUI.Button(new Rect(10,315,40,40),"", ArrowButtons[0]))
			{
				currentMoves--; 
			}
		}
		
		if(currentMoves < maxMoves)
		{
			if(GUI.Button(new Rect(440,315,40,40),"",ArrowButtons[1]))
			{
				currentMoves++;
			}
		}
		
		Rect trackArea = new Rect(45,265,400,170);
		
		GUI.BeginGroup(trackArea);
		
		float positionX = 0 - (currentMoves * 80);
		
		int contInstruments = 1;
		for(int i = 0; i < musicData.Instruments.Length; ++i)
		{
			if(i != trackSelected)
			{
				musicData.Instruments[i].volume = GUI.VerticalSlider(new Rect(positionX + 30, 5, 20,120), musicData.Instruments[i].volume, 1.27f,0f);
				GUI.Label(new Rect(positionX + 5,140,70,10), contInstruments.ToString() + ". " + SoundEngine.CurrentInstance.GetSampleSet(musicData.Instruments[i].hash).Label,trackLabel);
				contInstruments += 1;
				positionX += 80;
			}
		}
		
		GUI.EndGroup();
		
		
		//Som(instrumento)
		if(Menu.nextLevelName != Utils.Common.SCENE_REAL)
		{
			Rect instrumentRect = new Rect(240,90,200,25);
			GUI.Label(new Rect(instrumentRect.x, instrumentRect.y, instrumentRect.width -20,instrumentRect.height),
				 " " + currentSampleSet.Label, comboBoxLabel);
			if(GUI.Button(new Rect(instrumentRect.x + instrumentRect.width-30,instrumentRect.y -5,35,35), "", comboBoxButton))
			{
				comboBoxOpened = !comboBoxOpened;
				sampleSets = SoundEngine.CurrentInstance.GetSampleSets();
			}
			
			if(comboBoxOpened)
			{
				Rect instrumentSelectableRect = new Rect(instrumentRect.x,instrumentRect.y + 25,instrumentRect.width,100);
				GUI.Box(instrumentSelectableRect,"",comboBoxLabel);
				
				comboBoxScrollPosition = GUI.BeginScrollView(new Rect(instrumentSelectableRect.x+5,instrumentSelectableRect.y+5,instrumentSelectableRect.width -10, instrumentSelectableRect.height -10),
					comboBoxScrollPosition, new Rect(instrumentSelectableRect.x+5,instrumentSelectableRect.y+5,instrumentSelectableRect.width -30,25*sampleSets.Count));
				for(int i = 0; i < sampleSets.Count; ++i)
				{
					if(GUI.Button(new Rect(instrumentSelectableRect.x+5,instrumentSelectableRect.y +(i * 25) + 5,instrumentSelectableRect.width-30,20),sampleSets[i].Label,itemButton))
					{
						currentSampleSet = sampleSets[i];
						comboBoxOpened = false;
					}
				}
				GUI.EndGroup();
			}
			else
			{
				//Velocidade(BPM)
				Rect bpmRect = new Rect(240,160,0,0);
				if(GUI.Button(new Rect(bpmRect.x,bpmRect.y,30,30), "", ArrowButtons[0]))
				{
					currentSpeedSelected = (currentSpeedSelected == 0) ? 4 : currentSpeedSelected-1;
					UpdateBPM();
				}
				
				bpmRect.x += 30;
				GUI.Label(new Rect(bpmRect.x,bpmRect.y,140,30),speedNames[currentSpeedSelected],bpmLabel);
				bpmRect.x += 140;
				if(GUI.Button(new Rect(bpmRect.x,bpmRect.y,30,30), "", ArrowButtons[1]))
				{
					currentSpeedSelected = (currentSpeedSelected == 4) ? 0 : currentSpeedSelected+1;
					UpdateBPM();
				}
			}
		}
		else
		{
			
			Rect instrumentRect = new Rect(240,90,200,25);
			GUI.Label(new Rect(instrumentRect.x, instrumentRect.y, instrumentRect.width -20,instrumentRect.height),
				 " " + currentSampleSet.Label, comboBoxLabel);
			
			//Velocidade(BPM)
			Rect bpmRect = new Rect(240,160,0,0);
			if(GUI.Button(new Rect(bpmRect.x,bpmRect.y,30,30), "", ArrowButtons[0]))
			{
				currentSpeedSelected = (currentSpeedSelected == 0) ? 4 : currentSpeedSelected-1;
				UpdateBPM();
			}
			
			bpmRect.x += 30;
			GUI.Label(new Rect(bpmRect.x,bpmRect.y,140,30),speedNames[currentSpeedSelected],bpmLabel);
			bpmRect.x += 140;
			if(GUI.Button(new Rect(bpmRect.x,bpmRect.y,30,30), "", ArrowButtons[1]))
			{
				currentSpeedSelected = (currentSpeedSelected == 4) ? 0 : currentSpeedSelected+1;
				UpdateBPM();
			}
		}
		
		//Confirmar e cancelar
		if(GUI.Button(new Rect(30,dialogRect.height -50f,100f,20f),"Cancelar"))
		{
			Cancel();
		}
		
		if(GUI.Button(new Rect(360f,dialogRect.height -50f,100f,20f),"Aplicar"))
		{
			Apply();
		}
		
		GUI.EndGroup();
	}
	
	public void SetMusicData(DataSet.MusicData newMusicData, int trackSelected)
	{
		musicData = newMusicData;
		this.trackSelected = trackSelected;
		currentSampleSet = SoundEngine.CurrentInstance.GetSampleSet(musicData.Instruments[trackSelected].hash);
		maxMoves = musicData.Instruments.Length - 6;
		currentSpeedSelected = 2;
		
		//Abrir o arquivo para pegar o bpm original.
		DataSet.MusicData md = new DataSet.MusicData(musicData.path);
		initialBPM = md.bpm;
		musicData.bpm = initialBPM;
	}
	
	public void Apply()
	{
		if(Menu.nextLevelName != Utils.Common.SCENE_REAL)
			musicData.Instruments[trackSelected].hash = currentSampleSet.Hash;
		DataSet.MusicData md = new DataSet.MusicData(musicData.path);
		md.Instruments[trackSelected].hash = currentSampleSet.Hash;
		for(int i = 0; i < md.Instruments.Length; ++i)
		{
			md.Instruments[i].volume = musicData.Instruments[i].volume;
		}
		md.SaveFile(musicData.path);
		SoundEngine.CurrentInstance.Stop();
		
		EventHandler<EventArgs> e = OnClose;
		if(e != null)
			e(this, new EventArgs());
	}
	
	public void Cancel()
	{
		DataSet.MusicData md = new DataSet.MusicData(musicData.path);
		for(int i = 0; i < md.Instruments.Length; ++i)
		{
			musicData.Instruments[i].volume = md.Instruments[i].volume;
		}
		SoundEngine.CurrentInstance.Stop();
		
		EventHandler<EventArgs> e = OnClose;
		if(e != null)
			e(this, new EventArgs());
	}
	
	private void UpdateBPM()
	{
		musicData.bpm = initialBPM * speedPercents[currentSpeedSelected];
		if(musicData.bpm > 240f)
			musicData.bpm = 240f;
		else if(musicData.bpm < 30f)
			musicData.bpm = 30f;
	}
}
