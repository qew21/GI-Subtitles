﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;

namespace GI_Subtitles
{
    internal class INotifyIcon
    {
        System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        ToolStripMenuItem gameSelector;
        ToolStripMenuItem inputSelector;
        ToolStripMenuItem outputSelector;
        ToolStripMenuItem fontSizeSelector;
        string Game = ConfigurationManager.AppSettings["Game"];
        string InputLanguage = ConfigurationManager.AppSettings["Input"];
        string OutputLanguage = ConfigurationManager.AppSettings["Output"];
        string Size = ConfigurationManager.AppSettings["Size"];
        public Dictionary<string, string> contentDict = new Dictionary<string, string>();


        public NotifyIcon InitializeNotifyIcon()
        {
            NotifyIcon notifyIcon;
            contextMenuStrip = new ContextMenuStrip();

            gameSelector = new ToolStripMenuItem("游戏选择");
            gameSelector.DropDownItems.Add(CreateGameItem("Genshin", "原神"));
            gameSelector.DropDownItems.Add(CreateGameItem("StarRail", "星穹铁道"));

            inputSelector = new ToolStripMenuItem("语言选择");
            inputSelector.DropDownItems.Add(CreateInputItem("CHS", "简体中文"));
            inputSelector.DropDownItems.Add(CreateInputItem("EN", "English"));
            inputSelector.DropDownItems.Add(CreateInputItem("JP", "日本語"));

            outputSelector = new ToolStripMenuItem("输出选择");
            outputSelector.DropDownItems.Add(CreateOutputItem("CHS", "简体中文"));
            outputSelector.DropDownItems.Add(CreateOutputItem("EN", "English"));
            outputSelector.DropDownItems.Add(CreateOutputItem("JP", "日本語"));
            outputSelector.DropDownItems.Add(CreateOutputItem("CHT", "繁體中文"));
            outputSelector.DropDownItems.Add(CreateOutputItem("DE", "Deutsch"));
            outputSelector.DropDownItems.Add(CreateOutputItem("ES", "Español"));
            outputSelector.DropDownItems.Add(CreateOutputItem("FR", "Français"));
            outputSelector.DropDownItems.Add(CreateOutputItem("ID", "Bahasa Indonesia"));
            outputSelector.DropDownItems.Add(CreateOutputItem("KR", "한국어"));
            outputSelector.DropDownItems.Add(CreateOutputItem("PT", "Português"));
            outputSelector.DropDownItems.Add(CreateOutputItem("RU", "Русский"));
            outputSelector.DropDownItems.Add(CreateOutputItem("TH", "ไทย"));
            outputSelector.DropDownItems.Add(CreateOutputItem("VI", "Tiếng Việt"));

            fontSizeSelector = new ToolStripMenuItem("字号选择");
            fontSizeSelector.DropDownItems.Add(CreateSizeItem("10"));
            fontSizeSelector.DropDownItems.Add(CreateSizeItem("12"));
            fontSizeSelector.DropDownItems.Add(CreateSizeItem("14"));
            fontSizeSelector.DropDownItems.Add(CreateSizeItem("16"));
            fontSizeSelector.DropDownItems.Add(CreateSizeItem("18"));
            fontSizeSelector.DropDownItems.Add(CreateSizeItem("20"));


            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出程序");
            exitItem.Click += (sender, e) => { System.Windows.Application.Current.Shutdown(); }; 
            contextMenuStrip.Items.Add(gameSelector);
            contextMenuStrip.Items.Add(inputSelector);
            contextMenuStrip.Items.Add(outputSelector);
            contextMenuStrip.Items.Add(fontSizeSelector);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(exitItem);



            Uri iconUri = new Uri("pack://application:,,,/Resources/mask.ico");
            Stream iconStream = System.Windows.Application.GetResourceStream(iconUri).Stream;
            notifyIcon = new NotifyIcon
            {
                Icon = new Icon(iconStream),
                Visible = true,
                ContextMenuStrip = contextMenuStrip
            };
            if (File.Exists($"{Game}\\TextMap{InputLanguage}.json") && File.Exists($"{Game}\\TextMap{OutputLanguage}.json"))
            {
                contentDict = VoiceContentHelper.CreateVoiceContentDictionary($"{Game}\\TextMap{InputLanguage}.json", $"{Game}\\TextMap{OutputLanguage}.json");
            }
            else
            {
                MessageBox.Show($"请在{Game}文件夹内放置{InputLanguage}.json和{OutputLanguage}.json的语言包");
            }
            return notifyIcon;
        }

        private ToolStripMenuItem CreateGameItem(string code, string displayName)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(displayName);
            item.Tag = code; 
            item.CheckOnClick = true; 
            item.CheckedChanged += GameItem_CheckedChanged;

            
            if (Game == code)
            {
                item.Checked = true;
            }
            return item;
        }

        private void GameItem_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem selectedGame = sender as ToolStripMenuItem;
            if (selectedGame != null && selectedGame.Checked)
            {
                string newGame = selectedGame.Tag.ToString();
                if (Game != newGame)
                {
                    Game = newGame;

                    foreach (ToolStripMenuItem langItem in gameSelector.DropDownItems)
                    {
                        if (langItem != selectedGame)
                        {
                            langItem.Checked = false;
                        }
                    }
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["Game"].Value = newGame;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    if (File.Exists($"{Game}\\TextMap{InputLanguage}.json") && File.Exists($"{Game}\\TextMap{OutputLanguage}.json"))
                    {
                        contentDict = VoiceContentHelper.CreateVoiceContentDictionary($"{Game}\\TextMap{InputLanguage}.json", $"{Game}\\TextMap{OutputLanguage}.json");
                    }
                    else
                    {
                        MessageBox.Show($"请在{Game}文件夹内放置{InputLanguage}.json和{OutputLanguage}.json的语言包");
                    }
                }
            }
        }

        private ToolStripMenuItem CreateInputItem(string code, string displayName)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(displayName);
            item.Tag = code; 
            item.CheckOnClick = true; 
            item.CheckedChanged += InputItem_CheckedChanged;

            if (InputLanguage == code)
            {
                item.Checked = true;
            }
            return item;
        }

        
        private void InputItem_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem selectedLanguage = sender as ToolStripMenuItem;
            if (selectedLanguage != null && selectedLanguage.Checked)
            {
                string newLanguage = selectedLanguage.Tag.ToString();
                if (InputLanguage != newLanguage)
                {
                    InputLanguage = newLanguage;

                    foreach (ToolStripMenuItem langItem in inputSelector.DropDownItems)
                    {
                        if (langItem != selectedLanguage)
                        {
                            langItem.Checked = false;
                        }
                    }
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["Input"].Value = newLanguage;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");

                    System.Windows.Forms.Application.Restart();
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private ToolStripMenuItem CreateOutputItem(string code, string displayName)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(displayName);
            item.Tag = code; 
            item.CheckOnClick = true; 
            item.CheckedChanged += OutputItem_CheckedChanged;

            if (OutputLanguage == code)
            {
                item.Checked = true;
            }
            return item;
        }

        private void OutputItem_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem selectedOutput = sender as ToolStripMenuItem;
            if (selectedOutput != null && selectedOutput.Checked)
            {
                string newOutput = selectedOutput.Tag.ToString();
                if (OutputLanguage != newOutput)
                {
                    OutputLanguage = newOutput;

                    foreach (ToolStripMenuItem langItem in outputSelector.DropDownItems)
                    {
                        if (langItem != selectedOutput)
                        {
                            langItem.Checked = false;
                        }
                    }
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["Output"].Value = newOutput;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    if (File.Exists($"{Game}\\TextMap{InputLanguage}.json") && File.Exists($"{Game}\\TextMap{OutputLanguage}.json"))
                    {
                        contentDict = VoiceContentHelper.CreateVoiceContentDictionary($"{Game}\\TextMap{InputLanguage}.json", $"{Game}\\TextMap{OutputLanguage}.json");
                    }
                    else
                    {
                        MessageBox.Show($"请在{Game}文件夹内放置{InputLanguage}.json和{OutputLanguage}.json的语言包");
                    }

                }
            }
        }

        private ToolStripMenuItem CreateSizeItem(string code)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(code);
            item.Tag = code; 
            item.CheckOnClick = true; 
            item.CheckedChanged += SizeItem_CheckedChanged;
            if (Size == code)
            {
                item.Checked = true;
            }
            return item;
        }

        private void SizeItem_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem selectedSize = sender as ToolStripMenuItem;
            if (selectedSize != null && selectedSize.Checked)
            {
                string newSize = selectedSize.Tag.ToString();
                if (Size != newSize)
                {
                    Size = newSize;

                    foreach (ToolStripMenuItem langItem in fontSizeSelector.DropDownItems)
                    {
                        if (langItem != selectedSize)
                        {
                            langItem.Checked = false;
                        }
                    }
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.AppSettings.Settings["Size"].Value = newSize;
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                }
            }
        }
    }
}
