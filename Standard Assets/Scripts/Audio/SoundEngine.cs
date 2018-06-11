using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Linq;
using DataSet;
using Utils;

[RequireComponent(typeof(AudioListener), typeof(AudioSource))]
public class SoundEngine : MonoBehaviour
{
	const float SHORT_FLOAT_FACTOR = 1f/0x7fff;
	const int THREE_SECS_BYTES = 44100*4*3;
	const float AMP_LIMIT = 3f;
	
	static SoundEngine currentInstance;
	
	[DllImport(Common.SOUND_ENGINE)]
	extern static private bool tmdToWave(string tmdPath, string wavePath, float duration = 0.0f);
	
	[DllImport(Common.SOUND_ENGINE)]
	extern static private int waveToMp3(string wavePath, string mp3Path);
	
	[DllImport(Common.PLS)]
	extern static private bool Inicia(IntPtr alphaNum, IntPtr num, int flagHabTeclado = 1);
	
	[DllImport(Common.PLS)]
	extern static private bool Finaliza();
	
	[DllImport(Common.PLS)]
	extern static private bool Tocar(int note, int kbArea, bool flagMute = false);
	
	DeviceConfig[] deviceConfigs;
	List<SampleSet> sampleSets;
	int trackToPlay = -1;
	bool isPlaying = false;
	bool isRecActive = false;
	bool isKeysEnabled = false;
	bool isEnabled = true;
	bool firstTime = true;
	bool initialized;
	
	[StructLayout(LayoutKind.Explicit, Size=2)]
	struct Sample
	{
		[FieldOffset(0)]
		public byte data_byte0;
		
		[FieldOffset(1)]
		public byte data_byte1;
		
		[FieldOffset(0)]
		public short data_short;
	}
	
	public static SoundEngine CurrentInstance
	{
		get { return currentInstance ?? (new GameObject("SoundEngine")).AddComponent<SoundEngine>(); }
	}
	
	public bool IsPlaying
	{
		get { return this.isPlaying; }
	}
	
	public bool IsRecording
	{
		get { return this.isRecActive && this.isPlaying; }
	}
	
	public bool IsRecActive
	{
		get { return this.isRecActive; }
	}
	
	public float MainVolume { get; set; }
	
	public bool FirstTime
	{
		get { return firstTime; }
		set { firstTime = value; }
	}
	
	public int TrackToPlay
	{
		get { return this.trackToPlay; }
		set { trackToPlay = value; }
	}
	
	public bool Enabled
	{
		get { return isEnabled; }
		set
		{
			if (!initialized)
				return;
			
			if (value != isEnabled)
			{
				isEnabled = value;
				
				for (int i = 0; i < this.deviceConfigs.Length; ++i)
					this.LoadDeviceSamples(i, this.deviceConfigs[i].SampleSetIndex, true);
			}
		}
	}
	
	public bool KeysEnabled
	{
		get { return isKeysEnabled; }
		set
		{
			isKeysEnabled = value;
			
			if (!initialized)
				return;
				
			Inicia(deviceConfigs[0].SamplesPtr, deviceConfigs[1].SamplesPtr, Convert.ToInt32(isKeysEnabled));
		}
	}
	
	//Construtores
	void Awake()
	{
		if (currentInstance != null)
			throw new Exception("SoundEngine: Instance already created!");
		
		DontDestroyOnLoad(currentInstance = this);
		
		MainVolume = 1f;
		
		AudioSettings.outputSampleRate = INPUT_SAMPLE_RATE;
		AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
		//AudioSettings.SetDSPBufferSize(bufferSize = 1024, numBuffers = 2);
		
		Initialize();
		InitializeDeviceSounds();
	}
	
	/*void OnApplicationFocus(bool focus)
	{
		switch (Application.loadedLevelName)
		{
		case Common.SCENE_GUITAR:
		case Common.SCENE_REAL:
		case Common.SCENE_PIANO:
			KeysEnabled = focus;
			break;
		}
	}
	
	void OnApplicationQuit()
	{
		for (int i = 0; i < this.deviceConfigs.Length; ++i)
			Marshal.FreeHGlobal(deviceConfigs[i].SamplesPtr);
		
		initialized = false;
		
		Application.Quit();
	}*/
	
	public void PlayNote(int midiCode, int deviceIndex, bool flagMute = false)
	{
		PlayNoteByIndex(midiCode - GetBaseNote(sampleSets[deviceConfigs[deviceIndex].SampleSetIndex]), deviceIndex, flagMute);
	}
	
	public void PlayNoteByIndex(int index, int deviceIndex, bool flagMute = false)
	{
		bool isFirstDev = deviceIndex == 0;
		
		if (index >= 0 && index < Device.TotalKeysAll[0])
			Tocar(index + (isFirstDev ? 60 : 0), isFirstDev ? 2 : deviceIndex, flagMute);
	}
	
	public void SetInstrument(int deviceIndex, int sampleSetIndex, InstrumentData config = null, bool reload = false)
	{
		if (HasDevice(deviceIndex) && HasSampleSet(sampleSetIndex))
		{
			sampleSets[sampleSetIndex].instrData = config ?? new InstrumentData();
			
			LoadDeviceSamples(deviceIndex, sampleSetIndex, reload);
		}
	}
	
	public void SetInstrument(int deviceIndex, string sampleSetHash, InstrumentData config = null, bool reload = false)
	{
		this.SetInstrument(deviceIndex, this.GetSampleSetIndex(sampleSetHash), config, reload);
	}
	
	public int GetInstrument(int deviceIndex)
	{
		return this.HasDevice(deviceIndex) ? this.deviceConfigs[deviceIndex].SampleSetIndex : 0;
	}
	
	/**
	 * Retorna o SampleSet do instrumento selecionado
	 * no dispositivo especificado.
	 */
	public SampleSet GetSampleSet(int deviceIndex)
	{
		return HasDevice(deviceIndex) ? this.sampleSets[this.deviceConfigs[deviceIndex].SampleSetIndex] : null;
	}
	
	public SampleSet GetSampleSet(string hash)
	{
		return this.sampleSets.First(ss => ss.Hash == hash);
	}
	
	public List<SampleSet> GetSampleSets()
	{
		return this.sampleSets;
	}
	
	public SampleSet GetSampleSetAt(int index)
	{
		return HasSampleSet(index) ? sampleSets[index] : null;
	}
	
	public int GetSampleSetIndex(string hash)
	{
		return sampleSets.IndexOf(GetSampleSet(hash));
	}
	
	const string EXC_INVALID_DEVICE_INDEX = "Invalid \"deviceIndex\".";
	
	/**
	 * Altera o volume de uma tecla do dispositivo especificado ou de todas as teclas (keyIndex=-1).
	 */
	public void SetVolume(int deviceIndex, float volume, int keyIndex = -1, bool wait = false)
	{
		if (HasDevice(deviceIndex))
		{
			if (volume < 0 || float.IsNaN(volume))
				volume = 0;
			
			int totalKeys = Device.TotalKeysAll[deviceIndex];
			
			if (keyIndex == -1)
				for (int i = 0; i < totalKeys; ++i)
					deviceConfigs[deviceIndex].Volumes[i] = volume;
			else if (keyIndex < totalKeys)
				deviceConfigs[deviceIndex].Volumes[keyIndex] = volume;
			
			if (!wait)
				LoadDeviceSamples(deviceIndex, deviceConfigs[deviceIndex].SampleSetIndex, true);
		}
		else throw new Exception(EXC_INVALID_DEVICE_INDEX);
	}
	
	public void ApplyVolume(int deviceIndex)
	{
		if (HasDevice(deviceIndex))
			LoadDeviceSamples(deviceIndex, deviceConfigs[deviceIndex].SampleSetIndex, true);
		else throw new Exception(EXC_INVALID_DEVICE_INDEX);
	}
	
	public float[] GetVolume(int deviceIndex)
	{
		if (HasDevice(deviceIndex))
			return deviceConfigs[deviceIndex].Volumes;
		else throw new Exception(EXC_INVALID_DEVICE_INDEX);
	}
	
	public void SetConfig(int deviceIndex, int sampleSetIndex, int[] midiCodes)
	{
		SetConfig(deviceIndex, sampleSetIndex, midiCodes.Select(n => new int[]{n}).ToArray());
	}
	
	public void SetConfig(int deviceIndex, int sampleSetIndex, int[][] midiCodes)
	{
		if (this.HasDevice(deviceIndex) && this.HasSampleSet(sampleSetIndex))
		{
			sampleSets[sampleSetIndex].DeviceMap[deviceIndex] = midiCodes;
			LoadDeviceSamples(deviceIndex, sampleSetIndex, true);
		}
	}
	
	public void Clear()
	{
		//this.sampleSets.Clear();
		foreach (SampleSet ss in sampleSets)
			ss.DeviceMap.Clear();
		
		audioMapping.Clear();
		firstTime = true;
	}
	
	//função para armar para gravação
	public void ArmRecord() {
		isRecActive = true;
	}
	
	//função para desarmar a gravação
	public void UnarmRecord() {
		isRecActive = false;
	}
	
	#region PLAY (VARS)
	const int INPUT_SAMPLE_RATE = 44100;
	const int INPUT_MAX_SEC = 3;
	const int INPUT_CHANNELS = 2;
	const int INPUT_BLOCK_ALIGN = 4;
	const float FADE_OUT_TIME = 0.2f;
	
	int inputMaxSamples;
	int inputMaxBytes;
	int fadeOutSamples = (int)(FADE_OUT_TIME*INPUT_SAMPLE_RATE);
	int fadeOutChsSamples;
	bool isBuffering = false;
	MusicData musicData;
	InstrumentData instr;
	NoteData note;
	SampleSet sampleSet;
	InstrumentData[] instrs;
	NoteData[] notes;
	float length = 0f;
	int[] noteIndexes; // Controle para evitar repetiÃ§Ã£o de leitura das notas.
	float[][] audioData = new float[2][];
	byte[] noteAudioData;
	Dictionary<string, Dictionary<int, float[]>> audioMapping = new Dictionary<string, Dictionary<int, float[]>>();
	Dictionary<string, int> instrsDiffCounter;
	Dictionary<int, int> notesDiffCounter;
	float[] audioMapData;
	string audioPath;
	float bpm;
	int bufferSize;
	int bufferChsSize;
	int blockBufferChsSize;
	int numBuffers;
	long bufferPos = 0L;
	long notePos;
	long posRel;
	long fadeOutChsPos;
	float amp;
	//float ampFactor = 60f/1.27f/10f;
	float velocity;
	int dur;
	int durBytes;
	int noteIndex;
	int bufferIndex;
	int lastBufferIndex;
	int i;
	int j;
	int s;
	#endregion
	
	//função para iniciar.
	public void Play(MusicData musicData)
	{
		if (!isPlaying)
		{
			this.musicData = musicData;
			length = musicData.GetMusicDurationTime();
			bpm = musicData.bpm;
			
			isBuffering = true;
			isPlaying = true;
			lastTime = 0f;
			
			if (!Network.isClient)
			{
				inputMaxSamples = INPUT_SAMPLE_RATE*INPUT_MAX_SEC;
				inputMaxBytes = inputMaxSamples*INPUT_BLOCK_ALIGN;
				
				// (...) Migrar para inicialização.
				audioData[0] = new float[blockBufferChsSize = (inputMaxSamples + bufferSize)*INPUT_CHANNELS];
				audioData[1] = new float[blockBufferChsSize];
				noteIndexes = new int[musicData.instruments.Count];
				
				bufferIndex = 0;
				
				for (s = 0; s < blockBufferChsSize; ++s)
					audioData[0][s] = audioData[1][s] = 0f;
				
				#region MAPEAMENTO
				instrsDiffCounter = new Dictionary<string, int>();
				
				for (i = 0; i < musicData.instruments.Count; ++i)
				{
					instr = (InstrumentData)musicData.instruments[i];
					
					if (!instr.muted)
					{
						if (!audioMapping.ContainsKey(instr.hash))
							audioMapping[instr.hash] = new Dictionary<int, float[]>();
						
						if (instrsDiffCounter.ContainsKey(instr.hash))
							++instrsDiffCounter[instr.hash];
						else instrsDiffCounter[instr.hash] = 1;
						
						audioPath = Common.INSTRUMENT_DIR + instr.hash + ".drm";
						
						if (File.Exists(audioPath))
						{
							using (FileStream fs = new FileStream(audioPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
							{
								notes = instr.Notes;
								notesDiffCounter = new Dictionary<int, int>();
	
								for (j = 0; j < notes.Length; ++j)
								{
									note = notes[j];
									sampleSet = GetSampleSet(instr.hash);
									
									// (...) Descobrir porque algumas notas veem com cellData null.
									if (note.cellData != null)
										note.midiCode = note.cellData.col + (sampleSet.Octave + instr.octave)*12 + instr.transpose;
									
									noteIndex = note.midiCode - sampleSet.Lowest;
									
									// Se a nota estiver dentro dos limites do Áudio.
									if (noteIndex >= 0 && noteIndex < sampleSet.Span)
									{
										//dur = (int)BeatsToSamples(note.duration) + fadeOutSamples;
										
										if (!audioMapping[instr.hash].ContainsKey(note.midiCode)/*
											|| sampleSet.Behaviour != 3
											&& dur*INPUT_CHANNELS > audioMapping[instr.hash][note.midiCode].Length*/)
										{
											/*if (sampleSet.Behaviour == 3 || dur > inputMaxSamples)
												dur = inputMaxSamples;*/
											dur = inputMaxSamples + fadeOutSamples;
											
											noteAudioData = new byte[durBytes = dur*INPUT_BLOCK_ALIGN];
											
											fs.Seek(noteIndex*inputMaxBytes, SeekOrigin.Begin);
											fs.Read(noteAudioData, 0, durBytes);
											
											dur *= INPUT_CHANNELS;
											audioMapping[instr.hash][note.midiCode] = audioMapData = new float[dur];
											
											for (s = 0; s < dur; ++s)
												audioMapData[s] = BitConverter.ToInt16(noteAudioData, s*2)*SHORT_FLOAT_FACTOR;
										}
									}
									
									if (notesDiffCounter.ContainsKey(note.midiCode))
										++notesDiffCounter[note.midiCode];
									else notesDiffCounter[note.midiCode] = 1;
								}
								
								fs.Dispose();
								fs.Close();
							}
						}
					}
				}
				
				// Se houver instrumentos sobrando, os remove.
				/*if (audioMapping.Count > instrsDiffCounter.Count) {
					foreach (KeyValuePair<string, Dictionary<int, float[]>> pair in audioMapping) {
						foreach (int midiCode in pair.Value.Keys)
							if (!notesDiffCounter.ContainsKey(midiCode))
								pair.Value.Remove(midiCode);
						
						if (!instrsDiffCounter.ContainsKey(pair.Key))
							audioMapping.Remove(pair.Key);
					}
				}*/
				#endregion
				
				audio.Play();
			}
		}
	}
	
	void OnAudioFilterRead(float[] data, int channels) {
		if (musicData == null)
			return;
		
		if (bpm != musicData.bpm)
			bufferPos = Mathf.FloorToInt(bpm/musicData.bpm*bufferPos/(float)bufferSize)*bufferSize;
		
		bpm = musicData.bpm;
		
		if (bufferPos >= BeatsToSamples(musicData.GetMusicDuration())) {
			isBuffering = false;
			
			if (IsRecording)
				return;
			else isPlaying = false;
		}
		
		bufferChsSize = data.Length;
		instrs = musicData.Instruments;
		
		for (i = 0; i < instrs.Length; ++i) {
			instr = instrs[i];
			
			if (!instr.muted) {
				notes = instr.Notes;
				sampleSet = GetSampleSet(instr.hash);
				
				// Se houver audio na memória, lê da memória.
				if (audioMapping.ContainsKey(instr.hash)) {
					for (j = noteIndexes[i]; j < notes.Length; ++j) {
						note = notes[j];
						notePos = BeatsToSamples(note.deltaTimeAbs);
						posRel = notePos - bufferPos;
						
						if (posRel >= 0) {
							if (posRel < bufferSize) {
								if (audioMapping[instr.hash].ContainsKey(note.midiCode)) {
									noteIndexes[i] = j + 1;
									audioMapData = audioMapping[instr.hash][note.midiCode];
									
									if (sampleSet.Behaviour == 3
										|| (dur = (int)BeatsToSamples(note.duration) + fadeOutSamples) > inputMaxSamples)
										dur = inputMaxSamples;
									
									dur *= INPUT_CHANNELS;
									fadeOutChsSamples = fadeOutSamples*INPUT_CHANNELS;
									fadeOutChsPos = dur - fadeOutChsSamples;
									velocity = note.velocity*instr.volume*MainVolume;//Mathf.Pow(2, ampFactor*note.velocity - 4f);
									amp = 1f;
									
									for (s = 0; s < dur; ++s) {
										if (s > fadeOutChsPos)
											amp = 1f - (s - fadeOutChsPos)/(float)fadeOutChsSamples;
										
										/*amp *= audioMapData[s]*velocity;
										
										/*if (amp > AMP_LIMIT)
											amp = AMP_LIMIT;
										else if (amp < -AMP_LIMIT)
											amp = -AMP_LIMIT;*/
										
										audioData[bufferIndex][posRel + s] += audioMapData[s]*velocity;
									}
								}
							}
							else break;
						}
					}
				}
				// Lê do arquivo.
				else {
					audioPath = Common.INSTRUMENT_DIR + instr.hash + ".drm";
					
					if (File.Exists(audioPath)) {
						#region CONSUMO
						using (FileStream fs = new FileStream(audioPath, FileMode.Open, FileAccess.Read, FileShare.Read, inputMaxBytes)) {	
						#endregion
							noteAudioData = new byte[inputMaxBytes];
							
							for (j = noteIndexes[i]; j < notes.Length; ++j) {
								note = notes[j];
								notePos = BeatsToSamples(note.deltaTimeAbs);
								posRel = notePos - bufferPos;
								
								if (posRel >= 0) {
									if (posRel < bufferSize) {
										noteIndexes[i] = j + 1;
										
										if (note.cellData != null)
											note.midiCode = note.cellData.col + (sampleSet.Octave + instr.octave)*12 + instr.transpose;
										
										noteIndex = note.midiCode - sampleSet.Lowest;
										
										// Se a nota estiver dentro dos limites do Áudio.
										if (noteIndex >= 0 && noteIndex < sampleSet.Span) {
											if (sampleSet.Behaviour == 3
												|| (dur = (int)BeatsToSamples(note.duration)) > inputMaxSamples)
												dur = inputMaxSamples;
											
											durBytes = dur*INPUT_BLOCK_ALIGN;
											
											fs.Seek(noteIndex*inputMaxBytes, SeekOrigin.Begin);
											fs.Read(noteAudioData, 0, durBytes);
											
											dur *= INPUT_CHANNELS;
											fadeOutChsSamples = fadeOutSamples*INPUT_CHANNELS;
											fadeOutChsPos = dur - fadeOutChsSamples;
											velocity = note.velocity*instr.volume*MainVolume;//Mathf.Pow(2, ampFactor*note.velocity - 4f);
											amp = 1f;
											
											#region CONSUMO
											for (s = 0; s < dur; ++s) {
												if (s > fadeOutChsPos)
													amp = 1f - (s - fadeOutChsPos)/(float)fadeOutChsSamples;
												
												amp *= BitConverter.ToInt16(noteAudioData, s*2)*SHORT_FLOAT_FACTOR*velocity*amp;
												
												if (amp > AMP_LIMIT)
													amp = AMP_LIMIT;
												else if (amp < -AMP_LIMIT)
													amp = -AMP_LIMIT;
												
												audioData[bufferIndex][posRel + s] += amp;
											}
											#endregion
										}
									}
									else break;
								}
							}
							
							fs.Dispose();
							fs.Close();
						}
					}
				}
			}
		}
		
		for (s = 0; s < bufferChsSize; ++s)
			data[s] = audioData[bufferIndex][s];
		
		lastBufferIndex = bufferIndex;
		bufferIndex = bufferIndex == 0 ? 1 : 0;
		
		for (s = bufferChsSize; s < blockBufferChsSize; ++s)
			audioData[bufferIndex][s - bufferChsSize] = audioData[lastBufferIndex][s];
		
		bufferPos += bufferSize;
	}
	
	public void Pause() {
		audio.Stop();
		Timer.Stop();
		
		isPlaying = false;
		isBuffering = false;
	}
	
	//função para parar.
	public void Stop() {
		Pause();
		
		if (isRecActive)
			UnarmRecord();
		
		bufferPos = 0;
	}
	
	// Duração total da música em segundos.
	public float GetLength() {
		return length;
	}
	
	float lastTime;
	
	//retorna tempo corrente em segundos.
	public float GetTime() {
		float curTime = bufferPos/(float)INPUT_SAMPLE_RATE;
		
		// Se ainda houver sons pra tocar.
		if (isBuffering)
			// 0.02f: Latência aproximada
			if (curTime > 0.02f && curTime <= lastTime) {
				if (!Timer.Running)
					Timer.Start(curTime);
				
				curTime = Timer.CurrentTime;
			}
			else Timer.Start(curTime);
		// Se [não houver mais sons pra tocar e] estiver gravando, usa o timer.
		else if (IsRecording) {
			if (!Timer.Running) {
				Timer.Start(curTime);
				audio.Stop();
			}
			
			curTime = Timer.CurrentTime;
		}
		
		lastTime = curTime;
		
		return IsRecording || curTime < GetLength() ? curTime : GetLength();
	}
	
	public void SetTime(float timeOffset) {
		if (timeOffset >= GetLength())
			audio.Stop();
		
		bufferPos = (long)(timeOffset*INPUT_SAMPLE_RATE);
	}
	
	// :: Private ::
	bool HasDevice(int deviceIndex) {
		return deviceIndex >= 0 && deviceIndex < this.deviceConfigs.Length;
	}
	
	bool HasSampleSet(int sampleSetIndex) {
		return sampleSetIndex >= 0 && sampleSetIndex < this.sampleSets.Count;
	}
	
	public string GetMp3Dir()
	{
		string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
		
		if (!Directory.Exists(dir))
			dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		
		if (!Directory.Exists(dir))
			dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		
		return dir;
	}
	
	/**
	 * Retorna o código MIDI da nota referente à oitava base da configuração.
	 */
	public int GetBaseNote(SampleSet sampleSet, InstrumentData instrData = null)
	{
		if (instrData == null)
			instrData = sampleSet.instrData;
		
		return (sampleSet.Octave + instrData.octave)*12 + sampleSet.Transpose + instrData.transpose;
	}
	
	public int GetBaseNote(InstrumentData instrData)
	{
		return GetBaseNote(GetSampleSet(instrData.hash), instrData);
	}
	
	long BeatsToSamples(float beats)
	{
		return (long)(musicData.BeatsToTime(beats)*INPUT_SAMPLE_RATE);
	}
	
	void Initialize()
	{
		string[] paths = Directory.GetFiles(Common.INSTRUMENT_DIR, "*.txt");
		List<SampleSet> defaultSmpSets = new List<SampleSet>();
		SampleSet sampleSet;
		string[] kvPair;
		string[] maps;
		string[] mapInfo;
		
		this.sampleSets = new List<SampleSet>();
		
		Array.Sort(paths);
		
		// Carrega todas as configurações do banco de sons.
		for (int i = 0; i < paths.Length; ++i)
		{
			sampleSet = new SampleSet();
			sampleSet.Hash = System.IO.Path.GetFileNameWithoutExtension(paths[i]);
			
			foreach (string line in File.ReadAllLines(paths[i], System.Text.Encoding.GetEncoding("iso-8859-1")))
			{
				if (line.IndexOf("#") != -1)
					continue;
				
				kvPair = line.Split('=');
				
				if (kvPair.Length == 2)
				{
					switch (kvPair[0].ToLower())
					{
					case "index":
						sampleSet.Index = Convert.ToInt32(kvPair[1]);
						break;
					case "label":
						sampleSet.Label = kvPair[1];
						break;
					case "lowest":
						sampleSet.Lowest = Convert.ToInt32(kvPair[1]);
						break;
					case "span":
						sampleSet.Span = Convert.ToInt32(kvPair[1]);
						break;
					case "octave":
						sampleSet.Octave = Convert.ToInt32(kvPair[1]);
						break;
					case "transpose":
						sampleSet.Transpose = Convert.ToInt32(kvPair[1]);
						break;
					case "behaviour":
						sampleSet.Behaviour = Convert.ToInt32(kvPair[1]);
						break;
					case "devicemap":
						maps = kvPair[1].Split(';');
						sampleSet.DeviceMap = new Dictionary<int, int[][]>();
						
						int n;
						
						foreach (string map in maps)
						{
							mapInfo = map.Split(':');
							
							if (mapInfo.Length == 2)
								sampleSet.DeviceMap[Convert.ToInt32(mapInfo[0])] = mapInfo[1].Split(',').
									Select(s => int.TryParse(s, out n) ?
										new int[]{n} :
										s.Split(';').
									Select(d => int.Parse(d)).ToArray()).ToArray();
						}
						break;
					}
				}
			}
			
			if (sampleSet.Index == -1)
				this.sampleSets.Add(sampleSet);
			else defaultSmpSets.Add(sampleSet);
		}
		
		// Ordena os sons indexados.
		defaultSmpSets.Sort(delegate(SampleSet a, SampleSet b)
		{
			return a.Index.CompareTo(b.Index);
		});
		
		// Insere os sons indexados no início da lista para destacá-los para o usuário.
		this.sampleSets.InsertRange(0, defaultSmpSets);
	}
	
	void InitializeDeviceSounds()
	{
		DeviceConfig deviceConfig;
		int totalKeys;
		int[] instrsInit = {GetSampleSetIndex("GrandPiano"), GetSampleSetIndex("Drums")};
		
		this.deviceConfigs = new DeviceConfig[Device.TotalDevices];
		
		// Aloca memória para os ponteiros dos bancos de sons dos dispositivos.
		for (int i = 0; i < this.deviceConfigs.Length; ++i)
		{
			totalKeys = Device.TotalKeysAll[i];
			
			deviceConfig = new DeviceConfig();
			deviceConfig.SamplesPtr = Marshal.AllocHGlobal(totalKeys*THREE_SECS_BYTES);
			deviceConfig.Volumes = new float[totalKeys];
			
			for (int j = 0; j < totalKeys; ++j)
				deviceConfig.Volumes[j] = 1f;
			
			this.deviceConfigs[i] = deviceConfig;
			
			LoadDeviceSamples(i, instrsInit[i], true);
		}
		
		//Inicia(this.deviceConfigs[0].SamplesPtr, this.deviceConfigs[1].SamplesPtr);
		
		initialized = true;
		KeysEnabled = false;
	}
	
	void LoadDeviceSamples(int deviceIndex, int sampleSetIndex, bool reload = false)
	{
		DeviceConfig deviceConfig = this.deviceConfigs[deviceIndex];
		bool hasInstrChanged = deviceConfig.SampleSetIndex != sampleSetIndex;
		
		// Se tiver ocorrido mudança de instrumento no dispositivo
		// em questão ou se for para recarregar tudo incondicionalmente.
		if (hasInstrChanged || reload)
		{
			deviceConfig.SampleSetIndex = sampleSetIndex;
			
			int totalKeysBytes = Device.TotalKeysAll[deviceIndex]*THREE_SECS_BYTES;
			List<byte> samples = new List<byte>();
			
			// Se não for para preencher com silêncio e for um í­ndice de instrumento válido, carrega o banco de sons.
			if (isEnabled && sampleSetIndex >= 0 && sampleSetIndex < this.sampleSets.Count)
			{
				SampleSet sampleSet = this.sampleSets[sampleSetIndex];
				string audioPath = Common.INSTRUMENT_DIR + sampleSet.Hash + ".drm";
				
				// Se existerem os arquivos de áudio.
				if (File.Exists(audioPath))
				{
					// Lê o banco de sons.
					// Se a nota inicial estiver abaixo do limite inferior do banco de sons,
					// lê a partir do iní­cio; se estiver acima do limite superior, lê até o fim somente;
					// em ambos os casos, preenche a área vazia com silêncio.
					using (FileStream fs = File.OpenRead(audioPath))
					{
						int[][] deviceMap = {};
						
						if (sampleSet.DeviceMap.ContainsKey(deviceIndex))
							deviceMap = sampleSet.DeviceMap[deviceIndex];
						
						int totalConfigSamples = deviceMap.Length;
						int sampleBytesTotal = 0;
						byte[] samplesBuffer = {};
						Sample[] smps = {default(Sample), default(Sample)};
						bool silenceBegin = false;
						
						// Se não houver configuração customizada.
						if (totalConfigSamples == 0)
						{
							int sampleBytesOffset = (GetBaseNote(sampleSet) - sampleSet.Lowest)*THREE_SECS_BYTES;
							int sampleBytesSpan = sampleSet.Span*THREE_SECS_BYTES;
							
							sampleBytesTotal = totalKeysBytes;
							
							// Se precisar preencher com silêncio no iní­cio, reajusta o total.
							if (sampleBytesOffset < 0)
							{
								silenceBegin = true;
								sampleBytesTotal += sampleBytesOffset;
								sampleBytesOffset = 0;
							}
							
							if (sampleBytesSpan - sampleBytesOffset < totalKeysBytes)
								sampleBytesTotal = sampleBytesSpan - sampleBytesOffset;
							
							if (sampleBytesTotal > 0)
							{
								samplesBuffer = new byte[sampleBytesTotal];
								
								fs.Seek(sampleBytesOffset, SeekOrigin.Begin);
								fs.Read(samplesBuffer, 0, sampleBytesTotal);
							}
						}
						else
						{
							/*byte[] samplesBufferTemp = new byte[THREE_SECS_BYTES];
							short[] samplesBufferShort = new short[sampleBytesTotal/2];
							short[] samplesBufferShortTemp = new short[THREE_SECS_BYTES/2];
							//Sample sample = default(Sample);
							//Sample sampleTemp = default(Sample);
							int sampleIndex;
							
							samplesBuffer = new byte[sampleBytesTotal];
							
							// Carrega os samples de acordo com as configurações.
							for (int i = 0; i < totalSamples; ++i)
							{
								sampleIndex = deviceMap[i][0] - sampleSet.Lowest;
								
								fs.Seek(sampleIndex*THREE_SECS_BYTES, SeekOrigin.Begin);
								fs.Read(samplesBufferTemp, 0, THREE_SECS_BYTES);
							}*/
							
							sampleBytesTotal = totalConfigSamples*THREE_SECS_BYTES;
							
							byte[][] samplesBufferTemp = {new byte[THREE_SECS_BYTES], new byte[THREE_SECS_BYTES]};
							int sampleIndex;
							
							samplesBuffer = new byte[sampleBytesTotal];
							
							for (int i = 0; i < totalConfigSamples; ++i)
							{
								Array.Clear(samplesBufferTemp[1], 0, THREE_SECS_BYTES);
								
								// Percorre as notas em conjunto.
								for (int j = 0; j < deviceMap[i].Length; ++j)
								{
									sampleIndex = deviceMap[i][j] - sampleSet.Lowest;
									
									if (sampleIndex < 0)
										sampleIndex = 0;
									else if (sampleIndex >= sampleSet.Span)
										sampleIndex = sampleSet.Span - 1;
									
									fs.Seek(sampleIndex*THREE_SECS_BYTES, SeekOrigin.Begin);
									fs.Read(samplesBufferTemp[0], 0, THREE_SECS_BYTES);
									
									for (int s = 0; s < THREE_SECS_BYTES; s += 2)
									{
										/*sample = BitConverter.GetBytes(BitConverter.ToInt16(samplesBufferTemp[0], s) +
											BitConverter.ToInt16(samplesBufferTemp[1], s));
										samplesBufferTemp[1][s] = sample[0];
										samplesBufferTemp[1][s + 1] = sample[1];*/
										
										smps[1].data_byte0 = samplesBufferTemp[0][s];
										smps[1].data_byte1 = samplesBufferTemp[0][s + 1];
										smps[0].data_byte0 = samplesBufferTemp[1][s];
										smps[0].data_byte1 = samplesBufferTemp[1][s + 1];
										
										smps[0].data_short += smps[1].data_short;
										
										samplesBufferTemp[1][s] = smps[0].data_byte0;
										samplesBufferTemp[1][s + 1] = smps[0].data_byte1;
									}
									
									Array.Copy(samplesBufferTemp[1], 0, samplesBuffer, i*THREE_SECS_BYTES, THREE_SECS_BYTES);
								}
							}
						}
						
						int totalSamplesShort = sampleBytesTotal/THREE_SECS_BYTES;
						
						// Configura o volume dos samples.
						for (int i = 0, s = 0; i < totalSamplesShort; ++i)
						{
							if (deviceConfig.Volumes[i] != 1f)
							{
								s = i*THREE_SECS_BYTES;
								
								for (int sTotal = s + THREE_SECS_BYTES; s < sTotal; s += 2)
								{
									/*sample = BitConverter.GetBytes((short)(BitConverter.ToInt16(samplesBuffer, s)*deviceConfig.Volumes[i]));
									samplesBuffer[s] = sample[0];
									samplesBuffer[s + 1] = sample[1];*/
									
									smps[0].data_byte0 = samplesBuffer[s];
									smps[0].data_byte1 = samplesBuffer[s + 1];
									
									smps[0].data_short = (short)(smps[0].data_short*deviceConfig.Volumes[i]);
									
									samplesBuffer[s] = smps[0].data_byte0;
									samplesBuffer[s + 1] = smps[0].data_byte1;
								}
							}
						}
						
						// Se faltar som no iní­cio do vetor, preenche com silêncio.
						if (silenceBegin)
							samples.AddRange(new byte[totalKeysBytes - sampleBytesTotal]);
						
						samples.AddRange(samplesBuffer);
						
						fs.Close();
						fs.Dispose();
					}
				}
			}
			
			int silenceSize = totalKeysBytes - samples.Count;
			
			// Se faltar som no fim do dispositivo, preenche com silêncio.
			if (silenceSize > 0)
				samples.AddRange(new byte[silenceSize]);
			
			Marshal.Copy(samples.ToArray(), 0, deviceConfig.SamplesPtr, samples.Count);
			samples.Clear();
		}
	}
}