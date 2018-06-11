using UnityEngine;
using System.Collections;
using System.IO;
using Utils;

public class EditTXT
{
	public const string EDIT_FILE = "edit.txt";
	
	public EditTXT() {} 
	
	public void CreateFile(string tmdPath)
	{
		File.WriteAllText(Common.XMIDI_DIR + EDIT_FILE,tmdPath);
	}
	
	public string LoadFile()
	{
		string result = "";
		if(File.Exists(Common.XMIDI_DIR + EDIT_FILE))
		{
			result = File.ReadAllText(Common.XMIDI_DIR + EDIT_FILE);
			DeleteFile();
		}
		return result;
	}
	
	public void DeleteFile()
	{
		File.Delete(Common.XMIDI_DIR + EDIT_FILE);
	}
}
