using System;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using DataSet;
using Utils;

public class ImportDialog : MonoBehaviour 
{
	private const float TOTAL_WIDTH = 549f;
	private const float TOTAL_HEIGHT = 543f;
	private const string URL_LIST_MUSIC = "http://www.tomplay.com.br/api/?target=TomplayShow&action=ListMusics";
	private const string URL_GET_MUSIC = "http://www.tomplay.com.br/api/?target=TomplayShow&action=GetMusic";

	public const string OLD_LIST_FILE = "OldList.txt";
	
	public event EventHandler OnClose;
	public event EventHandler OnImport;
	
	public Texture2D blackBackground;
	public Texture2D background;
	public Texture2D emptyBar;
	public Texture2D bar;
	public GUIStyle backgroundTitle;
	public GUIStyle pathShower;
	public GUIStyle item;
	public GUIStyle itemSelected;
	public GUIStyle hideButton;
	
	public GUIStyle importButton;
	public GUIStyle closeButton;
	public GUIStyle arrowButtonUp;
	public GUIStyle arrowButtonDown;
	public GUIStyle configButton;
	public GUIStyle labelStyle;
	public Texture2D searchImage;
	public GUIStyle searchTextStyle;
	public GUISkin skinToUse;
	
	public Dictionary<int,MusicListItem> listFiles;
	
	[HideInInspector]
	public int totalImportedMusics = 0;

	private string title;
	private bool webImport = true;
	private List<string[]> webList;
	private List<string[]> webListToShow;
	private List<string[]> webListToImport;
	private Rect dialogRect;
	private Rect musicListRect;
	private DataList.TMDList importTMDList;
	private bool lockImport;
	
	private float musicSelectorSliderPosition = 0f;
	private float scrollViewMusicsTotalHeight = 0f;
	
	private List<string> strs = new List<string>();
	private string old_list_path;
	private string currentPath = "";
	private List<MusicListItem> sortedMusics;
	
	private string searchText = "";
	
	void Awake()
	{
		StartCoroutine(TryWebImport());
		webList = new List<string[]>();
		webListToShow = new List<string[]>();
		webListToImport = new List<string[]>();
	}
	
	void OnGUI()
	{
		GUI.depth = 0;
		GUI.enabled = !lockImport;
		GUI.skin = skinToUse;
		dialogRect = new Rect(Screen.width/2f - TOTAL_WIDTH/2f, Screen.height/2f - TOTAL_HEIGHT/2f,TOTAL_WIDTH,TOTAL_HEIGHT);
		musicListRect = new Rect(dialogRect.x + 45f, dialogRect.y + 45f , TOTAL_WIDTH -100f, TOTAL_HEIGHT - 100f);
		
		GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), blackBackground);
		GUI.DrawTexture(dialogRect, background);
		
		GUI.skin.settings.cursorColor = Color.black;
		string tempStr = searchText;
		searchText = GUI.TextField(new Rect(musicListRect.x + musicListRect.width - 220, musicListRect.y -25f,200f,25f),searchText,30, searchTextStyle);
		GUI.DrawTexture(new Rect(musicListRect.x + musicListRect.width - 217, musicListRect.y - 22f,18,18),searchImage);
		
		GUI.Label(new Rect(dialogRect.x + 50, dialogRect.y + 25,150, 40), title, backgroundTitle);
		
		if(!webImport)
		{
#if !UNITY_ANDROID
			if(GUI.Button(new Rect(musicListRect.x + musicListRect.width - 15f, musicListRect.y -35f, 40, 40), "", configButton))
			{
				lockImport = true;
				Dialog.OpenDialog od = new Dialog.OpenDialog("Selecione a pasta",false);
				od.OnClose += delegate(object sender, Events.OpenDialogEventArgs openEvt) 
				{
					if(openEvt.Confirmed)
					{
						currentPath = openEvt.Path;
						importTMDList = new DataList.TMDList(openEvt.Path + "\\");
						listFiles.Clear();
						sortedMusics = new List<MusicListItem>(importTMDList.sortedMusics);
					}
					lockImport = false;
				};
				od.Show();
			}
			
			if(tempStr != searchText)
			{
				if(searchText.Contains("\n"))
				{
					searchText = searchText.Replace("\n","");
				}
				else
				{
					importTMDList.SortMusicsByTitleUsingText(searchText);
					sortedMusics = new List<MusicListItem>(importTMDList.sortedMusics);
					foreach(string s in strs)
					{
						for(int i = 0; i < sortedMusics.Count; i++)
						{
							if(Path.GetFileName(sortedMusics[i].FilePath) == Path.GetFileName(s))
							{
								sortedMusics.RemoveAt(i);
								break;
							}
						}
					}
					if(searchText != "")
						musicSelectorSliderPosition = 0f;
				}
			}
			
			scrollViewMusicsTotalHeight = sortedMusics.Count * 32f;
			if(scrollViewMusicsTotalHeight > musicListRect.height)
			{
				if(GUI.Button(new Rect(musicListRect.x + musicListRect.width + 3f, musicListRect.y -5f, 25, 25), "", arrowButtonUp))
					ArrowButtonUp();
				
				musicSelectorSliderPosition = GUI.VerticalSlider(new Rect(musicListRect.x + musicListRect.width + 10f, musicListRect.y + 20f, 20, musicListRect.height - 30f)
					,musicSelectorSliderPosition , 0f, scrollViewMusicsTotalHeight - musicListRect.height);
		
				if(GUI.Button(new Rect(musicListRect.x + musicListRect.width + 3f,musicListRect.y + musicListRect.height -10f,25,25), "",arrowButtonDown))
					ArrowButtonDown();
			}
			
			GUI.BeginGroup(musicListRect);
			
			float buttonTop = 2f - musicSelectorSliderPosition;
			for(int i = 0; i < sortedMusics.Count; i++) 
			{
				if(listFiles.ContainsKey(i))
				{
					if(GUI.Button(new Rect(2f, buttonTop,musicListRect.width, 30f ), "", hideButton))
						listFiles.Remove(i);
					
					GUI.Label(new Rect(2f, buttonTop, musicListRect.width, 30f), sortedMusics[i].Title, itemSelected);
						
				}
				else
				{
					if(GUI.Button(new Rect(2f, buttonTop,musicListRect.width, 30f ), "", hideButton))
						listFiles.Add(i, sortedMusics[i]);
					
					GUI.Label(new Rect(2f, buttonTop, musicListRect.width, 30f), sortedMusics[i].Title, item);
				}
				buttonTop += 32f;
			}
#endif
			GUI.EndGroup();
		}
		else
		{
			if(tempStr != searchText)
			{
				if(searchText.Contains("\n"))
				{
					searchText = searchText.Replace("\n","");
				}
				else
				{
					SortWebListByText(searchText);
					if(searchText != "")
						musicSelectorSliderPosition = 0f;
				}
			}
			
			scrollViewMusicsTotalHeight = webList.Count * 32f;
			if(scrollViewMusicsTotalHeight > musicListRect.height)
			{
				if(GUI.Button(new Rect(musicListRect.x + musicListRect.width + 3f, musicListRect.y -5f, 25, 25), "", arrowButtonUp))
					ArrowButtonUp();
				
				musicSelectorSliderPosition = GUI.VerticalSlider(new Rect(musicListRect.x + musicListRect.width + 10f, musicListRect.y + 20f, 20, musicListRect.height - 30f)
					,musicSelectorSliderPosition , 0f, scrollViewMusicsTotalHeight - musicListRect.height);
		
				if(GUI.Button(new Rect(musicListRect.x + musicListRect.width + 3f,musicListRect.y + musicListRect.height -10f,25,25),"",arrowButtonDown))
					ArrowButtonDown();
			}
			
			GUI.BeginGroup(musicListRect);
			
			float buttonTop = 2f - musicSelectorSliderPosition;
			for(int i = 0; i < webListToShow.Count; i++) 
			{
				if(webListToImport.Contains(webListToShow[i]))
				{
					if(GUI.Button(new Rect(2f, buttonTop, musicListRect.width, 30f ), "", hideButton))
						webListToImport.Remove (webListToShow[i]);
					
					GUI.Label(new Rect(2f, buttonTop, musicListRect.width, 30f), webListToShow[i][1], itemSelected);
				}
				else
				{
					if(GUI.Button(new Rect(2f, buttonTop,musicListRect.width, 30f ), "", hideButton))
						webListToImport.Add (webListToShow[i]);
					
					GUI.Label(new Rect(2f, buttonTop, musicListRect.width, 30f), webListToShow[i][1], item);
				}
				buttonTop += 32f;
			}
			GUI.EndGroup();
			
			
		}
		
		if(GUI.Button (new Rect(dialogRect.x + dialogRect.width - 150f, dialogRect.y + dialogRect.height - 55f, 100f, 40f), "", importButton))
		{
			if(!webImport)
			{
				foreach(KeyValuePair<int,MusicListItem> kv in listFiles)
					if(!strs.Contains(Path.GetFileName(kv.Value.FilePath)))
						strs.Add(kv.Value.FilePath);
				
				File.WriteAllLines(old_list_path, strs.ToArray());
				
				lockImport = true;
			}
			Close();
		}
		if(GUI.Button(new Rect(dialogRect.x + 40f,dialogRect.y + dialogRect.height - 55f, 100f, 40f), "", closeButton))
		{
			if(!webImport)
				listFiles.Clear();
			else
				webListToImport.Clear();
			Close();
		}
		
		GUI.Label(new Rect(dialogRect.x + 140f, dialogRect.y + dialogRect.height - 55f,dialogRect.width - 290f, 40), (webImport) ? "www.tomplay.com.br/musics/" : currentPath, pathShower);
		
		if(lockImport)
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height),blackBackground);
			GUI.Label(new Rect(Screen.width/2f - 60f, Screen.height/2f - 20f, 120f, 20f), "Importando...",labelStyle);
			GUI.DrawTexture(new Rect(Screen.width/2f - 50f, Screen.height/2f, 100f, 20f), emptyBar);
			if(!webImport)
			{
				GUI.DrawTexture(new Rect(Screen.width/2f - 50f, Screen.height/2f, (100f/listFiles.Count) * totalImportedMusics, 20f), bar);
				if(totalImportedMusics >= listFiles.Count)
					Destroy (this.gameObject);
			}
			else
			{
				GUI.DrawTexture(new Rect(Screen.width/2f - 50f, Screen.height/2f, (100f/webListToImport.Count) * totalImportedMusics, 20f), bar);
				if(totalImportedMusics >= webListToImport.Count)
				{
					Destroy (this.gameObject);
					EventHandler e = OnClose;
					if(e != null)
						e(this, new EventArgs());
				}
			}
		}
	}
	
	void Update()
	{
		if(lockImport)
			return;
		
		float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
	
		if(scrollWheel < 0f && musicSelectorSliderPosition < scrollViewMusicsTotalHeight - musicListRect.height)
			musicSelectorSliderPosition += 32f;
		else if(scrollWheel > 0f)
		{
			musicSelectorSliderPosition -= 32f;
			if(musicSelectorSliderPosition < 0f)
				musicSelectorSliderPosition = 0f;
		}
	}
	
	private void LoadTmds(string wResult)
	{
		if(!webImport)
		{
			listFiles = new Dictionary<int, MusicListItem>();
#if UNITY_ANDROID
			currentPath = "Conexão a internet não encontrada.";
#else
			importTMDList = new DataList.TMDList(Utils.Common.GetDownloadPath() + "\\");
			
			currentPath = Utils.Common.GetDownloadPath();
			
			old_list_path = currentPath + "\\" + OLD_LIST_FILE;
			
			sortedMusics = new List<MusicListItem>(importTMDList.sortedMusics);
			
			if(File.Exists(old_list_path))
			{
				strs.AddRange(File.ReadAllLines(old_list_path));
			}
			
			foreach(string s in strs)
			{
				for(int i = 0; i < sortedMusics.Count; i++)
				{
					if(Path.GetFileName(sortedMusics[i].FilePath) == Path.GetFileName(s))
					{
						sortedMusics.RemoveAt(i);
						break;
					}
				}
			}
#endif
		}
		else
		{
			string[] temp = wResult.Split('\n');
			foreach(string s in temp)
			{
				if(s != "")
					webList.Add(Regex.Split(s, ";@;"));
			}
			SortWebListByText("");
		}
	}
	
	public void ArrowButtonDown()
	{
		if(musicSelectorSliderPosition <= (float)scrollViewMusicsTotalHeight - musicListRect.height)
			musicSelectorSliderPosition += 32f;
		else
			musicSelectorSliderPosition = (float)scrollViewMusicsTotalHeight - musicListRect.height;
	}
	
	public void ArrowButtonUp()
	{
		if(musicSelectorSliderPosition > 32f)
			musicSelectorSliderPosition -= 32f;
		else
			musicSelectorSliderPosition = 0;
	}
	
	private void SortWebListByText(string text)
	{
		webListToShow.Clear();
		string[][] strArray = webList.ToArray();
		System.Array.Sort(strArray, delegate (string[] e1, string[] e2)
		{
			return e1[1].CompareTo(e2[1]);
		});
		webListToShow.AddRange(strArray);
		if(text != "")
		{
			List<string[]> final = new List<string[]>();
			foreach(string[] s in webListToShow)
			{
				if(0 == s[1].ToUpper().IndexOf(text.ToUpper()))
					final.Add(s);
			}
			webListToShow.Clear();
			webListToShow.AddRange(final);
		}
	}
	
	private void Close()
	{
		if(!webImport)
		{
			if(listFiles.Count == 0)
			{
				EventHandler e = OnClose;
				if(e != null)
					e(this, new EventArgs());
				Destroy(gameObject);
			}
			else
			{
				EventHandler e = OnImport;
				if(e != null)
					e(this, new EventArgs());
			}
		}
		else
		{
			if(webList.Count > 0)
			{
				lockImport = true;
				foreach(string[] s in webListToImport)
				{
					for(int i = 0; i < webList.Count; i++)
					{
						if(webList[i][0] == s[0])
							StartCoroutine (DownloadTmdFile (s[0]));
					}
				}
			}
			else
			{
				EventHandler e = OnClose;
				if(e != null)
					e(this, new EventArgs());
				Destroy(gameObject);
			}
		}
	}
		
	IEnumerator TryWebImport()
	{
		WWW w = new WWW(URL_LIST_MUSIC);
		yield return w;
		if(w.error != null)
		{
			webImport = false;
			LoadTmds(w.error);
#if UNITY_ANDROID
			title = "CONECTE A INTERNET";
#else
			title = "MÚSICAS NO COMPUTADOR";
#endif
		}
		else
		{
			title = "MÚSICAS NA INTERNET";
			webImport = true;
			LoadTmds(w.text);
		}
	}
	
	
	
	IEnumerator DownloadTmdFile(string tmdToDownload)
	{
		WWWForm form = new WWWForm();
		
		form.AddField("id", tmdToDownload);
		WWW w = new WWW (URL_GET_MUSIC, form);
		yield return w;
		if(w.error != null)
		{
			Debug.Log (w.error);
		}
		else
		{
			string destFile = Common.MUSICS_DIR + (new TimeSpan(DateTime.Now.Ticks)).TotalMilliseconds + ".tmd";
			File.WriteAllText (destFile, w.text);
			totalImportedMusics++;
		}
	}
}
