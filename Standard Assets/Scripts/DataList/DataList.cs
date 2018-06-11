using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using DataSet;

namespace DataList
{
	public class TMDList
	{
		private string currentDirectory;
		
		private string musicListPath;
		
		private ArrayList tmdFileList;
		
		private List<MusicListItem> musics = new List<MusicListItem>();
		
		private List<string> exceptionsList = new List<string>();
		
		private DevicePlayType[] devicePlayType = new DevicePlayType[0];
		
		//private List<File> 
		
		public MusicData[] sortedTMDs;
		
		public MusicListItem[] sortedMusics;
		
		public string GetDirectory
		{
			get 
			{
				return currentDirectory;
			}
		}
		
		//Construtor
		public TMDList(string directoryToOpen)
		{
			currentDirectory = directoryToOpen;
			
			sortedMusics = musics.ToArray();
			
			tmdFileList = new ArrayList();
			
			musicListPath = currentDirectory + "MusicList.xml";
			
			ReadMusicList();
			CheckMusicList();
		}
		
		//carrega todos os tmds na lista.
		private void LoadAllTMDs()
		{
			if(tmdFileList.Count > 0)
			{
				tmdFileList.Clear();
				exceptionsList.Clear();
			}
			FileInfo[] files =  new DirectoryInfo(currentDirectory).GetFiles("*.tmd");
			foreach(FileInfo file in files)
			{
				if(file.Name.EndsWith(".tmd"))
				{
					try
					{
						StreamReader sr = file.OpenText();
						MusicData newMusic = new MusicData(sr.ReadToEnd());
						newMusic.path = file.Name;
						sr.Close();
						tmdFileList.Add(newMusic);
					}
					catch (System.Exception e)
					{
						exceptionsList.Add(file.Name + "\n" + e.Message);
					}	
				}
			}
			
			sortedTMDs = (MusicData[]) tmdFileList.ToArray(typeof(MusicData));
		}
		
		//ordena pela propriedade title.
		public void SortByTitle()
		{
			sortedMusics = musics.ToArray();
			System.Array.Sort(sortedMusics,delegate(MusicListItem md1,MusicListItem md2) 
			{
				return md1.Title.CompareTo(md2.Title);
			});
			
			if(devicePlayType.Length > 0)
				CutByDevicePlayType();
		}
		
		
		//ordena pela propriedade title com uma string.
		public void SortByTitleUsingText(string textToSearch)
		{
			List<MusicData> finalList = new List<MusicData>();
			foreach(MusicData md in tmdFileList.ToArray(typeof(MusicData)))
			{
				if(0 == md.title.ToUpper().IndexOf(textToSearch.ToUpper()))
				{
					finalList.Add(md);
				}
			}
			sortedTMDs = finalList.ToArray();
		}
		
		public void SortMusicsByTitleUsingText(string textToSearch)
		{
			sortedMusics = musics.ToArray();
			List<MusicListItem> finalList = new List<MusicListItem>();
			foreach(MusicListItem mli in sortedMusics)
			{
				if(0 == mli.Title.ToUpper().IndexOf(textToSearch.ToUpper()))
				{
					finalList.Add(mli);
				}
			}
			sortedMusics = finalList.ToArray();
			
			if(devicePlayType.Length > 0)
				CutByDevicePlayType();
		}
		
		public void RefreshList()
		{	
			if(!Directory.Exists(currentDirectory))
				Directory.CreateDirectory(currentDirectory);
			
			if(File.Exists(musicListPath) && Directory.GetFiles(currentDirectory,"*.tmd").Length > 0)
			{
				CheckMusicList();
			}
			else
			{
				CreateMusicList();
				ReadMusicList();
			}
			SortByTitle();
		}
		
		public void SetDevicePlayType(DevicePlayType[] newDevicePlayType)
		{
			this.devicePlayType = newDevicePlayType;
			RefreshList();
		}
		
		private void CutByDevicePlayType()
		{
			List<MusicListItem> finalList = new List<MusicListItem>();
			foreach(MusicListItem mli in sortedMusics)
			{
				if(mli.Devices.Length == 0)
					continue;
				for(int i = 0;i < devicePlayType.Length ; i++)
				{
					for(int j = 0; j < mli.Devices.Length; ++j)
					{
						if((DevicePlayType) mli.Devices[j] == devicePlayType[i])
						{
							finalList.Add(mli);
							i = devicePlayType.Length + 1;
							break;
						}
					}
				}
			}
		
			sortedMusics = finalList.ToArray();
		}
		
		private void ReadMusicList()
		{
			if(!File.Exists(musicListPath))
				return;
			int countSolos = 0;
			musics.Clear();
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(musicListPath);
			
			XmlNodeList attribNodeList = xmlDocument.GetElementsByTagName("attrib");
			if(attribNodeList.Count == 0)
			{
				Debug.Log ("asd");
				//CreateMusicList();
				//ReadMusicList();
				return;
			}
			XmlNodeList itensNodeList = xmlDocument.GetElementsByTagName("item");
			for(int i = 0; i < itensNodeList.Count; ++i)
			{
				MusicListItem musicListItem = new MusicListItem();
				musicListItem.Title = itensNodeList[i].InnerText;
				musicListItem.Attrib = attribNodeList[i].InnerText;
				musicListItem.FilePath = itensNodeList[i].Attributes.GetNamedItem("path").InnerText;
				musicListItem.Breakpoints = (itensNodeList[i].Attributes.GetNamedItem("breakpoints").InnerText == "true") ? true : false;
				countSolos = 0;
				if(itensNodeList[i].Attributes.GetNamedItem("devices") != null)
				{
					string[] devices = itensNodeList[i].Attributes.GetNamedItem("devices").InnerText.Split(';');
					musicListItem.Devices = new int[devices.Length];
					for(int j = 0; j < devices.Length; j++)
					{
						if(devices[j] != "")
						{
							musicListItem.Devices[j] = System.Convert.ToInt32(devices[j]);
							if((DevicePlayType)musicListItem.Devices[j] != DevicePlayType.NONE)
								countSolos++;
						}
					}
				}
				else
				{
					musicListItem.Devices = new int[0];
				}
				musicListItem.NumberSoloTracks = countSolos;
				musics.Add(musicListItem);
			}
		}
		
		public void CreateMusicList()
		{
			LoadAllTMDs();
			bool breaks = false;
			List<DevicePlayType> devices = new List<DevicePlayType>();
			
			string musicListStr = "<?xml version=\"1.0\" encoding=\"utf-8\"?><itens>";
			for(int i = 0; i < sortedTMDs.Length; ++i)
			{
				musicListStr += "<item ";
				musicListStr += "path=\"" + currentDirectory + sortedTMDs[i].path + "\"";
				breaks = false;
				devices.Clear();
				for(int j = 0; j < sortedTMDs[i].Instruments.Length; ++j)
				{
					if(sortedTMDs[i].Instruments[j].breakpoints.Count > 0)
					{
						breaks = true;
					}
					if(sortedTMDs[i].Instruments[j].playable != DevicePlayType.NONE)
					{
						devices.Add(sortedTMDs[i].Instruments[j].playable);
					}
				}
				
				musicListStr += " breakpoints=" + ((breaks == true) ? "\"true\"" : "\"false\"");
				if(devices.Count > 0)
				{
					musicListStr += " devices=\"";
					for(int j = 0; j < devices.Count; j++)
					{
						musicListStr += ((int)devices[j]).ToString() + ";";
					}
					musicListStr += "\"";
				}
				musicListStr += ">" + "<![CDATA[" + sortedTMDs[i].title + "]]></item>";
				musicListStr += "<attrib><![CDATA[" + sortedTMDs[i].attrib + "]]></attrib>";
			}
			
			for(int i = 0; i < exceptionsList.Count; ++ i)
			{
				string[] tempSTR = exceptionsList[i].Split('\n');
				musicListStr += "<exception path=\"" + tempSTR[0] + "\" message=\"" + tempSTR[1] + "\"></exception>";
			}
			musicListStr += "</itens>";
			
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(musicListStr);
			
			xmlDocument.Save(musicListPath);
		}
		
 		private void CheckMusicList()
		{
			string[] files = Directory.GetFiles(currentDirectory,"*.tmd");
			FileInfo musicListInfo = new FileInfo(musicListPath);
			FileInfo fileInfo;
			
			foreach(string s in files)
			{
				fileInfo = new FileInfo(s);
				bool addNew = true;
				for(int i = 0; i < musics.Count; i++)
				{
					if(Path.GetFileName(musics[i].FilePath) == Path.GetFileName(s))
					{
						addNew = false;
						if(musicListInfo.LastWriteTime <= fileInfo.LastWriteTime)
							UpdateInMusics(musics[i]);
					}
				}
				if(addNew)
					AddInMusics(s);
			}
			
			List<MusicListItem> listToRemove = new List<MusicListItem>();
			for(int i = 0; i < musics.Count; i++)
			{
				if(!File.Exists(musics[i].FilePath))
					listToRemove.Add(musics[i]);
			}
			
			foreach(MusicListItem mli in listToRemove)
				musics.Remove(mli);
			
			
			RecreateMusicList();
			SortByTitle();
		}
		
		private void UpdateInMusics(MusicListItem mli)
		{	
			MusicData md = new MusicData(mli.FilePath);
			mli.Title = md.title;
			mli.Attrib = md.attrib;
			bool breaks = false;
			List<DevicePlayType> devices = new List<DevicePlayType>();
			for(int i = 0; i < md.Instruments.Length; i++)
			{
				if(md.Instruments[i].breakpoints.Count > 0)
					breaks = true;
				if(md.Instruments[i].playable != DevicePlayType.NONE)
					devices.Add(md.Instruments[i].playable);
			}
			
			mli.Breakpoints = breaks;
			
			int[] d = new int[devices.Count];
			for(int i = 0; i < d.Length; i++)
			{
				d[i] = (int)devices[i];
			}
			mli.Devices = d;
		}
		
		private void AddInMusics(string filePath)
		{
			MusicData md;
			try
			{
				md = new MusicData(filePath);
			}
			catch
			{
				return;
			}
			MusicListItem mli = new MusicListItem();
			mli.Title = md.title;
			mli.Attrib = md.attrib;
			mli.FilePath = filePath;
			bool breaks = false;
			List<DevicePlayType> devices = new List<DevicePlayType>();
			for(int i = 0; i < md.Instruments.Length; i++)
			{
				if(md.Instruments[i].breakpoints.Count > 0)
					breaks = true;
				if(md.Instruments[i].playable != DevicePlayType.NONE)
					devices.Add(md.Instruments[i].playable);
			}
			mli.Breakpoints = breaks;
			
			int[] d = new int[devices.Count];
			for(int i = 0; i < d.Length; i++)
			{
				d[i] = (int)devices[i];
			}
			mli.Devices = d;
			
			musics.Add(mli);
		}
		
		private void RecreateMusicList()
		{
			string musicListStr = "<?xml version=\"1.0\" encoding=\"utf-8\"?><itens>";
			for(int i = 0; i < musics.Count; ++i)
			{
				musicListStr += "<item ";
				musicListStr += "path=\"" + musics[i].FilePath + "\"";
				
				musicListStr += " breakpoints=" + (musics[i].Breakpoints ? "\"true\"" : "\"false\"");
				if(musics[i].Devices.Length > 0)
				{
					musicListStr += " devices=\"";
					for(int j = 0; j < musics[i].Devices.Length; j++)
					{
						if(musics[i].Devices[j] != 0)
							musicListStr += musics[i].Devices[j].ToString() + ";";
					}
					musicListStr += "\"";
				}
				musicListStr += ">" + "<![CDATA[" + musics[i].Title + "]]></item>";
				musicListStr += "<attrib><![CDATA[" + musics[i].Attrib + "]]></attrib>";
			}
			musicListStr += "</itens>";
			
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(musicListStr);
			
			xmlDocument.Save(musicListPath);
		}
	}
}