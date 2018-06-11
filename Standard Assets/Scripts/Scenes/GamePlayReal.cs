using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSet;

public class GamePlayReal : GamePlay
{
	public Transform noteTmpl;
	
	protected static readonly int[] naturalIndexes = {0,0,1,1,2,3,3,4,4,5,5,6};

	static readonly string[] noteAccidents = {"","#","","#","","","#","","#","","#",""};
	static readonly string[] noteNames = {"Dó","Dó#","Ré","Ré#","Mi","Fá","Fá#","Sol","Sol#","Lá","Lá#","Si"};
	static readonly string[] noteSymbols = {"C","C#","D","D#","E","F","F#","G","G#","A","A#","B"};
	
	protected int baseNote;
	protected Dictionary<NoteData, Transform> noteMap = new Dictionary<NoteData, Transform>();
	
	protected bool IgnoreTransposeVisual { get; set; }
	protected bool UseSymbols { get; set; }
	protected bool NaturalOnly { get; set; }
	protected bool AccidentOnly { get; set; }
	
	// :: CUSTOM ::
	public override void OpenTrack(InstrumentData track)
	{
		base.OpenTrack(track);
		
		UseSymbols = Menu.useSymbols;
		
		int transposeVisual = IgnoreTransposeVisual ? 0 : track.transposeVisual;
		
		//baseNote = SoundEngine.CurrentInstance.GetBaseNote(track);
		baseNote = track.notes.Min(nd => nd.midiCode + transposeVisual)/12*12;
		
		int totalNotes = NaturalOnly ? Device.TotalKeysAll[0] - Device.TotalKeysAll[0]/12*5 : Device.TotalKeysAll[0];
		float halfNotes = totalNotes/2 + 0.5f;
		Vector3 pos;
		Transform note;
		Material mat;
		TextMesh label;
		int noteIndex;
		int col;
		
		foreach (NoteData noteData in sortedNotes)
		{
			col = noteData.midiCode + transposeVisual - baseNote;
			noteIndex = col%12;
			
			note = (Transform)Instantiate(noteTmpl);
			note.parent = notesContainer;
			
			pos = note.localPosition;
			pos.x = (NaturalOnly ? naturalIndexes[noteIndex] + col/12*7 : col) - halfNotes;
			pos.y = Game.Instance.MusicData.BeatsToMeasure(noteData.deltaTimeAbs);
			note.localPosition = pos;
			
			noteMap[noteData] = note;
			
			mat = note.FindChild("Shape").renderer.material;
			label = note.GetComponentInChildren<TextMesh>();
			label.text = AccidentOnly ? noteAccidents[noteIndex] :
				(UseSymbols ? noteSymbols[noteIndex] : noteNames[noteIndex]);
			label.fontSize = AccidentOnly ? 40 : 26;

			switch (naturalIndexes[noteIndex])
			{
			case 1:
			case 5:
				label.color = Color.white;
				break;
			}

			mat.mainTextureOffset = new Vector2(naturalIndexes[noteIndex]/8f, mat.mainTextureOffset.y);
		}
	}
}
