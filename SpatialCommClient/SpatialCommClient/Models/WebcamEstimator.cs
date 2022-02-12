using DlibDotNet;
using Emgu.CV;
using Emgu.CV.Structure;
using SpatialCommClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static DlibDotNet.ImageWindow;

namespace SpatialCommClient.Models
{
    class WebcamEstimator
    {
        MCvPoint3D32f PNT_NOSE = new MCvPoint3D32f(0, 0, 0);
        MCvPoint3D32f PNT_NOSE_DIR = new MCvPoint3D32f(0, 0, 1000);
        MCvPoint3D32f PNT_CHIN = new MCvPoint3D32f(0, -330, -65);
        MCvPoint3D32f PNT_LEYE = new MCvPoint3D32f(-225, 170, -135);
        MCvPoint3D32f PNT_REYE = new MCvPoint3D32f(225, 170, -135);
        MCvPoint3D32f PNT_LMOUTH = new MCvPoint3D32f(-150, 150, -125);
        MCvPoint3D32f PNT_RMOUTH = new MCvPoint3D32f(-150, 150, -125);
        public double FPS { get; set; } = 0;

        public WebcamEstimator(MainWindowViewModel mwvm)
        {

            using (var win = new ImageWindow())
            using (var detector = Dlib.GetFrontalFaceDetector())
            using (var sp = ShapePredictor.Deserialize("Assets/shape_predictor_68_face_landmarks.dat"))
            {
                VideoCapture capture = new VideoCapture(mwvm.SelectedCamera);
                DateTime lastFrame = DateTime.Now;
                while (true)
                {
                    Bitmap bmpSrc = capture.QueryFrame().ToBitmap();
                    Bitmap bmp = new Bitmap(160, 120, PixelFormat.Format24bppRgb);
                    Graphics g = Graphics.FromImage(bmp);
                    g.DrawImage(bmpSrc, 0, 0, 160, 120);




                    BitmapData bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
                    int numbytes = bmpdata.Stride * bmp.Height;
                    byte[] bytedata = new byte[numbytes];
                    IntPtr ptr = bmpdata.Scan0;
                    Marshal.Copy(ptr, bytedata, 0, numbytes);
                    uint stride = (uint)(bmp.Width * 3);
                    if (stride % 4 != 0)
                        stride = stride + (stride % 4);
                    using (var img = Dlib.LoadImageData<BgrPixel>(bytedata, (uint)bmp.Height, (uint)bmp.Width, stride))
                    {
                        Dlib.PyramidUp(img);

                        var dets = detector.Operator(img);

                        var shapes = new List<FullObjectDetection>();
                        foreach (var rect in dets)
                        {
                            var shape = sp.Detect(img, rect);
                            if (shape.Parts > 2)
                            {
                                shapes.Add(shape);
                            }
                        }

                        win.ClearOverlay();
                        win.SetImage(img);

                        if (shapes.Any())
                        {
                            var lines = Dlib.RenderFaceDetections(shapes);

                            win.AddOverlay(lines);

                            foreach (var l in lines)
                                l.Dispose();

                            MCvPoint3D32f[] FACE_PNTS = {
                        PNT_NOSE,
                        PNT_CHIN,
                        PNT_LEYE,
                        PNT_REYE,
                        PNT_LMOUTH,
                        PNT_RMOUTH
                    };

                            PointF[] IMAGE_PNTS =
                            {
                        ToPointF(shapes[0].GetPart(33)),
                        ToPointF(shapes[0].GetPart(8)),
                        ToPointF(shapes[0].GetPart(36)),
                        ToPointF(shapes[0].GetPart(45)),
                        ToPointF(shapes[0].GetPart(59)),
                        ToPointF(shapes[0].GetPart(55)),
                    };

                            Emgu.CV.Matrix<float> matrix = new Emgu.CV.Matrix<float>(3, 3)
                            {
                                Data = new float[,] {
                        { img.Columns, 0, img.Rows/2},
                        { 0, img.Columns, img.Columns/2},
                        { 0, 0, 1},
                    }
                            };
                            Emgu.CV.Matrix<float> distort = new Emgu.CV.Matrix<float>(1, 4)
                            {
                                Data = new float[,] { { 0 }, { 0 }, { 0 }, { 0 } }
                            };

                            Emgu.CV.Matrix<float> rotationMatrix = new Emgu.CV.Matrix<float>(3, 1);
                            Emgu.CV.Matrix<float> translationVector = new Emgu.CV.Matrix<float>(3, 1);

                            _ = CvInvoke.SolvePnP(FACE_PNTS, IMAGE_PNTS, matrix, distort, rotationMatrix, translationVector);

                            mwvm.HeadPosition = MatrixToString(translationVector);
                            mwvm.HeadRotation = MatrixToString(rotationMatrix);

                            FPS = (1000 / ((DateTime.Now - lastFrame).TotalMilliseconds));

                            PointF[] pnts = CvInvoke.ProjectPoints(new MCvPoint3D32f[] { PNT_NOSE_DIR }, rotationMatrix, translationVector, matrix, distort);
                            win.AddOverlay(new OverlayLine(shapes[0].GetPart(33), new DlibDotNet.Point((int)pnts[0].X, (int)pnts[0].Y), new BgrPixel(0, 255, 0)));
                            lastFrame = DateTime.Now;
                        }

                        foreach (var s in shapes)
                            s.Dispose();
                    }
                }
            }
        }

        private PointF ToPointF(DlibDotNet.Point pnt)
        {
            return new PointF(pnt.X, pnt.Y);
        }


        private string MatrixToString(Emgu.CV.Matrix<float> matrix)
        {
            string rtn = "{";
            for (int y = 0; y<matrix.Rows; y++)
            {
                rtn += "{";
                for (int x = 0; x < matrix.Cols; x++)
                {
                    rtn += matrix[y, x].ToString() + ", ";
                }
                rtn += "}";
            }

            return rtn;
        }

    }
}
