using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
// oh my god jc a bomb!

public static class StringExtension
{
	public static string Reverse( this string str )
	{
		string New = "";
		for( int i = str.Length - 1; i >= 0; i-- )
		{
			New += str.Substring (i, 1);
		}
		return New;
	}
	public static string ReplaceAt(this string input, int index, char newChar)
	{
		if (input == null)
		{
			throw new UnityException("ArgumentNullException: input");
		}
		char[] chars = input.ToCharArray();
		chars[index] = newChar;
		return new string(chars);
	}
	public static string ReplaceAt( this string input, int index, string newChar )
	{
		if (input == null)
		{
			throw new UnityException("ArgumentNullException: input");
		}
		if (newChar.Length == 0)
			throw new UnityException ("Replacement character must exist!");
		return input.ReplaceAt (index, newChar.ToCharArray () [0]);
	}

}

public class NumberPadModule : MonoBehaviour
{
	static string Wheel = "12305717506313801516106482983387430675373640290503098763385425943657584758364894743619461342450049788237734806272714"; // trust me this works
	public KMSelectable[] buttons;
	public TextMesh Display;


	string WorkingCode; // when the code starts being calculated, this will be the cumulative code to be referenced in other places
	float LastStrike = 0;

	bool isActivated = false;
	KMBombInfo Info;


	void Start()
	{
		Init();

		GetComponent<KMBombModule>().OnActivate += ActivateModule;
		Info = GetComponent<KMBombInfo>();
	}

	void Init()
	{

		for(int i = 0; i < buttons.Length; i++)
		{
			Animator anim = buttons [i].GetComponentInChildren<Animator> ();
			string Name = buttons [i].name;
			buttons[i].OnInteract += delegate () {
				anim.SetTrigger( "PushTrigger" );
				OnPress(Name.Substring ( 6 ) ); // button names all start with "Button", the rest is which one they are
				
				return false;
			};
		}
	}


	int GetPathForLevel( int level )
	{
		//Debug.Log ("getting path for level " + level);
		switch( level )
		{
		case 0:
			if (SerialLastDigit () % 2 == 0 && BatteryCount () % 2 == 0)
				return 0;
			else if (SerialLastDigit () % 2 == 1 && StrikeCount () > 0)
				return 1;
			else if (ContainsVowel())
				return 2;
			else
				return 3;
		case 1:
			if (Indicators (new string[]{ "FRQ" }, true))
				return 0;
			else if (Ports (new string[]{ "DVI", "PS2", "Serial" }))
				return 1;
			else if (PortCount () < 2)
				return 2;
			else
			{
				SubtractDigit (0);
				return 3;
			}

		case 2:

			if (SolvedModuleCount () % 2 == 1)
				return 0;
			else
			{
				return 1; // remember to reverse the code thus far
			}

		case 3:

			if (StrikeCount () % 2 == 0) {
				return 0; // remember to add 1 to each digit
			} else
				return 1;
		}

		return -1;

	}

	string GetCorrectCode()
	{

		string Cur = Wheel;
		WorkingCode = "";

		int Path = GetPathForLevel (0);
		string[] Status = PickFrom (Cur, Path, 4);
		WorkingCode = Status [0];
		//Debug.Log ("workingcode is " + WorkingCode);

		Path = GetPathForLevel (1);
		Status = PickFrom (Status [1], Path, 4);
		WorkingCode += Status [0];
		//Debug.Log ("workingcode is " + WorkingCode);

		Path = GetPathForLevel (2);
		Status = PickFrom (Status [1], Path, 2);
		WorkingCode += Status [0];
		if (Path == 1) {
			//print ("took second path, reversing code");
			WorkingCode = WorkingCode.Reverse ();
		}
		//Debug.Log ("workingcode is " + WorkingCode);

		Path = GetPathForLevel (3);
		Status = PickFrom (Status [1], Path, 2);
		WorkingCode += Status [0];
		if( Path == 0 )
		{
			//print ("took first path, adding 1 to all digits");
			for( int i = 0; i < 4; i++ )
			{
				AddDigit (i);
			}
		}
		//Debug.Log ("workingcode is " + WorkingCode);

		bool NotMet = true;
		if( !ContainsVowel () )
		{
			//print ("no vowels, swapping 1 and 3");
			string old = WorkingCode.Substring (2, 1);
			WorkingCode = WorkingCode.ReplaceAt (2, WorkingCode.Substring (0, 1));
			WorkingCode = WorkingCode.ReplaceAt (0, old);
			NotMet = false;
		}
		if( BatteryCount () < 3 )
		{
			//print ("battery count < 3, swapping 2 and 3");
			string old = WorkingCode.Substring (2, 1);
			WorkingCode = WorkingCode.ReplaceAt (2, WorkingCode.Substring (1, 1));
			WorkingCode = WorkingCode.ReplaceAt (1, old);
			NotMet = false;
		}
		if( NotMet )
		{
			//print ("neither conditions met, swapping 1 and 4");
			string old = WorkingCode.Substring (3, 1);
			WorkingCode = WorkingCode.ReplaceAt (3 ,WorkingCode.Substring (0, 1));
			WorkingCode = WorkingCode.ReplaceAt (0, old);
		}
		//Debug.Log ("workingcode is " + WorkingCode);

		int Sum = 0;
		for( int i = 0; i < 4; i++ )
		{
			Sum += int.Parse( WorkingCode.Substring (i, 1) );
		}
		if( Sum % 2 == 1 )
		{
			//Debug.Log ("sum is odd, reversing");
			WorkingCode = WorkingCode.Reverse ();
		}
		//Debug.Log ("workingcode is FINALLY " + WorkingCode);

		return WorkingCode;
	}

	void Submit()
	{
		if (Time.time - LastStrike < 1) // don't let the nervous fucker click the button twice
			return;
		string Correct = "";
		try
		{
			Correct = GetCorrectCode ();
		}
		catch (System.Exception e ) // something might still fuck up soooo
		{

			GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyActivated, transform);
			Display.text = "ERROR";
			Debug.LogError ("NUMBER PAD: hey something happened here it is:\n " + e.Message);

			GetComponent<KMBombModule> ().HandlePass (); // they basically can't get out of this one so let them go
			isActivated = false;
			throw e;
		}

		//Debug.LogError("correct code: " + Correct);

		if (Correct == Display.text) {
			GetComponent<KMBombModule> ().HandlePass ();
			isActivated = false;
		} else {
			GetComponent<KMBombModule> ().HandleStrike ();
			LastStrike = Time.time;
		}

	}

	void Update()
	{
		if( Time.time - LastStrike >= 1 && LastStrike != 0.0f )
		{
			Display.text = "";
			LastStrike = 0;
		}
	}

	void OnPress(string Name)
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

		if (isActivated)
		{

			if( Name.Length == 1 ) // this is a digit
			{
				if( Display.text.Length < 4 ) // don't overflow
				{
					Display.text += Name;
				}
			}
			else if( Name == "Clear" )
			{
				Display.text = "";
				//Debug.Log ("the hatch code is " + GetCorrectCode ());

			}
			else if( Display.text.Length > 0 )
			{
				Submit ();
			}

		}
	}



	void ActivateModule()
	{
		isActivated = true;
	}


	bool ArrayContains<T>( T[] Array, T Query )
	{
		foreach( T value in Array )
		{
			if (EqualityComparer<T>.Default.Equals (value, Query))
				return true;
		}
		return false;
	}

	bool ContainsVowel()
	{
		return StringContainsLetters ( GetSerial(), "AEIOU");
	}


	string GetSerial()
	{

		List<string> Response = Info.QueryWidgets (KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
		if( Response.Count == 0 )
		{
			return "0IDFK0"; // probably using TestHarness so just make one up
		}
		Dictionary<string,string> Dict = JsonConvert.DeserializeObject < Dictionary<string,string> >(Response [0]);
		string Serial = Dict["serial"];
		return Serial;
	}
	int SerialLastDigit()
	{
		
		string Serial = GetSerial ();

		return int.Parse (Serial.Substring (Serial.Length - 1));

	}
	int BatteryCount()
	{
		List<string> Response = Info.QueryWidgets (KMBombInfo.QUERYKEY_GET_BATTERIES, null);
		int count = 0;
		foreach( string Value in Response )
		{
			Dictionary< string,int > Batteries = JsonConvert.DeserializeObject <Dictionary<string,int>> (Value);
			count += Batteries ["numbatteries"];
		}
		return count;
	}
	int StrikeCount()
	{
		return Info.GetStrikes ();
	}
	bool StringContainsLetters( string Str, string Letters )
	{
		foreach( char Char in Str )
		{
			if (Letters.Contains( Char.ToString () ) )
				return true;
		}
		return false;
	}
	bool Indicators( string[] Indicators, bool Lit ) // checks if any indicators in the given array and state exist
	{
		List<string> Response = Info.QueryWidgets (KMBombInfo.QUERYKEY_GET_INDICATOR, null);

		foreach( string Value in Response )
		{
			Dictionary< string,string > Ind = JsonConvert.DeserializeObject< Dictionary< string,string >> (Value);
			string Label = Ind ["label"];
			bool On = Ind ["on"] == "True";
			if( ArrayContains<string>( Indicators, Label ) && On )
			{
				return true;
			}
		}

		return false;
	}
	bool Ports( string[] Ports ) // checks if any ports in the given array exist
	{
		List<string> Response = Info.QueryWidgets (KMBombInfo.QUERYKEY_GET_PORTS, null);
		foreach( string Value in Response )
		{
			Dictionary< string, List<string> > Ind = JsonConvert.DeserializeObject< Dictionary< string, List<string> >> (Value);

			foreach( string Name in Ind["presentPorts"] )
			{
				if (ArrayContains<string> (Ports, Name))
					return true;
			}
		}
		return false;
	}
	int PortCount()
	{
		int count = 0;
		List<string> Response = Info.QueryWidgets (KMBombInfo.QUERYKEY_GET_PORTS, null);
		foreach( string Value in Response )
		{
			Dictionary< string, List<string> > Ind = JsonConvert.DeserializeObject< Dictionary< string, List<string> >> (Value);

			count += Ind ["presentPorts"].Count;
		}
		return count;
	}
	void SubtractDigit( int Digit )
	{
		string text = WorkingCode;
		int Num = int.Parse( text.Substring (Digit, 1) );
		Num--;
		if (Num < 0)
			Num = 9;
		WorkingCode = WorkingCode.ReplaceAt (Digit, Num.ToString ());
	}
	void AddDigit( int Digit )
	{
		string text = WorkingCode;
		int Num = int.Parse( text.Substring (Digit, 1) );
		Num++;
		if (Num > 9)
			Num = 0;
		WorkingCode = WorkingCode.ReplaceAt (Digit, Num.ToString ());
	}
	int SolvedModuleCount()
	{
		return Info.GetSolvedModuleNames ().Count;
	}
	string[] PickFrom( string Input, int Choice, int Choices )
	{
		// string[]{
		//  digit
		//  containing stuff
		// }

		string[] ret = new string[2];
		if( Choice < 0 || Choice >= Choices )
		{
			throw new UnityException ("NUMBER PAD: Choice out of range!");
		}
		if( Input.Length % Choices != 0 )
		{
			throw new UnityException ("NUMBER PAD: While trying to pick a portion of the code wheel, the string's length (" + Input.Length + ") wasn't divisible by the choice count (" + Choices + ")!");
		}
		int idx = Input.Length / Choices * Choice;
		ret [0] = Input.Substring (idx, 1);
		ret [1] = Input.Substring (idx + 1, Input.Length / Choices - 1);
		//Debug.Log ("the chosen path is " + Choice + " with " + Choices + " choices, the input is \"" + Input + "\", the number is " + ret [0] + ", and the rest is \"" + ret [1] + "\"");
		return ret;

	}
}
