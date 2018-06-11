using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Utils;

public static class Validator {
	public const int TOTAL_APPS = 3;
	
#if UNITY_STANDALONE_WIN
	[DllImport(Common.PLS)]
	static extern bool Ativar(int diasValidade, long contraSenha, ref long Id_Instalacao, ref long Id_Hardware, ref int codRetorno, ref int codErro, ref int diaValidade, ref int mesValidade, ref int anoValidade, ref bool flagAtivado);
#endif

	static long Id_Instalacao = 0L;
	static long Id_Hardware = 0L;
	static int codRetorno = 0;
	static int codErro = 0;
	static int diaValidade = 0;
	static int mesValidade = 0;
	static int anoValidade = 0;
	static bool flagAtivado;
	static bool enabled = false;
	
	public enum Status : byte
	{
		Enabled,
		Disabled,
		Trial
	}
	
	public static void Initialize() {
#if UNITY_STANDALONE_WIN

		flagAtivado = true;
		return;
		
		Ativar(0, 0, ref Id_Instalacao, ref Id_Hardware, ref codRetorno, ref codErro, ref diaValidade, ref mesValidade, ref anoValidade, ref flagAtivado);
		//Ativar(0, Id_Instalacao^Id_Hardware, ref Id_Instalacao, ref Id_Hardware, ref codRetorno, ref codErro, ref diaValidade, ref mesValidade, ref anoValidade, ref flagAtivado);
		//Ativar(0, 192837, ref Id_Instalacao, ref Id_Hardware, ref codRetorno, ref codErro, ref diaValidade, ref mesValidade, ref anoValidade, ref flagAtivado);
		//Ativar(15, 0, ref Id_Instalacao, ref Id_Hardware, ref codRetorno, ref codErro, ref diaValidade, ref mesValidade, ref anoValidade, ref flagAtivado);
		//File.WriteAllText("aaa.txt", ToBase36(Id_Instalacao^Id_Hardware) + " " + ToBase36(FromBase36("1W8LU7VGAHZ3F")^FromBase36("1H65KVEZ0W9JH")));
		File.WriteAllText("aaa.txt", Common.PLS + " " + codRetorno + " " + codErro + " " + flagAtivado + " " + diaValidade + " " + mesValidade + " " + anoValidade);
		
		if (flagAtivado)
			return;
		
		int totalDays = 15;
		
		DateTime date = (new DateTime(anoValidade, mesValidade, diaValidade)).AddDays(totalDays);
		diaValidade = date.Day;
		mesValidade = date.Month;
		anoValidade = date.Year;
#else
		flagAtivado = true;
#endif
	}
	
	public static bool Enabled {
		get { return flagAtivado; }
	}
	
	public static bool Expired {
		get { return DaysLeft < 1; }
	}
	
	public static string EntryCode {
		get { return ToBase36(Id_Instalacao) + ":" + ToBase36(Id_Hardware); }
	}
	
	public static int ExitCode {
		get { return codRetorno; }
	}
	
	public static int ErrorCode {
		get { return codErro; }
	}
	
	public static int DayValid {
		get { return diaValidade; }
	}
	
	public static int MonthValid {
		get { return mesValidade; }
	}
	
	public static int YearValid {
		get { return anoValidade; }
	}
	
	public static int DaysLeft {
		get {
			return YearValid > 0 && MonthValid > 0 && DayValid > 0 ?
				(new DateTime(YearValid, MonthValid, DayValid)).Subtract(DateTime.Now).Days : -1;
		}
	}
	
	public static Status SetEnabled(bool enabledParam)
	{
		enabled=enabledParam;
		return Status.Enabled;
	}
	
	public static Status GetEnabled(int appId) {
#if UNITY_STANDALONE_WIN

		if(enabled)
			return Status.Enabled;
		
		long appsCodes = GetAppsCodes();
		
		// Se a APP estiver ativa.
		/*free validator*/ if (Enabled && appsCodes != 0 && (appsCodes & 1 << appId) >> appId == 1)
			return Status.Enabled;
		
		if (!Expired)
			return Status.Trial;
		
		return Status.Disabled;
#else
		return Status.Enabled;
#endif
	}
	
	public static bool Activate(string code) {
#if UNITY_STANDALONE_WIN
		string appsCodes;
		string atvCode = ReadActivationCode(code, out appsCodes);
		
		if (atvCode == null)
			return false;
	
		Ativar(0, FromBase36(atvCode), ref Id_Instalacao, ref Id_Hardware, ref codRetorno, ref codErro, ref diaValidade, ref mesValidade, ref anoValidade, ref flagAtivado);

		if (flagAtivado) {
			if (!Directory.Exists(Common.LICENSE_DIR))
				Directory.CreateDirectory(Common.LICENSE_DIR);
			
			File.WriteAllText(Common.LICENSE, code);
		}

		return flagAtivado;
#else
		return true;
#endif
	}
	
	static string ReadActivationCode(string code, out string appsCodes) {
		appsCodes = null;
		
		if (code != null && code != "") {
			int i;
			bool isNeg = code.IndexOf('-') == 0;
			
			if (isNeg)
				code = code.Remove(0, 1);
			
			int td;
			
			if (!int.TryParse(code.Substring(i = code.Length - 1), out td)){
				File.WriteAllText("a.txt", "ok");
				return null;
			}
			
			appsCodes = "";
			code = code.Remove(i);
			
			if (td*2 >= code.Length){
				File.WriteAllText("ab.txt", "ok");
				return null;
			}
			
			while (td-- > 0) {
				appsCodes = code.Substring(i = td*2 + 1, 1) + appsCodes;
				code = code.Remove(i, 1);
			}
			File.WriteAllText("ac.txt", "" + td);
			return isNeg ? "-" + code : code;
		}
		
		return null;
	}
	
	static long GetAppsCodes() {
		if (File.Exists(Common.LICENSE)) {
			string appsCodes;
			
			ReadActivationCode(File.ReadAllText(Common.LICENSE).Trim(), out appsCodes);
			
			return appsCodes != null ? FromBase36(appsCodes) : 0;
		}
		
		return 0;
	}
	
	const string CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	
	static string ToBase36(long n) {
		char[] charSet = CHARS.ToCharArray();
		Stack<char> chars = new Stack<char>();
		bool isNeg = false;
		
		if (n < 0) {
			n = -n;
			isNeg = true;
		}
		
		while (n != 0) {
			chars.Push(charSet[n%36]);
			n /= 36;
		}
		
		if (isNeg)
			chars.Push('-');
		
		return new string(chars.ToArray());
	}
	
	static long FromBase36(string chars) {
		long n = 0;
		bool isNeg = chars.IndexOf('-') == 0;
		
		if (isNeg)
			chars = chars.Remove(0, 1);
		
		chars = chars.ToUpper();
		
		for (int i = 0; i < chars.Length; ++i)
			n += CHARS.IndexOf(chars[chars.Length - i - 1])*(long)Math.Pow(36, i);
		
		return isNeg ? -n : n;
	}
}