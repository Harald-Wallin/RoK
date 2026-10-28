/*using System.Collections.Generic;
using UnityEngine;

public static class RuneTranslator
{
    private static Dictionary<char, string> runeMap = new Dictionary<char, string>()
{
    { 'A', "ᚨ" }, // ᚨ = "A"
    { 'Å', "ᚨ" }, // ᚨ = "Å"
    { 'Ä', "ᚨ" }, // ᚨ = "Ä"
    { 'B', "ᛒ" }, // ᛒ = "B"
    { 'C', "ᚲ" }, // ᚲ = "C"
    { 'D', "ᛞ" }, // ᛞ = "D"
    { 'E', "ᛖ" }, // ᛖ = "E"
    { 'F', "ᚠ" }, // ᚠ = "F"
    { 'G', "ᚷ" }, // ᚷ = "G"
    { 'H', "ᚺ" }, // ᚺ = "H"
    { 'I', "ᛁ" }, // ᛁ = "I"
    { 'J', "ᛃ" }, // ᛃ = "J"
    { 'K', "ᚲ" }, // ᚲ = "K"
    { 'L', "ᛚ" }, // ᛚ = "L"
    { 'M', "ᛗ" }, // ᛗ = "M"
    { 'N', "ᚾ" }, // ᚾ = "N"
    { 'O', "ᛟ" }, // ᛟ = "O"
    { 'Ö', "ᛟ" }, // ᛟ = "Ö"
    { 'P', "ᛈ" }, // ᛈ = "P"
    { 'R', "ᚱ" }, // ᚱ = "R"
    { 'S', "ᛋ" }, // ᛋ = "S"
    { 'T', "ᛏ" }, // ᛏ = "T"
    { 'U', "ᚢ" }, // ᚢ = "U"
    { 'V', "ᚡ" }, // ᚡ = "V"
    { 'W', "ᚹ" }, // ᚹ = "W"
    { 'Y', "ᛦ" }, // ᛦ = "Y"
};


    public static string ToRunes(string input)
    {
        input = input.ToUpper();
        string result = "";

        foreach (char c in input)
        {
            if (runeMap.ContainsKey(c))
                result += runeMap[c];
            else
                result += c;
        }

        return result;
    }
}
*/
