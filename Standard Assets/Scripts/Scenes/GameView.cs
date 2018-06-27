using UnityEngine;
using System.Collections;

public class GameView : MonoBehaviour
{
	public enum HudStyle : byte
	{
		Standard,
		Left,
		Right
	}
	
	protected const int MARGIN = 20;
	
	const float LYRICS_TIME = 3f;
	const float AR_MAX = 16f/9f;
	const float AR_MIN = 1f;
	
	static GameView instance;
	
	public Texture2D logo;
	public Texture2D background;
	public Texture2D pauseTexture;
	public Texture2D playTexture;
	
	public Texture2D scrollBar;
	public Texture2D scrollLimiter;
	public GUIStyle scrollBoxStyle;
	public GUIStyle scrollThumbStyle;
	
	public GameMenu gameMenuTmpl;
	public MessageUI messageUITmpl;
	
	protected bool scoreEnabled = true;
	protected bool musicLoaded;
	protected DataSet.MusicData musicData;
	protected GameMenu gameMenu;
	
	Camera cam;
	protected GUIStyle style;
	GUIStyle scoreStyle;
	GUIStyle boldStyle;
	GUIStyle lyricsStyle;
	Rect titleRect;
	Rect timeRect;
	Rect pauseRect;
	Rect lyricsRect;
	Rect lyricsScrollRect;
	bool isPaused;
	Transform gameBg;
	Transform panel;
	float camInitZ;
	float gameBgX;
	float gameBgZ;
	float lyricsBeat;
	float lyricsBeatTotal;
	float lyricsHeight = 0f;
	MessageUI messageUI;
	
	// :: AUX ::
	int time;
	float ar;
	float musicTime;
	float musicTotalTime;
	float musicBeat;
	string lyrics = "";
	string trackName = "";
	string trackNumber = "";
	string title = "Título da Música";
	string totalTime = "";
	Vector2 scrollPos = Vector2.zero;
	
	float yFactor;
	float zFactor;
	Vector3 pos;
	
	int w;
	int h;
	
	public int LastLyricsIndex { get; set; }
	public bool LyricsEnabled { get; set; }
	public HudStyle Hud { get; set; }
	
	public bool IsMenuOpened
	{
		get { return gameMenu != null || !musicLoaded; }
	}
	
	public bool HasMessage
	{
		get { return messageUI != null; }
	}
	
	// :: STATIC ::
	public static GameView Instance
	{
		get { return instance; }
	}
	
	// :: AUTO ::
	protected virtual void Awake()
	{
		instance = this;
		
		LyricsEnabled = true;
		Hud = HudStyle.Standard;
		
		style = new GUIStyle();
		style.fontSize = 20;
		style.normal.textColor = Color.white;
		style.alignment = TextAnchor.MiddleLeft;
		
		scoreStyle = new GUIStyle(style);
		scoreStyle.fontSize = 24;
		scoreStyle.fontStyle = FontStyle.Bold;
		scoreStyle.alignment = TextAnchor.MiddleRight;
		
		boldStyle = new GUIStyle(style);
		boldStyle.fontStyle = FontStyle.Bold;
		
		lyricsStyle = new GUIStyle(style);
		lyricsStyle.alignment = TextAnchor.UpperLeft;
		lyricsStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
		lyricsStyle.wordWrap = true;
		//lyricsStyle.normal.background = background;
		
		titleRect = new Rect(MARGIN, 90, 200, 20);
		timeRect = new Rect(MARGIN, titleRect.y + titleRect.height, titleRect.width, titleRect.height);
		pauseRect = new Rect(0, 60, 50, 50);
		lyricsRect = new Rect(0, 0, 0, 40);
		lyricsScrollRect = new Rect(0, 0, 0, 40);
		
		ScrollPos = 0f;
		
		Reset();
		// (...) debug
		//musicLoaded = true;
		//ShowMenu();
	}
	
	protected virtual void Start()
	{
		cam = GetComponent<Camera>();
		camInitZ = cam.transform.position.z;
		gameBg = GameObject.Find("GameBg").transform;
		gameBgX = gameBg.localScale.x;
		gameBgZ = gameBg.localScale.z;
		panel = GameObject.Find("Panel").transform;
	}
	
	protected virtual void Update()
	{
		ar = cam.aspect;
		
		if (ar > AR_MAX)
			ar = AR_MAX;
		else if (ar < AR_MIN)
			ar = AR_MIN;
		
		gameBg.localScale = new Vector3(gameBgX*ar, 1, gameBgZ*ar);
		
		pos = cam.transform.position;
		pos.z = camInitZ/ar;
		cam.transform.position = pos;
		
		if (Input.GetKeyDown(KeyCode.PageDown))
			if ((int)Hud < System.Enum.GetNames(typeof(HudStyle)).Length - 1)
				Hud = (HudStyle)((int)Hud + 1);
		
		if (Input.GetKeyDown(KeyCode.PageUp))
			if ((int)Hud > 0)
				Hud = (HudStyle)((int)Hud - 1);
		
		if (Input.GetKeyDown(KeyCode.Insert))
			LyricsEnabled = !LyricsEnabled;
	}
	
	protected virtual void OnGUI()
	{
		if (musicData == null)
			return;
		
		GUI.depth = 2;
		GUI.enabled = Game.Instance.IsPlaying;
		
		musicTime = Game.Instance.SoundEngine.GetTime();
		musicBeat = musicData.TimeToBeats(musicTime);
		time = musicLoaded ? Mathf.FloorToInt(musicTime) : 0;
		
		switch (Hud)
		{
		case HudStyle.Standard:
			GUILayout.BeginHorizontal(GUILayout.Width(Screen.width - MARGIN));
			
			GUILayout.Box(logo, GUIStyle.none, GUILayout.Height(90));
			GUILayout.FlexibleSpace();
			
			if (scoreEnabled)
				GUILayout.Label(Score + "/" + GamePlay.Instance.TotalScore, scoreStyle, GUILayout.Height(80));
			
			GUILayout.Space(10);
			GUILayout.EndHorizontal();
			
			pauseRect.x = Screen.width - 80;
			
			if (GUI.enabled && GUI.Button(pauseRect, pauseTexture, GUIStyle.none))
				Game.Instance.Pause();
			
			GUI.Label(titleRect, title, boldStyle);
			GUI.Label(timeRect, (time/60).ToString().PadLeft(2, '0') + ":" +
				(time%60).ToString().PadLeft(2, '0') + " / " + totalTime, style);
			break;
			
		case HudStyle.Left:
			GUILayout.BeginHorizontal(GUILayout.Width(Screen.width - MARGIN));
			GUILayout.Space(10f);
			
			GUILayout.BeginVertical(GUILayout.Height(Screen.height - MARGIN));
			
			GUILayout.Box(logo, GUIStyle.none, GUILayout.Width(200));
			
			if (scoreEnabled)
				GUILayout.Label(Score + "/" + GamePlay.Instance.TotalScore, scoreStyle, GUILayout.Height(80));
			
			GUILayout.Label(title, boldStyle);
			GUILayout.Label((time/60).ToString().PadLeft(2, '0') + ":" +
				(time%60).ToString().PadLeft(2, '0') + " / " + totalTime, style);
			
			if (GUI.enabled && GUILayout.Button(pauseTexture, GUIStyle.none, GUILayout.Width(pauseRect.width)))
				Game.Instance.Pause();
			
			GUILayout.EndVertical();
			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			break;
			
		case HudStyle.Right:
			GUILayout.BeginHorizontal(GUILayout.Width(Screen.width - MARGIN));
			GUILayout.FlexibleSpace();
			
			GUILayout.BeginVertical(GUILayout.Height(Screen.height - MARGIN));
			
			GUILayout.Box(logo, GUIStyle.none, GUILayout.Width(200));
			
			if (scoreEnabled)
				GUILayout.Label(Score + "/" + GamePlay.Instance.TotalScore, scoreStyle, GUILayout.Height(80));
			
			GUILayout.Label(title, boldStyle);
			GUILayout.Label((time/60).ToString().PadLeft(2, '0') + ":" +
				(time%60).ToString().PadLeft(2, '0') + " / " + totalTime, style);
			
			if (GUI.enabled && GUILayout.Button(pauseTexture, GUIStyle.none, GUILayout.Width(pauseRect.width)))
				Game.Instance.Pause();
			
			GUILayout.EndVertical();
			
			GUILayout.Space(10);
			GUILayout.EndHorizontal();
			break;
		}
		
		if (LyricsEnabled)
		{
			lyrics = "";
			
			if (LastLyricsIndex < musicData.lyrics.Count)
				lyrics = musicData.lyrics[LastLyricsIndex];
			
			/*while (LastLyricsIndex < musicData.markers.Count &&
				LastLyricsIndex < musicData.lyrics.Count)
			{
				lyricsBeat = musicBeat - musicData.markers[LastLyricsIndex];
				
				if (lyricsBeat >= 0)
				{
					if (lyricsBeat < lyricsBeatTotal)
					{
						lyrics = musicData.lyrics[LastLyricsIndex];
						break;
					}
					else ++LastLyricsIndex;
				}
				break;
			}*/
			
			if (lyrics != "")
			{
				/*lyricsRect.x = MARGIN;
				lyricsRect.y = timeRect.y + timeRect.height + MARGIN;
				lyricsRect.width = Screen.width/2;
				lyricsRect.height = ly;
				lyricsStyle.fontSize = Mathf.RoundToInt(lyricsRect.height*0.05f);*/
				
				/*GUI.BeginScrollView(lyricsRect, new Vector2(0, 0), lyricsScrollRect, false, true, GUIStyle.none, GUIStyle.none);
				GUI.Label(lyricsRect, lyrics, lyricsStyle);
				GUI.EndScrollView();*/
				
				lyricsScrollRect.x = MARGIN;
				lyricsScrollRect.y = timeRect.y + timeRect.height + MARGIN;
				lyricsScrollRect.width = Screen.width/2;
				lyricsScrollRect.height = Screen.height - lyricsScrollRect.y - MARGIN;
				
				lyricsStyle.fontSize = Mathf.RoundToInt(lyricsScrollRect.height*0.05f);
				
				scrollPos.y = musicTime/musicTotalTime*(lyricsHeight > lyricsScrollRect.height ? lyricsHeight - lyricsScrollRect.height : 0);
				
				GUILayout.BeginArea(lyricsScrollRect);
				GUILayout.BeginScrollView(scrollPos, false, false, GUIStyle.none, GUIStyle.none);
				GUILayout.Label(lyrics, lyricsStyle);
				
				// Só há como verificar as medidas visuais ao pintar a tela.
				if (Event.current.type == EventType.Repaint)
					lyricsHeight = GUILayoutUtility.GetLastRect().height;
				
				GUILayout.EndScrollView();
				GUILayout.EndArea();
			}
		}
		
		/*h = Screen.height/2;
		scrollBoxStyle.fixedWidth = scrollBoxStyle.normal.background.width*h/scrollBoxStyle.normal.background.height;
		w = (int)(scrollBoxStyle.fixedWidth + 10);
		ScrollPos = GUI.VerticalSlider(new Rect(Screen.width - w + 4, (Screen.height - h)/2, w, h), ScrollPos, 0f, 1f, scrollBoxStyle, scrollThumbStyle);*/
		
		if (messageUI != null && gameMenu != null && gameMenu.SubmenuOpened)
			HideMessage();
	}
	
	// :: CUSTOM ::
	public string Score { get; set; }
	public float ScrollPos { get; set; }
	
	public virtual void OpenMusic()
	{
		musicData = Game.Instance.MusicData;
		
		lyricsBeatTotal = musicData.TimeToBeats(LYRICS_TIME);
		musicTotalTime = musicData.GetMusicDurationTime();
		
		trackName = Game.Instance.SoundEngine.GetSampleSet(Game.Instance.CurrentTrack.hash).Label;
		trackNumber = (Game.Instance.CurrentTrackIndex + 1).ToString();
		title = musicData.title;
		
		time = Mathf.FloorToInt(musicTotalTime);
		
		totalTime = (time/60).ToString().PadLeft(2, '0') + ":" +
			(time%60).ToString().PadLeft(2, '0');
		
		musicLoaded = true;
	}
	
	public virtual void Play()
	{
		HideMenu();
		HideMessage();
	}
	
	public virtual void Pause()
	{
		ShowMenu();
	}
	
	public virtual void Reset()
	{
		Score = "0";
		LastLyricsIndex = 0;
		musicLoaded = false;
		
		HideMenu();
		HideMessage();
	}
	
	protected void ShowMenu()
	{
		if (!IsMenuOpened)
			gameMenu = (GameMenu)Instantiate(gameMenuTmpl);
	}
	
	protected void HideMenu()
	{
		if (gameMenu != null)
		{
			Destroy(gameMenu.gameObject);
			gameMenu = null;
		}
	}
	
	protected void ShowMessage(string msg)
	{
		if (!Network.isClient && (gameMenu == null || !gameMenu.SubmenuOpened))
		{
			if (messageUI == null)
				messageUI = (MessageUI)Instantiate(messageUITmpl);
			
			messageUI.Text = msg;
		}
	}
	
	protected void HideMessage()
	{
		if (messageUI != null)
		{
			Destroy(messageUI.gameObject);
			messageUI = null;
		}
	}
}
