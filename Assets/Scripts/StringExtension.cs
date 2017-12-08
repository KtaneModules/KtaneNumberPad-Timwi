using System;

namespace NumberPad
{
    public static class StringExtensions
    {
        public static string Reverse(this string str)
        {
            string newStr = "";
            for (int i = str.Length - 1; i >= 0; i--)
            {
                newStr += str.Substring(i, 1);
            }
            return newStr;
        }

        public static string ReplaceAt(this string input, int index, char newChar)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            char[] chars = input.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }
    }
}
