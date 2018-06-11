using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ActivationDialog : MonoBehaviour 
{
	enum ActivationType : byte
	{
		ContactDialog,
		EnterCodeDialog
	}
	
	private const float dialogWidth1 = 436.5f;
	private const float dialogHeight1 = 522.75f;
	
	private const float dialogWidth2 = 436.5f;
	private const float dialogHeight2 = 252.75f;
	
	public event EventHandler<EventArgs> OnClose;
	
	public Texture2D blackBackground;
	public Texture2D dialogBackground1;
	public Texture2D dialogBackground2;
	
	public GUIStyle[] appButtonStyle;
	public GUIStyle labelStyle;
	public GUIStyle sendButton;
	public GUIStyle nextButton;
	public GUIStyle exitButton;
	public GUIStyle backButton;
	
	public GUIStyle activateButton;
	
	public GUIStyle textFieldStyle;
	public GUIStyle textFieldEntryCodeStyle;
	public GUIStyle textAreaStyle;
	public GUIStyle labelInvalidCodeStyle;
	
	private Validator.Status activated;
	
	private ActivationType currentActivationType = ActivationType.ContactDialog;
	
	//form
	private DataSet.UserData userData;
	private string msgText = "";
	
	private string codeText = "";
	
	private string currentStatus = "";
	private string entryCode;
	
	private bool sendingForm = false;
	
	private bool showInvalidCode = false;
	
	void Start()
	{		
		userData = new DataSet.UserData();
		
		activated = Validator.GetEnabled(0);
		
		currentStatus = (activated == Validator.Status.Enabled) ? "Ativado" : ( (activated == Validator.Status.Disabled) ? "Expirado" : Validator.DaysLeft.ToString() + " dia(s) restante(s)");
		entryCode = Validator.EntryCode;
	}
	
	void OnGUI()
	{
		GUI.depth = 0;
		GUI.skin.settings.cursorColor = new Color(0.3f,0.3f,0.3f);
		GUI.enabled = !sendingForm;
		Rect dialogRect;
		GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),blackBackground);
		switch(currentActivationType)
		{
		case ActivationType.ContactDialog:
			dialogRect = new Rect((Screen.width/2) - (dialogWidth1/2f), (Screen.height/2) - (dialogHeight1/2f),dialogWidth1,dialogHeight1);
			
			
			GUI.BeginGroup(dialogRect);
			
			GUI.DrawTexture(new Rect(0,0,dialogRect.width, dialogRect.height), dialogBackground1);
			
			GUI.Label(new Rect(dialogRect.width - 75, 18, 50, 20),Utils.Common.VERSION);
			
			//Produtos.
			for(int i = 0; i < appButtonStyle.Length; ++i)
			{
				if(GUI.Button(new Rect(40 + (120 * i), 65, 120,50), "", appButtonStyle[i]))
				{
					
				}
			}
			
			//status e chave.
			GUI.Label(new Rect(195,110,100,20), currentStatus, labelStyle);
			GUI.TextField(new Rect(90,150,260,24), entryCode, textFieldEntryCodeStyle);
			
			//formulario. (nome, apelido, identificacao de usuario, email, telefone, mensagem)
			userData.Name = GUI.TextField(new Rect(195,218,200,19), userData.Name, textFieldStyle);
			userData.Nickname = GUI.TextField(new Rect(195,240,200,19), userData.Nickname, textFieldStyle);
			userData.UserCode = GUI.TextField(new Rect(195,262,200,19), userData.UserCode, textFieldStyle);
			userData.Email = GUI.TextField(new Rect(195,284,200,19), userData.Email, textFieldStyle);
			userData.Phone = GUI.TextField(new Rect(195,306,200,19), userData.Phone, textFieldStyle);
			
			msgText = GUI.TextArea(new Rect(30,355,365,45), msgText, textAreaStyle);
			
			if(userData.Name != "" && userData.Email != "" && userData.UserCode != "")
			{
				if(GUI.Button(new Rect(310,400,90,35), "", sendButton))
					StartCoroutine(SendForm());
			}
			
			//Contato
			GUI.Label(new Rect(25,425,370,50), "Para suporte ou outras dúvidas, envie mensagem para rbittar@tomplay.com.br");
			
			//Proximo e sair.
			if(activated == Validator.Status.Enabled || activated == Validator.Status.Trial)
			{
				if(GUI.Button(new Rect(30,470,100,40), "", exitButton))
				{
					EventHandler<EventArgs> e = OnClose;
					if(e != null)
						e(this, new EventArgs());
				}
			}
			else
			{
				if (GUI.Button (new Rect(30,470,100,40), "", exitButton))
					Application.Quit();
			}

			if(GUI.Button(new Rect(295,470,100,40),"", nextButton))
			{
				currentActivationType = ActivationType.EnterCodeDialog;
				userData.Save();
			}

			GUI.EndGroup();
			break;
		case ActivationType.EnterCodeDialog:
			dialogRect = new Rect((Screen.width/2) - (dialogWidth2/2f), (Screen.height/2) - (dialogHeight1/2f),dialogWidth2,dialogHeight2);
			
			GUI.BeginGroup(dialogRect);
			
			GUI.DrawTexture(new Rect(0,0,dialogRect.width, dialogRect.height), dialogBackground2);
			
			GUI.Label(new Rect(dialogRect.width - 75, 20, 50, 20),Utils.Common.VERSION);
			
			//Produtos.
			for(int i = 0; i < appButtonStyle.Length; ++i)
			{
				GUI.Button(new Rect(40 + (120 * i), 70, 120,50), "", appButtonStyle[i]);
			}
			
			//Estado
			GUI.Label(new Rect(195,120,200,20), currentStatus, labelStyle);
			GUI.TextField(new Rect(170,140,240,20), entryCode, textFieldEntryCodeStyle);
			codeText = GUI.TextField(new Rect(195,162,200,20), codeText, 45, textFieldStyle);
			if(showInvalidCode)
				GUI.Label(new Rect(195,180,200,20), "Código inválido.",labelInvalidCodeStyle);
			
			//Voltar/Ativar
			if(GUI.Button(new Rect(30,195,100,40),"", backButton))
			{
				showInvalidCode = false;
				currentActivationType = ActivationType.ContactDialog;
			}
			
			if(codeText != "")
			{
				if(GUI.Button(new Rect(315,195,100,40), "", activateButton) || (Event.current.keyCode == KeyCode.Return))
				{
					if(Validator.Activate(codeText))
					{
						EventHandler<EventArgs> e = OnClose;
						if(e != null)
							e(this, new EventArgs());
					}
					else
					{
						codeText = "";
						showInvalidCode = true;
					}
				}
			}
			
			GUI.EndGroup();
			break;
		}
		
	}
	
	IEnumerator SendForm()
	{
		userData.Save();
		
		sendingForm = true;
		
		WWWForm form = new WWWForm();
		form.AddField("products[]", 0);
		//form.AddField("products[]", 1);
		//form.AddField("products[]", 2);
		
		form.AddField("entryCode", ""+Validator.EntryCode);
		form.AddField("dayValid", ""+Validator.DayValid);
		form.AddField("monthValid", ""+Validator.MonthValid);
		form.AddField("yearValid", ""+Validator.YearValid);
		form.AddField("exitCode", ""+Validator.ExitCode);
		form.AddField("errorCode", ""+Validator.ErrorCode);
		form.AddField("name", userData.Name);
		form.AddField("nickname", userData.Nickname);
		form.AddField("userCode", userData.UserCode);
		form.AddField("email", userData.Email);
		form.AddField("phone", userData.Phone);
		form.AddField("msg", msgText);
		
		WWW w = new WWW("http://www.tomplay.com.br/api/?target=TomplayShow&action=activationRequest", form);
		
		yield return w;
		
		if (w.error != null)
		{
			Debug.Log(w.error);
		}
		else
		{
			msgText = "";
			currentActivationType = ActivationType.EnterCodeDialog;
		}
		
		sendingForm = false;
	}
}
