using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Linq;
using Utils;

namespace DataSet
{
	public class SampleSet
	{
		public int Index = -1;
		public string Label = "";
		public string Hash = "";
		public int Lowest = 48;
		public int Span = 36;
		public int Octave = 4;
		public int Transpose = 0;
		public int Behaviour = 0;
		public int DefautDevice = 0;
		public Dictionary<int, int[][]> DeviceMap = new Dictionary<int, int[][]>();
		public InstrumentData instrData = new InstrumentData();
		
		public override string ToString()
		{
			return string.Format("[InstrumentConfig Lowest={0}, Span={1}, Octave={2}, Transpose={3}, DefaultDevice={4}, DeviceMap{5}]",
				Lowest, Span, Octave, Transpose, DefautDevice, DeviceMap);
		}
	}
	
	public class DeviceConfig
	{
		public int SampleSetIndex;
		public IntPtr SamplesPtr;
		public float[] Volumes = {};
	}
	
	//enum comportamento do instrumento no wave.
	public enum InstrumentBehaviour
	{
		UNDEFINED = 0,
		STANDARD = 1,
		SUSTAIN,
		WHOLE_SOUND
	}
	
	public enum DevicePlayType
	{
		NONE = 0,
		SOLO = 1,
		DRUMS,
		BASS,
		ACCOMPANIMENT
	}
	
	//cabeçalho do arquivo musica.
	public class MusicData
	{
		public static void SortNotes(NoteData[] ndArray)
		{
			int comp;
			
			System.Array.Sort(ndArray, delegate(NoteData nd1, NoteData nd2)
			{
				return (comp = nd1.deltaTimeAbs.CompareTo(nd2.deltaTimeAbs)) != 0 ? comp : nd2.deltaTimeRel.CompareTo(nd1.deltaTimeRel);
			});
		}
		
		//construtor padrão.
		public MusicData()
		{
			this.TimeSignature = new TimeSignature();
			MusicDuration = float.NaN;
		}
		
		//consetrutor que ja le o arquivo.
		public MusicData(string path) : this()
		{
			LoadTMD(path);
		}
		
		//construtor que le o tmd de uma instancia MusicData.
		public MusicData(MusicData other, bool deepCopy = true) : this()
		{
			this.id = other.id;
			this.creationDate = other.creationDate;
			this.lastUpdate = other.lastUpdate;
			this.isPublic = other.isPublic;
			this.bpm = other.bpm;
			this.title = other.title;
			this.path = other.path;
			this.user = other.user;
			this.attrib = other.attrib;
			this.TimeSignature = new TimeSignature(other.TimeSignature);
			this.instruments = new List<InstrumentData>();
			this.lyrics = new List<string>();
			
			if (deepCopy)
				foreach (InstrumentData instrData in other.instruments)
					this.instruments.Add(new InstrumentData(instrData));
			
			foreach (string lyricsLine in other.lyrics)
				this.lyrics.Add(lyricsLine);
		}
		
		//variaveis.
		public uint id = 0; 
		public string creationDate = Common.GetStringTime();
		public string lastUpdate = Common.GetStringTime();
		public bool isPublic = true;
		public float bpm = 60;
		public string path = "";
		public string title = "";
		public string author = "";
		public UserInfo user = new UserInfo();
		public string attrib = "";
		public List<InstrumentData> instruments = new List<InstrumentData>();
		public List<float> markers = new List<float>();
		public List<string> lyrics = new List<string>();
		
		public TimeSignature TimeSignature { get; set; }
		public float MusicDuration { get; set; }
		
		public InstrumentData[] Instruments
		{
			get { return (InstrumentData[]) this.instruments.ToArray();}
		}
		
		//carrega TMD recebendo um path relativo.
		public void LoadTMD(string stringToRead)
		{
			XmlDocument documentTMD = new XmlDocument();
			
			try
			{
				if(stringToRead.EndsWith(".tmd"))
				{
					this.path = stringToRead;
					stringToRead = File.ReadAllText(stringToRead);
					//documentTMD.Load(stringToRead);
				}
				
				documentTMD.LoadXml(stringToRead);
			}
			catch
			{
				//Debug.Log(stringToRead.Substring(0, 400) + " " + e.Message);
				return;
			}
			
			XmlElement rootTMD = documentTMD.DocumentElement;// variavel para armazenar a raiz 'music'
			XmlNodeList tempNodeList;
			
			//Debug.Log(rootTMD.Attributes.GetNamedItem("id").InnerText);
			//carrega 'music' attributes.
			if(rootTMD.Attributes.GetNamedItem("id") != null)
				this.id = Convert.ToUInt32( rootTMD.Attributes.GetNamedItem("id").InnerText);// le id da musica.
			
			if(rootTMD.Attributes.GetNamedItem("creationDate") != null)
				this.creationDate = rootTMD.Attributes.GetNamedItem("creationDate").InnerText;//le a creation date.
			
			if(rootTMD.Attributes.GetNamedItem("lastUpdate") != null)
				this.lastUpdate = rootTMD.Attributes.GetNamedItem("lastUpdate").InnerText;//le a lastDate.
			
			if(rootTMD.Attributes.GetNamedItem("isPublic")!= null)
				this.isPublic = (rootTMD.Attributes[2].InnerText[0] == '0'? false : true); //le se a musica é publica.
			
			tempNodeList = rootTMD.GetElementsByTagName("user");
			if(tempNodeList.Count > 0)
			{
				switch(tempNodeList[0].Attributes.Count)
				{
				case 2:
					this.user.id = UInt32.Parse(tempNodeList[0].Attributes[0].InnerText); //le o id do usuario.
					
					this.user.login = tempNodeList[0].Attributes[1].InnerText;//le o login do usuario.
					break;
				case 1:
					this.user.id = UInt32.Parse(tempNodeList[0].Attributes[0].InnerText); //le o id do usuario.
					break;
				}
				this.user.name = tempNodeList[0].InnerText;//le o nome do usuario.
			}
			
			tempNodeList = documentTMD.GetElementsByTagName("bpm");
			if(tempNodeList.Count > 0)
				this.bpm = Convert.ToSingle(tempNodeList[0].InnerText);//le o bpm da musica.
			
			tempNodeList = documentTMD.GetElementsByTagName("duration");
			if(tempNodeList.Count > 0)
				this.MusicDuration = Convert.ToSingle(tempNodeList[0].InnerText);//le a dura�ao fixa da musica.
			
			/*tempNodeList = documentTMD.GetElementsByTagName("path");
			if(tempNodeList.Count > 0)
				this.path = tempNodeList[0].InnerText;*/
			
			tempNodeList = documentTMD.GetElementsByTagName("title");
			if(tempNodeList.Count > 0)
				this.title = tempNodeList[0].InnerText;//le o title da musica.
			
			tempNodeList = documentTMD.GetElementsByTagName("author");
			if(tempNodeList.Count > 0)
				this.author = tempNodeList[0].InnerText; //le o autor da musica.
			
			tempNodeList = documentTMD.GetElementsByTagName("attrib");
			if(tempNodeList.Count > 0)
				this.attrib = tempNodeList[0].InnerText;
			
			tempNodeList = documentTMD.GetElementsByTagName("instruments"); //abre as tags de instrumento.
			
			
			for(int i = 0; i < tempNodeList.Count; i++)
			{
				if (this.markers.Count == 0 && tempNodeList[i].Attributes.GetNamedItem("markers") != null)
					this.markers = tempNodeList[i].Attributes.GetNamedItem("markers").InnerText.Split(';').Select(s => float.Parse(s)).ToList();
				
				for(int j = 0; j < tempNodeList[i].ChildNodes.Count; j++)
				{
					InstrumentData newInstrumentData = new InstrumentData();
					XmlAttributeCollection attr = tempNodeList[i].ChildNodes[j].Attributes;
					
					if (attr.GetNamedItem("id") != null)
						newInstrumentData.id = Int32.Parse(attr.GetNamedItem("id").InnerText);
					
					if (attr.GetNamedItem("hash") != null)
						newInstrumentData.hash = attr.GetNamedItem("hash").InnerText;
					
					if (attr.GetNamedItem("muted") != null)
						newInstrumentData.muted = Int32.Parse(attr.GetNamedItem("muted").InnerText) == 1;
					
					if (attr.GetNamedItem("device") != null)
						newInstrumentData.deviceType = Int32.Parse(attr.GetNamedItem("device").InnerText);
					
					if (attr.GetNamedItem("octave") != null)
						newInstrumentData.octave = Int32.Parse(attr.GetNamedItem("octave").InnerText);
					
					if (attr.GetNamedItem("volume") != null)
						newInstrumentData.volume = float.Parse(attr.GetNamedItem("volume").InnerText);
					
					if (attr.GetNamedItem("transpose") != null)
						newInstrumentData.transpose = Int32.Parse(attr.GetNamedItem("transpose").InnerText);
					
					if (attr.GetNamedItem("playable") != null)
						newInstrumentData.playable = (DevicePlayType)Int32.Parse(attr.GetNamedItem("playable").InnerText);
					
					if (attr.GetNamedItem("behaviour") != null)
						newInstrumentData.behaviour = (InstrumentBehaviour) (Int32.Parse(attr.GetNamedItem("behaviour").InnerText));
					
					if (attr.GetNamedItem("breakpoints") != null)
						newInstrumentData.breakpoints = attr.GetNamedItem("breakpoints").InnerText.Split(';').Select(s => float.Parse(s)).ToList();
					
					if (attr.GetNamedItem("keysOrder") != null)
						newInstrumentData.keysOrder = attr.GetNamedItem("keysOrder").InnerText.Split(';').Select(s => int.Parse(s)).ToArray();
					
					string cdata = tempNodeList[i].ChildNodes[j].ChildNodes[0].InnerText;
					string[] cdataSplit = cdata.Replace("[[", "").Replace("]]", "").Split(new string[]{"],["}, StringSplitOptions.None);
					
					float tempDeltaTime = 0.0f;
					string[] noteTexts;
					NoteData noteData;
					float maxTime = TimeToBeats(3);
					
					foreach (string noteTextSet in cdataSplit)
					{
						if (noteTextSet != "")
						{
							noteData = new NoteData();
							noteTexts = noteTextSet.Split(',');
							
							noteData.midiCode = Int32.Parse(noteTexts[0]);
							noteData.duration = Convert.ToSingle(noteTexts[1]);
							
							if (noteData.duration > maxTime)
								noteData.duration = maxTime;
							
							noteData.deltaTimeRel = Convert.ToSingle(noteTexts[2]);
							noteData.deltaTimeAbs = tempDeltaTime += noteData.deltaTimeRel;
							noteData.velocity = Convert.ToSingle(noteTexts[3]);
							
							newInstrumentData.notes.Add(noteData);
						}
					}
					
					this.instruments.Add(newInstrumentData);
				}
			}
			
			// (...) Formatador de letra: var w=open();w.document.body.innerText=document.getElementById("div_letra").innerText.split(/[\r\n]+/).join("]]></line><line><![CDATA[");w.document.close();
			
			tempNodeList = documentTMD.GetElementsByTagName("lyrics");
			
			if (tempNodeList.Count > 0 && tempNodeList[0].ChildNodes.Count > 0)
				foreach (XmlNode node in tempNodeList[0].ChildNodes[0].ChildNodes)
					lyrics.Add(node.InnerText);
		}
		
		/**
		 * Cria uma string xml sem um instrumento especifico.
		 * As trilhas que estiverem no array serão silenciadas.
		 * Se invert for true, somente as trilhas no array serão tocadas.
		 */
		public string CreateXmlString(int[] excludeIndexes = null, bool invert = false)
		{
			float lastDtAbs;
			NoteData[] ndArray;
			
			string newTMD = "<?xml version=\"1.0\" encoding=\"utf-8\"?><music id=\"";
			newTMD += this.id + "\" " + "creationDate=\"" + this.creationDate + "\" " + "lastUpdate=\"" + this.lastUpdate + "\" " + "isPublic=\"" + (this.isPublic? "1\">" : "0\">");
			newTMD += "<user id=\"" + this.user.id + "\">" +  this.user.name + "</user>";
			newTMD += "<bpm>" + this.bpm + "</bpm>";
			
			if (!float.IsNaN(MusicDuration))
				newTMD += "<duration>" + MusicDuration + "</duration>";
			
			newTMD += "<path><![CDATA[" + this.path + "]]></path>";
			newTMD += "<title><![CDATA[" + this.title + "]]></title>";
			newTMD += "<author><![CDATA[" + this.author + "]]></author>";
			newTMD += "<attrib><![CDATA[" + this.attrib + "]]></attrib>";
			newTMD += "<instruments";
			
			// Adiciona markers se houver.
			if (this.markers.Count > 0)
				newTMD += " markers=\"" + this.markers.Select(n => n.ToString()).Aggregate((n1, n2) => n1 + ";" + n2) + "\"";
			
			newTMD += ">";
			
			for(int j = 0; j < this.instruments.Count; j++)//(InstrumentData ins in this.instruments)
			{
				InstrumentData ins = (InstrumentData)this.instruments[j];
				
				newTMD += "<instrument id=\"" + ins.id + "\"";
				
				// Adiciona hash diferente se houver.
				if (ins.hash != "")
					newTMD += " hash=\"" + ins.hash + "\"";
				
				// Adiciona muted se houver.
				if ((excludeIndexes != null && excludeIndexes.Contains(j))^invert)
					newTMD += " muted=\"1\"";
				
				// Adiciona device diferente se houver.
				if (ins.deviceType != -1)
					newTMD += " device=\"" + ins.deviceType + "\"";
				
				// Adiciona behaviour diferente se houver.
				if (ins.behaviour != InstrumentBehaviour.UNDEFINED)
					newTMD += " behaviour=\"" + (int)ins.behaviour + "\"";
				
				// Adiciona octave diferente se houver.
				if (ins.octave != 0)
					newTMD += " octave=\"" + ins.octave + "\"";
				
				// Adiciona volume.
				newTMD += " volume=\"" + ins.volume + "\"";
				
				// Adiciona transpose diferente se houver.
				if (ins.transpose != 0)
					newTMD += " transpose=\"" + ins.transpose + "\"";
				
				// Adiciona modo de execução diferente se houver.
				if (ins.playable != DevicePlayType.NONE)
					newTMD += " playable=\"" + (int)ins.playable + "\"";
				
				// Adiciona breakpoints se houver.
				if (ins.breakpoints.Count > 0)
				{
					ins.breakpoints.Sort();
					newTMD += " breakpoints=\"" + ins.breakpoints.Select(n => n.ToString()).Aggregate((n1, n2) => n1 + ";" + n2) + "\"";
				}
				
				// Adiciona breakpoints se houver.
				if (ins.keysOrder.Length > 0)
					newTMD += " keysOrder=\"" + ins.keysOrder.Select(n => n.ToString()).Aggregate((n1, n2) => n1 + ";" + n2) + "\"";
				
				newTMD += "><![CDATA[" + ((ins.notes.Count > 0) ? "[" : "");
				
				// Define os delta-times relativos das notas.
				lastDtAbs = 0f;
				ndArray = ins.Notes;
				
				string sep = "";
				//Fazer isso externamente.
				//int baseNote = Game.Instance.SoundEngine.GetBaseNote(ins);
				
				for (int i = 0; i < ndArray.Length; ++i)
				{
					NoteData note = ndArray[i];
					
					note.deltaTimeRel = note.deltaTimeAbs - lastDtAbs;
					lastDtAbs = note.deltaTimeAbs;
					
					//newTMD += sep + "[" + (note.cellData != null ? baseNote + note.cellData.col : note.midiCode)
					newTMD += sep + "[" + note.midiCode
						+ "," + note.duration + "," + note.deltaTimeRel + "," + note.velocity + "]";
					
					sep = ",";
				}
				
				newTMD += ((ins.notes.Count > 0) ? "]" : "") + "]]></instrument>";
			}
			
			newTMD += "</instruments>";
			
			if (lyrics.Count > 0)
			{
				newTMD += "<lyrics><set>";
				
				foreach (string line in lyrics)
					newTMD += "<line><![CDATA[" + line + "]]></line>";
				
				newTMD += "</set></lyrics>";
			}
			
			newTMD += "</music>";
			
			return newTMD;
		}
		
		//salva o arquivo no hd recebendo o path(relativo) e o nome.
		public void SaveFile(string filePath)
		{
			string tmd = this.CreateXmlString();
			XmlDocument newXmlDocument = new XmlDocument();
			
			newXmlDocument.LoadXml(tmd);
			if(!filePath.EndsWith(".tmd"))
			{
				filePath += ".tmd";
			}
			newXmlDocument.Save(filePath);
		}
		
		//pega a dura��o da musica.
		public float GetMusicDuration()
		{
			if (!float.IsNaN(MusicDuration))
               return MusicDuration;
            
			float finalMusicDuration = 0.0f;
			
			foreach(InstrumentData instrument in this.instruments)
			{
				foreach(NoteData currentNote in instrument.notes)
				{
					//float currentMusicDuration = currentNote.deltaTimeAbs + (currentNote.durationSample != 0f ? currentNote.durationSample : currentNote.duration);
					float currentMusicDuration = currentNote.deltaTimeAbs + currentNote.duration;
					if(currentMusicDuration > finalMusicDuration)
					{
						finalMusicDuration = currentMusicDuration;
					}
				}
			}
			
			return finalMusicDuration;
		}
		
		//retorna o tamanho do tmd em miliseconds
		public float GetMusicDurationTime()
		{	
			return BeatsToTime(GetMusicDuration());
		}
		
		public float BeatsToMeasure(float dt)
		{
			return dt*TimeSignature.NoteValue;
		}
		
		public float MeasureToBeats(float measure)
		{
			return measure/TimeSignature.NoteValue;
		}
		
		public float TimeToBeats(float time)
		{
			return time*bpm/60f;
		}
		
		public float BeatsToTime(float beats)
		{
			return 60f/bpm*beats;
		}
		
		//imprimi os valores da instancia para debug.
		public void DebugData()
		{
			//debugs.
			Debug.Log("id:" + this.id.ToString() + "\ncreationDate: " + this.creationDate + "\nlastUpdate: " + this.lastUpdate + "\nisPublic: " + this.isPublic.ToString() + "\nbpm: " + this.bpm.ToString() + "\npath: " + this.path + "\nauthor: " + this.author);
			
			this.user.DebugData();
			
			foreach(InstrumentData ins in this.instruments)
			{
				ins.DebugData();
			}
		}
	}
	
	//classe com informações do usuario criador do arquivo.
	public class UserInfo
	{
		//construtor padrão.
		public UserInfo()
		{
			id = 0;
			login = "";
			name = "";
		}
		
		//variaveis.
		public uint id;
		public string login;
		public string name;
		
		public void DebugData()
		{
			Debug.Log("id: " + id + "\nlogin: " + login + "\nname: " + name);
		}
	}
	
	//classe com o cabeçalho do instrumento.
	public class InstrumentData
	{
		//construtor padrão.
		public InstrumentData()
		{
			id = 0;
			hash = "GrandPiano";
			muted = false;
			deviceType = 0;
			behaviour = InstrumentBehaviour.UNDEFINED;
			octave = 0;
			transpose = 0;
			transposeVisual = 0;
			channel = 0;
			volume = 1f;
			playable = DevicePlayType.NONE;
		}
		
		public InstrumentData(InstrumentData source, bool copyNotes = true)
		{
			this.id = source.id;
			this.hash = source.hash;
			this.muted = source.muted;
			this.deviceType = source.deviceType;
			this.behaviour = source.behaviour;
			this.octave = source.octave;
			this.transpose = source.transpose;
			this.transposeVisual = source.transposeVisual;
			this.channel = source.channel;
			this.volume = source.volume;
			this.playable = source.playable;
			
			if (copyNotes)
			{
				foreach (NoteData nd in source.notes)
					this.notes.Add(new NoteData(nd));
				
				foreach (float bp in source.breakpoints)
					this.breakpoints.Add(bp);
			}
		}
		
		//variaveis.
		public int id;
		public string hash;
		public bool muted;
		public int deviceType;
		public InstrumentBehaviour behaviour;
		public int octave;
		public int transpose;
		public float volume;
		public int[] keysOrder = {};
		public DevicePlayType playable;
		public List<NoteData> notes = new List<NoteData>();
		public List<float> breakpoints = new List<float>();
		
		// Informa��es tempor�rias
		public int transposeVisual;
		
		// :: MIDI
		public uint channel;
		public bool damperPedal;
		public int bankSelect = -1;
		
		public NoteData[] Notes
		{
			get
			{
				NoteData[] nds = (NoteData[])this.notes.ToArray();
				
				MusicData.SortNotes(nds);
				
				return nds;
			}
		}
		
		//debug
		public void DebugData()
		{
			Debug.Log("id: " + id + "\nhash: " + hash + "\nbehaviour: " + behaviour.ToString() + "\nchannel: " + channel + "\nnumberOfNotes: " + this.notes.Count);
			foreach(NoteData note in notes)
			{
				note.DebugData();
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[InstrumentData: id={0}, Notes={1}]", this.id, Notes);
		}
	}
	
	//classe que armazena dados de uma nota especifica.
	public class NoteData
	{
		//construtor padrão.
		public NoteData()
		{
			midiCode = 0;
			deltaTimeRel = 0.0f;
			deltaTimeAbs = 0.0f;
			duration = 0.0f;
			velocity = 1.0f;
			sustained = false;
			notPlayed = true;
		}
		
		public NoteData(NoteData source)
		{
			this.midiCode = source.midiCode;
			this.sustained = source.sustained;
			this.deltaTimeAbs = source.deltaTimeAbs;
			this.deltaTimeRel = source.deltaTimeRel;
			this.duration = source.duration;
			this.velocity = source.velocity;
			this.notPlayed = source.notPlayed;
			this.cellData = source.cellData;
		}
		
		//variaveis.
		private int _midi = 0;
		public int midiCode { set { _midi = value; } get { return _midi; }}
		public bool sustained;
		public float deltaTimeAbs;
		public float deltaTimeRel;
		public float duration;
		public float velocity;
		public bool notPlayed;
		public CellData cellData;
		
		public void DebugData()
		{
			Debug.Log("midiCode: " + midiCode + "\ndeltaTimeRel: " + deltaTimeRel + "\ndeltaTimeAbs: " + deltaTimeAbs + "\nduration: " + duration + "\nvelocity: " + velocity);
		}
		
		public override string ToString ()
		{
			return string.Format ("[NoteData: midiCode={0}, duration={1}, deltaTime={2} ({3}), velocity={4}]",
				midiCode, duration, deltaTimeRel, deltaTimeAbs, velocity);
		}
	}
	
	public class TimeSignature
	{
		public uint Beats { get; set; }
		public uint NoteValue { get; set; }
		
		public TimeSignature()
		{
			this.Beats = 4;
			this.NoteValue = 4;
		}
		
		public TimeSignature(TimeSignature other)
		{
			Beats = other.Beats;
			NoteValue = other.NoteValue;
		}
	}
	
	public class CellData
	{
		public const float DEFAULT_INTESITY = 0.8f;
		
		public int col = 0;
		public float row = 0f;
		public float size = 0f;
		public float intensity = DEFAULT_INTESITY;
		
		public static CellData Zero
		{
			get { return new CellData(0, 0f, 0f, 0f); }
		}
		
		// :: Constructs ::
		public CellData() {}
		
		public CellData(int col, float row, float size = 0f, float intensity = DEFAULT_INTESITY)
		{
			this.col = col;
			this.row = row;
			this.size = size;
			this.intensity = intensity;
		}
		
		public static CellData operator -(CellData cd1, CellData cd2)
		{
			return new CellData(cd1.col - cd2.col, cd1.row - cd2.row, cd1.size - cd2.size, cd1.intensity - cd2.intensity);
		}
		
		public static CellData operator +(CellData cd1, CellData cd2)
		{
			return new CellData(cd1.col + cd2.col, cd1.row + cd2.row, cd1.size + cd2.size, cd1.intensity + cd2.intensity);
		}
		
		public static bool operator ==(CellData cd1, CellData cd2)
		{
			// Se ambos forem null ou forem a mesma instância de objeto.
			if (System.Object.ReferenceEquals(cd1, cd2))
				return true;
			
			// Se algum objeto for null.
			// Castings para Object para gerar cópia das instâncias e evitar
			// chamar o mesmo operador sobrecarregado, o que geraria loop infinito.
			if ((System.Object)cd1 == null ^ (System.Object)cd2 == null)
				return false;
			
			return cd1.col == cd2.col && cd1.row == cd2.row && cd1.size == cd2.size && cd1.intensity == cd2.intensity;
		}
		
		public static bool operator !=(CellData cd1, CellData cd2)
		{
			// Usa a sobrecarga de == para verificar a diferença.
			return !(cd1 == cd2);
		}
		
		public override bool Equals(object obj)
		{
			return this == obj;
		}
		
		public override int GetHashCode() { return base.GetHashCode(); }
		
		// :: Overrides ::
		public override string ToString()
		{
			return string.Format("[CellData: col={0}, row={1}, size={2}, intensity={3}]", this.col, this.row, this.size, this.intensity);
		}
	}
	
	public class SampleSetData
	{
		public uint Lowest { get; set; }
		public uint Span { get; set; }
		public uint StartingC { get; set; }
		public uint DefautDevice { get; set; }
		public uint[,] DeviceMap { get; set; }
	}
}