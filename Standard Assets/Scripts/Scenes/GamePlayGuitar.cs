using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using DataSet;

public class GamePlayGuitar : GamePlay
{
	const int TOTAL_OCTAVES = 8;
	const int TOTAL_KEYS = 6;
	
	struct NoteHash
	{
		public NoteData nd;
		public string hash;
		
		public NoteHash(NoteData nd, string hash)
		{
			this.nd = nd;
			this.hash = hash;
		}
	}
	
	struct NotePair
	{
		public string hash1;
		public NoteHash note2;
		
		public NotePair(string hash1, NoteHash note2)
		{
			this.hash1 = hash1;
			this.note2 = note2;
		}
	}
	
	public Transform[] notes;
	public Transform[] keys;
	public ParticleSystem[] sparks;
	
	string[] noteNames = {"Do","Do#","Re","Re#","Mi","Fa","Fa#","Sol","Sol#","La","La#","Si"};
	
	//int[] keyIds = {1,2,4,8,16,24,12,6,14,10,20,28,32,48,56,40,60,30};
	//int[] keyIds = {1,4,8,16,32,48,24,12,28,56,40,20,60,2,6,10,14,30,36,44,52,18,22,26};
	//int[] keyIds = {1,4,8,12,24,16,48,32,56,28,20,40,2,6,14,10,30,60,36,44,52,18,22,26};
	//int[] keyIds = {1,4,12,8,24,16,48,32,56,28,20,40,60,36,44,52,2,6,14,10,18,22,26,30};
	int[] keyIds = {1,4,8,16,32,12,24,48,40,56,20,28,60,44,36,52,6,2,10,14,30,26,18,22};
	float confNotesLimitPerc = 80f;
	int confNotesLimit = 8;
	int confOrderBy = 0;
	float confMinInterval = .6f;
	
	List<List<NoteData>> groupedNotes;
	Dictionary<int, int> keyIdIndexes;
	Dictionary<int, int> midiCodeKeyIds;
	string[] midiHashes = {};
	Transform texPlaneTmpl;
	List<Texture2D> noteTexs;
	
	Vector3[] keyPositions = new Vector3[TOTAL_KEYS];
	Quaternion[] keyRotations = new Quaternion[TOTAL_KEYS];
	Vector3[] keyScales = new Vector3[TOTAL_KEYS];
	
	// :: AUTO ::
	protected override void Awake()
	{
		base.Awake();
		
		TryAltConfig();
		
		notesContainer.localPosition = new Vector3(-3.9f, 0, 0);
		
		noteTexs = new List<Texture2D>();
		
		/*foreach (string name in noteNames)
			for (int j = 1; j <= TOTAL_OCTAVES; ++j)
				noteTexs.Add(((Texture2D)Resources.Load("Textures/Notes/" + name + j)));*/
		
		foreach (string name in noteNames)
			noteTexs.Add(((Texture2D)Resources.Load("Textures/Notes/" + name)));
		
		texPlaneTmpl = ((GameObject)Resources.Load("Models/PlaneT")).transform;
		
		int i = 0;
		
		keyIdIndexes = keyIds.ToDictionary(k => k, v => i++);
		
		i = 0;
		
		foreach (Transform key in keys)
		{
			keyPositions[i] = key.position;
			keyRotations[i] = key.rotation;
			keyScales[i] = key.localScale;
			++i;
		}
	}
	
	// :: CUSTOM ::
	public override void OpenTrack(InstrumentData track)
	{
		base.OpenTrack(track);
		
		MusicData musicData = Game.Instance.MusicData;
		Dictionary<string, int> midiCodesKeyIds = GetKeyBindings();
		Quaternion rot = Quaternion.Euler(70f, 0f, 180f);
		Vector3 noteNameScale = new Vector3(1.3f, 1f, 1.3f);
		int totalNotes = track.notes.Count;
		float minDist = .6f;
		float chordThreshold = musicData.TimeToBeats(CHORD_THRESHOLD);
		float minDistNote = musicData.BeatsToMeasure(track.notes.Where(nd => nd.deltaTimeRel > chordThreshold).Min(nd => nd.deltaTimeRel));
		float maxBpm = 200f;
		
		Vector3 pos = Vector3.zero;
		Transform texPlane;
		NoteData noteData;
		Transform note = null;
		string hash;
		int i = 0;
		int c;
		int n;
		
		// Se a nota de menor distância for menor que o mínimo permitido, reescala as distâncias.
		if (minDistNote < minDist)
			MusicScale = minDist/minDistNote;
		
		if (musicData.bpm*MusicScale > maxBpm)
			MusicScale = maxBpm/musicData.bpm;
		
		midiHashes = midiCodesKeyIds.Keys.ToArray();
		TotalScore = 0;
		
		ConfigInstrument();
		
		while (i < totalNotes)
		{
			noteData = sortedNotes[i];
			hash = GetMidiHash(sortedNotes, ref i);
			n = midiCodesKeyIds[hash];
			
			// Define o total de notas a tocar considerando os agrupamentos.
			++TotalScore;
			
			if (n != 0)
			{
				// Instancia as teclas correspondentes às notas.
				for (int j = 0; j < TOTAL_KEYS; ++j)
				{
					if ((n & 1 << j) != 0)
					{
						note = (Transform)Instantiate(notes[j], keyPositions[j], keyRotations[j]);
						note.parent = notesContainer;
						
						pos = note.localPosition;
						pos.y = musicData.BeatsToMeasure(noteData.deltaTimeAbs)*MusicScale;
						note.localPosition = pos;
					}
				}
				
				c = 0;
				
				// Se for bateria, não mostra o nome das notas.
				if (track.hash != "Drums")
				{
					foreach (int midiCode in hash.ToCharArray().Select(ch => (int)ch))
					{
						// Instancia as texturas correspondentes às notas.
						texPlane = (Transform)Instantiate(texPlaneTmpl, note.position, rot);
						texPlane.parent = notesContainer;
						//texPlane.renderer.material.mainTexture = noteTexs[noteIndex%12*TOTAL_OCTAVES + octave];
						texPlane.renderer.material.mainTexture = noteTexs[midiCode%12];
						pos = texPlane.position;
						pos.x = 4.3f + c++*noteNameScale.x;
						texPlane.position = pos;
						texPlane.localScale = noteNameScale;
						texPlane.gameObject.layer = LayerMask.NameToLayer("notas");
					}
				}
			}
		}
	}
		
	public void ConfigInstrument()
	{
		SoundEngine.CurrentInstance.SetConfig(Game.Instance.Device.currentDeviceID,
			SoundEngine.CurrentInstance.GetSampleSetIndex(currentTrack.hash),
			midiHashes.Select(mc => mc.ToCharArray().Select(ch => (int)ch).ToArray()).ToArray());
	}
	
	void TryAltConfig()
	{
		string confPath = "GamePlayGuitar.txt";
		
		if (File.Exists(confPath))
		{
			string[] lines = File.ReadAllLines(confPath);
			
			foreach (string line in lines)
			{
				string[] kv = line.Split('=');
				
				if (kv.Length == 2)
				{
					switch (kv[0].ToLower())
					{
					case "limiteperc":
						confNotesLimitPerc = Convert.ToSingle(kv[1].Replace(',', '.'));
						break;
					case "limitenotas":
						confNotesLimit = Convert.ToInt32(kv[1]);
						break;
					case "ordem":
						confOrderBy = Convert.ToInt32(kv[1]);
						break;
					case "intervalominino":
						confMinInterval = Convert.ToSingle(kv[1].Replace(',', '.'));
						break;
					}
				}
			}
			
			int n;
			
			int[] keyIdsTmp = lines.
				Where(line => line != "" && int.TryParse(line, out n)).
				Select(line => int.Parse(line)).ToArray();
			
			if (keyIdsTmp.Length == keyIds.Length)
				keyIds = keyIdsTmp;
		}
	}
	
	const float CHORD_THRESHOLD = 0.05f; // s
	
	string GetMidiHash(NoteData[] ndList, ref int i)
	{
		float chordThreshold = Game.Instance.MusicData.TimeToBeats(CHORD_THRESHOLD);
		List<int> midiCodes = new List<int>();
		
		do midiCodes.Add(ndList[i].midiCode);
		while (++i < ndList.Length && ndList[i].deltaTimeRel <= chordThreshold);
		
		midiCodes.Sort();
		
		return midiCodes.Select(n => System.Convert.ToChar(n).ToString()).
			Aggregate((a, b) => a + b);
	}
	
	/**
	 * Associa hashes de códigos MIDI com os IDs das teclas.
	 */
	Dictionary<string, int> GetKeyBindings()
	{
		int i = 0;
		int j = 0;
		
		// Ordem de frequência
		/*return sortedNotes.
			GroupBy(nd => i < sortedNotes.Length ? GetMidiHash(sortedNotes, ref i) : "").
			OrderByDescending(g => g.Count()).
			Select(g => g.Key).
			ToDictionary(hash => hash, hash => j < keyIds.Length ? keyIds[j++] : 0);*/
		
		float tsMin = Game.Instance.MusicData.TimeToBeats(confMinInterval);
		List<NoteHash> hashes = new List<NoteHash>();
		
		while (i < sortedNotes.Length)
			hashes.Add(new NoteHash(sortedNotes[i], GetMidiHash(sortedNotes, ref i)));
		
		var notesFreq = hashes.GroupBy(nh => nh.hash).
			OrderByDescending(g => g.Count());
		
		// X% das notas.
		int limitNotes = (int)(hashes.Count*confNotesLimitPerc/100f);
		
		i = 0;
		
		int currentNotes = 0;
		
		// Pega até X ou até X% das notas.
		List<string> g1 = notesFreq.
			Where(g => (currentNotes += g.Count()) - g.Count() < limitNotes && i++ < confNotesLimit).
			Select(g => g.Key).
			ToList();
		
		string lastHash = null;
		List<NotePair> notePairs = new List<NotePair>();
		//string t = "Anterior;Corrente;Intervalo\r\n";
		
		// Coleciona pares de notas com intervalo abaixo da
		// tolerância mínima de tempo, e que a primeira nota do par
		// pertença ao primeiro grupo (prioridades) e não seja
		// a primeira nota do grupo (barra - combinação fácil).
		foreach (NoteHash nh in hashes)
		{
			if (nh.hash != g1.First())
			{
				if (g1.Any(h => nh.hash == h))
					lastHash = nh.hash;
				else if (lastHash != null)
				{
					if (nh.nd.deltaTimeRel < tsMin)
					{
						notePairs.Add(new NotePair(lastHash, nh));
						//t += lastHash + ";" + nh.hash + "=" + noteNames[nh.nd.midiCode%12] + " " + nh.nd.midiCode/12 + ";" + nh.nd.deltaTimeRel.ToString().Replace('.', ',') + "\r\n";
					}
					
					lastHash = null;
				}
			}
		}
		
		/*File.WriteAllText("duplas_criterios.csv", t);
		t = "Anterior;Corrente;Intervalo\r\n";
		
		foreach (NoteHash nh in hashes)
		{
			if (lastHash != null)
				t += lastHash + ";" + nh.hash + "=" + noteNames[nh.nd.midiCode%12] + " " + nh.nd.midiCode/12 + ";" + nh.nd.deltaTimeRel.ToString().Replace('.', ',') + "\r\n";
			
			lastHash = nh.hash;
		}
		
		File.WriteAllText("duplas.csv", t);*/
		
		var harderNotes = notePairs.
			GroupBy(n => n.note2.hash).
			OrderBy(g => confOrderBy == 0 ? g.Count() : g.Min(n => n.note2.nd.deltaTimeRel)).
			ThenBy(g => g.Count()).
			Select(n => n.Key);
		
		// Notas que não sejam a primeira e que estejam fora dos dois primeiros grupos.
		var easierNotes = notesFreq.
			Where(g => g.Key != g1.First() && g1.All(h => g.Key != h) && harderNotes.All(h => g.Key != h)).
			Select(g => g.First().hash);
		
		return g1.Concat(harderNotes.Concat(easierNotes)).
			ToDictionary(hash => hash, hash => j < keyIds.Length ? keyIds[j++] : 0);
	}
	
	// :: RECEIVERS ::
	public void KeyDownReceiver(int n, bool playSound = true)
	{
		// Se houver alguma tecla pressionada, remove o indicador de barra.
		if (n > 1)
			n ^= 1;
		
		if (keyIdIndexes.ContainsKey(n) == false)//&& keyIdIndexes[n] < Game.Instance.Device.TotalKeys
			return;

		int keyIdIndex = keyIdIndexes[n];

		if (playSound)
			SoundEngine.CurrentInstance.PlayNoteByIndex(keyIdIndex, Game.Instance.Device.currentDeviceID);
		
		if (keyIdIndex >= 0 && keyIdIndex < midiHashes.Length)
		{
			string midiHash = midiHashes[keyIdIndex];
			NoteData nd;
			float noteTime;
			int i = lastNoteIndex;
			
			// Verificação de resposta
			while (i < sortedNotes.Length)
			{
				nd = sortedNotes[i];
				noteTime = Game.Instance.MusicData.BeatsToTime(nd.deltaTimeAbs + HIT_AREA_CORRECTION);
				
				// Se a nota estiver dentro do tempo.
				if (noteTime + timeHalfLimit >= time)
				{
					if (noteTime - timeHalfLimit <= time)
					{
						// Se a nota estiver correta.
						if (midiHash == GetMidiHash(sortedNotes, ref i))
						{
							++Score;
							
							for (int j = 0; j < TOTAL_KEYS; ++j)
								if ((n & 1 << j) != 0)
									//Instantiate(sparks[j], keyPositions[j], Quaternion.identity);
									sparks[j].Play();
						}
						
						lastNoteIndex = i;
					}
					break;
				}
				
				++i;
			}
		}
		else Debug.Log("Invalid key!");
	}
}
