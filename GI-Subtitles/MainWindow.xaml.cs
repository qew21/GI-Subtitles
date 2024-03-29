﻿using Emgu.CV.Dnn;
using PaddleOCRSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Timers;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;
using System.Configuration;


[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace GI_Subtitles
{
    public static class Logger
    {

        public static log4net.ILog Log = log4net.LogManager.GetLogger("LogFileAppender");
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int OCR_TIMER = 0;  
        private static int UI_TIMER = 0;
        private PaddleOCREngine engine;
        string ocrText = null;
        double Scale;
        private NotifyIcon notifyIcon;
        string lastRes = null;
        Dictionary<string, string> resDict = new Dictionary<string, string>();
        public System.Windows.Threading.DispatcherTimer OCRTimer = new System.Windows.Threading.DispatcherTimer();
        public System.Windows.Threading.DispatcherTimer UITimer = new System.Windows.Threading.DispatcherTimer();
        private bool isDraggable = false;
        string outpath = Path.Combine(Environment.CurrentDirectory, "out");
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);
        [DllImport("User32.dll")]
        private static extern int GetDpiForSystem();
        Dictionary<string, string> BitmapDict = new Dictionary<string, string>();
        string InputLanguage = ConfigurationManager.AppSettings["Input"];
        string OutputLanguage = ConfigurationManager.AppSettings["Output"];
        string userName = "Traveler";
        INotifyIcon notify;


        public MainWindow()
        {
            InitializeComponent();
            notify = new INotifyIcon();
            notifyIcon = notify.InitializeNotifyIcon();
            LoadEngine();
            string testFile = "testOCR.png";
            if (File.Exists(testFile))
            {
                OCRResult ocrResult = engine.DetectText(testFile);
                ocrText = ocrResult.Text;
                Console.WriteLine( ocrText );
            }
            else
            {
                if (OutputLanguage == "CHS")
                {
                    userName = "旅行者";
                }
                

                OCRTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                OCRTimer.Tick += GetOCR;    //委托，要执行的方法
                OCRTimer.Start();

                UITimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                UITimer.Tick += UpdateText;    //委托，要执行的方法
                UITimer.Start();

                SetWindowPos(new WindowInteropHelper(this).Handle, -1, 0, 0, 0, 0, 1 | 2 | 0x0010);
                Scale = GetDpiForSystem() / 96f;
                System.Drawing.Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                this.Width = workingArea.Width;
                this.Top = workingArea.Bottom / Scale - this.Height;
                this.Left = workingArea.Left / Scale;
            }
        }

        public void GetOCR(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref OCR_TIMER, 1) == 0)
            {
                Logger.Log.Debug("Start OCR");
                try
                {
                    System.Drawing.Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                    Bitmap target;
                    if (ConfigurationManager.AppSettings["Game"] == "Genshin")
                    {
                        target = CaptureRegion(Convert.ToInt16(workingArea.Left), Convert.ToInt16(workingArea.Bottom - 150 * Scale), Convert.ToInt16(workingArea.Width), Convert.ToInt16(65 * Scale));
                    } else
                    {
                        target = CaptureRegion(Convert.ToInt16(workingArea.Left), Convert.ToInt16(workingArea.Bottom - 190 * Scale), Convert.ToInt16(workingArea.Width), Convert.ToInt16(105 * Scale));
                    }
                    
                    target = ImageProcessor.EnhanceTextInImage(target);
                    string bitStr = Bitmap2String(target);
                    if (BitmapDict.ContainsKey(bitStr))
                    {
                        ocrText = BitmapDict[bitStr];
                    }
                    else
                    {
                        OCRResult ocrResult = engine.DetectText(target);
                        ocrText = ocrResult.Text;
                        if (false)
                        {
                            Logger.Log.Debug(DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss_ffffff") + ".png");
                            target.Save(DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss_ffffff") + ".png");
                            Logger.Log.Debug(ocrText);
                        }
                        var maxY = 0;
                        foreach (var i in ocrResult.TextBlocks)
                        {
                            foreach (var j in i.BoxPoints)
                            {
                                if (j.Y > maxY)
                                {
                                    maxY = j.Y;
                                }
                            }
                        }
                        this.Left = workingArea.Left;
                        this.Top = workingArea.Bottom / Scale - 20;
                        this.Width = workingArea.Width / Scale;
                        this.Height = 100;
                        BitmapDict.Add(bitStr, ocrText);
                        if (BitmapDict.Count > 10)
                        {
                            BitmapDict.Remove(BitmapDict.ElementAt(0).Key);
                        }
                    }
                    Logger.Log.Debug($"OCR Content: {ocrText}");
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex);
                }
                Interlocked.Exchange(ref OCR_TIMER, 0);
            }
        }

        public void UpdateText(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref UI_TIMER, 1) == 0)
            {
                Logger.Log.Debug("Start UI");
                try
                {
                    string res = "";
                    if (ocrText.Length > 1)
                    {
                        if (resDict.ContainsKey(ocrText))
                        {
                            res = resDict[ocrText];
                        }
                        else
                        {
                            res = VoiceContentHelper.FindClosestMatch(ocrText, notify.contentDict);
                            Logger.Log.Debug($"Convert ocrResult: {res}");
                            res = res.Replace("{NICKNAME}", userName);
                            resDict[ocrText] = res;
                            if (BitmapDict.Count > 10)
                            {
                                BitmapDict.Remove(BitmapDict.ElementAt(0).Key);
                            }
                        }
                    }
                    if (res != lastRes)
                    {
                        lastRes = res;
                        SubtitleText.Text = res;
                        SubtitleText.FontSize = Convert.ToInt16(ConfigurationManager.AppSettings["Size"]);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex);
                }
                Interlocked.Exchange(ref UI_TIMER, 0);
            }
        }

        public static string Bitmap2String(Bitmap bmp)
        {
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] arr = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(arr, 0, (int)ms.Length);
            ms.Close();
            return Convert.ToBase64String(arr);
        }


        public static Bitmap CaptureRegion(int x, int y, int width, int height)
        {
            using (Bitmap bitmap = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
                }
                return (Bitmap)bitmap.Clone();
            }
        }


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Dispose();
            notifyIcon = null;
        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && isDraggable)
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            notifyIcon.Dispose(); // 清理资源
        }

        public void LoadEngine()
        {
            if (!Directory.Exists(outpath))
            { Directory.CreateDirectory(outpath); }

            OCRModelConfig config = null;
            OCRParameter oCRParameter = new OCRParameter();
            oCRParameter.cpu_math_library_num_threads = 10;//预测并发线程数
            oCRParameter.enable_mkldnn = true;//web部署该值建议设置为0,否则出错，内存如果使用很大，建议该值也设置为0.
            oCRParameter.cls = false; //是否执行文字方向分类；默认false
            oCRParameter.det = false;//是否开启方向检测，用于检测识别180旋转
            oCRParameter.use_angle_cls = false;//是否开启方向检测，用于检测识别180旋转
            oCRParameter.det_db_score_mode = true;//是否使用多段线，即文字区域是用多段线还是用矩形，
            oCRParameter.max_side_len = 1560;
            oCRParameter.cls = true;
            oCRParameter.det = true;

            if (InputLanguage == "JP") {
                config = new OCRModelConfig();
                string root = System.IO.Path.GetDirectoryName(typeof(OCRModelConfig).Assembly.Location);
                string modelPathroot = root + @"\inference";
                config.det_infer = modelPathroot + @"\ch_PP-OCRv3_det_infer";
                config.cls_infer = modelPathroot + @"\ch_ppocr_mobile_v2.0_cls_infer";
                config.rec_infer = modelPathroot + @"\japan_PP-OCRv3_rec_infer";
                config.keys = modelPathroot + @"\japan_dict.txt";
                oCRParameter.max_side_len = 1560;
            }
            

            //初始化OCR引擎
            engine = new PaddleOCREngine(config, oCRParameter);
        }
    }
}
