using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using Events;

namespace Dialog
{
	public class OpenDialog
	{
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]  
		class OpenFileName 
		{
		    public int structSize = 0;
		    public IntPtr dlgOwner = IntPtr.Zero; 
		    public IntPtr instance = IntPtr.Zero;
		
		    public String filter = null;
		    public String customFilter = null;
		    public int maxCustFilter = 0;
		    public int filterIndex = 0;
		
		    public String file = null;
		    public int maxFile = 0;
		
		    public String fileTitle = null;
		    public int maxFileTitle = 0;
		
		    public String initialDir = null;
		
		    public String title = null;   
		
		    public int flags = 0; 
		    public short fileOffset = 0;
		    public short fileExtension = 0;
		
		    public String defExt = null; 
		
		    public IntPtr custData = IntPtr.Zero;
		    public IntPtr hook = IntPtr.Zero;
		
		    public String templateName = null;
		
		    public IntPtr reservedPtr = IntPtr.Zero;
		    public int reservedInt = 0;
		    public int flagsEx = 0;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		class BrowseInfo
		{
		    public IntPtr hwndOwner = IntPtr.Zero;
		    public IntPtr pidlRoot = IntPtr.Zero;
			
		    public String pszDisplayName = null;
			//[MarshalAs(UnmanagedType.LPTStr)]
		    public String lpszTitle = null;
			
		    public uint ulFlags = 0;
		    public BrowseCallbackProc lpfn = null;
		    public IntPtr lParam = IntPtr.Zero;
		    public int iImage = 0;
		}
		
		[DllImport("Comdlg32.dll", CharSet=CharSet.Auto)]
		static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
		
		[DllImport("Shell32.dll", CharSet=CharSet.Auto)]
		static extern IntPtr SHBrowseForFolder(BrowseInfo bi);
		
		[DllImport("shell32.dll", CharSet=CharSet.Auto)]
        static extern bool SHGetPathFromIDList(IntPtr pidList, IntPtr pszPath);
		
		delegate int BrowseCallbackProc(IntPtr hwnd, int msg, IntPtr lp, IntPtr wp);
		
		public event EventHandler<OpenDialogEventArgs> OnClose;
		
		public bool IsFileDialog { get; set; }
		public string Title { get; set; }
		public string InitialDir { get; set; }
		public string Filter { get; set; }
		
		string currentDirectory;
		bool isFileDialog;
		
		public OpenDialog(string title = "Open File", bool isFileDialog = true)
		{
			Title = title;
			IsFileDialog = isFileDialog;
			
			InitialDir = "C:\\";
			Filter = "All files;*.*";
		}
		
		public void Show()
		{
			currentDirectory = Directory.GetCurrentDirectory();
			
			if (IsFileDialog)
			{
				OpenFileName ofn = new OpenFileName();
		
		        ofn.structSize = Marshal.SizeOf(ofn);
		
				//"Músicas\0*.txt\0MIDI\0*.midi\0";
		        ofn.filter = Filter.Replace(";", "\0") + "\0";
		
		        ofn.file = new String(new char[512]);
		        ofn.maxFile = ofn.file.Length;
		
		        ofn.fileTitle = new String(new char[64]);
		        ofn.maxFileTitle = ofn.fileTitle.Length;
				
		        ofn.initialDir = InitialDir;
		        ofn.title = Title;
		        //ofn.defExt = "txt";
				
				/*Thread t = new Thread(p =>
					DispatchEvent(GetOpenFileName(ofn), ofn));
				
				t.Start();*/
				
				/*GUILayout.Label(String.Format("Selected file with full path: {0}", ofn.file));
		        GUILayout.Label(String.Format("Selected file name: {0}", ofn.fileTitle));
		        GUILayout.Label(String.Format("Offset from file name: {0}", ofn.fileOffset));
		        GUILayout.Label(String.Format("Offset from file extension: {0}", ofn.fileExtension));*/
				
				DispatchEvent(GetOpenFileName(ofn), ofn.file, ofn.fileTitle);
			}
			else
			{
				BrowseInfo bi = new BrowseInfo();
				string path = null;
				
	            bi.lpszTitle = Title;
				bi.ulFlags = 0x00000040 | 0x00008000;
				
				IntPtr pidList = SHBrowseForFolder(bi);
				bool confirmed = pidList != IntPtr.Zero;
				
				if (confirmed)
				{
					IntPtr buffer = Marshal.AllocHGlobal(512);
					
					if (SHGetPathFromIDList(pidList, buffer))
						path = Marshal.PtrToStringAuto(buffer);
					
					Marshal.FreeHGlobal(buffer);
				}
				
				DispatchEvent(confirmed, path, "");
			}
		}
		
		void DispatchEvent(bool confirmed, string path, string name)
		{
			Directory.SetCurrentDirectory(currentDirectory);
			
			EventHandler<OpenDialogEventArgs> handler = OnClose;
			
			if (handler != null)
				handler(this, new OpenDialogEventArgs(confirmed, path, name));
		}
	}
}