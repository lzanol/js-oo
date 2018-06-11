using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using DataSet;
using CSharpSynth.Midi;

namespace Readers
{
	/**
	 * ReferÃªncias:
	 * http://www.sonicspot.com/guide/midifiles.html
	 * http://www.somascape.org/midi/tech/mfile.html
	 * http://en.wikipedia.org/wiki/Modern_musical_symbols
	 * 
	 * @todo Notas com damperPedal/maxDuration não estão sendo "cortadas".
	 */
	public class MidiReader
	{
		MidiFile midiFile;
		Dictionary<int, Dictionary<int, int>> noteTracker;
		Dictionary<int, InstrumentData> instrDataChannel;
		Dictionary<int, int> sum = new Dictionary<int, int>();
		Dictionary<int, int> tNotes = new Dictionary<int, int>();
		
		public MidiReader(string path)
		{
			if (File.Exists(path))
				midiFile = new MidiFile(File.Open(path, FileMode.Open, FileAccess.Read));
			else throw new Exception("File not found!");
		}
		
		public MusicData GetMusicData()
		{
			if (midiFile == null)
				throw new Exception("No data loaded!");
			
			if (midiFile.MidiHeader.MidiFormat == MidiHelper.MidiFormat.MultiSong)
				throw new Exception("Unsupported MIDI format!");
			
			MusicData musicData = new MusicData();
			int midiChannel;
			float maxDuration;
			double deltaFactor = 1f;
			
			// :: Temp ::
			double deltaTime;
			double absTime;
			int midiCode;
			int velocity;
			
			musicData.bpm = GetBpm();
			noteTracker = new Dictionary<int, Dictionary<int, int>>();
			instrDataChannel = new Dictionary<int, InstrumentData>();
			sum = new Dictionary<int, int>();
			tNotes = new Dictionary<int, int>();
			
			// Duração max. por nota (3 segs. em delta-time)
			maxDuration = musicData.bpm/60f*3;
			
			foreach (MidiTrack track in midiFile.Tracks)
			{
				absTime = 0.0;
				
				foreach (MidiEvent e in track.MidiEvents)
				{
					midiChannel = e.channel;
					deltaTime = e.deltaTime/midiFile.MidiHeader.DeltaTiming*deltaFactor;
					e.absTimeBpm = absTime += deltaTime;
					
					if (e.isChannelEvent())
					{
						switch (e.midiChannelEvent)
						{
						case MidiHelper.MidiChannelEvent.Note_Off:
							CloseNote(e, maxDuration);
							break;
							
						case MidiHelper.MidiChannelEvent.Note_On:
							midiCode = (int)e.parameter1;
							velocity = (int)e.parameter2;
							
							CloseNote(e, maxDuration, true);
							
							#region TMD
							if (velocity > 0)
							{
								CreateTrack(ref e.channel, musicData, ref absTime);
								
								NoteData noteData = new NoteData();
								noteData.deltaTimeRel = (float)deltaTime;
								noteData.deltaTimeAbs = (float)absTime;
								noteData.midiCode = midiCode;
								noteData.velocity = velocity*instrDataChannel[midiChannel].volume/10000f;
								
								if (instrDataChannel[midiChannel].damperPedal)
									noteData.duration = maxDuration;
								
								if (!noteTracker.ContainsKey(midiChannel))
									noteTracker[midiChannel] = new Dictionary<int, int>();
								
								// Insere a nota na trilha associada ao canal atual e
								// associa o í­ndice da nota ao canal e ao cod. MIDI.
								noteTracker[midiChannel][midiCode] = instrDataChannel[midiChannel].notes.Count;
								instrDataChannel[midiChannel].notes.Add(noteData);
								
								sum[midiChannel] += velocity;
								++tNotes[midiChannel];
							}
							#endregion
							break;
							
						case MidiHelper.MidiChannelEvent.Controller:
							if (instrDataChannel.ContainsKey(midiChannel))
							{
								int controllerValue = (int)e.parameter2;
								
								switch ((MidiHelper.ControllerType)e.parameter1)
								{
								// :: MSB ::
								case MidiHelper.ControllerType.BankSelect:
									instrDataChannel[midiChannel].bankSelect = controllerValue;
									break;
									
								case MidiHelper.ControllerType.MainVolume:
									instrDataChannel[midiChannel].volume = controllerValue;
									break;
									
								// :: LSB ::
								case MidiHelper.ControllerType.BankSelectLSB:
									if (instrDataChannel[midiChannel].bankSelect == -1)
										instrDataChannel[midiChannel].bankSelect = 0;
									
									instrDataChannel[midiChannel].bankSelect = instrDataChannel[midiChannel].bankSelect*128 + controllerValue;
									break;
									
								case MidiHelper.ControllerType.MainVolumeLSB:
									instrDataChannel[midiChannel].volume = instrDataChannel[midiChannel].volume*128 + controllerValue;
									break;
									
								case MidiHelper.ControllerType.DamperPedal:
									instrDataChannel[midiChannel].damperPedal = controllerValue > 63;
									break;
									
								case MidiHelper.ControllerType.ResetControllers:
									instrDataChannel[midiChannel].volume = 100;
									instrDataChannel[midiChannel].damperPedal = false;
									break;
								}
							}
							break;
							
						case MidiHelper.MidiChannelEvent.Program_Change:
							int instrId = (int)e.parameter1;
							
							CreateTrack(ref e.channel, musicData, ref absTime);
							
							InstrumentData instrData = instrDataChannel[midiChannel];
							
							#region TMD
							if (midiChannel != 9)
							{
								if (instrId < 8)
									instrData.hash = "GrandPiano";
								/*else if (instrId < 6)
									instrData.hash = "Vibrafone";
								else if (instrId < 8)
									instrData.hash = "GrandPiano";*/
								else if (instrId < 16)
									instrData.hash = "Vibrafone";
								else if (instrId < 24)
									instrData.hash = "GrandPiano"; // (...) trocar por Organ
								else if (instrId < 29)
									instrData.hash = "GuitarAcoustic";
								else if (instrId < 32)
									instrData.hash = "GuitarDist";
								else if (instrId < 40)
									instrData.hash = "Bass";
								else if (instrId < 42)
									instrData.hash = "Violin";
								else if (instrId < 44)
									instrData.hash = "Cello";
								else if (instrId < 45)
									instrData.hash = "Violin";
								else if (instrId < 48) // (?) 49
									instrData.hash = "CelloPizzicato";
								else if (instrId < 55)
									instrData.hash = "Violin";
								else if (instrId < 56)
									instrData.hash = "CelloPizzicato";
								else if (instrId < 71)
									instrData.hash = "GrandPiano"; // (...) trocar por Sax
								else if (instrId < 80)
									instrData.hash = "Flute";
								else if (instrId < 104)
									instrData.hash = "GagaSynth";
								else if (instrId < 109)
									instrData.hash = "GuitarAcoustic";
								else if (instrId < 110)
									instrData.hash = "Flute";
								else if (instrId < 111)
									instrData.hash = "Violin";
								else if (instrId < 112)
									instrData.hash = "GrandPiano";
								else if (instrId < 113)
									instrData.hash = "Vibrafone";
								else if (instrId < 117)
									instrData.hash = "Tabla";
								else if (instrId < 118)
									instrData.hash = "Vibrafone";
								else if (instrId < 120)
									instrData.hash = "GagaSynth";
								else if (instrId < 128)
									instrData.hash = "Tabla";
								else instrData.hash = "GrandPiano";
							}
							else instrData.hash = "Drums";
							#endregion
							
							break;
							
						/*default:
							Debug.Log("midi " + e.midiChannelEvent + " " + e.parameter1 + " " + e.parameter2 + " " + e.Parameters[0]);
							break;*/
						}
					}
					else if (e.isMetaEvent())
					{
						switch (e.midiMetaEvent)
						{
						case MidiHelper.MidiMetaEvent.Tempo:
							deltaFactor = musicData.bpm/(double)(new BpmEvent(e)).bpm;
							break;
						
						case MidiHelper.MidiMetaEvent.End_of_Track:
							noteTracker.Clear();
							instrDataChannel.Clear();
							break;
							
						/*default:
							Debug.Log("meta " + e.midiMetaEvent + " " + e.parameter1 + " " + e.parameter2 + " " + e.Parameters[0]);
							break;*/
						}
					}
				}
			}
			
			return musicData;
		}
		
		class BpmEvent
		{
			public float bpm = float.NaN;
			public MidiEvent midiEvent;
			public double absTime;
			public double deltaTime;
			
			public bool IsValid
			{
				get { return !float.IsNaN(bpm); }
			}
			
			public BpmEvent(MidiEvent e)
			{
				object p = e.Parameters[0];
				float n;
				
				if (p != null && float.TryParse(p.ToString(), out n))
					bpm = MidiHelper.MicroSecondsPerMinute/n;
				else throw new Exception("Invalid Tempo: " + p);
				
				midiEvent = e;
			}
			
			public BpmEvent(MidiEvent e, int deltaTiming, double factor)
				: this(e)
			{
				// Converte em segundos.
				absTime = e.absTime/deltaTiming*factor;
				deltaTime = e.deltaTime/deltaTiming*factor;
			}
		}
		
		double GetHighestAbsTime()
		{
			MidiTrack[] tracks = midiFile.Tracks;
			MidiTrack track = tracks[tracks.Length - 1];
			
			return track.MidiEvents[track.MidiEvents.Length - 1].absTime;
		}
		
		float GetBpm()
		{
			const float DEFAULT_BPM = 120f;
			
			List<MidiEvent> midiEvents = midiFile.getAllMidiEventsofType(MidiHelper.MidiChannelEvent.None, MidiHelper.MidiMetaEvent.Tempo);
			BpmEvent[] bpms = new BpmEvent[midiEvents.Count];
			float bpm = DEFAULT_BPM;
			double bpmFactor = 60.0/bpm;
			BpmEvent be;
			
			// Coleta todos os eventos de BPM.
			for (int i = 0; i < midiEvents.Count; ++i)
			{
				bpms[i] = be = new BpmEvent(midiEvents[i], midiFile.MidiHeader.DeltaTiming, bpmFactor);
				
				if (be.IsValid)
					bpmFactor = 60.0/be.bpm;
			}
			
			double highestTimeInterval = 0.0;
			double highestAbsTime = GetHighestAbsTime()/midiFile.MidiHeader.DeltaTiming*bpmFactor;
			double lastAbsTime = highestAbsTime;
			
			// Verifica qual BPM dura por mais tempo e o define como padrÃ£o.
			for (int i = bpms.Length - 1; i >= 0; --i)
			{
				be = bpms[i];
				
				if (be.IsValid && lastAbsTime - be.absTime > highestTimeInterval)
				{
					highestTimeInterval = lastAbsTime - be.absTime;
					lastAbsTime = be.absTime;
					bpm = be.bpm;
				}
			}
			
			return lastAbsTime < highestAbsTime ? bpm : DEFAULT_BPM;
		}
		
		void CloseNote(MidiEvent e, float maxDuration, bool noteOn = false)
		{
			int midiChannel = e.channel;
			int midiCode = (int)e.parameter1;
			
			if (noteTracker.ContainsKey(midiChannel) && noteTracker[midiChannel].ContainsKey(midiCode))
			{
				InstrumentData instrData = instrDataChannel[midiChannel];
				bool adjustDuration = true;
				
 				if (noteTracker[midiChannel][midiCode] < instrData.notes.Count)
				{
					NoteData noteData = instrData.Notes[noteTracker[midiChannel][midiCode]];
					adjustDuration = noteOn || noteData.duration <= 0;
					
					if (instrData.hash != "Drums")
					{
						if (adjustDuration)
							noteData.duration = (float)e.absTimeBpm - noteData.deltaTimeAbs;
					}
					else noteData.duration = 0.25f; // Duração de acordo com visual míni­mo permitido (quadrado).
					
					#region TMD
					// Se a nota tiver duração zero, a remove.
					if (noteData.duration <= 0)
						instrData.notes.Remove(noteData);
					// Limita a nota a 3 segs. de duração.
					else if (noteData.duration > maxDuration)
						noteData.duration = maxDuration;
					#endregion
				}
				
				if (adjustDuration)
					noteTracker[midiChannel].Remove(midiCode);
			}
		}
		
		void CreateTrack(ref byte midiChannel, MusicData musicData, ref double absTime)
		{
			if (!instrDataChannel.ContainsKey(midiChannel))
			{
				instrDataChannel[midiChannel] = new InstrumentData();
				instrDataChannel[midiChannel].hash = midiChannel != 9 ? "GrandPiano" : "Drums";
				instrDataChannel[midiChannel].volume = 100;
				sum[midiChannel] = 0;
				tNotes[midiChannel] = 0;
				
				if (midiFile.MidiHeader.MidiFormat == MidiHelper.MidiFormat.SingleTrack)
					absTime = 0.0;
			}
			
			if (!musicData.instruments.Contains(instrDataChannel[midiChannel]))
				musicData.instruments.Add(instrDataChannel[midiChannel]);
		}
		
		void Log(string msg, bool print = false)
		{
			if (print)
				Debug.Log(msg + "\n");
		}
	}
}