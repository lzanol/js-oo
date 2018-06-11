using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using DataSet;

namespace Utils
{
	public static class Common
	{
		public const string VERSION = "v13071";
#if UNITY_ANDROID
		public static string ROOT = Application.persistentDataPath + "/";
		public static string COMMONS = ROOT + "Commons/";
		public static string LICENSE_DIR = ROOT + "Utils/";
		public static string SDB_DIR = COMMONS + "sdb/";
		public static string MUSICS_DIR = COMMONS + "Musics/";
		public static string INSTRUMENT_DIR = SDB_DIR + "instruments/";
		public static string XMIDI_DIR = ROOT + "Tomplay X Midi/";
		public static string TUTOR_GUITARRA_DIR = ROOT + "Tomplay TutorGuitarra/";

		public static string SCENE_SPLASH = "Splash";
		public static string SCENE_MENU = "Menu";
		public static string SCENE_GUITAR = "GameGuitar";
		public static string SCENE_PIANO = "GamePiano";
		public static string SCENE_DRUMS = "GameDrums";
		public static string SCENE_REAL = "GameReal";
		public static string SCENE_SONGBOOK = "GameSongbook";
		
		public static string PLS = COMMONS + "PLS";
		public static string SOUND_ENGINE = COMMONS + "SoundEngine";
		public static string LICENSE = LICENSE_DIR + "tomplaysec.atv";
		public static string APP_XMIDI = XMIDI_DIR + "Tomplay X Midi.exe";
		public static string APP_TUTOR_GUITARRA = TUTOR_GUITARRA_DIR + "TutorGuitarra.exe";
		public static string APP_SHOW = ROOT + "Tomplay Show/Tomplay Show.exe";

#else

		public const string ROOT = "../";
		public const string COMMONS = ROOT + "Commons/";
		public const string LICENSE_DIR = ROOT + "Utils/";
		public const string SDB_DIR = COMMONS + "sdb/";
		public const string MUSICS_DIR = COMMONS + "Musics/";
		public const string INSTRUMENT_DIR = SDB_DIR + "instruments/";
		public const string XMIDI_DIR = ROOT + "Tomplay X Midi/";
		public const string TUTOR_GUITARRA_DIR = ROOT + "Tomplay TutorGuitarra/";
	
		public const string SCENE_SPLASH = "Splash";
		public const string SCENE_MENU = "Menu";
		public const string SCENE_GUITAR = "GameGuitar";
		public const string SCENE_PIANO = "GamePiano";
		public const string SCENE_PIANO_H = "GamePianoHorizontal";
		public const string SCENE_DRUMS = "GameDrums";
		public const string SCENE_REAL = "GameReal";
		public const string SCENE_REAL_H = "GameRealHorizontal";
		public const string SCENE_SONGBOOK = "GameSongbook";
		
		public const string PLS = COMMONS + "PLS";
		public const string SOUND_ENGINE = COMMONS + "SoundEngine";
		public const string LICENSE = LICENSE_DIR + "tomplaysec.atv";
		public const string APP_XMIDI = XMIDI_DIR + "Tomplay X Midi.exe";
		public const string APP_TUTOR_GUITARRA = TUTOR_GUITARRA_DIR + "TutorGuitarra.exe";
		public const string APP_SHOW = ROOT + "Tomplay Show/Tomplay Show.exe";
#endif			
		//retorna a data e a hora atual em formato string.
		public static string GetStringTime()
		{
			return System.DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
		}
		
		/**
		 * Retorna ~0..1,27
		 */
		public static float IntensityToFactor(float intensity)
		{
			//return Mathf.Pow(intensity/100f + 1f, INTENSITY_FACTOR) - 1f;
			return intensity/100f;
		}
		
		/**
		 * Retorna ~0..127.
		 */
		public static float FactorToIntensity(float factor)
		{
			//return (Mathf.Pow(velocity + 1f, 1f/INTENSITY_FACTOR) - 1f)*100f;
			return factor*100f;
		}
		
		public static string GetDownloadPath()
		{
			string path = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
			path = Path.Combine(path, "Downloads");
			return path;
		}
	}
	
	public static class Timer
	{
		private static float startTime = -1f;
		
		public static bool Running
		{
			get { return startTime != -1f; }
		}
		
		public static float CurrentTime
		{
			get { return Running ? Time.time - startTime : 0f; }
		}
		
		public static void Start(float startOffset = 0f)
		{
			startTime = Time.time - startOffset;
			//instance.DispatchEvent(new MainEvent(Timer.STOP));
		}
		
		public static void Stop()
		{
			startTime = -1f;
			//instance.DispatchEvent(new MainEvent(Timer.STOP));
		}
	}
}