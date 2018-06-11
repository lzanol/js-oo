using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DataSet;

public class BandNetworkController : MonoBehaviour
{
	public static List<int> tracksToMute;
	public List<TrackSimpleInfo> freeTracks;
	public Dictionary<NetworkPlayer, ClientInfo> clientsDictionary;
	
	public static BandNetworkController CurrentInstance { get; set; }
	public bool IsSelectedGameType { get; set; }
	public bool IsTrackSelected { get; set; }
	public bool IsSelectingNotesTransposition { get; set; }
	public bool IsSelectingSymbols { get; set; }
	public static string CurrentPlayerName { get; set; }
	public string CurrentSceneToLoad { get; set; }
	
	public bool play = false;
	
	private float synchronizingTime = 0f;
	
	private float playTime;
	
	private void Awake()
	{
		tracksToMute = new List<int>();
		CurrentInstance = this;
		CurrentSceneToLoad = Utils.Common.SCENE_GUITAR;
		IsSelectedGameType = false;
		IsTrackSelected = false;
		IsSelectingNotesTransposition = false;
		freeTracks = new List<TrackSimpleInfo>();
		clientsDictionary = new Dictionary<NetworkPlayer, ClientInfo>();
	}
	
	private void OnLevelWasLoaded(int level)
	{
		if(Application.loadedLevelName == Utils.Common.SCENE_MENU)
		{
			Network.Disconnect();
			Destroy(gameObject);
		}
	}
	
	private void OnGUI()
	{
		if(!play)
			return;
		GUI.depth = 10;
		GUI.skin.label.fontSize = 62;
		GUI.Label(new Rect( 100, Screen.height/2 - 60, 100, 100),"" + (int)(playTime - Time.time + 1f));
	}
	
	private void Update()
	{
		if(play)
		{
			if(Time.time >= playTime)
			{
				if(!SoundEngine.CurrentInstance.IsPlaying)
					Game.Instance.Play();
				play = false;
				synchronizingTime = Time.time + 1.5f;
			}
		}
	}
	
	public void StartServer()
	{
		CurrentSceneToLoad = Menu.nextLevelName;
		
		for(int i = 0; i < Menu.musicSelected.Instruments.Length; i++)
		{
			if(Menu.musicSelected.Instruments[i].playable != DataSet.DevicePlayType.NONE)
			{
				TrackSimpleInfo tsi = new TrackSimpleInfo();
				tsi.Id = i;
				tsi.Hash = SoundEngine.CurrentInstance.GetSampleSet(Menu.musicSelected.Instruments[i].hash).Label + " - " + Menu.DEVICE_NAMES[((int)Menu.musicSelected.Instruments[i].playable) - 1];
				freeTracks.Add(tsi);
			}
		}
		Network.InitializeServer(32, 10050, false);
	}
	
	
	public void StartBand()
	{
		networkView.RPC("ReceiveStartBand", RPCMode.Others);
		DontDestroyOnLoad(gameObject);
		Application.LoadLevel(CurrentSceneToLoad);
	}
	
	public void GameOverClients()
	{
		networkView.RPC("ReceiveGameOver", RPCMode.Others);
	}
	
	public void Disconnect()
	{
		Network.Disconnect();
		Destroy(gameObject);
	}
	
	private int playerConnectedCount = 0;
	
	private void OnPlayerConnected(NetworkPlayer np)
	{
		playerConnectedCount ++;
		string s = "";
		foreach(TrackSimpleInfo tsi in freeTracks)
			s += tsi.Id.ToString() + "@" + tsi.Hash + "@";
		networkView.RPC("ReceiveFreeTracks", np, s);
		clientsDictionary.Add(np, new ClientInfo("Cliente " + playerConnectedCount));
	}
	
	private void OnConnectedToServer()
	{
		networkView.RPC ("ReceivePlayerName", RPCMode.Server, CurrentPlayerName, Network.player);
	}
	
	private void OnPlayerDisconnected(NetworkPlayer np)
	{
		clientsDictionary.Remove(np);
	}
		
	public void Replay()
	{
		networkView.RPC ("ReceivePlayTogether", RPCMode.Others);
		playTime = Time.time + 3f;
		play = true;
	}
	
	public void SelectTrack(int trackSelected)
	{
		Menu.trackSelected = trackSelected;
		networkView.RPC("ReceivePlayerSelectedTrack", RPCMode.Server, trackSelected, Network.player);
	}
	
	public void SelectGameType(int gameType)
	{
		networkView.RPC("ReceivePlayerGameType", RPCMode.Server, gameType, Network.player);
		IsSelectedGameType = true;
		switch(gameType)
		{
		case 0:
			CurrentSceneToLoad = Utils.Common.SCENE_GUITAR;
			networkView.RPC ("ReceivePlayerIsReady", RPCMode.Server, true, Network.player);
			SendReadyToPlay();
			break;
		case 1:
			CurrentSceneToLoad = Utils.Common.SCENE_PIANO;
			IsSelectingSymbols = true;
			SendReadyToPlay();
			break;
		case 2:
			CurrentSceneToLoad = Utils.Common.SCENE_REAL;
			IsSelectingNotesTransposition = true;
			break;
		}
	}
	
	public void SendReadyToPlay()
	{
		networkView.RPC ("ReceivePlayerIsReady", RPCMode.Server, true, Network.player);
	}	
	
	[RPC]
	private void ReceivePlayerIsReady(bool isReady, NetworkPlayer np)
	{
		clientsDictionary[np].IsReady = isReady;
	}
	
	[RPC]
	private void ReceivePlayerGameType(int gameType, NetworkPlayer np)
	{
		clientsDictionary[np].GameTypeSelected = gameType;
	}
	
	[RPC]
	private void ReceiveGameOver()
	{
		Game.Instance.GameOver();
	}
	
	[RPC]
	private void ReceivePlayTogether(NetworkMessageInfo nmi)
	{
		float timeDelay = (float)(Network.time - nmi.timestamp);
		playTime = Time.time + 3f - timeDelay;
		play = true;
		clientLastCurrentTime = 0f;
	}
	
	[RPC]
	private void ReceiveStartBand()
	{
		DontDestroyOnLoad(gameObject);
		Application.LoadLevel(CurrentSceneToLoad);
	}
	
	[RPC] 
	private void ReceivePlayerSelectedTrack(int trackSelected, NetworkPlayer playerWhoSent)
	{	
		if(Network.isServer)
		{
			MusicData md = new MusicData(Menu.musicSelected, false);
			md.instruments.Add(Menu.musicSelected.Instruments[trackSelected]);
			md.MusicDuration = Menu.musicSelected.GetMusicDuration();
			string newTMD = md.CreateXmlString(new int[0], false);
			List<string> splitTMD = new List<string>();
			int newTMDL = newTMD.Length;
			for(int i = 0; i < newTMDL; i+= 4094)
			{
				string sub = newTMD.Substring(i, ((newTMD.Length - i) > 4094) ? 4094 : (newTMD.Length - i));
				splitTMD.Add(sub);
			}
			
			foreach(string s in splitTMD)
			{
				networkView.RPC("ReceiveNewTMD", playerWhoSent, s);
			}
			
			networkView.RPC("ReceiveFinishNewTMD", playerWhoSent);
			
			tracksToMute.Add(trackSelected);
			
			clientsDictionary[playerWhoSent].TrackSelected = trackSelected;
		}
	}
	
	private string newTMD = "";
	
	[RPC]
	private void ReceivePlayerName(string playerName, NetworkPlayer np)
	{
		clientsDictionary[np].PlayerName = playerName;
	}
	
	[RPC]
	private void ReceiveNewTMD(string newTMD)
	{
		this.newTMD += newTMD;
	}
	
	[RPC]
	private void ReceiveFinishNewTMD()
	{
		MusicData newMD = new MusicData();
		newMD.LoadTMD(this.newTMD);
		Menu.musicSelected = newMD;
		Menu.trackSelected = 0;
		IsTrackSelected = true;
	}
	
	[RPC]
	private void ReceiveFreeTracks(string freeTracks)
	{
		string[] s = freeTracks.Split('@');
		for(int i = 0; i < s.Length - 1; i+=2)
		{
			TrackSimpleInfo tsi = new TrackSimpleInfo();
			tsi.Id = int.Parse(s[i]);
			tsi.Hash = s[i+1];
			this.freeTracks.Add(tsi);
		}
	}
	
	private float clientLastCurrentTime = 0f;
	
	private void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo nmi)
	{
		float currentTime = 0f;
		if(Network.isServer && SoundEngine.CurrentInstance.IsPlaying)
		{
			if(stream.isWriting)
			{
				currentTime = SoundEngine.CurrentInstance.GetTime();
				stream.Serialize(ref currentTime);
			}
		}
		else if(stream.isReading && Network.isClient)
		{
			stream.Serialize(ref currentTime);
			currentTime += (float)(Network.time - nmi.timestamp) + 0.1f;
			if(currentTime > clientLastCurrentTime)
			{
				clientLastCurrentTime = currentTime;
				if(Time.time < synchronizingTime)
					SoundEngine.CurrentInstance.SetTime(clientLastCurrentTime);
				else if(synchronizingTime != 0f)
					synchronizingTime = 0f;
			}
		}
	}
	
	private void OnDestroy()
	{
		Network.Disconnect();
	}
	
	public class TrackSimpleInfo
	{
		public int Id { get; set; }
		public string Hash { get; set; }
	}
	
	public class ClientInfo
	{
		public string PlayerName { get; set; }
		public int TrackSelected { get; set; }
		public int GameTypeSelected { get; set; }
		public bool IsReady { get; set; }
		
		public ClientInfo(string playerName) 
		{
			PlayerName = playerName;
			IsReady = false;
			TrackSelected = -1;
			GameTypeSelected = -1;
		}
	}
}
