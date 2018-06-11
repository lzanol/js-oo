//#define IS_CLIENT
//#define BAND
using UnityEngine;
using System.Collections;
using DataList;
using DataSet;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using Utils;


public class Menu : MonoBehaviour 
{
#if IS_CLIENT
	public static bool IS_CLIENT = true;
#else
	public static bool IS_CLIENT = false;
#endif
	
	public Texture TeamLogo;
	
	private GUISkin guitarranew;
	private GUISkin guitarranew2;
	
	public static string[] DEVICE_NAMES = {"SOLO", "RITMO", "RITMO", "ACOMP." };
	
	public static DataSet.MusicData musicSelected;
	public static int trackSelected = -1;
	public static bool useSymbols = false;
	public static int currentTranspose = 0;
	private static int musicSelectedIndex = -1;
	private static GameObject showExpiredTime;
	private static Process currentXMidiProccess = null;
	private static Process currentSaberProccess = null;
	private static Process currentGuitarTutorialProccess = null;
	public static string nextLevelName = Common.SCENE_GUITAR;
	
	private UnityEngine.Object configTemplate;
	private ConfigDialog configDialog;
	
	private UnityEngine.Object activationTemplate;
	private ActivationDialog activationDialog;
	private UnityEngine.Object showExpiredTimeTemplate;
	
	private UnityEngine.Object importTemplate;
	private ImportDialog importDialog;
	
	private UnityEngine.Object removeDialogTemplate;
	private RemoveDialog removeDialog;
	
	private UnityEngine.Object difficultyDialogTemplate;
	private DifficultyDialog difficultyDialog;
	
	private UnityEngine.Object trackEditDialogTemplate;
	private TrackEditDialog trackEditDialog;
	
	private UnityEngine.Object bandDialogTemplate;
	private BandDialog bandDialog;
	
	private string[] transposeLabels = {"-6","","-2","-1,5","-1","-0,5","0","+0,5","+1","+1,5","+2"};
	
	private TMDList tmdList;
	
	private bool lockMenu = false;
	
	private bool renaming = false;
	private bool excluding = false;
	private bool changingAttrib = false; 

	private string newTitle = "";
	private string newAttrib = "";
	
	private bool showFilePath = false;
	
	private float scrollViewMusicsTotalHeight;
	private float scrollViewTracksTotalHeight;
	private Rect musicSelectorRect;
	private Rect trackSelectorRect;
	private Rect realConfigRect;
	
	private bool needMoveMusicSelectorSliderPositionToMusicSelected = false;
	private bool edit = false;
	
	private string searchText = "";
	
	public Texture2D[] backgroundImage;
	
	public GUIStyle[] buttonGuitarSolo;
	public GUIStyle[] buttonDrum;
	public GUIStyle[] buttonPiano;
	public GUIStyle[] buttonReal;
	
	public GUIStyle[] buttonRealH;
	
	public GUIStyle buttonXMidi;
	public GUIStyle buttonSaber;
	
	public GUIStyle arrowButtonUp;
	public GUIStyle arrowButtonDown;
	
	public GUIStyle buttonClose;
	public GUIStyle buttonInfo;
	public GUIStyle buttonTutorial;
	public GUIStyle buttonConfig;
	public GUIStyle button4Play;
	public GUIStyle buttonPlay;
	
	public GUIStyle buttonItem;
	public GUIStyle boxCategory;
	public GUIStyle labelItem;
	public GUIStyle labelItemSelected;
	public GUIStyle labelWhiteTitle;
	public GUIStyle labelCurrentTranspose;
	public GUIStyle[] arrows;
	
	public GUIStyle buttonImport;
	public GUIStyle buttonRename;
	public GUIStyle buttonRemove;
	public GUIStyle buttonYes;
	public GUIStyle buttonNo;
	public GUIStyle buttonEdit;
	public GUIStyle ballonImportReminder;
	
	public Texture2D searchImage;
	public GUIStyle searchTextStyle;
	
	public GUIStyle backgroundRealConfig;
	
	public GUISkin skinToUse;
	
	public Texture2D secondaryBar;
	
	public GUIStyle[] starsStyle;
	
	public SoundEngine SoundEngine { get; set; }
	
	private void Awake()
	{
		configTemplate = Resources.Load("Prefabs/Dialogs/ConfigDialog");
		showExpiredTimeTemplate = Resources.Load("Prefabs/Dialogs/ShowExpiredTime");
		importTemplate = Resources.Load("Prefabs/Dialogs/ImportDialog");
		removeDialogTemplate = Resources.Load("Prefabs/Dialogs/RemoveDialog");
		difficultyDialogTemplate = Resources.Load ("Prefabs/Dialogs/DifficultyDialog");
		trackEditDialogTemplate = Resources.Load ("Prefabs/Dialogs/TrackEditDialog");
		bandDialogTemplate = Resources.Load("Prefabs/Dialogs/BandDialog");
		guitarranew = Resources.Load("newguitarbt") as GUISkin;
		guitarranew2 = Resources.Load("newguitarbt2") as GUISkin;
		SoundEngine = SoundEngine.CurrentInstance;
	}
	
	private void Start()
	{
		AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
#if UNITY_ANDROID
		nextLevelName = Common.SCENE_SONGBOOK;
#endif

		BandNetworkController.tracksToMute = new List<int>();

#if UNITY_ANDROID
		tmdList = new TMDList(Utils.Common.MUSICS_DIR);
#else
		tmdList = new TMDList(Utils.Common.MUSICS_DIR);
#endif
		DevicePlayType[] devices = new DevicePlayType[4];
		devices[0] = DevicePlayType.SOLO;
		devices[1] = DevicePlayType.BASS;
		devices[2] = DevicePlayType.DRUMS;
		devices[3] = DevicePlayType.ACCOMPANIMENT;
		tmdList.SetDevicePlayType(devices);
		if(tmdList.sortedMusics.Length == 0)
			tmdList.CreateMusicList();
		
		if(searchText != "")
			tmdList.SortMusicsByTitleUsingText(searchText);
#if !UNITY_ANDROID
		if(IS_CLIENT)
		{
			bandDialog = (Instantiate(bandDialogTemplate) as GameObject).GetComponent<BandDialog>();
			bandDialog.OnClose += delegate {
				lockMenu = false;
			};
			lockMenu = true;
			enabled = false;
		}
		else
		{

			Validator.Status status;

			status = Validator.GetEnabled(2);
			
			if (status == Validator.Status.Disabled)
				OpenActivationDialog();
			else if(status == Validator.Status.Trial)
				showExpiredTime = showExpiredTime ?? Instantiate(showExpiredTimeTemplate) as GameObject;
			
			
			if(musicSelected != null)
			{
				for(int i = 0; i < tmdList.sortedMusics.Length;i++)
				{
					if(tmdList.sortedMusics[i].Title == musicSelected.title)
					{
						musicSelectedIndex = i;
						needMoveMusicSelectorSliderPositionToMusicSelected = true;
						break;
					}
				}
			}
		}
#endif
	}
	
	private static float musicSelectorSliderPosition = 0f;
	private float trackSelectorSliderPosition = 0f;
	private int trackCounter = 0;

	private int bufferSize;
	private int numBuffers;
	
	void OnGUI () 
	{
		//print(nextLevelName);
		GUI.depth = 10;
		GUI.enabled = !lockMenu;

		GUI.skin = skinToUse;
		
		GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),backgroundImage[1]);
		GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),backgroundImage[0]);
		
		GUI.DrawTexture(new Rect(0,Screen.height - (Screen.height/20),Screen.width,Screen.height/20),secondaryBar);

#if UNITY_ANDROID
		GUI.Label (new Rect(0,0,100f, 100f), bufferSize.ToString());
		GUI.Label (new Rect(100f,0,100f, 100f), numBuffers.ToString());

		float closeSize = Screen.height/8f;
		if(GUI.Button(new Rect(Screen.width - closeSize, 0, closeSize, closeSize), "", buttonClose))
			Application.Quit();
#endif

#if !UNITY_ANDROID
		
		float factor = Screen.width/8f / buttonGuitarSolo[0].onNormal.background.width;
		Rect appButtonRect = new Rect(Screen.width/4.5f, 10f, Screen.width/8f,  buttonGuitarSolo[0].onNormal.background.height * factor);
		
		if(TeamLogo)
		{
			GUI.DrawTexture(new Rect(Screen.width*.2f,Screen.height*.01f,Screen.height*.1f,Screen.height*.1f),TeamLogo);
			appButtonRect.x += appButtonRect.width/2;
		}
		
		//App Buttons
		
		
		
		/*/begin drums
		if(GUI.Button (new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "", (nextLevelName == Common.SCENE_DRUMS) ? buttonDrum[1] : buttonDrum[0]))
		{
			nextLevelName = Common.SCENE_DRUMS;
		}
		appButtonRect.x += appButtonRect.width;//end drums */
		GUI.skin = skinToUse;
		if(TeamLogo)
		{
			if(GUI.Button(new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "5", (nextLevelName == Common.SCENE_GUITAR) ? buttonGuitarSolo[1] : buttonGuitarSolo[0]))
			{
				nextLevelName = Common.SCENE_GUITAR;
			}
			appButtonRect.x += appButtonRect.width;
			
			GUI.skin = (nextLevelName.Contains("GameGuitarMidi")) ? guitarranew2 : guitarranew;
			if(GUI.Button(new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), ""))
			{
				nextLevelName = "GameGuitarMidi";
			}
		}
		else
		{
			if(GUI.Button(new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "5", (nextLevelName == Common.SCENE_GUITAR) ? buttonGuitarSolo[1] : buttonGuitarSolo[0]))
			{
				nextLevelName = Common.SCENE_GUITAR;
			}
			appButtonRect.x += appButtonRect.width;
			
			/*GUI.skin = (nextLevelName.Contains("GameGuitarMidi")) ? guitarranew2 : guitarranew;
			if(GUI.Button(new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), ""))
			{
				nextLevelName = "GameGuitarMidi";
			}
			appButtonRect.x += appButtonRect.width;*/
			GUI.skin = skinToUse;
		
		
			if(GUI.Button(new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "", (nextLevelName == Common.SCENE_PIANO) ? buttonPiano[1] : buttonPiano[0]))
			{
				nextLevelName = Common.SCENE_PIANO;
			}
			appButtonRect.x += appButtonRect.width;
			if(GUI.Button (new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "", (nextLevelName == Common.SCENE_REAL) ? buttonReal[1] : buttonReal[0]))
			{
				nextLevelName = Common.SCENE_REAL;
			}
			appButtonRect.x += appButtonRect.width;
			/*RealHorizontal
			if(GUI.Button (new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "", (nextLevelName == Common.SCENE_REAL_H) ? buttonRealH[1] : buttonRealH[0]))
			{
				nextLevelName = Common.SCENE_REAL_H;
			}//*/
			appButtonRect.x += appButtonRect.width;
			if(GUI.Button (new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "", buttonXMidi))
			{
				if(currentXMidiProccess == null || currentXMidiProccess.HasExited)
				{
					currentXMidiProccess = new Process();
					currentXMidiProccess.StartInfo.FileName = System.IO.Path.GetFullPath(Utils.Common.APP_XMIDI);
					currentXMidiProccess.StartInfo.Arguments = "";
					currentXMidiProccess.Start();
				}
			}
			
			appButtonRect.x += appButtonRect.width;
			if(GUI.Button (new Rect(appButtonRect.x, appButtonRect.y, appButtonRect.width, appButtonRect.height), "", buttonSaber))
			{
				if(currentSaberProccess == null || currentSaberProccess.HasExited)
				{
					currentSaberProccess = new Process();
					currentSaberProccess.StartInfo.FileName = System.IO.Path.GetFullPath(Application.persistentDataPath + "/ChildPlay Saber/ChildPlay Saber.exe");
					currentSaberProccess.StartInfo.Arguments = "";
					currentSaberProccess.Start();
				}
			}
		}
#endif
		// Selecionar Musicas
		musicSelectorRect = new Rect(Screen.width / 10.5f, Screen.height/5.95f, (Screen.width/2.13f),  (Screen.height/1.67f));
		
		scrollViewMusicsTotalHeight = 32f * tmdList.sortedMusics.Length;
		
		if(scrollViewMusicsTotalHeight > musicSelectorRect.height)
		{
			if(GUI.Button(new Rect(musicSelectorRect.x + musicSelectorRect.width - 16,musicSelectorRect.y-15,50,45), "", arrowButtonUp))
				ArrowButtonUp();
			
			musicSelectorSliderPosition = GUI.VerticalSlider(new Rect(musicSelectorRect.x + musicSelectorRect.width -3, musicSelectorRect.y + 25, 60f, musicSelectorRect.height - 25f)
				,musicSelectorSliderPosition , 0f, scrollViewMusicsTotalHeight - musicSelectorRect.height);
	
			if(GUI.Button(new Rect(musicSelectorRect.x + musicSelectorRect.width - 16,musicSelectorRect.y + musicSelectorRect.height -5f,50,45), "", arrowButtonDown))
				ArrowButtonDown();
		}
		
		GUI.skin.settings.cursorColor = Color.black;
		string tempStr = searchText;
		
		GUI.SetNextControlName("SearchMusics");
		searchText = GUI.TextField(new Rect(musicSelectorRect.x + musicSelectorRect.width - 210f, musicSelectorRect.y - 24f,200f,22f), searchText, 30, searchTextStyle);
		
		if(!lockMenu && !renaming && (GUI.GetNameOfFocusedControl() != "SearchMusics"))
			GUI.FocusControl("SearchMusics");
		
		
		GUI.DrawTexture(new Rect(musicSelectorRect.x + musicSelectorRect.width - 207f, musicSelectorRect.y - 21f, 15f, 15f), searchImage);
		if(searchText != tempStr)
		{
			searchText = searchText.Replace("\n","");
			
			musicSelectorSliderPosition = 0f;
			musicSelectedIndex = -1;
			CleanMusicSelectedStatus();
		
			tmdList.SortMusicsByTitleUsingText(searchText);
				
			if(searchText == "")
				tmdList.RefreshList();
		}
		
		GUI.BeginGroup(new Rect(musicSelectorRect.x, musicSelectorRect.y , musicSelectorRect.width + 20, musicSelectorRect.height));
		float boxWidth = (musicSelectorRect.width/34f);
		float boxHeight = 0;
		
		GUI.BeginGroup(new Rect(0, boxHeight +5, musicSelectorRect.width + 15, musicSelectorRect.height - boxHeight - 5));
		
		float buttonPositionY = 2f - musicSelectorSliderPosition;
		float buttonPositionX = 0f;
		for(int i = 0; i < tmdList.sortedMusics.Length; ++i)
		{
			buttonPositionX = 0f;
			
			if(i == musicSelectedIndex)
			{
				if(!renaming)
				{
					GUI.Label(new Rect(buttonPositionX,buttonPositionY,boxWidth * 27f,30), tmdList.sortedMusics[i].Title, labelItemSelected);
				}
				else
				{
					GUI.SetNextControlName("NewTitle");
					newTitle = GUI.TextField( new Rect(buttonPositionX, buttonPositionY, boxWidth * 27f,30), newTitle, 100);
					GUI.FocusControl("NewTitle");
					
					if(Event.current.keyCode == KeyCode.Return)
						Rename();
					else if(Event.current.keyCode == KeyCode.Escape)
						renaming = false;
				}
				
				if(changingAttrib)
				{
					if(Event.current.keyCode == KeyCode.Return)
						CleanMusicSelectedStatus();
					GUI.SetNextControlName("NewAttrib");
					newAttrib = GUI.TextField( new Rect(buttonPositionX + boxWidth*27f + 2f, buttonPositionY, boxWidth * 6f,30), newAttrib, 5);
					GUI.FocusControl("NewAttrib");
				}
				else
				{
					GUI.Label(new Rect(buttonPositionX + boxWidth*27f + 2f, buttonPositionY, boxWidth * 6f, 30), "", labelItemSelected);
					int starsNumber = (tmdList.sortedMusics[i].Attrib == "4") ? 4 : (tmdList.sortedMusics[i].Attrib == "3") ? 3 : (tmdList.sortedMusics[i].Attrib == "2") ? 2 : (tmdList.sortedMusics[i].Attrib == "1") ? 1 : 0;
					if(GUI.Button(new Rect(buttonPositionX + boxWidth*27f + 2f,buttonPositionY,(boxWidth*6f), 30), "", starsStyle[ starsNumber ]))
					{
						CleanMusicSelectedStatus();
						
						lockMenu = true;
					
						difficultyDialog = (Instantiate(difficultyDialogTemplate) as GameObject).GetComponent<DifficultyDialog>();
						difficultyDialog.OnClose += delegate {
							lockMenu = false;
							tmdList.RefreshList();
						};
					}
				}
			}
			else
			{
				if(GUI.Button(new Rect(buttonPositionX-2,buttonPositionY,(boxWidth*27f),30), "", buttonItem))
				{
					CleanMusicSelectedStatus();
					
					musicSelected = new DataSet.MusicData(tmdList.sortedMusics[i].FilePath);
					musicSelectedIndex = i;
					trackSelected = -1;
					/*if(searchText != "")
					{
						searchText = "";
						tmdList.RefreshList();
						for(int j = 0; j < tmdList.sortedMusics.Length; j++)
						{
							if(tmdList.sortedMusics[i].Title == musicSelected.title)
							{
								musicSelectedIndex = i;
								needMoveMusicSelectorSliderPositionToMusicSelected = true;
								break;
							}
						}
					}*/
				}
				
				int starsNumber = (tmdList.sortedMusics[i].Attrib == "4") ? 4 : (tmdList.sortedMusics[i].Attrib == "3") ? 3 : (tmdList.sortedMusics[i].Attrib == "2") ? 2 : (tmdList.sortedMusics[i].Attrib == "1") ? 1 : 0;
				if(GUI.Button(new Rect(buttonPositionX + boxWidth*27f + 2f,buttonPositionY,(boxWidth*6f),30), "", starsStyle[ starsNumber ]))
				{
					CleanMusicSelectedStatus();
					
					musicSelected = new DataSet.MusicData(tmdList.sortedMusics[i].FilePath);
					musicSelectedIndex = i;
					trackSelected = -1;
					
					lockMenu = true;
					
					difficultyDialog = (Instantiate(difficultyDialogTemplate) as GameObject).GetComponent<DifficultyDialog>();
					difficultyDialog.OnClose += delegate {
						lockMenu = false;
						tmdList.RefreshList();
					};
				}
				
				GUI.Label(new Rect(buttonPositionX, buttonPositionY, boxWidth*27f, 30),tmdList.sortedMusics[i].Title,labelItem);
				GUI.Label(new Rect(buttonPositionX + boxWidth*27f + 2f, buttonPositionY, boxWidth*6f, 30), "", labelItem);
			}
			buttonPositionY += 32;
		}
		GUI.EndGroup();
		GUI.EndGroup();
		
		if(GUI.Button(new Rect(musicSelectorRect.x,musicSelectorRect.y + musicSelectorRect.height + 5,100,40),"",buttonImport))
		{
			ImportMusics();
		}
		
		if(musicSelectedIndex > -1)
		{
			if(!renaming)
			{
				if(GUI.Button(new Rect(musicSelectorRect.x + 110, musicSelectorRect.y + musicSelectorRect.height + 5,100,40)
					, ""
					, buttonRemove))
				{
					lockMenu = true;
					removeDialog = (Instantiate(removeDialogTemplate) as GameObject).GetComponent<RemoveDialog>();
					removeDialog.OnRemove += delegate {
						musicSelectedIndex = -1;
						trackSelected = -1;
					};
					removeDialog.OnClose += delegate {
						tmdList.RefreshList();
						lockMenu = false;
					};
				}
				
				if(GUI.Button(new Rect(musicSelectorRect.x + 220, musicSelectorRect.y + musicSelectorRect.height + 5,100,40)
					, ""
					, buttonRename))
				{
					CleanMusicSelectedStatus();
					newTitle = tmdList.sortedMusics[musicSelectedIndex].Title.ToString();
					renaming = true;
				}
#if !UNITY_ANDROID
				if(!TeamLogo)
				{
					if(currentXMidiProccess == null || currentXMidiProccess.HasExited)
					{
						if(GUI.Button (new Rect(musicSelectorRect.x + 330, musicSelectorRect.y + musicSelectorRect.height + 5, 100, 40)
							, "", buttonEdit))
						{						
							edit = true;
							EditTXT eTXT = new EditTXT();
							eTXT.CreateFile(Path.GetFullPath(tmdList.sortedMusics[musicSelectedIndex].FilePath));
							
							currentXMidiProccess = new Process();
							currentXMidiProccess.StartInfo.FileName = System.IO.Path.GetFullPath(Utils.Common.APP_XMIDI);
							currentXMidiProccess.StartInfo.Arguments = "";
							currentXMidiProccess.Start();
						}
					}
				}
#endif
			}
		}
		
		trackSelectorRect = new Rect(Screen.width - (Screen.width/2.94f), (Screen.height/5.8f), (Screen.width/3.93f), Screen.height/4.5f);
		
		if(musicSelectedIndex > -1)
		{	
			if(needMoveMusicSelectorSliderPositionToMusicSelected)
			{
				needMoveMusicSelectorSliderPositionToMusicSelected = false;
				MoveMusicSelectorSliderPositionToMusicSelected();
			}
			//TrackSelector
			if(musicSelected.Instruments.Length > 0)
			{
				scrollViewTracksTotalHeight = 32 * tmdList.sortedMusics[musicSelectedIndex].NumberSoloTracks;
				
				if(scrollViewTracksTotalHeight > trackSelectorRect.height)
				{
					if(GUI.Button(new Rect(trackSelectorRect.x + trackSelectorRect.width-7,trackSelectorRect.y -25,25,35),"",arrowButtonUp))
					{
						if(trackSelectorSliderPosition > 32f)
							trackSelectorSliderPosition -= 32f;
						else
							trackSelectorSliderPosition = 0;
					}
					
					trackSelectorSliderPosition = GUI.VerticalSlider(new Rect(trackSelectorRect.x + trackSelectorRect.width, trackSelectorRect.y + 10f, 20, trackSelectorRect.height - 15f)
						,trackSelectorSliderPosition ,0f,(float)scrollViewTracksTotalHeight - trackSelectorRect.height);
			
					if(GUI.Button(new Rect(trackSelectorRect.x + trackSelectorRect.width-7,trackSelectorRect.y + trackSelectorRect.height -5f,25,35), "", arrowButtonDown))
					{
						if(trackSelectorSliderPosition < (float)scrollViewTracksTotalHeight - trackSelectorRect.height)
							trackSelectorSliderPosition += 32f;
						else
							trackSelectorSliderPosition = (float)scrollViewTracksTotalHeight - trackSelectorRect.height + 30f;
					}
				}
				
				GUI.BeginGroup(trackSelectorRect);
				boxWidth = (trackSelectorRect.width/34f);
				boxHeight = (trackSelectorRect.height/7f);
				
				buttonPositionY = 2f - trackSelectorSliderPosition;
				
				trackCounter = 0;
				
				for(int i = 0; i < musicSelected.Instruments.Length; ++i)
				{
					if(musicSelected.Instruments[i].playable != DevicePlayType.NONE)
					{
						trackCounter ++;
						
						if(trackSelected < 0)
							trackSelected = i;
						
						buttonPositionX = 0f;
						if(GUI.Button(new Rect(buttonPositionX, buttonPositionY, (boxWidth * 33f), 30), new GUIContent(""), buttonItem))
							trackSelected = i;
						
						GUI.Label(new Rect(buttonPositionX,buttonPositionY,boxWidth * 33f,30), 
							(trackCounter) + ". " + SoundEngine.CurrentInstance.GetSampleSet(musicSelected.Instruments[i].hash).Label + " - " + DEVICE_NAMES[((int)musicSelected.Instruments[i].playable) -1],
							(i == trackSelected) ? labelItemSelected : labelItem);
						buttonPositionY += 32;
					}
				}
				GUI.EndGroup();
#if !UNITY_ANDROID
				if(GUI.Button(new Rect(trackSelectorRect.xMax -100f,trackSelectorRect.yMax + 10f, 100f, 40f),"", buttonEdit))
				{
					lockMenu = true;
					trackEditDialog = (Instantiate(trackEditDialogTemplate) as GameObject).GetComponent<TrackEditDialog>();
					trackEditDialog.OnClose += delegate {
						lockMenu = false;
						tmdList.RefreshList();
					};
				}
#endif
			}
		}
		
		if(nextLevelName == Common.SCENE_REAL || nextLevelName == Common.SCENE_REAL_H || nextLevelName == Common.SCENE_SONGBOOK)
		{
			realConfigRect = new Rect(Screen.width - (Screen.width/2.68f), (Screen.height/1.9f),(Screen.width/3.19f),70f);
			GUI.BeginGroup(new Rect(realConfigRect));
				GUI.Box(new Rect(0f,0f,realConfigRect.width, realConfigRect.height),"", backgroundRealConfig);
				GUI.Label(new Rect(10f,2f,realConfigRect.width-10f,40f),"Transposição de Notas:", labelWhiteTitle);
				if(currentTranspose > -12)
					if(GUI.Button(new Rect(50f, 35f, 40f,40f), "", arrows[0]))
						currentTranspose--;
			
				GUI.Label(new Rect(0f,40f,realConfigRect.width,realConfigRect.height - 50f), (currentTranspose/2f).ToString(), labelCurrentTranspose);
				if(currentTranspose < 12)
					if(GUI.Button(new Rect( realConfigRect.width - 90f , 35f, 40f,40f), "", arrows[1]))
						currentTranspose++;
			
			GUI.EndGroup();
		}
#if !UNITY_ANDROID				
		//Info button
		int Wee = 70;
		
		
			if(GUI.Button(new Rect((Screen.width/9)-Wee,Screen.height - (Screen.height/6.5f), 60, 60),new GUIContent( "", "Informações do produto."), buttonInfo))
				OpenActivationDialog();
			
			Wee-=60;
		
		if(!TeamLogo)
		{
		//Tutorial button
		if(GUI.Button(new Rect((Screen.width/9)-Wee,Screen.height - (Screen.height/6.5f), 60, 60),new GUIContent( "", "Tutorial."), buttonTutorial))
			OpenGuitarTutorial();
		}
#endif
		
		//Play/Config Button
		if(trackSelected > -1)
		{
			if(GUI.Button(new Rect(Screen.width - (Screen.width/9) - 230,Screen.height - (Screen.height/6.5f),60,60), new GUIContent("", "Configurar."), buttonConfig))
			{
				configDialog = ((GameObject)Instantiate(configTemplate)).GetComponent<ConfigDialog>();
				configDialog.SetMusicData(musicSelected, trackSelected);
				configDialog.OnClose += OnCloseConfigDialog;
				lockMenu = true;
			}
			
			if(nextLevelName ==  Common.SCENE_PIANO || nextLevelName == Common.SCENE_REAL || nextLevelName == Common.SCENE_REAL_H)
				useSymbols = GUI.Toggle(new Rect(Screen.width - (Screen.width/9) - 60, Screen.height - (Screen.height/6.5f) - 80, 120,20), useSymbols, "Cifra");

#if !UNITY_ANDROID && BAND
			if(GUI.Button(new Rect(Screen.width - (Screen.width/9) - 165, Screen.height - (Screen.height/6.5f) - 45, 105,105), new GUIContent("", "Banda."), button4Play))
			{
				if(nextLevelName == Common.SCENE_REAL || nextLevelName == Common.SCENE_REAL_H)
					musicSelected.instruments[trackSelected].transposeVisual = currentTranspose;

				bandDialog = (Instantiate(bandDialogTemplate) as GameObject).GetComponent<BandDialog>();
				bandDialog.OnClose += delegate {
					lockMenu = false;
				};
				lockMenu = true;
			}
#endif
			if(GUI.Button(new Rect(Screen.width - (Screen.width/9) - 60, Screen.height - (Screen.height/6.5f) - 50, 120,120), new GUIContent("", "Jogar."), buttonPlay))
			{
				if(nextLevelName == Common.SCENE_REAL || nextLevelName == Common.SCENE_REAL_H || nextLevelName == Common.SCENE_SONGBOOK)
					musicSelected.instruments[trackSelected].transposeVisual = currentTranspose;
				
				Application.LoadLevel(nextLevelName);
			}
		}
		
		if(showFilePath && musicSelectedIndex > 0)
		{
			GUI.Label(new Rect(0,Screen.height - 20, 500,20),System.IO.Path.GetFullPath(tmdList.sortedMusics[musicSelectedIndex].FilePath));
		}
	}
	
	private void Update()
	{
		if(lockMenu)
		{
			if(Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.R) && Input.GetKey(KeyCode.B))
			{
				Validator.SetEnabled(true);
				activationDialog.enabled=false;
				lockMenu=false;
			}
			
			return;
		}
		
		if(Input.GetKeyDown(KeyCode.F12) && Input.GetKey(KeyCode.LeftControl))
			showFilePath = !showFilePath;
		
		if(trackSelected > -1)
			if(Input.GetButtonDown("9")
				|| Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
					Application.LoadLevel(nextLevelName);
		
		float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
	
		if(scrollWheel < 0f && musicSelectorSliderPosition < scrollViewMusicsTotalHeight - musicSelectorRect.height)
			musicSelectorSliderPosition += 32f;
		else if(scrollWheel > 0f)
		{
			musicSelectorSliderPosition -= 32f;
			if(musicSelectorSliderPosition < 0f)
				musicSelectorSliderPosition = 0f;
		}
	}
	
	private void Rename()
	{
		renaming = false;
		if(newTitle != musicSelected.title)
		{
			musicSelected.title = newTitle;
			musicSelected.SaveFile(tmdList.sortedMusics[musicSelectedIndex].FilePath);
			musicSelectedIndex = -1;
			trackSelected = -1;
			tmdList.RefreshList();
		}
	}

	private void OpenActivationDialog()
	{
		if(activationTemplate == null)
			activationTemplate = Resources.Load("Prefabs/Dialogs/ActivationDialog");

		activationDialog = ((GameObject)Instantiate(activationTemplate)).GetComponent<ActivationDialog>();
		activationDialog.OnClose += OnCloseActivationDialog;

		lockMenu = true;
	}
	
	private void OnCloseConfigDialog(object sender, EventArgs e)
	{
		Destroy(configDialog.gameObject);
		lockMenu = false;
	}
	
	private void OnCloseActivationDialog(object sender, EventArgs e)
	{
		Destroy(activationDialog.gameObject);
		lockMenu = false;
	}
	
	private TMDList importList;
	
	private void ImportMusics()
	{
		lockMenu = true;
		
		CleanMusicSelectedStatus();
		
		importDialog = ((GameObject)Instantiate (importTemplate)).GetComponent<ImportDialog>();
		
		importDialog.OnImport += delegate
		{
			Thread t = new Thread(CopyMusics);
			t.Start();
		};
		
		importDialog.OnClose += delegate 
		{
			tmdList.RefreshList();
			lockMenu = false;
		};
	}
	
	private void CopyMusics()
	{
		string destFile;
		int countImported = 0;
		if(importDialog.listFiles.Count > 0)
		{
			foreach(KeyValuePair<int, MusicListItem> file in importDialog.listFiles)
			{
				destFile = System.IO.Path.GetFullPath(tmdList.GetDirectory) + (new TimeSpan(DateTime.Now.Ticks)).TotalMilliseconds + ".tmd";
				MusicData md = new MusicData(file.Value.FilePath);
				
				md.title = BuildTitle(md.title, tmdList.sortedMusics, md.title);
				
				md.SaveFile(destFile);
				countImported++;
				importDialog.totalImportedMusics = countImported;
			}
		}
		
		lockMenu = false;
		
		tmdList.RefreshList();
		
		return;
	}
	
	private string BuildTitle(string titleOriginal, MusicListItem[] list, string title, int index = 1)
	{
		foreach (MusicListItem item in list)
			if (title == item.Title)
				return BuildTitle(titleOriginal, list, titleOriginal + " (" + index + ")", ++index);
		
		return title;
	}
	
	private void ArrowButtonDown()
	{
		if(lockMenu)
			return;
		
		if(importDialog != null)
			importDialog.ArrowButtonDown();
		else
		{
			if(musicSelectorSliderPosition <= (float)scrollViewMusicsTotalHeight - musicSelectorRect.height)
				musicSelectorSliderPosition += 32f;
			else
				musicSelectorSliderPosition = (float)scrollViewMusicsTotalHeight - musicSelectorRect.height;
		}
	}
	
	private void MoveMusicSelectorSliderPositionToMusicSelected()
	{
		float newPosition = musicSelectedIndex * 32f;
		
		if(newPosition <= (float)scrollViewMusicsTotalHeight - musicSelectorRect.height)
			musicSelectorSliderPosition = newPosition;
		else
			musicSelectorSliderPosition = (float)scrollViewMusicsTotalHeight - musicSelectorRect.height;
	}
	
	private void ArrowButtonUp()
	{
		if(lockMenu)
			return;
		
		if(importDialog != null)
			importDialog.ArrowButtonUp();
		else
		{
			if(musicSelectorSliderPosition > 32f)
				musicSelectorSliderPosition -= 32f;
			else
				musicSelectorSliderPosition = 0;
		}
	}
	
	private void CleanMusicSelectedStatus()
	{
		if(renaming)
			Rename();
		else if(excluding)
			excluding = false;
		
		if(changingAttrib)
		{
			if(newAttrib != musicSelected.attrib)
			{
				musicSelected.attrib = newAttrib;
				musicSelected.SaveFile(tmdList.sortedMusics[musicSelectedIndex].FilePath);
				tmdList.RefreshList();
			}
			changingAttrib = false;
		}
	}
	
	public void ReceiveStroke(float currentStroke)
	{
		if(currentStroke < 0 && musicSelectedIndex < tmdList.sortedMusics.Length - 1)
		{
			CleanMusicSelectedStatus();
			
			musicSelectedIndex ++;
			musicSelected = new DataSet.MusicData(tmdList.sortedMusics[musicSelectedIndex].FilePath);
			trackSelected = -1;
			if(musicSelectedIndex * 32f >= musicSelectorSliderPosition + musicSelectorRect.height -64f )
			{
				ArrowButtonDown();
			}
		}
		else if(currentStroke > 0 && musicSelectedIndex > 0)
		{
			CleanMusicSelectedStatus();
			
			musicSelectedIndex --;
			musicSelected = new DataSet.MusicData(tmdList.sortedMusics[musicSelectedIndex].FilePath);
			trackSelected = -1;
			if(musicSelectedIndex * 32f < musicSelectorSliderPosition)
			{
				ArrowButtonUp();
			}
		}
	}
	
	public void OpenGuitarTutorial()
	{
		if(currentGuitarTutorialProccess == null)
		{
			currentGuitarTutorialProccess = new Process();
			currentGuitarTutorialProccess.StartInfo.FileName = System.IO.Path.GetFullPath(Utils.Common.APP_TUTOR_GUITARRA);
			currentGuitarTutorialProccess.StartInfo.Arguments = "";
			currentGuitarTutorialProccess.Start();
		}
		else if(currentGuitarTutorialProccess.HasExited)
		{
			currentGuitarTutorialProccess = new Process();
			currentGuitarTutorialProccess.StartInfo.FileName = System.IO.Path.GetFullPath(Utils.Common.APP_TUTOR_GUITARRA);
			currentGuitarTutorialProccess.StartInfo.Arguments = "";
			currentGuitarTutorialProccess.Start();
		}
	}
		
	void OnApplicationFocus(bool focus)
	{
		tmdList.RefreshList();
		if(edit)
		{
			edit = false;
			musicSelected = new MusicData(tmdList.sortedMusics[musicSelectedIndex].FilePath);
		}
	}
}
