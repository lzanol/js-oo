using UnityEngine;
using System;
#if !UNITY_ANDROID
using System.Net;
using System.Net.Sockets;
#endif
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using DataSet;

public class BandDialog : MonoBehaviour 
{
	private enum ReceivedMessageType : byte
	{
		ServerMessage = 15,
	}
	
	private const float TOTAL_WIDTH = 640f;
	private const float TOTAL_HEIGHT = 480f;
	
	public event EventHandler OnClose;
	
	private UnityEngine.Object BandNetworkControllerTemplate;
	private BandNetworkController bandNetworkController;
	
	//private string[] transposeLabels = {"-2","-1,5","-1","-0,5","0","+0,5","+1","+1,5","+2"};
	
	private Rect dialogRect;

	private NetworkConnectionError networkConnectionError = NetworkConnectionError.NoError;
#if !UNITY_ANDROID	
	private Socket listenerSocket;
#endif
	private Thread listenerThread;
	private bool isListening = false;
	private bool isPlayerCreatingName = false;
	private Dictionary<string, ServerInfo> serverDictionary;
	
#if !UNITY_ANDROID
	private Socket broadcastSocket;
#endif
	private Thread broadcastThread;
	private bool isCreatingBand = false;
	private bool isHost = false;
	private string bandName = "";
	private ServerInfo currentServerInfo;
	
	private string[] gameTypeStr = {"Guitarra", "Piano", "Real"};
	public GUIStyle[] gameTypeButtons;
	
	public GUIStyle buttonChangeNick;
	public GUIStyle buttonOk;
	public GUIStyle buttonCancel;
	public GUIStyle buttonEntry;
	public GUIStyle buttonStart;
	public GUIStyle buttonNotes;
	public GUIStyle buttonSymbols;
	public GUIStyle whiteBoad;
	public GUIStyle labelItem;
	public GUIStyle labelItemSelected;
	public GUIStyle labelItemTitle;
	public GUIStyle labelTitle;
	public GUIStyle buttonEmpty;
	public GUIStyle labelNoteTransposition;
	private Rect whiteBoardRect;
	
	//server dialogs
	public GUIStyle textBox;
	public Rect creatingBand;
	public GUIStyle creatingBandBackground;
	public Rect startingBand;
	public GUIStyle startingBandbackground;
	public Texture2D waitingClient;
	public Texture2D readyClient;
	
	//client dialogs
	public Texture2D menuBackground;
	public Rect creatingPlayerName;
	public GUIStyle creatingPlayerNameBackground;
	public Rect selectingBand;
	public GUIStyle selectingBandBackground;
	private string bandSelected = "";
	public Rect selectingTrack;
	public GUIStyle selectingTrackBackground;
	private int trackSelected = -1;
	public Rect selectingGameType;
	public GUIStyle selectingGameTypeBackground;
	public Rect selectingSymbols;
	public GUIStyle selectingSymbolsBackground;
	public Rect selectingNotesTransposition;
	public GUIStyle selectingNotesTranspositionBackground;
	public Rect waitingServer;
	public GUIStyle waitingServerBackground;
	public GUIStyle[] arrows;
	
	public GUIStyle blackBackground;
	
	private void Awake()
	{
		BandNetworkControllerTemplate = Resources.Load("Prefabs/Utils/BandNetworkController");
		bandNetworkController = (Instantiate(BandNetworkControllerTemplate) as GameObject).GetComponent<BandNetworkController>();
		if(Menu.IS_CLIENT)
		{
			if(BandNetworkController.CurrentPlayerName == null)
			{
				BandNetworkController.CurrentPlayerName = "";
				isPlayerCreatingName = true;
			}
			else
				StartListener();
		}
		else
			isCreatingBand = true;

		serverDictionary = new Dictionary<string, ServerInfo>();
	}
	
	private void OnGUI()
	{
		GUI.depth = 0;
		dialogRect = new Rect(Screen.width/2f - TOTAL_WIDTH/2f, Screen.height/2f - TOTAL_HEIGHT/2f , TOTAL_WIDTH, TOTAL_HEIGHT);
		
		if(isPlayerCreatingName)
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), menuBackground);
			GUI.skin.settings.cursorColor = Color.black;
			dialogRect = new Rect(Screen.width/2 - creatingPlayerName.width/2, Screen.height/2 - creatingPlayerName.height/2, creatingPlayerName.width, creatingPlayerName.height);
			GUI.Box(dialogRect,"\nDigite seu nome", creatingPlayerNameBackground);
			GUI.SetNextControlName("PlayerName");
			BandNetworkController.CurrentPlayerName = GUI.TextField(new Rect(dialogRect.x + 40f, dialogRect.y + 70f, dialogRect.width - 90f, 30f), BandNetworkController.CurrentPlayerName, 9, textBox);
			GUI.FocusControl("PlayerName");
			BandNetworkController.CurrentPlayerName = BandNetworkController.CurrentPlayerName.Replace("\n", "");
			
			if(BandNetworkController.CurrentPlayerName != "" && BandNetworkController.CurrentPlayerName[0] != ' ' && GUI.Button(new Rect(dialogRect.xMax - 130f, dialogRect.yMax - 70f, 100f, 50f ), "", buttonOk))
			{
				StartListener();
				isPlayerCreatingName = false;
			}
		}
		else if(isListening)
		{
			List<string> serverToRemove = new List<string>();
			GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), menuBackground);
			dialogRect = new Rect(Screen.width/2f - selectingBand.width/2f, Screen.height/2f - selectingBand.height/2f , selectingBand.width, selectingBand.height);
			GUI.Box(dialogRect, "", selectingBandBackground);
			
			GUI.Label(new Rect(dialogRect.xMax - 220f, dialogRect.y + 20f, 80f, 30f), BandNetworkController.CurrentPlayerName, labelTitle);
			if(GUI.Button(new Rect(dialogRect.xMax - 110f, dialogRect.y + 10f,100f, 50f), "", buttonChangeNick))
			{
				BandNetworkController.CurrentPlayerName = null;
				Close();
				Application.LoadLevel(Utils.Common.SCENE_MENU);
			}
			whiteBoardRect = new Rect(dialogRect.x + 40f, dialogRect.y + 50f, dialogRect.width - 90f, dialogRect.height - 130f);
			GUI.Box(whiteBoardRect, "", whiteBoad);
			int count = 0;
			foreach(KeyValuePair<string, ServerInfo> kv in serverDictionary)
			{
				if(DateTime.Compare(kv.Value.LastTimeReceived, DateTime.Now) > 0)
				{
					if(kv.Key == bandSelected)
					{
						GUI.Label(new Rect(whiteBoardRect.x + 5f, whiteBoardRect.y + 10f + (count*30f), 100f, 25f), kv.Key, labelItemSelected);
						GUI.Label(new Rect(whiteBoardRect.x + 110f, whiteBoardRect.y + 10f + (count*30f), whiteBoardRect.width - 115f, 25f), kv.Value.MusicName, labelItemSelected);
					}
					else
					{
						if(GUI.Button(new Rect(whiteBoardRect.x + 5f, whiteBoardRect.y + 10f + (count*30f), whiteBoardRect.width -10f, 25f), "", buttonEmpty))
							bandSelected = kv.Key;
						GUI.Label(new Rect(whiteBoardRect.x + 5f, whiteBoardRect.y + 10f + (count*30f), 100f, 25f), kv.Key, labelItem);
						GUI.Label(new Rect(whiteBoardRect.x + 110f, whiteBoardRect.y + 10f + (count*30f), whiteBoardRect.width - 115f, 25f), kv.Value.MusicName, labelItem);
					}
					
					count++;
				}
				else
					serverToRemove.Add(kv.Key);
			}
			
			foreach(string s in serverToRemove)
				serverDictionary.Remove(s);
			
			if(serverDictionary.ContainsKey(bandSelected))
			{
				if(GUI.Button(new Rect(dialogRect.xMax - 140f, dialogRect.yMax - 70f, 100f,50f), "", buttonEntry))
				{
#if !UNITY_ANDROID
					networkConnectionError = Network.Connect(serverDictionary[bandSelected].EP.ToString().Split(':')[0], 10050);
#endif
					StopListener();
				}
			}
		}
		else if(isHost)
		{
			dialogRect = new Rect( Screen.width/2 - startingBand.width/2, Screen.height/2 - startingBand.height/2, startingBand.width, startingBand.height);
			GUI.Box(dialogRect, "", startingBandbackground);
			GUI.Label(new Rect(dialogRect.x + 40f, dialogRect.y + 25f, 200f, 40f), "AGUARDANDO INTEGRANTES DA BANDA", labelTitle);
			whiteBoardRect = new Rect ( dialogRect.x + 40f,dialogRect.y + 50f, dialogRect.width - 90f, dialogRect.height - 130f);
			GUI.Box(whiteBoardRect, "", whiteBoad);
			int countReady = 0;
			if(bandNetworkController.clientsDictionary.Count > 0)
			{
				int count = 0;
				foreach(KeyValuePair<NetworkPlayer, BandNetworkController.ClientInfo> kv in bandNetworkController.clientsDictionary)
				{
					if(kv.Value.IsReady)
						countReady ++;
					
					GUI.Label(new Rect(whiteBoardRect.x + 5f, whiteBoardRect.y + 10f + (count*30f), 70f, 25f), kv.Value.PlayerName ,labelItem );
					GUI.Label(new Rect(whiteBoardRect.x + 80f, whiteBoardRect.y + 10f + (count*30f), 140f, 25f), 
						((kv.Value.TrackSelected == -1) ? "" : SoundEngine.CurrentInstance.GetSampleSet(Menu.musicSelected.Instruments[kv.Value.TrackSelected].hash).Label) ,labelItem );
					GUI.Label(new Rect(whiteBoardRect.x + 225f, whiteBoardRect.y + 10f + (count*30f), 100f, 25f), 
						((kv.Value.GameTypeSelected == -1) ? "" : gameTypeStr[kv.Value.GameTypeSelected]) ,labelItem );
					GUI.DrawTexture(new Rect(whiteBoardRect.x + 330f, whiteBoardRect.y + 10f + (count*30f), whiteBoardRect.width -330f, 25f), 
						((kv.Value.IsReady) ? readyClient : waitingClient ));
					
					count ++;
				}
			}
				
			if(bandNetworkController.clientsDictionary.Count == countReady)
			{
				if(GUI.Button(new Rect(dialogRect.x + 25f, dialogRect.yMax - 75f, 60f, 60f), "", buttonStart))
				{
					StopBroadcast();
					bandNetworkController.StartBand();
				}
			}
			
			GUI.Label(new Rect(dialogRect.x + 160f, dialogRect.yMax - 60f, 100f, 60f), Network.player.ipAddress , labelTitle);
			
			if(GUI.Button(new Rect(dialogRect.xMax - 130f, dialogRect.yMax - 65f, 100f, 50f), "", buttonCancel))
			{
				StopBroadcast();
				Network.Disconnect();
				Close();
			}
		}
		else if(isCreatingBand)
		{
			GUI.skin.settings.cursorColor = Color.black;
			dialogRect = new Rect(Screen.width/2 - creatingBand.width/2, Screen.height/2 - creatingBand.height/2, creatingBand.width, creatingBand.height);
			GUI.Box(dialogRect,"",creatingBandBackground);
			GUI.SetNextControlName("BandName");
			bandName = GUI.TextField(new Rect(dialogRect.x + 40f, dialogRect.y + 70f, dialogRect.width - 90f, 30f), bandName, 12, textBox);
			GUI.FocusControl("BandName");
			bandName = bandName.Replace("\n","");
			
			if(bandName != "" && bandName[0] != ' ' && GUI.Button(new Rect(dialogRect.xMax - 240f, dialogRect.yMax - 70f, 100, 50f ), "", buttonOk))
			{
				bandNetworkController.StartServer();
				isHost = true;
				currentServerInfo = new ServerInfo();
				int tracksNumber = 0;
				
				foreach(InstrumentData id in Menu.musicSelected.Instruments)
					if(id.playable != DevicePlayType.NONE)
						tracksNumber++;

				currentServerInfo.Update(Menu.musicSelected.title, 1, tracksNumber);

				StartBroadcast();
				isCreatingBand = false;
			}
			
			if(GUI.Button(new Rect(dialogRect.xMax - 130f, dialogRect.yMax - 70f, 100f, 50f ), "", buttonCancel))
				Close();
		}
		else if(Network.isClient)
		{
			GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), menuBackground);
			if(!bandNetworkController.IsTrackSelected)
			{
				dialogRect = new Rect(Screen.width/2f - selectingTrack.width/2f, Screen.height/2f - selectingTrack.height/2f , selectingTrack.width, selectingTrack.height);
				GUI.Box(dialogRect, "", selectingTrackBackground);
				whiteBoardRect = new Rect( dialogRect.x + 70f,dialogRect.y + 70f, dialogRect.width - 150f, dialogRect.height - 160f);
				GUI.Box(whiteBoardRect, "", whiteBoad);
				
				for(int i = 0; i < bandNetworkController.freeTracks.Count; i++)
				{
					if(i == trackSelected)
						GUI.Label(new Rect(whiteBoardRect.x + 5f, whiteBoardRect.y + 10f + (i*30f), whiteBoardRect.width -10f, 25f), bandNetworkController.freeTracks[i].Hash, labelItemSelected);
					else
						GUI.Label(new Rect(whiteBoardRect.x + 5f, whiteBoardRect.y + 10f + (i*30f), whiteBoardRect.width -10f, 25f), bandNetworkController.freeTracks[i].Hash, labelItem);
						
					if(GUI.Button(new Rect(whiteBoardRect.x + 5f, whiteBoardRect.y + 10f + (i*30f), whiteBoardRect.width -10f, 25f), "", buttonEmpty))
						trackSelected = i;
				}
				
				if(trackSelected != -1 && GUI.Button(new Rect(dialogRect.x + 30f, dialogRect.yMax - 90f, 100f,50f), "", buttonOk))
					bandNetworkController.SelectTrack(bandNetworkController.freeTracks[trackSelected].Id);
				
				if(GUI.Button (new Rect(dialogRect.xMax - 140f, dialogRect.yMax - 90f, 100f, 50f), "", buttonCancel))
				{
					Destroy(bandNetworkController.gameObject);
					Application.LoadLevel(Utils.Common.SCENE_MENU);
				}
			}
			else if(!bandNetworkController.IsSelectedGameType)
			{
				dialogRect = new Rect(Screen.width/2f - selectingGameType.width/2f, Screen.height/2f - selectingGameType.height/2f , selectingGameType.width, selectingGameType.height);
				GUI.Box(dialogRect, "", selectingGameTypeBackground);
		
				for(int i = 0; i < gameTypeStr.Length; i++)
					if(GUI.Button(new Rect(dialogRect.x + 40f + (135f*i), dialogRect.y + 80f , 135f , 80f), "",  gameTypeButtons[i] ))
						bandNetworkController.SelectGameType(i);
				
				if(GUI.Button (new Rect(dialogRect.x + dialogRect.width/2f - 50f , dialogRect.y + 160f, 100f, 50f), "", buttonCancel))
				{
					Destroy(bandNetworkController.gameObject);
					Application.LoadLevel(Utils.Common.SCENE_MENU);
				}
				
			}
			else if(bandNetworkController.IsSelectingSymbols)
			{
				dialogRect = new Rect(Screen.width/2f - selectingSymbols.width/2f,
					Screen.height/2f - selectingSymbols.height/2f,
					selectingSymbols.width, selectingSymbols.height);
				
				GUI.Box(dialogRect, "\nSELECIONE", selectingSymbolsBackground);
				
				if(GUI.Button(new Rect(dialogRect.x + 50f, dialogRect.y + 55f, 120, 60f), "", buttonNotes))
				{
					Menu.useSymbols = false;
					bandNetworkController.IsSelectingSymbols = false;
					bandNetworkController.SendReadyToPlay();
				}
				if(GUI.Button(new Rect(dialogRect.x + 190f, dialogRect.y + 55f, 120, 60f), "", buttonSymbols))
				{
					Menu.useSymbols = true;
					bandNetworkController.IsSelectingSymbols = false;
					bandNetworkController.SendReadyToPlay();
				}
				
				if(GUI.Button (new Rect(dialogRect.x + dialogRect.width/2f - 50f , dialogRect.y + 115f, 100f, 50f), "", buttonCancel))
				{
					Destroy(bandNetworkController.gameObject);
					Application.LoadLevel(Utils.Common.SCENE_MENU);
				}
				
			}
			else if(bandNetworkController.IsSelectingNotesTransposition)
			{
				dialogRect = new Rect(Screen.width/2f - selectingNotesTransposition.width/2f,
					Screen.height/2f - selectingNotesTransposition.height/2f,
					selectingNotesTransposition.width,
					selectingNotesTransposition.height);
				GUI.Box(dialogRect, "\nTRANSPOSIÇÃO DE NOTAS", selectingNotesTranspositionBackground);
				
				if(Menu.currentTranspose > -12)
					if(GUI.Button(new Rect(dialogRect.x + dialogRect.width/2f -80f, dialogRect.y + dialogRect.height/2f -30f, 60f,60f), "", arrows[0]))
						Menu.currentTranspose--;
				
				GUI.Label(new Rect( dialogRect.x + dialogRect.width/2f -20f, dialogRect.y + dialogRect.height/2f -20f, 40f,40f), (Menu.currentTranspose/2f).ToString(), labelNoteTransposition);
				
				if(Menu.currentTranspose < 12)
					if(GUI.Button(new Rect(dialogRect.x + dialogRect.width/2f +30f, dialogRect.y + dialogRect.height/2f -30f, 60f,60f), "", arrows[1]))
						Menu.currentTranspose++;
				
				if(GUI.Button(new Rect(dialogRect.x + 20f, dialogRect.yMax - 80f, 100f, 50f),"", buttonOk))
				{
					bandNetworkController.IsSelectingNotesTransposition = false;
					bandNetworkController.IsSelectingSymbols = true;
					Menu.musicSelected.Instruments[0].transposeVisual = Menu.currentTranspose;
				}
				
				if(GUI.Button (new Rect(dialogRect.xMax - 120f, dialogRect.yMax - 80f, 100f, 50f), "", buttonCancel))
				{
					Destroy(bandNetworkController.gameObject);
					Application.LoadLevel(Utils.Common.SCENE_MENU);
				}
			}
			else
			{
				dialogRect = new Rect(Screen.width/2f - waitingServer.width/2f, Screen.height/2f - waitingServer.height/2f , waitingServer.width, waitingServer.height);
				GUI.Box(dialogRect, "AGUARDANDO SERVIDOR...", waitingServerBackground);
				
				if(GUI.Button (new Rect(dialogRect.x + dialogRect.width/2f - 50f , dialogRect.yMax - 80f, 100f, 50f), "", buttonCancel))
				{
					Destroy(bandNetworkController.gameObject);
					Application.LoadLevel(Utils.Common.SCENE_MENU);
				}
			}
		}
		else if(networkConnectionError != NetworkConnectionError.NoError)
		{
			dialogRect = new Rect(Screen.width/2f - waitingServer.width/2f, Screen.height/2f - waitingServer.height/2f , waitingServer.width, waitingServer.height);

			GUI.Box(dialogRect, "" + networkConnectionError.ToString(), waitingServerBackground);

			if(GUI.Button (new Rect(dialogRect.x + dialogRect.width/2f - 50f , dialogRect.yMax - 80f, 100f, 50f), "", buttonCancel))
			{
				Destroy(bandNetworkController.gameObject);
				Application.LoadLevel(Utils.Common.SCENE_MENU);
			}
		}
		else
		{
			dialogRect = new Rect(Screen.width/2f - waitingServer.width/2f, Screen.height/2f - waitingServer.height/2f , waitingServer.width, waitingServer.height);
			
			GUI.Box(dialogRect, "POR FAVOR AGUARDE...", waitingServerBackground);
			
			if(GUI.Button (new Rect(dialogRect.x + dialogRect.width/2f - 50f , dialogRect.yMax - 80f, 100f, 50f), "", buttonCancel))
			{
				Destroy(bandNetworkController.gameObject);
				Application.LoadLevel(Utils.Common.SCENE_MENU);
			}
		}
	}
	
	private void OnDisconnectedFromServer(NetworkDisconnection nd)
	{
		if(Menu.IS_CLIENT)
		{
			if(isListening)
				StopListener();
			Destroy(bandNetworkController.gameObject);
			Application.LoadLevel(Utils.Common.SCENE_MENU);
		}
	}
	
	private void Close()
	{
		if(bandNetworkController)
			Destroy(bandNetworkController.gameObject);
		
		EventHandler e = OnClose;
		if(e != null)
			e(this, new EventArgs());
		if(isListening)
			StopListener();
		if(isHost)
			StopBroadcast();
		Destroy(gameObject);
	}
	
	private void OnApplicationQuit()
	{
		StopListener();
		StopBroadcast();
	}
	
	private void StartBroadcast()
	{
#if !UNITY_ANDROID
		broadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		
		broadcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
		
		isHost = true;
		
		broadcastThread = new Thread(WriteBroadcast);
		
		broadcastThread.Start ();
#endif
	}
	
	private void StopBroadcast()
	{
#if !UNITY_ANDROID
		if(isHost)
		{
			isHost = false;
			broadcastSocket.Close();
			broadcastThread.Abort();
		}
#endif
	}
	
	private void WriteBroadcast()
	{
#if !UNITY_ANDROID
		IPEndPoint ipEP = new IPEndPoint(IPAddress.Broadcast, 9050);
		
		string s;
		byte[] sendData;
		
		while(isHost)
		{
			try
			{
				sendData = new byte[2048];
				sendData[0] = (byte) ReceivedMessageType.ServerMessage;
				s = bandName + ";@;" + currentServerInfo.MusicName + ";@;" + currentServerInfo.NumPlayers + ";@;" + currentServerInfo.TracksNumber + ";@;";
				byte[] buffer = Encoding.UTF8.GetBytes(s);
				buffer.CopyTo(sendData,1);
				broadcastSocket.SendTo(sendData, ipEP);
				Thread.Sleep(1000);
			}
			catch(Exception e)
			{
				Debug.Log(e.Message);
			}
		}
#endif
	}
	
	private void StartListener()
	{
#if !UNITY_ANDROID
		listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		
		IPEndPoint ipEP = new IPEndPoint(IPAddress.Any, 9050);
		
		listenerSocket.Bind(ipEP);
		
		isListening = true;
		
		listenerThread = new Thread(ReadListener);
		
		listenerThread.Start();
#endif
	}
	
	private void StopListener()
	{
#if !UNITY_ANDROID
		if(isListening)
		{
			isListening = false;
			listenerSocket.Close();
			listenerThread.Abort();
			serverDictionary.Clear();
		}
#endif
	}
			
	private void ReadListener()
	{
#if !UNITY_ANDROID
		byte[] receiveBuffer = new byte[2048];
		string receivedStr;
		string[] s;
		EndPoint remoteEP;
		ServerInfo newServer;
		int numBytes;
		
		while(isListening)
		{
			remoteEP = (EndPoint) new IPEndPoint(IPAddress.Any, 9050);
			try
			{
				numBytes = listenerSocket.ReceiveFrom(receiveBuffer, ref remoteEP);
				
				if(receiveBuffer[0] == (byte) ReceivedMessageType.ServerMessage)
				{
					receivedStr = Encoding.UTF8.GetString(receiveBuffer, 1, (numBytes-1)/2);
					s = Regex.Split(receivedStr, ";@;");
					
					if(serverDictionary.ContainsKey(s[0]))
						serverDictionary[s[0]].Update (s[1], int.Parse(s[2]), int.Parse(s[3]), remoteEP);
					else
					{
						newServer = new ServerInfo();
						newServer.Update(s[1], int.Parse(s[2]), int.Parse(s[3]), remoteEP);
						newServer.LastTimeReceived = DateTime.Now.AddSeconds(10);
						serverDictionary.Add(s[0], newServer);
					}
				}
			}
			catch(Exception e)
			{
				Debug.Log(e.Message);
			}
		}
#endif
	}
	
	private class ServerInfo
	{
		public string MusicName {get; set;}
		public int NumPlayers {get; set;}
		public int TracksNumber {get; set;}
#if !UNITY_ANDROID
		public EndPoint EP {get; set;}
#endif
		public DateTime LastTimeReceived {get; set;}
		
		public ServerInfo() {}
#if !UNITY_ANDROID
		public void Update(string music, int numPlayers, int tracksNumber, EndPoint eP)
		{
			MusicName = music;
			NumPlayers = numPlayers;
			TracksNumber = tracksNumber;
			EP = eP;
		}
#endif		
		public void Update(string music, int numPlayers, int tracksNumber)
		{
			MusicName = music;
			NumPlayers = numPlayers;
			TracksNumber = tracksNumber;
		}
	}
}
