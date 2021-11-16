using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NumberPad;
using UnityEngine;
using Random = UnityEngine.Random;

public class NumberPadModule : MonoBehaviour
{
    private static readonly string _wheel = "22468313395143690979890789940526034176635285026086097984297491480871855832860082003490389675061692920733696061238335";

    public KMBombModule Module;
    public KMAudio Audio;
    public KMColorblindMode ColorblindMode;

    public KMSelectable[] DigitButtons;
    public KMSelectable ClearButton;
    public KMSelectable SubmitButton;
    public TextMesh Display;
    public Texture Texture;
    public Shader Shader;
    public KMBombInfo Info;
    public TextMesh[] ButtonLabels;
    public MeshRenderer[] Buttons;

    private string _solution; // when the code starts being calculated, this will be the cumulative code to be referenced in other places
    private float _lastStrike = 0;

    private bool _isActivated = false;
    private int _moduleId = 0;
    private static int _moduleIdCounter = 1;
    private bool _colorblind;

    private readonly int[] ButtonColors = new int[10];
    private const float LowColor = 0.3f;

    private const int COLOR_WHITE = 0;
    private const int COLOR_GREEN = 1;
    private const int COLOR_YELLOW = 2;
    private const int COLOR_BLUE = 3;
    private const int COLOR_RED = 4;

    private static readonly Color[] Colors = {
        new Color (1, 1, 1),				// white
		new Color (LowColor, 1, LowColor),	// green
		new Color (1, 1, LowColor ),		// yellow
		new Color (LowColor, LowColor, 1 ),	// blue
		new Color (1, LowColor, LowColor )	// red
	};
    private static readonly string[] ColorNames = { "white", "green", "yellow", "blue", "red" };

    void Start()
    {
        _moduleId = _moduleIdCounter++;
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

            ButtonColors[i] = Random.Range(0, 5);

            Color col = Colors[ButtonColors[i]];

            Material mat = new Material(Shader);
            mat.SetTexture("_MainTex", Texture);
            mat.color = col;
            Buttons[i].material = mat;
        }

        ClearButton.OnInteract += delegate
        {
            ClearButton.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            Display.text = "";
            SetColorblind(true);
            return false;
        };
        ClearButton.OnInteractEnded += delegate
        {
            SetColorblind(false);
        };
        SubmitButton.OnInteract += delegate
        {
            SubmitButton.AddInteractionPunch();
            Submit();
            return false;
        };

        Debug.LogFormat("[Number Pad #{0}] Button colors are (0–9): {1}", _moduleId, Enumerable.Range(0, 10).Select(i => ColorNames[ButtonColors[i]]).Join(", "));
        Module.OnActivate += ActivateModule;
        _colorblind = ColorblindMode.ColorblindModeActive;
    }

    private void SetColorblind(bool on)
    {
        for (var i = 0; i < 10; i++)
            ButtonLabels[i].text = _colorblind && on ? ColorNames[ButtonColors[i]].Substring(0, 1).ToUpperInvariant() : i.ToString();
    }

    int GetPathForLevel(int level)
    {
        var colorCounts = Enumerable.Range(0, 5).Select(color => ButtonColors.Count(c => c == color)).ToArray();
        switch (level)
        {
            case 0:
                if (colorCounts[COLOR_YELLOW] >= 3)
                    return 0;
                else if (
                    ArrayContains<int>(new int[] { COLOR_WHITE, COLOR_BLUE, COLOR_RED }, ButtonColors[4]) &&
                    ArrayContains<int>(new int[] { COLOR_WHITE, COLOR_BLUE, COLOR_RED }, ButtonColors[5]) &&
                    ArrayContains<int>(new int[] { COLOR_WHITE, COLOR_BLUE, COLOR_RED }, ButtonColors[6]))
                    return 1;
                else if (ContainsVowel())
                    return 2;
                else
                    return 3;
            case 1:
                if (colorCounts[COLOR_BLUE] >= 2 && colorCounts[COLOR_GREEN] >= 3)
                    return 0;
                else if (ButtonColors[5] != COLOR_BLUE && ButtonColors[5] != COLOR_WHITE)
                    return 1;
                else if (PortCount() < 2)
                    return 2;
                else
                {
                    if (ButtonColors[7] == COLOR_GREEN || ButtonColors[8] == COLOR_GREEN || ButtonColors[9] == COLOR_GREEN)
                        SubtractDigit(0);
                    return 3;
                }

            case 2:
                if (colorCounts[COLOR_WHITE] > 2 && colorCounts[COLOR_YELLOW] > 2)
                    return 0;
                else
                    return 1; // remember to reverse the code thus far

            case 3:
                if (colorCounts[COLOR_YELLOW] <= 2)
                    return 0; // remember to add 1 to each digit
                else
                    return 1;
        }

        return -1;

    }

    void Submit()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Debug.LogFormat("[Number Pad #{0}] You submitted: {1}, which is {2}", _moduleId, Display.text, _solution == Display.text ? "correct — module solved." : "wrong — strike!");

        if (_solution == Display.text)
        {
            Module.HandlePass();
            _isActivated = false;
        }
        else
        {
            Module.HandleStrike();
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
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

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
    private string TwitchHelpMessage = @"!{0} submit 4236 [submit a four-digit answer] | !{0} colorblind";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*(cb|colou?rblind)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            _colorblind = true;
            yield return ClearButton;
            yield return new WaitForSeconds(2f);
            yield return ClearButton;
            yield break;
        }

        var m = Regex.Match(command, @"^\s*(?:submit|press)\s+([0-9]{4})\s*$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;

        yield return null;
        var buttonList = new List<KMSelectable>();
        buttonList.Add(ClearButton);
        buttonList.AddRange(m.Groups[1].Value.Select(c => DigitButtons[c - '0']));
        buttonList.Add(SubmitButton);
        yield return buttonList;
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
