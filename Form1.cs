using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using System.IO;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace EMGUCV
{
    
    public partial class Form1 : Form
    {
        
        private Capture capture;        //takes images from camera as image frames
        
        HaarCascade face;
        HaarCascade eye;
        
        CascadeClassifier eyeglass;
        DBConn mydb;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        
        Stopwatch stopWatch = new Stopwatch();
        Image<Gray, byte>[] allimage;
        
        int count = 0;
        Classifier_Train Eigen_Recog = new Classifier_Train();
       // FisherClass Fish_Recog = new FisherClass();
       
        Image<Gray, float>[] EigenimageARR;
        //TestRecog t;
        double ROImargin = 1.00;
        double widthScale = 1.00;
        int ROIwidth = 200;
        int ROIheight = 200;
        string name = "Processing...";
        List<string> result;
        public Form1()
        {
            InitializeComponent();
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            eye = new HaarCascade("haarcascade_eye.xml");
            eyeglass = new CascadeClassifier("haarcascade_eye_tree_eyeglasses.xml");
            mydb = new DBConn();
            result = new List<string>();
            //t = new TestRecog();
            /*if(Eigen_Recog.IsTrained){
                EigenimageARR = Eigen_Recog.getEigenfaceArray();
                allimage = Eigen_Recog.getTrainingImage();
            }*/
            
        }

        
        
        private void button1_Click(object sender, EventArgs e)
        {            
                capture = new Capture();
                capture.QueryFrame();                
                Application.Idle += new EventHandler(ProcessFrame);
                                               
        }
        private void button2_Click(object sender, EventArgs e)
        {
            TrainFrame();
        }    
        private void button5_Click(object sender, EventArgs e)
        {
            if(Eigen_Recog.IsTrained){
                //imageBox7.Image = t.getAVGface(mydb.getTrainedImageList());
            }
            
        }
        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }
        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> ImageFrame = capture.QueryFrame();  //line 1
            

            if(ImageFrame != null){
                string matchedname;
                Image<Gray, byte> greyimage = ImageFrame.Convert<Gray, byte>();
                Image<Gray, byte> imageroi;
                
                
                
                stopWatch.Start();
                var faces = face.Detect(greyimage,1.3,6,HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT,new Size(100,100),new Size(300,300));
                if (faces.Length == 0)
                {
                    name = "Processing...";
                    result.Clear();
                }
                    Parallel.ForEach(faces, facecount =>
                    {
                        try
                        {
                            //if (Fish_Recog.IsTrained)
                            ImageFrame.ROI = new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), facecount.rect.Height);
                            imageroi = ImageFrame.Copy().Convert<Gray, byte>();
                            ImageFrame.ROI = new Rectangle();
                            if (Eigen_Recog.IsTrained)
                            {
                                imageroi._EqualizeHist();
                                //imageroi = convertLBP(imageroi, 1);
                                
                                //matchedname = Fish_Recog.FisherRecognize(imageroi);
                                //find the most relative face
                                if (result.Count == 21) {
                                    int max = 0;
                                    string mostFace = "";
                                    foreach (string value in result.Distinct())
                                    {
                                        System.Diagnostics.Debug.WriteLine("\"{0}\" occurs {1} time(s).", value, result.Count(v => v == value));
                                        if (result.Count(v => v == value) > max)
                                        {
                                            max = result.Count(v => v == value);
                                            mostFace = value;
                                        }
                                    }
                                    name = mostFace;
                                }
                                else
                                {
                                    matchedname = Eigen_Recog.Recognise(imageroi.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR));
                                    if (!matchedname.Equals("UnknownNull") && !matchedname.Equals("UnknownFace"))
                                    {
                                        Console.WriteLine(matchedname);
                                        result.Add(matchedname);
                                    }
                                    
                                }

                                ImageFrame.Draw(name, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.Red));
                                
                            }
                            
                            ImageFrame.Draw(new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), facecount.rect.Height), new Bgr(Color.LawnGreen), 2);

                            //detect eyeglass section
                            /*var eyeglasses = eyeglass.DetectMultiScale(imageroi, 1.3, 4, new Size(5, 5), new Size(50, 50));
                            foreach (var eyeglasseses in eyeglasses)
                            {
                                ImageFrame.Draw(new Rectangle(facecount.rect.X + eyeglasseses.X, facecount.rect.Y + eyeglasseses.Y, eyeglasseses.Width, eyeglasseses.Height), new Bgr(Color.Aquamarine), 2);
                            }*/
                            
                            
                            //ImageFrame.Draw(new CircleF(new PointF(facecount.rect.X + facecount.rect.Width / 2, facecount.rect.Y + facecount.rect.Height / 2), facecount.rect.Width / 2), new Bgr(Color.Green), 3);
                        }
                        catch(Exception e)
                        {
                            Console.Write(e);
                        }


                    });
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value. 
                    string elapsedTime = String.Format("{0}",

                        ts.TotalMilliseconds * 10000);
                    textBox2.Text = elapsedTime;
                    listView1.Items.Add(elapsedTime);

                    stopWatch.Reset();
                    CvInvoke.cvSmooth(ImageFrame, ImageFrame, SMOOTH_TYPE.CV_GAUSSIAN, 1, 1, 1, 1);
                    
                imageBox1.Image = ImageFrame;//line 2
                
            }
            
            
        }
        private void TrainFrame()
        {
            try
            {
                string tempPath = "E:/Images/tmp.jpg";
                Image<Bgr, Byte> ImageFrame = capture.QueryFrame();  //line 1
                Image<Gray, byte> darkimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                Image<Gray, byte> cropimage = new Image<Gray, byte>(ROIwidth, ROIheight);

                //ArrayList pic = new ArrayList();
                if (ImageFrame != null)
                {
                    Image<Gray, byte> greyimage = ImageFrame.Convert<Gray, byte>();


                    var faces = face.Detect(greyimage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(100, 100), new Size(300, 300));
                    if (faces.Length > 0)
                    {
                        foreach (var facecount in faces)
                        {
                            ImageFrame.Draw(facecount.rect, new Bgr(Color.Red), 2);
                            ImageFrame.Draw(facecount.rect.Height + "," + facecount.rect.Width, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.LightGreen));
                            greyimage.ROI = new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y,(int)(facecount.rect.Width * widthScale), facecount.rect.Height);
                            // CropFrame = greyimage.Copy();
                            //pic.Add(CropFrame);
                        }
                        //get bigger face in frame
                        cropimage = greyimage.Resize(ROIwidth,ROIheight, INTER.CV_INTER_LINEAR);
                        if (!cropimage.Equals(darkimage)){
                            //cropimage._EqualizeHist();
                            //CvInvoke.cvSmooth(cropimage, cropimage, SMOOTH_TYPE.CV_GAUSSIAN, 1, 1, 1, 1);
                            //cropimage = Eigen_Recog.convertLBP(cropimage,1);
                            imageBox7.Image = cropimage;     //line 2


                            cropimage.Save(tempPath);
                            mydb.InsertImageTraining(textBox1.Text, tempPath);

                            //File.Delete(tempPath);
                            Eigen_Recog.reloadData();
                            //Fish_Recog.reloadData();
                        }
                        
                    }
                    imageBox8.Image = cropimage;
                }
            }
            catch
            {
               // MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }            
        }
        private void SpecialTrainFrame()
        {
            try
            {
                string tempPath = "E:/Images/tmp.jpg";
                Image<Bgr, Byte> ImageFrame = capture.QueryFrame();  //line 1
                Image<Gray, byte> darkimage = new Image<Gray, byte>(200,200);
                Image<Gray, byte> cropimage = new Image<Gray, byte>(200, 200);
                Image<Gray, byte> resultimage = new Image<Gray, byte>(200, 200);
                List<Image<Gray, byte>> ImageList = new List<Image<Gray, byte>>();

                for (int i = 0; i<10;i++ )
                {
                    if (ImageFrame != null)
                    {
                        Image<Gray, byte> greyimage = ImageFrame.Convert<Gray, byte>();


                        var faces = face.Detect(greyimage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(100, 100), new Size(300, 300));
                        if (faces.Length > 0)
                        {
                            foreach (var facecount in faces)
                            {
                                ImageFrame.Draw(facecount.rect, new Bgr(Color.Red), 2);
                                ImageFrame.Draw(facecount.rect.Height + "," + facecount.rect.Width, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.LightGreen));
                                greyimage.ROI = new Rectangle(facecount.rect.X, facecount.rect.Y, facecount.rect.Width, facecount.rect.Height);
                                // CropFrame = greyimage.Copy();
                                //pic.Add(CropFrame);
                            }
                            //get bigger face in frame
                            cropimage = greyimage.Resize(200, 200, INTER.CV_INTER_LINEAR);
                            if (!cropimage.Equals(darkimage))
                            {
                                cropimage._EqualizeHist();
                                CvInvoke.cvSmooth(cropimage, cropimage, SMOOTH_TYPE.CV_GAUSSIAN, 1, 1, 1, 1);
                                ImageList.Add(cropimage);
                            }

                        }
                        imageBox8.Image = cropimage;
                    }
                }
                //resultimage = t.getAVGface(ImageList.ToArray());
                resultimage.Save(tempPath);
                mydb.InsertImageTraining(textBox1.Text, tempPath);

                //File.Delete(tempPath);
                Eigen_Recog.reloadData();
                
            }
            catch
            {
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void testEyeglass(object sender, EventArgs arg)
        {

            float[,] sobelA = new float[3, 3] { { -4, 8, -4 }, { -5, 10, -5 }, { -4, 8, -4 } };
            float[,] sobelB = new float[3, 3] { { 6, -4, -3 }, { -4, 10, -4 }, { -3, -4, 6 } };
            float[,] sobelC = new float[3, 3] { { -4, -5, -4 }, { 8,10,8 }, { -4, -5, -4 } };
            float[,] sobelD = new float[3, 3] { { -3, -4, 6 }, { -5, 10, -5 }, { 6, -4, -3 } };
            float[,] sobel2A = new float[3, 3] { { -1, 0, 1 }, { -2,0,2 }, { -1, 0, 1 } };
            float[,] sobel2B = new float[3, 3] { { 0, 1, 2 }, { -1, 0, 1 }, { -2,-1,0 } };
            float[,] sobel2C = new float[3, 3] { { 1,2,1 }, {0,0,0 }, { -1,-2,-1 } };
            float[,] sobel2D = new float[3, 3] { { -2,-1,0 }, { -1,0,1 }, { 0,1,2 } };
            Matrix<float> sobel0 = new Matrix<float>(sobelA);
            Matrix<float> sobel45 = new Matrix<float>(sobelB);
            Matrix<float> sobel90 = new Matrix<float>(sobelC);
            Matrix<float> sobel135 = new Matrix<float>(sobelD);
            Matrix<float> sobel0x = new Matrix<float>(sobel2A);
            Matrix<float> sobel45x = new Matrix<float>(sobel2B);
            Matrix<float> sobel90x = new Matrix<float>(sobel2C);
            Matrix<float> sobel135x = new Matrix<float>(sobel2D);
            Point anc = new Point();
            anc.X = -1;
            anc.Y = -1;
            Image<Gray, byte> test = new Image<Gray, byte>("test.bmp");
            imageBox7.Image = test;
            Image<Gray, float> test2 = test.Laplace(1);
            Image<Gray, float> test0 = new Image<Gray, float>(200, 200);
            Image<Gray, float> test45 = new Image<Gray, float>(200, 200);
            Image<Gray, float> test90 = new Image<Gray, float>(200, 200);
            Image<Gray, float> test135 = new Image<Gray, float>(200, 200);
            Image<Gray, float> test0x = new Image<Gray, float>(200, 200);
            Image<Gray, float> test45x = new Image<Gray, float>(200, 200);
            Image<Gray, float> test90x = new Image<Gray, float>(200, 200);
            Image<Gray, float> test135x = new Image<Gray, float>(200, 200);
            CvInvoke.cvFilter2D(test, test0, sobel0, anc);
            CvInvoke.cvFilter2D(test, test45, sobel45, anc);
            CvInvoke.cvFilter2D(test, test90, sobel90, anc);
            CvInvoke.cvFilter2D(test, test135, sobel135, anc);
            CvInvoke.cvFilter2D(test, test0x, sobel0x, anc);
            CvInvoke.cvFilter2D(test, test45x, sobel45x, anc);
            CvInvoke.cvFilter2D(test, test90x, sobel90x, anc);
            CvInvoke.cvFilter2D(test, test135x, sobel135x, anc);
            Image<Gray, byte> test4 = new Image<Gray, byte>(200,200);
            CvInvoke.cvInpaint(test, test2.Convert<Gray, byte>().Canny(29, 200).Dilate(1), test4, 7, INPAINT_TYPE.CV_INPAINT_NS);
            imageBox8.Image = ((test0 + test45 + test90 + test135 + test0x + test45x + test90x + test135x) / 8).Laplace(3).Convert<Gray, byte>();//.Canny(trackBar3.Value, trackBar2.Value);
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Eigen_Recog.Set_Eigen_Threshold = trackBar1.Value;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Idle += new EventHandler(testEyeglass); 
        }

        private void button4_Click(object sender, EventArgs e)
        {
           
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //Image<Gray, byte>[] dif = t.getDiffFace();
            //imageBox9.Image = dif[0];
        }
       
                        
    }
}
