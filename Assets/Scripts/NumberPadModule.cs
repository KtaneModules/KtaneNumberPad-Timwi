using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NumberPad;
using UnityEngine;

public class NumberPadModule : MonoBehaviour
{
    private static string _wheel = "22468313395143690979890789940526034176635285026086097984297491480871855832860082003490389675061692920733696061238335";

    public KMSelectable[] buttons;
    public TextMesh Display;
    public Texture Texture;
    public Shader Shader;
    public KMBombInfo Info;

    string _solution; // when the code starts being calculated, this will be the cumulative code to be referenced in other places
    float _lastStrike = 0;

    bool isActivated = false;

    int[,] ButtonColors = new int[,] {
        {0,0,0},
        {0,0,0},
        {0,0,0},
        {0,0,0}
    };
    static float LowColor = 0.3f;

    static int COLOR_WHITE = 0;
    static int COLOR_GREEN = 1;
    static int COLOR_YELLOW = 2;
    static int COLOR_BLUE = 3;
    static int COLOR_RED = 4;

    Color[] Colors = {
        new Color (1, 1, 1),				// white
		new Color (LowColor, 1, LowColor),	// green
		new Color (1, 1, LowColor ),		// yellow
		new Color (LowColor, LowColor, 1 ),	// blue
		new Color (1, LowColor, LowColor )	// red
	};


    void Start()
    {
        Init();
        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void Init()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Animator anim = buttons[i].GetComponentInChildren<Animator>();
            string Name = buttons[i].name;


            buttons[i].OnInteract += delegate ()
            {
                anim.SetTrigger("PushTrigger");
                OnPress(Name.Substring(6)); // button names all start with "Button", the rest is which one they are

                return false;
            };


            MeshRenderer renderer = buttons[i].GetComponentInChildren<MeshRenderer>();

            if (Name.Length == 7)
            {
                int Number = int.Parse(Name.Substring(6));
                int[] idx = GetButtonIndices(Number);

                ButtonColors[idx[0], idx[1]] = Random.Range(0, 5);

                Color col = Colors[ButtonColors[idx[0], idx[1]]];

                Material mat = new Material(Shader);
                mat.SetTexture("_MainTex", this.Texture);
                mat.color = col;
                renderer.material = mat;
            }
        }
    }

    int[] GetButtonIndices(int Number)
    {
        if (Number == 0)
            return new int[] { 3, 0 };
        int y = 2 - Mathf.FloorToInt((float) (Number - 1) / 3);
        int x = (Number - 1) % 3;
        return new int[] { x, y };
    }
    int GetButtonColor(int Number)
    {
        int[] i = GetButtonIndices(Number);
        return ButtonColors[i[0], i[1]];
    }

    int GetColorCount(int Color)
    {
        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            if (GetButtonColor(i) == Color)
                count++;
        }
        return count;
    }

    int GetPathForLevel(int level)
    {
        //print ("getting path for level " + level);
        switch (level)
        {
            case 0:
                if (GetColorCount(COLOR_YELLOW) >= 3)
                    return 0;
                else if (
                    ArrayContains<int>(new int[] { COLOR_WHITE, COLOR_BLUE, COLOR_RED }, GetButtonColor(4)) &&
                    ArrayContains<int>(new int[] { COLOR_WHITE, COLOR_BLUE, COLOR_RED }, GetButtonColor(5)) &&
                    ArrayContains<int>(new int[] { COLOR_WHITE, COLOR_BLUE, COLOR_RED }, GetButtonColor(6)))
                    return 1;
                else if (ContainsVowel())
                    return 2;
                else
                    return 3;
            case 1:
                if (GetColorCount(COLOR_BLUE) >= 2 && GetColorCount(COLOR_GREEN) >= 3)
                    return 0;
                else if (GetButtonColor(5) != COLOR_BLUE && GetButtonColor(5) != COLOR_WHITE)
                    return 1;
                else if (PortCount() < 2)
                    return 2;
                else
                {
                    if (GetButtonColor(7) == COLOR_GREEN || GetButtonColor(8) == COLOR_GREEN || GetButtonColor(9) == COLOR_GREEN)
                        SubtractDigit(0);
                    return 3;
                }

            case 2:

                if (GetColorCount(COLOR_WHITE) > 2 && GetColorCount(COLOR_YELLOW) > 2)
                    return 0;
                else
                {
                    return 1; // remember to reverse the code thus far
                }

            case 3:

                if (GetColorCount(COLOR_YELLOW) <= 2)
                {
                    return 0; // remember to add 1 to each digit
                }
                else
                    return 1;
        }

        return -1;

    }

    void Submit()
    {
        if (Time.time - _lastStrike < 1) // don't let the nervous fucker click the button twice
            return;

        //Debug.LogError("correct code: " + Correct);

        if (_solution == Display.text)
        {
            GetComponent<KMBombModule>().HandlePass();
            isActivated = false;
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
            _lastStrike = Time.time;
        }

    }

    void Update()
    {
        if (Time.time - _lastStrike >= 1 && _lastStrike != 0.0f)
        {
            Display.text = "";
            _lastStrike = 0;
        }
    }

    void OnPress(string Name)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

        if (isActivated)
        {

            if (Name.Length == 1) // this is a digit
            {
                if (Display.text.Length < 4) // don't overflow
                {
                    Display.text += Name;
                }
            }
            else if (Name == "Clear")
            {
                Display.text = "";
                //print ("the hatch code is " + GetCorrectCode ());

            }
            else if (Display.text.Length > 0)
            {
                Submit();
            }

        }
    }

    void ActivateModule()
    {
        isActivated = true;
        int path = GetPathForLevel(0);
        string[] status = PickFrom(_wheel, path, 4);
        _solution = status[0];

        path = GetPathForLevel(1);
        status = PickFrom(status[1], path, 4);
        _solution += status[0];

        path = GetPathForLevel(2);
        status = PickFrom(status[1], path, 2);
        _solution += status[0];

        if (path == 1)
        {
            //print ("took second path, reversing code");
            _solution = _solution.Reverse();
        }

        path = GetPathForLevel(3);
        status = PickFrom(status[1], path, 2);
        _solution += status[0];
        if (path == 0)
        {
            //print ("took first path, adding 1 to all digits");
            for (int i = 0; i < 4; i++)
            {
                AddDigit(i);
            }
        }

        bool notMet = true;
        if (SerialLastDigit() % 2 == 0)
        {
            //print ("serial even, swapping 1 and 3");
            var old = _solution[2];
            _solution = _solution.ReplaceAt(2, _solution[0]);
            _solution = _solution.ReplaceAt(0, old);
            notMet = false;
        }
        if (BatteryCount() % 2 == 1)
        {
            //print ("battery count odd, swapping 2 and 3");
            var old = _solution[2];
            _solution = _solution.ReplaceAt(2, _solution[1]);
            _solution = _solution.ReplaceAt(1, old);
            notMet = false;
        }
        if (notMet)
        {
            //print ("neither conditions met, swapping 1 and 4");
            var old = _solution[3];
            _solution = _solution.ReplaceAt(3, _solution[0]);
            _solution = _solution.ReplaceAt(0, old);
        }
        //print ("workingcode is " + WorkingCode);

        int sum = 0;
        for (int i = 0; i < 4; i++)
            sum += int.Parse(_solution.Substring(i, 1));

        if (sum % 2 == 0)
        {
            //print ("sum is even reversing");
            _solution = _solution.Reverse();
        }
        //print ("workingcode is FINALLY " + WorkingCode);
    }

    bool ArrayContains<T>(T[] array, T query)
    {
        foreach (T value in array)
            if (Equals(value, query))
                return true;
        return false;
    }

    bool ContainsVowel()
    {
        return GetSerial().Any(ch => "AEIOU".Contains(ch));
    }

    string GetSerial()
    {
        List<string> response = Info.QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(response[0])["serial"];
    }

    int SerialLastDigit()
    {
        string serial = GetSerial();
        return int.Parse(serial.Substring(serial.Length - 1));
    }

    int BatteryCount()
    {
        List<string> response = Info.QueryWidgets(KMBombInfo.QUERYKEY_GET_BATTERIES, null);
        int count = 0;
        foreach (string value in response)
        {
            Dictionary<string, int> batteries = JsonConvert.DeserializeObject<Dictionary<string, int>>(value);
            count += batteries["numbatteries"];
        }
        return count;
    }

    int StrikeCount()
    {
        return Info.GetStrikes();
    }

    bool StringContainsLetters(string Str, string Letters)
    {
        foreach (char Char in Str)
        {
            if (Letters.Contains(Char.ToString()))
                return true;
        }
        return false;
    }

    bool Indicators(string[] Indicators, bool Lit) // checks if any indicators in the given array and state exist
    {
        List<string> Response = Info.QueryWidgets(KMBombInfo.QUERYKEY_GET_INDICATOR, null);

        foreach (string Value in Response)
        {
            Dictionary<string, string> Ind = JsonConvert.DeserializeObject<Dictionary<string, string>>(Value);
            string Label = Ind["label"];
            bool On = Ind["on"] == "True";
            if (ArrayContains<string>(Indicators, Label) && On)
            {
                return true;
            }
        }

        return false;
    }
    bool Ports(string[] Ports) // checks if any ports in the given array exist
    {
        List<string> Response = Info.QueryWidgets(KMBombInfo.QUERYKEY_GET_PORTS, null);
        foreach (string Value in Response)
        {
            Dictionary<string, List<string>> Ind = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(Value);

            foreach (string Name in Ind["presentPorts"])
            {
                if (ArrayContains<string>(Ports, Name))
                    return true;
            }
        }
        return false;
    }
    int PortCount()
    {
        int count = 0;
        List<string> Response = Info.QueryWidgets(KMBombInfo.QUERYKEY_GET_PORTS, null);
        foreach (string Value in Response)
        {
            Dictionary<string, List<string>> Ind = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(Value);

            count += Ind["presentPorts"].Count;
        }
        return count;
    }
    void SubtractDigit(int Digit)
    {
        _solution = _solution.ReplaceAt(Digit, (char) ('0' + ((_solution[Digit] - '0' + 9) % 10)));
    }
    void AddDigit(int Digit)
    {
        _solution = _solution.ReplaceAt(Digit, (char) ('0' + ((_solution[Digit] - '0' + 1) % 10)));
    }
    int SolvedModuleCount()
    {
        return Info.GetSolvedModuleNames().Count;
    }
    string[] PickFrom(string Input, int Choice, int Choices)
    {
        string[] ret = new string[2];

        if (Choice < 0 || Choice >= Choices)
            throw new UnityException("NUMBER PAD: Choice out of range!");

        if (Input.Length % Choices != 0)
            throw new UnityException("NUMBER PAD: While trying to pick a portion of the code wheel, the string's length (" + Input.Length + ") wasn't divisible by the choice count (" + Choices + ")!");

        int idx = Input.Length / Choices * Choice;
        ret[0] = Input.Substring(idx, 1);
        ret[1] = Input.Substring(idx + 1, Input.Length / Choices - 1);
        //print ("the chosen path is " + Choice + " with " + Choices + " choices, the input is \"" + Input + "\", the number is " + ret [0] + ", and the rest is \"" + ret [1] + "\"");
        return ret;

    }
}
