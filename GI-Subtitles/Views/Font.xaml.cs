using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GI_Subtitles.Core.Config;

namespace GI_Subtitles.Views
{
    public class ViewListRow
    {
        public string FontName { get; set; }
        public string PreviewText { get; set; }
    }

    public partial class Font : Window
    {
        private ObservableCollection<ViewListRow> Row { get; set; }
        
        public Font()
        {
            InitializeComponent();
            
            Row = new ObservableCollection<ViewListRow>();
            AddFontListViewItems();
            FontListView.ItemsSource = Row;

            var prevFont = Config.Get<string>("Font");
            var prevFontIndexInList = Row.IndexOf(Row.First(t => t.FontName == prevFont));
            if (prevFontIndexInList != -1)
            {
                FontListView.SelectedIndex = prevFontIndexInList;
                FontListView.ScrollIntoView(FontListView.Items[prevFontIndexInList]);
            }
        }

        private void FontListView_Loaded(object sender, RoutedEventArgs e)
        {    
            double totalWidth = FontListView.ActualWidth;
            FontNameCol.Width = totalWidth * 0.20;
            FontPreviewCol.Width = totalWidth * 0.75;
        }

        private void AddFontListViewItems()
        {
            var fonts = Fonts.SystemFontFamilies.Select(f => f.Source).ToList();
            foreach (var fontName in fonts)
            {
                var item = new ViewListRow
                {
                    FontName = fontName,
                    PreviewText = SampleTextTextBox.Text,
                };
                Row.Add(item);
            }
        }

        private void SampleTextTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO
            // try...catch... is not a good way to handle the issue that the NullReferenceException is thrown because
            // a TextBox control is created and the method triggered while the TextBox itself not initialized
            // See the URL for a better solution: https://stackoverflow.com/a/33599469
            try
            {
                Row = new ObservableCollection<ViewListRow>();
                AddFontListViewItems();
                FontListView.ItemsSource = Row;
            }
            catch(NullReferenceException)
            { /* Do nothing */ }
        }

        private void Btn_FontPreviewSelect_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = (ViewListRow)FontListView.SelectedItem;
            if (selectedItem != null)
            {
                Config.Set("Font", selectedItem.FontName);
                App.ApplySubtitleFont(selectedItem.FontName);
                var result = MessageBox.Show(
                    string.Format(GetLocalizedString("FontSetMessage"), selectedItem.FontName),
                    GetLocalizedString("FontSetTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Asterisk);
                if (result == MessageBoxResult.Yes)
                    Close();
            }
            else
            {
                MessageBox.Show(
                    GetLocalizedString("FontChoosePrompt"),
                    GetLocalizedString("FontNoSelectionTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Btn_FontPreviewCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static string GetLocalizedString(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }
    }
}
