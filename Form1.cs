using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Timers;

using System.Windows.Forms;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;

using System.IO;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace EMGUCV
{
    
    public partial class Form1 : Form
    {
        private Thread t;
        private DBConn mydb;
        private MCvFont font;
        private Stopwatch stopWatch = new Stopwatch();
        private Capture capture;
        private HaarCascade face;
        private HaarCascade calcface;

        private List<string> recogNameResult;
        private List<double> recogDistanceResult;
        private double meanDistance;

        private Image<Bgr, Byte> imageFrame;
        private Image<Bgr, Byte> drawFrame;

        private Point facePosition;
        private Rectangle faceRectangle;
        private Size faceRectangleSize;

        private Point realFacePosition;
        private Rectangle realfaceRectangle;
        private Size realfaceRectangleSize;

        private Image<Gray, byte> imageroi;
        private Image<Gray, byte> learnImage;
        private string matchedResult;

        private Size ROIFaceSize = new Size(140,175);

        private int maxImageCount = 21;
        private Classifier_Train eigenRecog = new Classifier_Train();
        private CascadeClassifier eyeWithGlass;
        private CascadeClassifier nose;
        private CascadeClassifier mouth;
        private Size minEye;
        private Size maxEye;
        private Size minNose;
        private Size maxNose;
        private Size minMouth;
        private Size maxMouth;
        private Point[] coord;
        private double ROImargin = 1.00;
        private double widthScale = 1.00;
        private int ROIwidth = 140;
        private int ROIheight = 175;
        private bool learningTag = true;
        private System.Timers.Timer timer;

        private string name = "Processing...";
        private string tempPath = "E:/Images/tmp.jpg";
        private string logFolder = "E:/Images/log/";
        private string logName;

        public Form1()
        {
            InitializeComponent();
            
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            calcface = new HaarCascade("haarcascade_frontalface_default.xml");
            eyeWithGlass = new CascadeClassifier("haarcascade_eye_tree_eyeglasses.xml");
            nose = new CascadeClassifier("haarcascade_mcs_nose.xml");
            mouth = new CascadeClassifier("haarcascade_mcs_mouth.xml");

            mydb = new DBConn();

            recogNameResult = new List<string>();
            recogDistanceResult = new List<double>();
            minEye = new Size(10, 10);
            maxEye = new Size(225, 225);
            minNose = new Size(10, 10);
            maxNose = new Size(225, 225);
            minMouth = new Size(10, 10);
            maxMouth = new Size(225, 225);
            font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);    

            //log record
            DateTime now = DateTime.Now;
            logName = now.ToString();
            logName = logName.Replace("/", "").Replace(":", "").Replace(" ", "");      
        }
     
        private void button1_Click(object sender, EventArgs e)
        {            
                capture = new Capture();
                Console.WriteLine("resolution:"+capture.Height +","+capture.Width);
                //Form1.CheckForIllegalCrossThreadCalls = false;
                t = new Thread(delegate()
                {
                    timer = new System.Timers.Timer(TimeSpan.FromSeconds(5).TotalMilliseconds); // set the time

                    timer.AutoReset = true;

                    timer.Elapsed += new System.Timers.ElapsedEventHandler(updateDistanceTreshold);

                    timer.Start();
                    
                });
                t.Start();
                
                Application.Idle += new EventHandler(ProcessFrame);
                Application.Idle += new EventHandler(runningFrame);
                Application.Idle += new EventHandler(runningCropFrame);
                button1.Enabled = false;
                button2.Enabled = false;  
        }
        
        private void updateDistanceTreshold(object sender, EventArgs e)
        {

            Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt"));
            
            Image<Bgr, byte> calcFrame = capture.QueryFrame();
            if (calcFrame != null){
                Image<Gray, byte> calcGrayFrame = calcFrame.Convert<Gray,byte>();
                var calcfaces = calcface.Detect(calcGrayFrame, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(120, 120), new Size(200, 200));
                if (calcfaces.Length != 0)
                {
                    calcGrayFrame.ROI = new Rectangle(new Point(calcfaces[0].rect.X, calcfaces[0].rect.Y), new Size(calcfaces[0].rect.Width, calcfaces[0].rect.Height));
                    int area = calcGrayFrame.Width * calcGrayFrame.Height;
                    Int32 sumIntensity = 0;
                    for (int i = 0; i < calcGrayFrame.Width; i++)
                    {
                        for (int j = 0; j < calcGrayFrame.Height; j++)
                        {
                            sumIntensity += calcGrayFrame.Data[j, i, 0];
                        }
                    }
                    int avgIntensity = sumIntensity / area;
                    Console.WriteLine("------------------Intensity:" + avgIntensity);
                    File.AppendAllText(@logFolder + logName + "_ver1.0.txt", "------------------Intensity:" + avgIntensity + "\r\n");
                }
                else
                {
                    Console.WriteLine("------------------Fail");
                    int area = calcGrayFrame.Width * calcGrayFrame.Height;
                    Int32 sumIntensity = 0;
                    for (int i = 0; i < calcGrayFrame.Width; i++)
                    {
                        for (int j = 0; j < calcGrayFrame.Height; j++)
                        {
                            sumIntensity += calcGrayFrame.Data[j, i, 0];
                        }
                    }
                    int avgIntensity = sumIntensity / area;
                    Console.WriteLine("------------------Intensity:" + avgIntensity);
                    File.AppendAllText(@logFolder + logName + "_ver1.0.txt", "------------------Intensity:" + avgIntensity + "\r\n");
                }
                
            }

        }            
        private void button2_Click(object sender, EventArgs e)
        {
            if(t != null){

                Console.WriteLine(t.ThreadState);
                t.Abort();
                timer.Stop();
                timer.Close();
              
            }
            
            Application.Idle -= ProcessFrame;
            Application.Idle -= runningFrame;
            ReleaseData();
            FormTrain frmTrain = new FormTrain(this);
            frmTrain.Show();
            button1.Enabled = true;
            this.Hide();
        }    
        
        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }
        private void runningFrame(object sender, EventArgs arg)
        {
            imageFrame = capture.QueryFrame();
            
            if (imageFrame != null)
            {
                drawFrame = imageFrame.Copy();
                imageBox1.Image = drawFrame;
                if (!realfaceRectangle.IsEmpty)
                {
                    drawFrame.Draw(realfaceRectangle, new Bgr(Color.LimeGreen), 2);
                    drawFrame.Draw(faceRectangle, new Bgr(Color.LawnGreen), 2);
                    drawFrame.Draw(name, ref font, facePosition, new Bgr(Color.Red));
                    
                }
            }
        }
        private void runningCropFrame(object sender, EventArgs arg)
        {
            
            if (imageroi != null)
            {
                imageBox2.Image = imageroi;
                
            }
        }
        private void ProcessFrame(object sender, EventArgs arg)
        {                   
            if(imageFrame != null){
                
                Image<Gray, byte> greyImage = imageFrame.Copy().Convert<Gray, byte>();
                
                
                greyImage._SmoothGaussian(3);
                //greyImage._EqualizeHist();
                stopWatch.Start();
                var faces = face.Detect(greyImage,1.3,6,HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT,new Size(120,120),new Size(200,200));
                
                if (faces.Length == 0)
                {
                    var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                    if(eyeObjects.Length == 2)
                    {
                        #region comment
                        /*Console.WriteLine("helper");
                        if(eyeObjects[0].X > eyeObjects[1].X)
                        {
                            var temp = eyeObjects[0];
                            eyeObjects[0] = eyeObjects[1];
                            eyeObjects[1] = temp;
                        }
                        int betweeneLength = eyeObjects[1].X - eyeObjects[0].X;
                        int middleposition = eyeObjects[0].X + ((betweeneLength + eyeObjects[1].Width )/ 2);
                        int forheadpeak = (int)(0.8 * betweeneLength);//
                        int forheadpeakpeak = (int)(0.7 * betweeneLength);//
                        int forheadbelowpeak = (int)(0.4 * betweeneLength);
                        int foreheadpoint = (int)(0.6 * betweeneLength);
                        int neareyebrowpoint = (int)(0.2 * betweeneLength);
                        int lefteyebrowpoint = eyeObjects[0].X;//
                        int righteyebrowpoint = eyeObjects[0].X + betweeneLength + eyeObjects[1].Width;//
                        //int nosepoint =
                        int xx = (int)((5.0 / 12.0) * betweeneLength);
                        int xxx = (int)((1.5 / 8.0) * betweeneLength);

                        int x1 = (int)((1.0 / 16.0) * betweeneLength);

                        
                        int round = 3;
                        int around = round-2;
                        double tempcolor = 0;
                        for (int i = 0; i<round; i++)
                        {
                            for (int j = 0; j < round; j++)
                            {
                                tempcolor += greyImage[middleposition - around + i, eyeObjects[0].Y + forheadpeak - around + j].Intensity;
                            }
                        }
                        Gray skincolor = new Gray(tempcolor/(round*round));
                        Point[] p = new Point[7];
                        p[0] = new Point(middleposition, eyeObjects[0].Y - forheadpeak);

                        p[1] = new Point(eyeObjects[0].X + (eyeObjects[0].Width / 2), eyeObjects[0].Y - forheadpeakpeak);
                        p[2] = new Point(eyeObjects[0].X - x1, eyeObjects[0].Y - forheadbelowpeak);
                        p[3] = new Point(lefteyebrowpoint - xxx, eyeObjects[0].Y + (eyeObjects[0].Height/6));

                        p[4] = new Point(righteyebrowpoint + xxx, eyeObjects[0].Y + (eyeObjects[0].Height /6));
                        p[5] = new Point(righteyebrowpoint + x1, eyeObjects[0].Y - forheadbelowpeak);
                        p[6] = new Point(eyeObjects[1].X + (eyeObjects[1].Width / 2), eyeObjects[0].Y - forheadpeakpeak);

                        //imageFrame.Draw(new Rectangle(new Point(eyeObjects[0].X, eyeObjects[0].Y), new Size(betweeneLength + eyeObjects[1].Width, eyeObjects[0].Height)), new Bgr(Color.Aqua), 2);
                        //imageFrame.Draw(new CircleF(new PointF(middleposition,eyeObjects[0].Y+ foreheadpoint), 1), new Bgr(Color.Yellow), 2);
                        //imageFrame.Draw(new CircleF(new PointF(middleposition,eyeObjects[0].Y - forheadpeak), 1), new Bgr(Color.Yellow), 2);
                        //imageFrame.Draw(new CircleF(new PointF(middleposition, eyeObjects[0].Y - neareyebrowpoint), 1), new Bgr(Color.Gold), 2);
                        //imageFrame.Draw(new CircleF(new PointF(lefteyebrowpoint - xxx, eyeObjects[0].Y), 1), new Bgr(Color.AliceBlue), 2);
                        //imageFrame.Draw(new CircleF(new PointF(righteyebrowpoint + xxx, eyeObjects[0].Y), 1), new Bgr(Color.AliceBlue), 2);
                        //imageFrame.Draw(new CircleF(new PointF(lefteyebrowpoint, eyeObjects[0].Y - neareyebrowpoint), 1), new Bgr(Color.LimeGreen), 2);
                        //imageFrame.Draw(new CircleF(new PointF(righteyebrowpoint, eyeObjects[0].Y - neareyebrowpoint), 1), new Bgr(Color.LimeGreen), 2);
                        //imageFrame.DrawPolyline(p,true, new Bgr(Color.Azure), 2);
                        greyImage.FillConvexPoly(p, skincolor);
                        //imageFrame.Draw(new CircleF(new PointF(eyeObjects[0].X - x1, eyeObjects[0].Y - forheadbelowpeak), 1), new Bgr(Color.LimeGreen), 2);
                        //imageFrame.Draw(new CircleF(new PointF(righteyebrowpoint + x1, eyeObjects[0].Y - forheadbelowpeak), 1), new Bgr(Color.LimeGreen), 2);

                        //imageFrame.Draw(new CircleF(new PointF(eyeObjects[0].X + (eyeObjects[0].Width / 2), eyeObjects[0].Y - forheadpeakpeak), 1), new Bgr(Color.LimeGreen), 2);
                        //imageFrame.Draw(new CircleF(new PointF(eyeObjects[1].X + (eyeObjects[1].Width / 2), eyeObjects[0].Y - forheadpeakpeak), 1), new Bgr(Color.LimeGreen), 2);
                        */
                        #endregion
                    }
                    else //not see eye in frame
                    {
                        name = "Processing...";
                        learningTag = true;
                        faceRectangle = Rectangle.Empty;
                        realfaceRectangle = Rectangle.Empty;
                        recogNameResult.Clear();
                        recogDistanceResult.Clear();
                        Console.WriteLine("Clear");
                    }
                       
                }
                else 
                { 
                    Parallel.ForEach(faces, facecount =>
                    {
                        try
                        {
                            facePosition = new Point(facecount.rect.X, facecount.rect.Y);
                            faceRectangleSize = new Size(facecount.rect.Width,facecount.rect.Height);
                            faceRectangle = new Rectangle(facePosition, faceRectangleSize);
                            greyImage.ROI = faceRectangle;
                            var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                            greyImage.ROI = System.Drawing.Rectangle.Empty;
                            if (eyeObjects.Length == 2)
                            {

                                Console.WriteLine("eye detected...");
                                if (eyeObjects[0].X > eyeObjects[1].X)
                                {
                                    var temp = eyeObjects[0];
                                    eyeObjects[0] = eyeObjects[1];
                                    eyeObjects[1] = temp;
                                }

                                int betweeneLength = eyeObjects[1].X - eyeObjects[0].X;
                                int lefteyebrowpoint = eyeObjects[0].X;//
                                int righteyebrowpoint = eyeObjects[0].X + betweeneLength + eyeObjects[1].Width;//
                                int xxx = (int)((1.5 / 8.0) * betweeneLength);
                                int neareyebrowpoint = (int)(0.2 * betweeneLength);
                                int faceheight = (int)(2.3*betweeneLength);

                                realFacePosition = new Point(facePosition.X + lefteyebrowpoint - xxx, facePosition.Y+ eyeObjects[0].Y - neareyebrowpoint);
                                realfaceRectangleSize = new Size((righteyebrowpoint + xxx) - (lefteyebrowpoint - xxx), faceheight);
                                realfaceRectangle = new Rectangle(realFacePosition, realfaceRectangleSize);


                                greyImage.ROI = realfaceRectangle;
                                imageroi = greyImage.Copy();
                                greyImage.ROI = new Rectangle();

                                
                                if (eigenRecog.IsTrained)
                                {
                                    
                                    imageroi._EqualizeHist();
                                    imageroi = imageroi.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR);
                                    
                                    //find the most relative face
                                    if (recogNameResult.Count == maxImageCount)
                                    {
                                        Console.WriteLine("Processing...");
                                        int max = 0;
                                        string mostFace = "";
                                        foreach (string value in recogNameResult.Distinct())
                                        {
                                            Console.WriteLine("\"{0}\" occurs {1} time(s).\n", value, recogNameResult.Count(v => v == value));
                                            if (recogNameResult.Count(v => v == value) > max)
                                            {
                                                max = recogNameResult.Count(v => v == value);
                                                mostFace = value;
                                            }
                                        }
                                        name = mostFace;
                                        if (learningTag && !(name.Equals("UnknownNull") || name.Equals("UnknownFace")))
                                        {
                                            learnImage = imageroi;
                                            matchedResult = eigenRecog.Recognise(learnImage);
                                            string[] matchedData = matchedResult.Split(' ');
                                            if ((Double.Parse(matchedData[1]) <= eigenRecog.getRecognizeTreshold) && (Double.Parse(matchedData[1]) != 0))
                                            {
                                                meanDistance = recogDistanceResult.Sum() / maxImageCount;
                                                if (meanDistance <= eigenRecog.getRecognizeTreshold)
                                                {
                                                    learnImage.Save(tempPath);
                                                    mydb.InsertImageTraining(name, tempPath, false);
                                                    if (mydb.getSpecifyImageCount(name) > 3)
                                                    {
                                                        mydb.DeleteOldestImage(name);
                                                    }
                                                    eigenRecog.reloadData();
                                                    learningTag = false;
                                                    Console.WriteLine("Learning:" + name + "  Distance:" + meanDistance);
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Distance:" + meanDistance + "\n");
                                                }
                                            }

                                        }

                                    }
                                    else
                                    {
                                        Console.WriteLine("recognizing...");
                                        matchedResult = eigenRecog.Recognise(imageroi);
                                        Console.WriteLine("Result:" + matchedResult + "\n");
                                        File.AppendAllText(@logFolder + logName + "_ver1.0.txt", "Result:" + matchedResult + "\r\n");
                                        string[] matchedData = matchedResult.Split(' ');
                                        if (!matchedResult[0].Equals("UnknownNull") && !matchedResult[0].Equals("UnknownFace"))
                                        {
                                            //Console.WriteLine(matchedData[0] +" "+ matchedData[1]);
                                            recogNameResult.Add(matchedData[0]);
                                            recogDistanceResult.Add(Double.Parse(matchedData[1]));
                                        }

                                    }

                                    

                                }

                            }
                        }
                        catch (Exception e)
                        {
                            Console.Write(e);
                        }


                    });
            }
                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value. 
                    string elapsedTime = String.Format("{0}",

                        ts.TotalMilliseconds * 10000);
                    textBox2.Text = elapsedTime;
                    //listView1.Items.Add(elapsedTime);
                    //File.AppendAllText(@logFolder + logName + "_ver1.0.txt", "Frametime: "+elapsedTime+"\r\n");
                    stopWatch.Reset();
                   
                   
                
            }
            
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (t != null)
            {

                Console.WriteLine(t.ThreadState);
                t.Abort();
                timer.Stop();
                timer.Close();

            }

            Application.Idle -= ProcessFrame;
            Application.Idle -= runningFrame;
            ReleaseData();
            FormManualTrain frmManTrain = new FormManualTrain(this);
            frmManTrain.Show();
            button1.Enabled = true;
            this.Hide();
        }

        
                                           
    }
}
