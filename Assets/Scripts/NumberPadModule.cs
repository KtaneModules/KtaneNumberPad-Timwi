using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NumberPad;
using UnityEngine;
using Random = UnityEngine.Random;

public class NumberPadModule : MonoBehaviour
{
    private static string _wheel = "22468313395143690979890789940526034176635285026086097984297491480871855832860082003490389675061692920733696061238335";

    public KMSelectable[] DigitButtons;
    public KMSelectable ClearButton;
    public KMSelectable SubmitButton;
    public TextMesh Display;
    public Texture Texture;
    public Shader Shader;
    public KMBombInfo Info;

    string _solution; // when the code starts being calculated, this will be the cumulative code to be referenced in other places
    float _lastStrike = 0;

    bool _isActivated = false;
    int _moduleId = 0;
    static int _moduleIdCounter = 1;

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
    string[] ColorNames = { "white", "green", "yellow", "blue", "red" };

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        Init();
        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }

    void Init()
    {
        for (int i = 0; i < DigitButtons.Length; i++)
        {
            Animator anim = DigitButtons[i].GetComponentInChildren<Animator>();

            var j = i;
            DigitButtons[i].OnInteract += delegate ()
            {
                DigitButtons[j].AddInteractionPunch(.1f);
                anim.SetTrigger("PushTrigger");
                OnPress(j); // button names all start with "Button", the rest is which one they are
                return false;
            };

            MeshRenderer renderer = DigitButtons[i].GetComponentInChildren<MeshRenderer>();

            int[] idx = GetButtonIndices(i);

            ButtonColors[idx[0], idx[1]] = Random.Range(0, 5);

            Color col = Colors[ButtonColors[idx[0], idx[1]]];

            Material mat = new Material(Shader);
            mat.SetTexture("_MainTex", Texture);
            mat.color = col;
            renderer.material = mat;
        }

        ClearButton.OnInteract += delegate
        {
            ClearButton.AddInteractionPunch();
            Display.text = "";
            return false;
        };
        SubmitButton.OnInteract += delegate
        {
            SubmitButton.AddInteractionPunch();
            Submit();
            return false;
        };

        Debug.LogFormat("[Number Pad #{0}] Button colors are: {1}", _moduleId, string.Join(", ", Enumerable.Range(0, 3).SelectMany(y => Enumerable.Range(0, 3).Select(x => ColorNames[ButtonColors[x, y]])).Concat(new[] { ColorNames[ButtonColors[3, 0]] }).ToArray()));
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
        Debug.LogFormat("[Number Pad #{0}] You submitted: {1}, which is {2}", _moduleId, Display.text, _solution == Display.text ? "correct — module solved." : "wrong — strike!");

        if (_solution == Display.text)
        {
            GetComponent<KMBombModule>().HandlePass();
            _isActivated = false;
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

    void OnPress(int btnIx)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

        if (_isActivated && Display.text.Length < 4)
            Display.text += btnIx;
    }

    void ActivateModule()
    {
        _isActivated = true;
        int path = GetPathForLevel(0);
        string[] status = PickFrom(_wheel, path, 4);
        _solution = status[0];
        Debug.LogFormat("[Number Pad #{0}] Level 1 path = {1}; code is now: {2}", _moduleId, path + 1, _solution);

        path = GetPathForLevel(1);
        status = PickFrom(status[1], path, 4);
        _solution += status[0];
        Debug.LogFormat("[Number Pad #{0}] Level 2 path = {1}; code is now: {2}", _moduleId, path + 1, _solution);

        path = GetPathForLevel(2);
        status = PickFrom(status[1], path, 2);
        _solution += status[0];
        Debug.LogFormat("[Number Pad #{0}] Level 3 path = {1}; code is now: {2}", _moduleId, path + 1, _solution);

        if (path == 1)
        {
            _solution = _solution.Reverse();
            Debug.LogFormat("[Number Pad #{0}] Took second path; reversing code: {2}", _moduleId, path + 1, _solution);
        }

        path = GetPathForLevel(3);
        status = PickFrom(status[1], path, 2);
        _solution += status[0];
        Debug.LogFormat("[Number Pad #{0}] Level 4 path = {1}; code is now: {2}", _moduleId, path + 1, _solution);

        if (path == 0)
        {
            for (int i = 0; i < 4; i++)
                AddDigit(i);
            Debug.LogFormat("[Number Pad #{0}] Took first path; adding 1 to each digit: {2}", _moduleId, path + 1, _solution);
        }

        bool notMet = true;
        if (SerialLastDigit() % 2 == 0)
        {
            var old = _solution[2];
            _solution = _solution.ReplaceAt(2, _solution[0]);
            _solution = _solution.ReplaceAt(0, old);
            notMet = false;
            Debug.LogFormat("[Number Pad #{0}] Serial number is even, swapping 1 and 3: {1}", _moduleId, _solution);
        }
        if (BatteryCount() % 2 == 1)
        {
            var old = _solution[2];
            _solution = _solution.ReplaceAt(2, _solution[1]);
            _solution = _solution.ReplaceAt(1, old);
            notMet = false;
            Debug.LogFormat("[Number Pad #{0}] Battery count is odd, swapping 2 and 3: {1}", _moduleId, _solution);
        }
        if (notMet)
        {
            var old = _solution[3];
            _solution = _solution.ReplaceAt(3, _solution[0]);
            _solution = _solution.ReplaceAt(0, old);
            Debug.LogFormat("[Number Pad #{0}] Neither serial number nor battery condition applies, swapping 1 and 4: {1}", _moduleId, _solution);
        }

        int sum = 0;
        for (int i = 0; i < 4; i++)
            sum += int.Parse(_solution.Substring(i, 1));

        if (sum % 2 == 0)
        {
            _solution = _solution.Reverse();
            Debug.LogFormat("[Number Pad #{0}] Sum is even, reversing: {1}", _moduleId, _solution);
        }
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
        List<string> response = Info.QueryWidgets(KMBombInfo.QUERYKEY_GET_PORTS, null);
        foreach (string Value in response)
        {
            Dictionary<string, List<string>> ind = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(Value);
            count += ind["presentPorts"].Count;
        }
        return count;
    }
    void SubtractDigit(int digit)
    {
        _solution = _solution.ReplaceAt(digit, (char) ('0' + ((_solution[digit] - '0' + 9) % 10)));
    }
    void AddDigit(int digit)
    {
        _solution = _solution.ReplaceAt(digit, (char) ('0' + ((_solution[digit] - '0' + 1) % 10)));
    }
    int SolvedModuleCount()
    {
        return Info.GetSolvedModuleNames().Count;
    }
    string[] PickFrom(string input, int choice, int choices)
    {
        string[] ret = new string[2];

        if (choice < 0 || choice >= choices)
            throw new UnityException("NUMBER PAD: Choice out of range!");

        if (input.Length % choices != 0)
            throw new UnityException("NUMBER PAD: While trying to pick a portion of the code wheel, the string's length (" + input.Length + ") wasn't divisible by the choice count (" + choices + ")!");

        int idx = input.Length / choices * choice;
        ret[0] = input.Substring(idx, 1);
        ret[1] = input.Substring(idx + 1, input.Length / choices - 1);
        //print ("the chosen path is " + Choice + " with " + Choices + " choices, the input is \"" + Input + "\", the number is " + ret [0] + ", and the rest is \"" + ret [1] + "\"");
        return ret;
    }

#pragma warning disable 414
    private string TwitchHelpMessage = @"Submit your four-digit answer with “!{0} submit 4236”.";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string inputCommand)
    {
        var commands = inputCommand.ToLowerInvariant().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

        if (commands.Length != 2 || (commands[0] != "submit" && commands[0] != "press"))
            yield break;

        var buttonList = commands[1].Where(c => !char.IsWhiteSpace(c)).Select(c => DigitButtons[c - '0']).ToList();
        if (buttonList.Count() != 4 || buttonList.Any(num => num == null))
            yield break;

        buttonList.Insert(0, ClearButton);
        buttonList.Add(SubmitButton);

        yield return null;
        foreach (var button in buttonList)
        {
            button.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!_isActivated)
            yield break;

        if (Display.text.Length > 0)
        {
            ClearButton.OnInteract();
            yield return new WaitForSeconds(.25f);
        }

        for (var i = 0; i < _solution.Length; i++)
        {
            DigitButtons[_solution[i] - '0'].OnInteract();
            yield return new WaitForSeconds(.1f);
        }

        SubmitButton.OnInteract();
    }
}
