using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using DataSet;

public class GamePlayDrums : GamePlay
{
	public Transform[] keys;
	public ParticleSystem[] sparks;

	Dictionary<int, int> keyIdIndexes;

	// :: AUTO ::
	public override void OpenTrack(InstrumentData track)
	{
		base.OpenTrack(track);

		track.muted = false;

		// Se houver alguma trilha de bateria, a silencia.
		foreach (InstrumentData instr in Game.Instance.MusicData.Instruments)
			if (instr.hash == "Drums")
				instr.muted = true;

		// Configura as 5 peças de bateria.
		SoundEngine.CurrentInstance.SetConfig(Game.Instance.Device.currentDeviceID,
			SoundEngine.CurrentInstance.GetSampleSetIndex("Drums"),
			new int[]{40,47,42,46,35});
	}

	int i;

	// :: RECEIVERS ::
	public void KeyDownReceiver(int n)
	{
		for (i = 0; i < 5; ++i)
		{
			if ((n & 1 << i) > 0)
			{
				SoundEngine.CurrentInstance.PlayNoteByIndex(i, Game.Instance.Device.currentDeviceID);
				sparks[i].Play();
			}
		}
	}
}
