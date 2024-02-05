using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace GI_Subtitles
{
    public class ImageProcessor
    {
        public static Bitmap EnhanceTextInImage(Bitmap inputImage)
        {
            Image<Bgr, byte> img = new Image<Bgr, byte>(inputImage);

            // 转换为灰度图像
            Image<Gray, byte> gray = img.Convert<Gray, byte>();

            // 调整对比度（可选）
            double alpha = 1.5; // 对比度控制（1.0 - 3.0）
            gray._GammaCorrect(alpha);

            // 应用二值化
            double thresholdValue = 128; // 阈值（0 - 255）
            double maxValue = 255;       // 最大值
            gray = gray.ThresholdBinary(new Gray(thresholdValue), new Gray(maxValue));

            // 将处理后的Emgu CV Image转换回Bitmap
            Bitmap processedImage = gray.Bitmap;

            return processedImage;
        }

        public static Bitmap EnhanceAndExtractText(Bitmap inputImage)
        {
            Image<Gray, byte> img = new Image<Gray, byte>(inputImage);

            // 二值化
            double thresholdValue = 128;
            double maxValue = 255;
            img = img.ThresholdBinary(new Gray(thresholdValue), new Gray(maxValue));

            // 形态学操作：去除小的噪点
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
            CvInvoke.MorphologyEx(img, img, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Reflect, new MCvScalar(0));

            // 找到轮廓并提取最大轮廓区域
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hier = new Mat();
            CvInvoke.FindContours(img, contours, hier, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            if (contours.Size > 0)
            {
                // 假设最大轮廓是我们感兴趣的区域
                double maxArea = 0;
                int maxAreaIndex = 0;
                for (int i = 0; i < contours.Size; i++)
                {
                    double area = CvInvoke.ContourArea(contours[i]);
                    if (area > maxArea)
                    {
                        maxArea = area;
                        maxAreaIndex = i;
                    }
                }

                Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[maxAreaIndex]);
                img.ROI = boundingBox;
            }

            return img.ToBitmap();
        }

        public static Bitmap ExtractTextRegion(Bitmap inputImage)
        {
            Image<Gray, byte> img = new Image<Gray, byte>(inputImage);

            // 二值化
            img = img.ThresholdBinaryInv(new Gray(200), new Gray(255));

            // 形态学操作：去除小噪声
            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2, 2), new Point(-1, -1));
            CvInvoke.MorphologyEx(img, img, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Reflect, new MCvScalar(0));

            // 边缘检测
            Image<Gray, byte> cannyEdges = img.Canny(120, 180);

            // 找轮廓
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hier = new Mat();
            CvInvoke.FindContours(cannyEdges, contours, hier, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            // 找到最合适的轮廓
            Rectangle bestRect = Rectangle.Empty;
            double bestRectArea = 0;
            for (int i = 0; i < contours.Size; i++)
            {
                Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                double area = rect.Width * rect.Height;
                // 确保轮廓区域足够大，并且长宽比合适（根据实际文本的长宽比调整条件）
                if (area > bestRectArea && rect.Width > rect.Height)
                {
                    bestRect = rect;
                    bestRectArea = area;
                }
            }

            // 如果找到合适的轮廓，则裁剪图像
            Bitmap extractedTextRegion = null;
            if (!bestRect.IsEmpty)
            {
                extractedTextRegion = new Bitmap(bestRect.Width, bestRect.Height);
                using (Graphics g = Graphics.FromImage(extractedTextRegion))
                {
                    g.DrawImage(inputImage, 0, 0, bestRect, GraphicsUnit.Pixel);
                }
            }

            return extractedTextRegion;
        }
    }

}

