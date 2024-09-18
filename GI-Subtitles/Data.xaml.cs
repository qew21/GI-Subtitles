using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Net.Http;
using Newtonsoft.Json;
using System.Configuration;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace GI_Subtitles
{
    /// <summary>
    /// Data.xaml 的交互逻辑
    /// </summary>
    public partial class Data : Window
    {
        string repoUrl = "https://gitlab.com/Dimbreath/AnimeGameData/-/refs/master/logs_tree/TextMap?format=json&offset=0&ref_type=heads";
        string Game = ConfigurationManager.AppSettings["Game"];
        string InputLanguage = ConfigurationManager.AppSettings["Input"];
        string OutputLanguage = ConfigurationManager.AppSettings["Output"];
        string userName = "Traveler";
        private const int MaxRetries = 1; // 最大重试次数
        private static readonly HttpClient client = new HttpClient();
        public Dictionary<string, string> contentDict = new Dictionary<string, string>();

        Stopwatch sw = new Stopwatch();

        public Data(string version)
        {
            InitializeComponent();
            this.Title += $"({version})";
            GameSelector.SelectionChanged += OnGameSelectorChanged;
            InputSelector.SelectionChanged += OnInputSelectorChanged;
            OutputSelector.SelectionChanged += OnOutputSelectorChanged;
            DownloadURL1.Text = $"https://gitlab.com/Dimbreath/AnimeGameData/-/raw/master/TextMap/TextMap{InputLanguage}.json?inline=false";
            DownloadURL2.Text = $"https://gitlab.com/Dimbreath/AnimeGameData/-/raw/master/TextMap/TextMap{OutputLanguage}.json?inline=false";
            if (Game == "StarRail")
            {
                repoUrl = "https://api.github.com/repos/Dimbreath/StarRailData";
                DownloadURL1.Text = $"https://raw.kkgithub.com/Dimbreath/StarRailData/master/TextMap/TextMap{InputLanguage}.json";
                DownloadURL2.Text = $"https://raw.kkgithub.com/Dimbreath/StarRailData/master/TextMap/TextMap{OutputLanguage}.json";
            }
            DisplayLocalFileDates();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
        }

        public async Task Load()
        {
            await CheckDataAsync();
        }

        private async void OnGameSelectorChanged(object sender, SelectionChangedEventArgs e)
        {
            Dictionary<string, string> GameDict = new Dictionary<string, string>
            {
                ["原神"] = "Genshin",
                ["星穹铁道"] = "StarRail",
            };

            System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox == null)
            {
                return;
            }

            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string newValue = GameDict[selectedItem.Content.ToString()];
                if (Game != newValue)
                {
                    Game = newValue;

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["Game"].Value = newValue;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    await CheckDataAsync();
                }
            }
        }

        private async void OnInputSelectorChanged(object sender, SelectionChangedEventArgs e)
        {
            Dictionary<string, string> InputLanguages = new Dictionary<string, string>()
            {
                { "简体中文", "CHS"},
                { "English", "EN"},
                { "日本語", "JP"}
            };
            System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox == null)
            {
                return;
            }

            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string newValue = InputLanguages[selectedItem.Content.ToString()];
                if (InputLanguage != newValue)
                {
                    InputLanguage = newValue;

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["Input"].Value = InputLanguage;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    await CheckDataAsync();
                }
            }
        }

        private async void OnOutputSelectorChanged(object sender, SelectionChangedEventArgs e)
        {
            Dictionary<string, string> OutputLanguages = new Dictionary<string, string>() { { "简体中文", "CHS" }, { "English", "EN" }, { "日本語", "JP" }, { "繁體中文", "CHT" }, { "Deutsch", "DE" }, { "Español", "ES" }, { "Français", "FR" }, { "Bahasa Indonesia", "ID" }, { "한국어", "KR" }, { "Português", "PT" }, { "Русский", "RU" }, { "ไทย", "TH" }, { "Tiếng Việt", "VI" } };
            System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
            if (comboBox == null)
            {
                return;
            }

            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string newValue = OutputLanguages[selectedItem.Content.ToString()];
                Console.WriteLine(newValue);
                if (OutputLanguage != newValue)
                {
                    OutputLanguage = newValue;

                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["Output"].Value = OutputLanguage;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    await CheckDataAsync();
                }
            }
        }
        public bool FileExists()
        {
            return File.Exists($"{Game}\\TextMap{InputLanguage}.json") &&
                              File.Exists($"{Game}\\TextMap{OutputLanguage}.json");
        }

        public async Task CheckDataAsync()
        {
            if (OutputLanguage == "CHS")
            {
                userName = "旅行者";
            }
            if (FileExists())
            {
                contentDict = await Task.Run(() =>
                    VoiceContentHelper.CreateVoiceContentDictionary(
                        $"{Game}\\TextMap{InputLanguage}.json",
                        $"{Game}\\TextMap{OutputLanguage}.json",
                        userName)
                    );
            }
            DisplayLocalFileDates();
        }

        private void DisplayLocalFileDates()
        {
            string localFile1 = $"{Game}\\TextMap{InputLanguage}.json";
            string localFile2 = $"{Game}\\TextMap{OutputLanguage}.json";
            if (File.Exists(localFile1))
            {
                DateTime modDate1 = File.GetLastWriteTime(localFile1);
                LocalFile1Date.Text = $"{localFile1}修改日期 {modDate1}";
            }
            else
            {
                LocalFile1Date.Text = $"{localFile1}不存在";
            }

            if (File.Exists(localFile2))
            {
                DateTime modDate2 = File.GetLastWriteTime(localFile2);
                LocalFile2Date.Text = $"{localFile2}修改日期 {modDate2}";
            }
            else
            {
                LocalFile2Date.Text = $"{localFile2}不存在";
            }
        }


        private async Task GetRepositoryModificationDateAsync()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(repoUrl);
                response.EnsureSuccessStatusCode();

                string responseText = await response.Content.ReadAsStringAsync();
                if (Game == "StarRail")
                {

                    dynamic json = JsonConvert.DeserializeObject(responseText);
                    RepoModifiedDate.Text = !string.IsNullOrEmpty(json.pushed_at.ToString()) ? json.pushed_at.ToString() : "无法获取 committed_date";
                }
                else
                {
                    JArray jsonArray = JArray.Parse(responseText);
                    if (jsonArray.Count > 0)
                    {
                        JObject firstElement = (JObject)jsonArray[0];
                        string committedDate = firstElement["commit"]?["committed_date"]?.ToString();
                        RepoModifiedDate.Text = !string.IsNullOrEmpty(committedDate) ? committedDate : "无法获取 committed_date";
                    }
                    else
                    {
                        RepoModifiedDate.Text = "响应列表为空";
                    }
                }

            }
            catch (Exception ex)
            {
                RepoModifiedDate.Text = "错误: " + ex.Message;
            }
        }

        private async void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            await GetRepositoryModificationDateAsync();
        }

        private async void DownloadButton1_Click(object sender, RoutedEventArgs e)
        {
            string localFile1 = $"{Game}\\TextMap{InputLanguage}.json";
            await DownloadFileAsync(DownloadURL1.Text, localFile1);
        }

        private async void DownloadButton2_Click(object sender, RoutedEventArgs e)
        {
            string localFile2 = $"{Game}\\TextMap{OutputLanguage}.json";
            await DownloadFileAsync(DownloadURL2.Text, localFile2);
            await CheckDataAsync();
        }

        private async Task DownloadFileAsync(string url, string fileName)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                System.Windows.MessageBox.Show("无效的下载 URL");
                return;
            }

            int attempt = 0;
            bool success = false;
            long existingLength = 0;
            string tmpFileName = fileName.Replace("json", "jsontmp");


            while (attempt < MaxRetries && !success)
            {
                try
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {

                        sw.Start();
                        using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();

                            // 获取总大小
                            long totalBytes = response.Content.Headers.ContentLength.Value;

                            using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                          fileStream = new FileStream(tmpFileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    existingLength += bytesRead;

                                    // 更新进度
                                    double progressPercentage = (double)existingLength / totalBytes * 100;
                                    DownloadProgressBar.Value = progressPercentage;

                                    // 计算下载速度
                                    double speed = existingLength / 1024d / sw.Elapsed.TotalSeconds;
                                    DownloadSpeedText.Text = $"{speed:0.00} KB/s";
                                }
                            }
                        }
                    }

                    sw.Reset();
                    if (File.Exists(tmpFileName))
                    {
                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }
                        File.Move(tmpFileName, fileName);
                        string directoryPath = Path.GetDirectoryName(fileName);

                        string baseFileName = Path.GetFileNameWithoutExtension(fileName);
                        string[] matchingFiles = Directory.GetFiles(directoryPath);
                        foreach (string file in matchingFiles)
                        {
                            try
                            {
                                string baseName = Path.GetFileNameWithoutExtension(file);
                                if (baseName.Contains(baseFileName) && baseName.Contains("_"))
                                {
                                    File.Delete(file);
                                    Logger.Log.Info($"Deleted: {file}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log.Error($"Failed to delete {file}: {ex.Message}");
                            }
                        }
                    }

                    DisplayLocalFileDates(); // 更新本地文件日期
                    success = true;
                }
                catch (Exception ex)
                {
                    sw.Reset();
                    attempt++;
                    if (attempt >= MaxRetries)
                    {
                        System.Windows.MessageBox.Show($"下载错误: {ex.Message}");
                    }
                    else
                    {
                        await Task.Delay(2000);
                    }
                }
                finally
                {
                    DownloadProgressBar.Value = 0;
                    DownloadSpeedText.Text = "";
                }
            }
        }
    }
}
