using System;
using System.Collections.Generic;
using System.IO;
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
        var chsData = JsonConvert.DeserializeObject<Dictionary<string, VoiceContentData>>(File.ReadAllText(chsFilePath));
        var enData = JsonConvert.DeserializeObject<Dictionary<string, VoiceContentData>>(File.ReadAllText(enFilePath));

        var enDataByFileName = new Dictionary<string, string>();
        foreach (var enItem in enData)
        {
            enDataByFileName[enItem.Value.SourceFileName] = enItem.Value.VoiceContent;
        }

        var voiceContentDict = new Dictionary<string, string>();

        foreach (var chsItem in chsData)
        {
            if (enDataByFileName.TryGetValue(chsItem.Value.SourceFileName, out var enVoiceContent))
            {
                voiceContentDict[chsItem.Value.VoiceContent] = enVoiceContent;
            }
        }

        return voiceContentDict;
    }

    public static string FindClosestMatch(string input, Dictionary<string, string> voiceContentDict)
    {
        string closestKey = null;
        int closestDistance = int.MaxValue;

        foreach (var key in voiceContentDict.Keys)
        {
            int distance = CalculateLevenshteinDistance(input, key);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestKey = key;
            }
        }
        if (closestDistance < 50)
        {
            return voiceContentDict[closestKey];
        } else
        {
            return null;
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
