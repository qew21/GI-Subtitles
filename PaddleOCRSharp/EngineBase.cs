// Copyright (c) 2021 raoyutian Authors. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.Drawing;
using System.Runtime.InteropServices;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace PaddleOCRSharp
{
    /// <summary>
    /// PaddleOCR识别引擎对象
    /// </summary>
    public abstract class EngineBase : IDisposable
    {
        /// <summary>
        /// PaddleOCR.dll自定义加载路径，默认为空，如果指定则需在引擎实例化前赋值。
        /// </summary>
        public static string PaddleOCRdllPath { get; set; }

        internal const string PaddleOCRdllName = "PaddleOCR.dll";
        internal const string yt_CPUCheckdllName = "yt_CPUCheck.dll";

        #region PaddleOCR API
        [DllImport(yt_CPUCheckdllName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern int IsCPUSupport();
        [DllImport("kernel32.dll")]
        private extern static IntPtr LoadLibrary(String path);
        #endregion

        /// <summary>
        /// 初始化
        /// </summary>
        public EngineBase()
        {
            //此行代码无实际意义，用于后面的JsonHelper.DeserializeObject的首次加速，首次初始化会比较慢，放在此处预热。
            var temp = JsonHelper.DeserializeObject<TextBlock>("{}");
            try
            {
                string osVersion = $"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}";
                if (osVersion == "6.1")
                {
                    #region win7
                    try
                    {
                        string root = GetRootDirectory();
                        string dllPath = root + @"\inference\win7_dll\";
                        if (Directory.Exists(dllPath))
                        {
                            string Envpath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Process);
                            if (!string.IsNullOrEmpty(Envpath))
                            {
                                Environment.SetEnvironmentVariable("path", Envpath + ";" + dllPath, EnvironmentVariableTarget.Process);
                            }
                        }
                    }
                    catch
                    {
                        throw new Exception($"Win7依赖dll动态加载失败。请手动复制文件夹【inference\\win7_dll】文件到PaddleOCR.dll目录。");
                    }
                    #endregion
                }

                if (!string.IsNullOrEmpty(PaddleOCRdllPath))
                {
                    string Envpath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Process);
                    if (!string.IsNullOrEmpty(Envpath))
                    {
                        Environment.SetEnvironmentVariable("path", Envpath + ";" + PaddleOCRdllPath, EnvironmentVariableTarget.Process);
                        LoadLibrary(System.IO.Path.Combine(PaddleOCRdllPath, PaddleOCRdllName));
                        LoadLibrary(System.IO.Path.Combine(PaddleOCRdllPath, "onnxruntime.dll"));
                    }
                }
            }
            catch (Exception e) 
            {
                throw new Exception("设置自定义加载路径失败。"+e.Message);
            }
          
        }
        #region private
        /// <summary>
        /// 获取程序的当前路径;
        /// </summary>
        /// <returns></returns>
        internal string GetRootDirectory()
        {
            string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            #if NET46_OR_GREATER || NETCOREAPP
            if(string.IsNullOrEmpty(root))
            {
            return AppContext.BaseDirectory;
            }  
            #endif
            return root;
        }

        /// <summary>
        /// 环境监测
        /// </summary>
        internal protected void CheckEnvironment()
        {
#if NET35
#else
            if (!Environment.Is64BitProcess) throw new Exception("暂不支持32位程序使用本OCR");
#endif
        }
        /// <summary>
        /// Convert Image to Byte[]
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        internal protected byte[] ImageToBytes(Image image)
        {
            ImageFormat format = image.RawFormat;
            using (MemoryStream ms = new MemoryStream())
            {
                if (format.Guid == ImageFormat.Jpeg.Guid)
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                else if (format.Guid == ImageFormat.Png.Guid)
                {
                    image.Save(ms, ImageFormat.Png);
                }
                else if (format.Guid == ImageFormat.Bmp.Guid)
                {
                    image.Save(ms, ImageFormat.Bmp);
                }
                else if (format.Guid == ImageFormat.Gif.Guid)
                {
                    image.Save(ms, ImageFormat.Gif);
                }
                else if (format.Guid == ImageFormat.Icon.Guid)
                {
                    image.Save(ms, ImageFormat.Icon);
                }
                else
                {
                    image.Save(ms, ImageFormat.Png);
                }
                byte[] buffer = new byte[ms.Length];
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        #endregion

        /// <summary>
        /// 释放内存
        /// </summary>
        public virtual void Dispose()
        {
        }

    }
}