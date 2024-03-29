﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GI_Subtitles;
using Newtonsoft.Json;

public class VoiceContentData
{
    public string AvatarName { get; set; }
    public string TalkName { get; set; }
    public string AvatarSwitch { get; set; }
    public string VoiceContent { get; set; }
    public string SourceFileName { get; set; }
}

public static class VoiceContentHelper
{
    public static Dictionary<string, string> CreateVoiceContentDictionary(string chsFilePath, string enFilePath)
    {
        var chsData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(chsFilePath));
        var enData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(enFilePath));

        var voiceContentDict = new Dictionary<string, string>();

        foreach (var chsItem in chsData)
        {
            if (enData.TryGetValue(chsItem.Key, out var enVoiceContent))
            {
                voiceContentDict[chsItem.Value] = enVoiceContent;
            }
        }

        return voiceContentDict;
    }

    public static string FindClosestMatch(string input, Dictionary<string, string> voiceContentDict)
    {
        string closestKey = null;
        int closestDistance = int.MaxValue;
        int length = input.Length;
        string pattern1 = @"\{.*?\}";
        string pattern2 = @"</?unbreak>";
        foreach (var key in voiceContentDict.Keys)
        {
            if (key.StartsWith(input))
            {
                return voiceContentDict[key];
            }
            string temp = key;
            if (length <= 5 && temp.Length >= length * 3) {
                continue;
            }
            temp = Regex.Replace(temp, pattern1, "");
            temp = Regex.Replace(temp, pattern2, "");
            if (length > 5 && temp.Length > length)
            {
                temp = temp.Substring(0, length);
            }
            
            int distance = CalculateLevenshteinDistance(input, temp);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestKey = key;
            }
        }
        //Console.WriteLine($"closestKey {closestKey} length {length} closestDistance {closestDistance}");
        if (closestDistance < length / 1.5)
        {
            string  res = voiceContentDict[closestKey];
            res = Regex.Replace(res, pattern1, "");
            res = Regex.Replace(res, pattern2, "");
            return res;
        } else
        {
            return "";
        }
        
    }

    private static int CalculateLevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
        {
            return string.IsNullOrEmpty(b) ? 0 : b.Length;
        }

        if (string.IsNullOrEmpty(b))
        {
            return a.Length;
        }

        int lengthA = a.Length;
        int lengthB = b.Length;
        var distances = new int[lengthA + 1, lengthB + 1];

        for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
        for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

        for (int i = 1; i <= lengthA; i++)
        {
            for (int j = 1; j <= lengthB; j++)
            {
                int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[lengthA, lengthB];
    }
}
