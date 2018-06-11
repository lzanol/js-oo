using UnityEngine;
using System.Collections;
using System.IO;

namespace DataSet
{
	public class UserData
	{
		public static string FILE_PATH = Utils.Common.LICENSE_DIR + "UserInfo.kvd";
		
		public string Name { get; set; }
		public string Nickname { get; set; }
		public string UserCode { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		
		public UserData()
		{
			Name = "";
			Nickname = "";
			UserCode = "";
			Email = "";
			Phone = "";
			Load ();
		}
		
		private void Load()
		{
			if(File.Exists(FILE_PATH))
			{
				string[] kv;
				foreach(string line in File.ReadAllLines(FILE_PATH)) 
				{
					kv = line.Split('=');
					
					if(kv.Length != 2)
						continue;
					
					switch(kv[0])
					{
					case "name":
						Name = kv[1];
						break;
					case "nickname":
						Nickname = kv[1];
						break;
					case "userCode":
						UserCode = kv[1];
						break;
					case "email":
						Email = kv[1];
						break;
					case "phone":
						Phone = kv[1];
						break;
					}
				}
			}
		}
		
		public void Save()
		{
			string text = "";
			
			if(Name != "")
				text += "name=" + Name + "\n\r";
			if(Nickname != "")
				text += "nickname=" + Nickname + "\n\r";
			if(UserCode != "")
				text += "userCode=" + UserCode + "\n\r";
			if(Email != "")
				text += "email=" + Email + "\n\r";
			if(Phone != "")
				text += "phone=" + Phone + "\n\r";
			
			if(!Directory.Exists(Utils.Common.LICENSE_DIR))
				Directory.CreateDirectory(Utils.Common.LICENSE_DIR);
			File.WriteAllText(FILE_PATH, text);
		}
	}
}
