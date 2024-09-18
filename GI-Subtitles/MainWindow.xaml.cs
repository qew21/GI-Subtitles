using Emgu.CV.Dnn;
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
using System.Media;
using static log4net.Appender.RollingFileAppender;
using System.Runtime.Remoting.Contexts;
using System.Reflection;
using System.Threading.Tasks;


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
        private NotifyIcon notifyIcon;
        string lastRes = null;
        readonly Dictionary<string, string> resDict = new Dictionary<string, string>();
        public System.Windows.Threading.DispatcherTimer OCRTimer = new System.Windows.Threading.DispatcherTimer();
        public System.Windows.Threading.DispatcherTimer UITimer = new System.Windows.Threading.DispatcherTimer();
        private bool isDraggable = true;
        readonly string outpath = Path.Combine(Environment.CurrentDirectory, "out");
        readonly bool debug = ConfigurationManager.AppSettings["Debug"] == "1";
        readonly bool mtuliline = ConfigurationManager.AppSettings["Multiline"] == "1";
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);
        [DllImport("User32.dll")]
        private static extern int GetDpiForSystem();
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_1 = 9000; // 自定义热键ID
        private const int HOTKEY_ID_2 = 9001; // 自定义热键ID
        private const uint MOD_CTRL = 0x0002; // Ctrl键
        private const uint MOD_SHIFT = 0x0004; // Shift键
        private const uint VK_S = 0x53; // S键的虚拟键码
        private const uint VK_R = 0x52; // R键的虚拟键码
        private readonly double Scale = GetDpiForSystem() / 96f;
        Dictionary<string, string> BitmapDict = new Dictionary<string, string>();
        string InputLanguage = ConfigurationManager.AppSettings["Input"];
        string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        INotifyIcon notify;
        Data data;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取窗口句柄
            IntPtr handle = new WindowInteropHelper(this).Handle;
            RegisterHotKey(handle, HOTKEY_ID_1, MOD_CTRL | MOD_SHIFT, VK_S);
            RegisterHotKey(handle, HOTKEY_ID_2, MOD_CTRL | MOD_SHIFT, VK_R);

            // 监听窗口消息
            HwndSource source = HwndSource.FromHwnd(handle);
            source.AddHook(WndProc);

            notify = new INotifyIcon();
            notifyIcon = notify.InitializeNotifyIcon(Scale);
            data = new Data(version);
            if (!data.FileExists())
            {
                data.ShowDialog();
            }
            else
            {
                Task.Run(async () => await data.Load());
            }
            LoadEngine();
            string testFile = "testOCR.png";
            if (File.Exists(testFile))
            {
                DateTime dateTime = DateTime.Now;
                OCRResult ocrResult = engine.DetectText(testFile);
                ocrText = ocrResult.Text;
                ocrText += '.';
                Console.WriteLine($"Convert ocrResult: {ocrText}, cost {(DateTime.Now - dateTime).TotalMilliseconds}ms");
                dateTime = DateTime.Now;
                string res = VoiceContentHelper.FindClosestMatch(ocrText, data.contentDict);
                Console.WriteLine($"Convert ocrResult: {res}, cost {(DateTime.Now - dateTime).TotalMilliseconds}ms");
            }

            OCRTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            OCRTimer.Tick += GetOCR;    //委托，要执行的方法


            UITimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            UITimer.Tick += UpdateText;    //委托，要执行的方法

            SetWindowPos(new WindowInteropHelper(this).Handle, -1, 0, 0, 0, 0, 1 | 2 | 0x0010);
            System.Drawing.Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            this.Width = workingArea.Width;
            this.Top = workingArea.Bottom / Scale - this.Height;
            this.Left = workingArea.Left / Scale;
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
                    if (notify.Region[1] == "0")
                    {
                        notify.ChooseRegion();
                    }
                    target = CaptureRegion(Convert.ToInt16(notify.Region[0]), Convert.ToInt16(notify.Region[1]), Convert.ToInt16(notify.Region[2]), Convert.ToInt16(notify.Region[3]));
                    var enhanced = ImageProcessor.EnhanceTextInImage(target);
                    string bitStr = Bitmap2String(enhanced);
                    if (BitmapDict.ContainsKey(bitStr))
                    {
                        ocrText = BitmapDict[bitStr];
                    }
                    else
                    {
                        OCRResult ocrResult = engine.DetectText(enhanced);
                        ocrText = ocrResult.Text;
                        if (debug)
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
                            DateTime dateTime = DateTime.Now;
                            res = VoiceContentHelper.FindClosestMatch(ocrText, data.contentDict);
                            Logger.Log.Debug($"Convert ocrResult: {res}");
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

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && isDraggable)
            {
                DragMove();
            }
        }


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.Dispose();
            notifyIcon = null;
            IntPtr handle = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(handle, HOTKEY_ID_1);
            UnregisterHotKey(handle, HOTKEY_ID_2);
        }


        public void SwitchIcon(string iconName)
        {
            Uri iconUri = new Uri($"pack://application:,,,/Resources/{iconName}");
            Stream iconStream = System.Windows.Application.GetResourceStream(iconUri).Stream;

            // 创建新的Icon对象
            Icon newIcon = new Icon(iconStream);

            // 更新NotifyIcon的图标
            notifyIcon.Icon = newIcon;
        }

        // 处理窗口消息
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                if (wParam.ToInt32() == HOTKEY_ID_1)
                {
                    if (OCRTimer.IsEnabled)
                    {
                        OCRTimer.Stop();
                        UITimer.Stop();
                        SystemSounds.Hand.Play();
                        SwitchIcon("mask.ico");
                    }
                    else
                    {
                        OCRTimer.Start();
                        UITimer.Start();
                        SystemSounds.Exclamation.Play();
                        SwitchIcon("running.ico");
                    }
                    handled = true;
                }
                else if (wParam.ToInt32() == HOTKEY_ID_2)
                {
                    notify.ChooseRegion();
                }
            }
            return IntPtr.Zero;
        }

        public void LoadEngine()
        {
            if (!Directory.Exists(outpath))
            { Directory.CreateDirectory(outpath); }

            OCRModelConfig config = null;
            OCRParameter oCRParameter = new OCRParameter
            {
                cpu_math_library_num_threads = 5,//预测并发线程数
                enable_mkldnn = true,//web部署该值建议设置为0,否则出错，内存如果使用很大，建议该值也设置为0.
                cls = false, //是否执行文字方向分类；默认false
                det = false,//是否开启方向检测，用于检测识别180旋转
                use_angle_cls = false,//是否开启方向检测，用于检测识别180旋转
                det_db_score_mode = false,//是否使用多段线，即文字区域是用多段线还是用矩形，
                max_side_len = 1560
            };
            oCRParameter.cls = mtuliline;
            oCRParameter.det = mtuliline;

            if (InputLanguage == "JP")
            {
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
