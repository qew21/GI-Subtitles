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
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Drawing;
using System.IO;

namespace PaddleOCRSharp
{
    /// <summary>
    /// PaddleOCR NET帮助类
    /// </summary>
    public  class PaddleStructureEngine:EngineBase
    {
        #region PaddleOCR API
       
        [DllImport(PaddleOCRdllName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern void StructureInitialize(string det_infer, string rec_infer, string keys, string table_model_dir, string table_char_dict_path, StructureParameter parameter);
        [DllImport(PaddleOCRdllName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern void StructureInitializejson(string det_infer, string rec_infer, string keys, string table_model_dir, string table_char_dict_path, string parameter);

        [DllImport(PaddleOCRdllName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr GetStructureDetectFile(  string imagefile);
       
        [DllImport(PaddleOCRdllName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr GetStructureDetectByte(  byte[] imagebytedata, long size);

        [DllImport(PaddleOCRdllName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr GetStructureDetectBase64( string imagebase64);
       
        [DllImport(PaddleOCRdllName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern void FreeStructureEngine( );
        #endregion

        /// <summary>
        /// PaddleStructureEngine识别引擎对象初始化
        /// </summary>
        /// <param name="config">模型配置对象，如果为空则按默认值</param>
        /// <param name="parameter">识别参数，为空均按缺省值</param>
        public PaddleStructureEngine(StructureModelConfig config, StructureParameter parameter) : base()
        {
            if (IsCPUSupport() <= 0) throw new NotSupportedException($"当前CPU的指令集不支持PaddleOCR");
           
            if (parameter == null) parameter = new StructureParameter();
            if (config == null)
            {
                string root = GetRootDirectory();
                config = new StructureModelConfig();
                string modelPathroot = root + @"\inference";
             
                config.det_infer = modelPathroot + @"\ch_PP-OCRv3_det_infer";
                config.rec_infer = modelPathroot + @"\ch_PP-OCRv3_rec_infer";
                config.keys = modelPathroot + @"\ppocr_keys.txt";
                config.table_model_dir = modelPathroot + @"\ch_ppstructure_mobile_v2.0_SLANet_infer";
                config.table_char_dict_path = modelPathroot + @"\table_structure_dict_ch.txt";
            }
            StructureInitialize(config.det_infer,  config.rec_infer, config.keys, config.table_model_dir, config.table_char_dict_path, parameter);
        }
        /// <summary>
        /// PaddleStructureEngine识别引擎对象初始化
        /// </summary>
        /// <param name="config">模型配置对象，如果为空则按默认值</param>
        /// <param name="parameterjson">识别参数Json格式，为空均按缺省值</param>
        public PaddleStructureEngine(StructureModelConfig config, string parameterjson) : base()
        {
            if (IsCPUSupport() <= 0) throw new NotSupportedException($"当前CPU的指令集不支持PaddleOCR");

           
            if (config == null)
            {
                string root = GetRootDirectory();
                config = new StructureModelConfig();
                string modelPathroot = root + @"\inference";

                config.det_infer = modelPathroot + @"\ch_PP-OCRv3_det_infer";
                config.rec_infer = modelPathroot + @"\ch_PP-OCRv3_rec_infer";
                config.keys = modelPathroot + @"\ppocr_keys.txt";
                config.table_model_dir = modelPathroot + @"\ch_ppstructure_mobile_v2.0_SLANet_infer";
                config.table_char_dict_path = modelPathroot + @"\table_structure_dict_ch.txt";
            }
            if (string.IsNullOrEmpty(parameterjson))
            {
                parameterjson = GetRootDirectory();
                parameterjson += @"\inference\PaddleOCRStructure.config.json";
                if (!File.Exists(parameterjson)) throw new FileNotFoundException(parameterjson);
                parameterjson = File.ReadAllText(parameterjson);
            }
            StructureInitializejson(config.det_infer, config.rec_infer, config.keys, config.table_model_dir, config.table_char_dict_path, parameterjson);
        }
        /// <summary>
        /// 对图像文件进行表格文本识别
        /// </summary>
        /// <param name="imagefile">图像文件</param>
        /// <returns>表格识别结果</returns>
        public string StructureDetectFile(string imagefile)
        {
            if (!System.IO.File.Exists(imagefile)) throw new Exception($"文件{imagefile}不存在");
            IntPtr presult =  GetStructureDetectFile( imagefile);
            var result= Marshal.PtrToStringUni(presult);
            Marshal.FreeHGlobal(presult);   
            return result;
        }

        /// <summary>
        ///对图像对象进行表格文本识别
        /// </summary>
        /// <param name="image">图像</param>
        /// <returns>表格识别结果</returns>
        public string StructureDetect(Image image)
        {
            if (image == null) throw new ArgumentNullException("image");
            var imagebyte = ImageToBytes(image);
            var result = StructureDetect(imagebyte);
            imagebyte = null;
            return result;
        }
        /// <summary>
        /// 对图像Byte数组进行表格文本识别
        /// </summary>
        /// <param name="imagebyte">图像字节数组</param>
        /// <returns>表格识别结果</returns>
        public string StructureDetect(byte[] imagebyte)
        {
           if (imagebyte == null) throw new ArgumentNullException("imagebyte");
            IntPtr presult=  GetStructureDetectByte(imagebyte, imagebyte.LongLength);
            var  result= Marshal.PtrToStringUni(presult);
            Marshal.FreeHGlobal(presult);
            return result;
        }
        /// <summary>
        /// 对图像Base64进行表格文本识别
        /// </summary>
        /// <param name="imagebase64">图像Base64</param>
        /// <returns>表格识别结果</returns>
        public string StructureDetectBase64(string imagebase64)
        {
            if (imagebase64 == null || imagebase64 == "") throw new ArgumentNullException("imagebase64");
            IntPtr presult= GetStructureDetectBase64( imagebase64);
            var result = Marshal.PtrToStringUni(presult);
            Marshal.FreeHGlobal(presult);
            return result;
        }
        #region Dispose
        /// <summary>
        /// 释放对象
        /// </summary>
        public override void Dispose()
        {
            FreeStructureEngine();
        }
        #endregion
    }
}
