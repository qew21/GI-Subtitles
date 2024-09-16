using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GI_Subtitles;
using Newtonsoft.Json;


public static class VoiceContentHelper
{
    public static Dictionary<string, string> CreateVoiceContentDictionary(string chsFilePath, string enFilePath, string userName)
    {
        var jsonFilePath = Path.Combine(Path.GetDirectoryName(chsFilePath),
            $"{Path.GetFileNameWithoutExtension(chsFilePath)}_{Path.GetFileNameWithoutExtension(enFilePath)}.json");
        if (File.Exists(jsonFilePath))
        {
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(jsonFilePath));
        }

        var chsData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(chsFilePath));
        var enData = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(enFilePath));
        var voiceContentDict = new Dictionary<string, string>();
        foreach (var chsItem in chsData)
        {
            if (enData.TryGetValue(chsItem.Key, out var enVoiceContent))
            {
                string pattern1 = @"\{.*?\}";
                string pattern2 = @"</?unbreak>";
                string temp = chsItem.Value;
                temp = Regex.Replace(temp, pattern1, "");
                enVoiceContent = enVoiceContent.Replace("{NICKNAME}", userName).Replace("#", "");
                enVoiceContent = Regex.Replace(enVoiceContent, pattern1, "");
                temp = Regex.Replace(temp, pattern2, "");
                enVoiceContent = Regex.Replace(enVoiceContent, pattern2, "");
                voiceContentDict[temp] = enVoiceContent;
            }
        }

        var contentJson = JsonConvert.SerializeObject(voiceContentDict, Formatting.Indented);
        File.WriteAllText(jsonFilePath, contentJson);
        return voiceContentDict;
    }


    public static string FindClosestMatch(string input, Dictionary<string, string> voiceContentDict)
    {
        string closestKey = null;
        int closestDistance = int.MaxValue;
        int length = input.Length;
        var keys = voiceContentDict.Keys.AsParallel();

        keys = keys.Where(key => !(length <= 5 && key.Length >= length * 3));

        keys.ForAll(key =>
        {

            string temp = key;
            if (length > 5 && temp.Length > length)
            {
                temp = temp.Substring(0, length);
            }

            int distance = CalculateLevenshteinDistance(input, temp);

            lock (voiceContentDict)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestKey = key;
                }
            }
        });
        //Console.WriteLine($"closestKey {closestKey} length {length} closestDistance {closestDistance}");
        if (closestDistance < length / 1.5)
        {
            return voiceContentDict[closestKey];
        }
        else
        {
            return "";
        }

    }

    private static int CalculateLevenshteinDistance(string a, string b)
    {

        if (string.IsNullOrEmpty(a)) return b.Length;
        if (string.IsNullOrEmpty(b)) return a.Length;

        int[] prev = new int[b.Length + 1];
        int[] curr = new int[b.Length + 1];

        for (int j = 0; j <= b.Length; j++)
            prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }

            var temp = prev;
            prev = curr;
            curr = temp;
        }
        return prev[b.Length];
    }
}
