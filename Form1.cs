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
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;

using System.IO;
using System.Collections;
using System.Threading;

namespace EMGUCV
{
    
    public partial class Form1 : Form
    {
          
        DBConn mydb;
        MCvFont font;        
        Stopwatch stopWatch = new Stopwatch();
        Capture capture;
        HaarCascade face;

        List<string> recogResult;
        Classifier_Train eigenRecog = new Classifier_Train();  
        CascadeClassifier eyeWithGlass;
        CascadeClassifier nose;
        CascadeClassifier mouth;
        Size minEye;
        Size maxEye;
        Size minNose;
        Size maxNose;
        Size minMouth;
        Size maxMouth;
        Point[] coord;
        double ROImargin = 1.00;
        double widthScale = 1.00;
        int ROIwidth = 140;
        int ROIheight = 175;
        bool learningTag = true;

        string name = "Processing...";
        string tempPath = "E:/Images/tmp.jpg";
        string logName;

        public Form1()
        {
            InitializeComponent();

            face = new HaarCascade("haarcascade_frontalface_default.xml");
            eyeWithGlass = new CascadeClassifier("haarcascade_eye_tree_eyeglasses.xml");
            nose = new CascadeClassifier("haarcascade_mcs_nose.xml");
            mouth = new CascadeClassifier("haarcascade_mcs_mouth.xml");

            mydb = new DBConn();

            recogResult = new List<string>();
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
                capture.QueryFrame();                
                Application.Idle += new EventHandler(ProcessFrame);
                                               
        }
        private void button2_Click(object sender, EventArgs e)
        {
            TrainFrame();
        }    
        
        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }
        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> imageFrame = capture.QueryFrame();
            Image<Gray, byte> learnImage = new Image<Gray, byte>(ROIwidth,ROIheight);           

            if(imageFrame != null){
                string matchedname;
                Image<Gray, byte> greyImage = imageFrame.Convert<Gray, byte>();
                Image<Gray, byte> imageroi;
                greyImage._EqualizeHist();
                
                
                stopWatch.Start();
                var faces = face.Detect(greyImage,1.3,6,HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT,new Size(120,120),new Size(200,200));
                
                if (faces.Length == 0)
                {
                    var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                    if(eyeObjects.Length == 2){
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
                    }
                    else //not see eye in frame
                    {
                        name = "Processing...";
                        learningTag = !learningTag;
                        recogResult.Clear();
                    }
                    
                    
                }
                else 
                { 
                    Parallel.ForEach(faces, facecount =>
                    {
                        try
                        {

                            imageFrame.Draw(facecount.rect.Width + " " + facecount.rect.Height, ref font, new Point(facecount.rect.X - 25, facecount.rect.Y - 25), new Bgr(Color.Red));
                            var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                            if (eyeObjects.Length == 2)
                            {
                                Console.WriteLine("eye");
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

                                Console.WriteLine((righteyebrowpoint + xxx) - (lefteyebrowpoint - xxx)+" "+ faceheight);
                                imageFrame.ROI = new Rectangle(new Point(lefteyebrowpoint - xxx, eyeObjects[0].Y - neareyebrowpoint), new Size((righteyebrowpoint + xxx) - (lefteyebrowpoint - xxx), faceheight));
                                imageroi = imageFrame.Copy().Convert<Gray, byte>();
                                imageFrame.ROI = new Rectangle();
                                imageFrame.Draw(new Rectangle(new Point(lefteyebrowpoint - xxx, eyeObjects[0].Y - neareyebrowpoint), new Size((righteyebrowpoint + xxx) - (lefteyebrowpoint - xxx), faceheight)), new Bgr(Color.SpringGreen), 2);
                                if (eigenRecog.IsTrained)
                                {
                                    imageroi._EqualizeHist();

                                    //find the most relative face
                                    if (recogResult.Count == 21)
                                    {
                                        int max = 0;
                                        string mostFace = "";
                                        foreach (string value in recogResult.Distinct())
                                        {
                                            System.Diagnostics.Debug.WriteLine("\"{0}\" occurs {1} time(s).", value, recogResult.Count(v => v == value));
                                            if (recogResult.Count(v => v == value) > max)
                                            {
                                                max = recogResult.Count(v => v == value);
                                                mostFace = value;
                                            }
                                        }
                                        name = mostFace;
                                        if (learningTag)
                                        {
                                            learnImage = imageroi.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR);
                                            learnImage.Save(tempPath);
                                            mydb.InsertImageTraining(name, tempPath);
                                            if (mydb.getSpecifyImageCount(name) > 10)
                                            {
                                                mydb.DeleteOldestImage(name);
                                            }
                                            eigenRecog.reloadData();
                                            learningTag = !learningTag;
                                            Console.WriteLine("Learning " + name);
                                        }
                                        
                                    }
                                    else
                                    {
                                        Console.WriteLine("recog");
                                        matchedname = eigenRecog.Recognise(imageroi.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR));

                                        if (!matchedname.Equals("UnknownNull") && !matchedname.Equals("UnknownFace"))
                                        {
                                            Console.WriteLine(matchedname);
                                            recogResult.Add(matchedname);
                                        }

                                    }

                                    imageFrame.Draw(name, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.Red));

                                }

                                imageFrame.Draw(new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), (int)(facecount.rect.Height * 1.1)), new Bgr(Color.LawnGreen), 2);

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
                    listView1.Items.Add(elapsedTime);
                    File.AppendAllText(@"E:\Images\log\" + logName + "_ver1.0.txt", "Frametime: "+elapsedTime+"\r\n");
                    stopWatch.Reset();
                    CvInvoke.cvSmooth(imageFrame, imageFrame, SMOOTH_TYPE.CV_GAUSSIAN, 1, 1, 1, 1);
                   
                //imageBox1.Image = greyImage;//line 2
               imageBox1.Image = imageFrame;//line 2
            }
            
            
        }
        
        private void TrainFrame()
        {
            try
            {
                
                Image<Bgr, Byte> imageFrame = capture.QueryFrame();  //line 1
                Image<Gray, byte> darkimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                Image<Gray, byte> cropimage = new Image<Gray, byte>(ROIwidth, ROIheight);

                //ArrayList pic = new ArrayList();
                if (imageFrame != null)

                {
                    Image<Gray, byte> greyImage = imageFrame.Convert<Gray, byte>();


                    var faces = face.Detect(greyImage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(120, 120), new Size(200, 200));
                    if (faces.Length > 0)
                    {
                        foreach (var facecount in faces)
                        {
                            var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                            if (eyeObjects.Length == 2)
                            {
                                Console.WriteLine("eye");
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
                                int faceheight = (int)(2.3 * betweeneLength);

                                imageFrame.Draw(facecount.rect, new Bgr(Color.Red), 2);
                                imageFrame.Draw(facecount.rect.Height + "," + facecount.rect.Width, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.LightGreen));
                                greyImage.ROI = new Rectangle(new Point(lefteyebrowpoint - xxx, eyeObjects[0].Y - neareyebrowpoint), new Size((righteyebrowpoint + xxx) - (lefteyebrowpoint - xxx), faceheight));
                                //CropFrame = greyImage.Copy();
                                //pic.Add(CropFrame);

                                //get bigger face in frame
                                cropimage = greyImage.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR);
                                if (!cropimage.Equals(darkimage))
                                {
                                    cropimage._EqualizeHist();
                                    //CvInvoke.cvSmooth(cropimage, cropimage, SMOOTH_TYPE.CV_GAUSSIAN, 1, 1, 1, 1);
                                    //cropimage = eigenRecog.convertLBP(cropimage,1);
                                    imageBox7.Image = cropimage;     //line 2


                                    cropimage.Save(tempPath);
                                    mydb.InsertImageTraining(textBox1.Text, tempPath);

                                    //File.Delete(tempPath);
                                    eigenRecog.reloadData();
                                    //Fish_Recog.reloadData();
                                }
                                
                            }
                            imageBox8.Image = cropimage;
                        }
                    }
                }
            }
            catch
            {
               // MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }            
        }                                    
    }
}
