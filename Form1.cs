﻿using System;
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
       
        private DBConn mydb;
        private MCvFont font;
        private MCvFont fontbig;
        private MCvFont fontverybig;
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
        private Classifier_Train eigenRecog;
        private Classifier_LBPH lbphRecog;
        private CascadeClassifier eyeWithGlass;
        private CascadeClassifier nose;
        private CascadeClassifier mouth;
        private Size minEye;
        private Size maxEye;
        private Size minNose;
        private Size maxNose;
        private Size minMouth;
        private Size maxMouth;
        private int ROIwidth = 140;
        private int ROIheight = 175;
        private bool learningTag = true;
        

        private string name = "Processing...";
        private string tempPath = "\\tmp.jpg";
        private string logFolder = "\\log\\";
        private string logName;
        private string folderPath = "";
        
        private Size frameSize = new Size(400, 400);
        private Point framePoint = new Point(30, 30);
        Image<Gray, Byte> imgAR = new Image<Gray, Byte>(140, 175);
        private string txtAR;
        private bool ARDisplayFlag = false;
        private bool recogButtonState = false;
        int frameCount = 0;
        bool countFlag = true;
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
            font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.4d, 0.4d);
            fontbig = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6d, 0.6d);
            fontverybig = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.8d, 0.8d); 
            //log record
            DateTime now = DateTime.Now;
            logName = now.ToString();
            logName = logName.Replace("/", "").Replace(":", "").Replace(" ", "");
            label2.Text = "Idle";
            if(File.Exists("setting.txt")){
                
                folderPath = File.ReadAllText("setting.txt");
            }
            else
            {
                FolderBrowserDialog b = new FolderBrowserDialog();
                b.Description = "Please select your installation path";
                DialogResult r = b.ShowDialog();
                if (r == DialogResult.OK) // Test result.
                {
                    folderPath = b.SelectedPath;
                    Console.WriteLine(folderPath);
                    File.WriteAllText(@"setting.txt", folderPath);
                    MessageBox.Show("Path is at " + folderPath);
                }
            }                       
        }
     
        private void button1_Click(object sender, EventArgs e)
        {
            if (!recogButtonState)
            {
                if (mydb.IsServerConnected())
                {
                    //Console.WriteLine(mydb.getUserData("54010001"));
                    eigenRecog = new Classifier_Train();
                    //lbphRecog = new Classifier_LBPH();
                    capture = new Capture();
                    Console.WriteLine("resolution:" + capture.Height + "," + capture.Width);
                    Application.Idle += new EventHandler(ProcessFrame);
                    Application.Idle += new EventHandler(runningFrame);
                    // Application.Idle += new EventHandler(runningCropFrame);

                    button1.Text = "STOP RECOGNIZE";
                    button2.Enabled = false;
                    button3.Enabled = false;
                    recogButtonState = true;
                }
                else
                {
                    MessageBox.Show("Database not connect.");
                }
            }
            else
            {
                
                Application.Idle -= new EventHandler(ProcessFrame);
                Application.Idle -= new EventHandler(runningFrame);
                imageBox1.Image = null;
                imageFrame = null;
                realfaceRectangle = Rectangle.Empty;
                button1.Text = "START RECOGNIZE";
                recogButtonState = false;
                label2.Text = "Idle";
                progressBar1.Value = 0;
                button2.Enabled = true;
                button3.Enabled = true;
                capture.Dispose();
            }
            
                
        }
        
              
        private void button2_Click(object sender, EventArgs e)
        {
            if (mydb.IsServerConnected())
            {
                
                Application.Idle -= ProcessFrame;
                Application.Idle -= runningFrame;
                ReleaseData();
                FormTrain frmTrain = new FormTrain(this);
                frmTrain.Show();
                button1.Enabled = true;
                this.Hide();
            }
            else
            {
                MessageBox.Show("Database not connect.");
            }
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
                
                if (!realfaceRectangle.IsEmpty)
                {
                    drawFrame.Draw(realfaceRectangle, new Bgr(Color.LimeGreen), 2);
                    //drawFrame.Draw(faceRectangle, new Bgr(Color.LimeGreen), 2);
                    drawFrame.Draw(name, ref fontbig, facePosition, new Bgr(Color.Red));
                    if (name != "Processing...")
                    {
                        if(name.Equals("UnknownNull")){
                            label2.Text = "Fail";
                        }
                        else
                        {
                            label2.Text = "Success";
                        }
                        
                        runAR(name);
                    }
                    else
                    {
                        label2.Text = name;
                    }
                }
                else
                {
                    if(countFlag){
                        drawFrame.Draw("PLEASE EXPOSE YOUR", ref fontverybig, new Point(175, 220), new Bgr(Color.Lime));
                        drawFrame.Draw("FACE TO CAMERA", ref fontverybig, new Point(210, 260), new Bgr(Color.Lime));
                    }
                    frameCount++;
                    if(frameCount >= 10){
                        countFlag = !countFlag;
                        frameCount = 0;
                    }
                }
                imageBox1.Image = drawFrame;
                
            }
        }

        private void runAR(string nameID)
        {
            Rectangle drawArea = new Rectangle(framePoint, frameSize);
            Rectangle drawArea2 = new Rectangle(framePoint, new Size(140, 175));
            Image<Bgr, Byte> opacityOverlay = new Image<Bgr, Byte>(drawArea.Width, drawArea.Height, new Bgr(Color.Black));
            drawFrame.ROI = drawArea;
            opacityOverlay.CopyTo(drawFrame);
            drawFrame.ROI = Rectangle.Empty;

            double alpha = 0.7;
            double beta = 1 - alpha;
            double gamma = 0;
            drawFrame.Draw(drawArea, new Bgr(Color.Black), 2);
            drawFrame = imageFrame.AddWeighted(drawFrame, alpha, beta, gamma);
            drawFrame.Draw(drawArea, new Bgr(Color.LimeGreen), 2);
            ////***********TEXT***********
            if (!ARDisplayFlag)
            {
                if (!nameID.Equals("UnknownNull"))
                {
                    txtAR = mydb.getUserData(nameID);
                }
                else
                {
                    txtAR = "Can not recognize any face";
                }
            }
            //txtAR = "abc def ghi";
            
            int tmpY = framePoint.Y;
            if (!nameID.Equals("UnknownNull"))
            {
                string[] txtSet = txtAR.Split(' ');
                for (int i = 0; i < txtSet.Length; i++)
                {

                    
                    switch (i)
                    {
                        case 0:
                            drawFrame.Draw("     User ID: " + txtSet[i], ref font, new Point(framePoint.X + 170, tmpY + 30), new Bgr(Color.LawnGreen));
                            label9.Text = "User ID: " + txtSet[i];
                            tmpY += 30;
                            break;
                        case 1:
                            drawFrame.Draw("       Name: " + txtSet[i], ref font, new Point(framePoint.X + 170, tmpY + 30), new Bgr(Color.LawnGreen));
                            label4.Text = "Name: " + txtSet[i];
                            tmpY += 30;
                            break;
                        case 2:
                            drawFrame.Draw("   Surname: " + txtSet[i], ref font, new Point(framePoint.X + 170, tmpY + 30), new Bgr(Color.LawnGreen));
                            label5.Text = "Surname: " + txtSet[i];
                            tmpY += 30;
                            break;
                        case 3:
                            drawFrame.Draw("   Birthdate: " + txtSet[i], ref font, new Point(framePoint.X + 170, tmpY + 30), new Bgr(Color.LawnGreen));
                            label7.Text = "Birthdate: " + txtSet[i];
                            tmpY += 30;
                            break;
                        case 4:
                            break;
                        case 5:
                            drawFrame.Draw("Blood group: " + txtSet[i], ref font, new Point(framePoint.X + 170, tmpY + 30), new Bgr(Color.LawnGreen));
                            label6.Text = "Blood group: " + txtSet[i];
                            tmpY += 30;
                            break;
                        case 6:
                            drawFrame.Draw("     Gender: " + txtSet[i], ref font, new Point(framePoint.X + 170, tmpY + 30), new Bgr(Color.LawnGreen));
                            label8.Text = "Gender: " + txtSet[i];
                            tmpY += 30;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                string txtSet = txtAR;
                label4.Text = "";
                label5.Text = "";
                label6.Text = "";
                label7.Text = "";
                label8.Text = "";
                label9.Text = "";
                drawFrame.Draw(txtSet, ref font, new Point(framePoint.X + 170, tmpY + 30), new Bgr(Color.LawnGreen));
            }
            ////***********Picture***********
                if (!ARDisplayFlag)
                {
                    imgAR = mydb.getResultImage(nameID);
                }
            Image<Bgr, Byte> imageSrc = imgAR.Convert<Bgr,byte>();
            drawFrame.ROI = drawArea2;
            CvInvoke.cvCopy(imageSrc, drawFrame, IntPtr.Zero);
            drawFrame.ROI = Rectangle.Empty;
            ARDisplayFlag = true;
        }

        /*private void runningCropFrame(object sender, EventArgs arg)
        {
            
            if (imageroi != null)
            {
                imageBox2.Image = imageroi;
                
            }
        }*/
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
                        ARDisplayFlag = false;
                        faceRectangle = Rectangle.Empty;
                        realfaceRectangle = Rectangle.Empty;
                        label2.Text = "Idle";
                        progressBar1.Value = 0;
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
                            greyImage.ROI = Rectangle.Empty;
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

                                //if(lbphRecog.IsTrained)
                                if (eigenRecog.IsTrained)
                                {
                                    
                                    imageroi._EqualizeHist();
                                    imageroi = imageroi.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR);
                                    
                                    //find the most relative face
                                    progressBar1.Value = recogNameResult.Count;
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
                                            //matchedResult = lbphRecog.Recognise(learnImage);
                                            string[] matchedData = matchedResult.Split(' ');
                                            if ((Double.Parse(matchedData[1]) <= eigenRecog.getRecognizeTreshold) && (Double.Parse(matchedData[1]) != 0))
                                            //if ((Double.Parse(matchedData[1]) <= lbphRecog.getRecognizeTreshold) && (Double.Parse(matchedData[1]) != 0))
                                            {
                                                meanDistance = recogDistanceResult.Sum() / maxImageCount;
                                                if (meanDistance <= eigenRecog.getRecognizeTreshold)
                                                //if (meanDistance <= lbphRecog.getRecognizeTreshold)
                                                {
                                                    learnImage.Save(folderPath + tempPath);
                                                    Console.WriteLine(folderPath + tempPath);
                                                    string dbPath = (folderPath + tempPath).Replace("\\","/");
                                                    mydb.InsertImageTraining(int.Parse(name), dbPath, false);
                                                    if (mydb.getSpecifyImageCount(name) > 3)
                                                    {
                                                        mydb.DeleteOldestImage(name);
                                                    }
                                                    eigenRecog.reloadData();
                                                    //lbphRecog.reloadData();
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
                                        //matchedResult = lbphRecog.Recognise(imageroi);
                                        Console.WriteLine("Result:" + matchedResult + "\n");
                                        //Console.WriteLine("path"+folderPath+logFolder + logName + "_ver1.0.txt");
                                        File.AppendAllText(@folderPath+logFolder + logName + "_ver1.0.txt", "Result:" + matchedResult + "\r\n");
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
                    string elapsedTime = String.Format("{0}",ts.TotalMilliseconds * 10000);
                    //textBox2.Text = elapsedTime;
                    //listView1.Items.Add(elapsedTime);
                    //File.AppendAllText(@logFolder + logName + "_ver1.0.txt", "Frametime: "+elapsedTime+"\r\n");
                    stopWatch.Reset();
                   
                   
                
            }
            
            
        }

        private void button3_Click(object sender, EventArgs e)
        {

            if (mydb.IsServerConnected())
            {
                Application.Idle -= ProcessFrame;
                Application.Idle -= runningFrame;
                ReleaseData();
                FormManualTrain frmManTrain = new FormManualTrain(this);
                frmManTrain.Show();
                button1.Enabled = true;
                this.Hide();
            }
            else
            {
                MessageBox.Show("Database not connect.");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string agreementText = "ข้อตกลงในการใช้ซอฟต์แวร์\n\nซอฟต์แวร์นี้เป็นผลงานที่พัฒนาขึ้นโดย นาย ณัฐพงษ์ ไทยอุบุญ และ นาย ปฐมพล สงวนพานิช  จาก สถาบันเทคโนโลยีพระจอมเกล้าเจ้าคุณทหารลาดกระบัง ภายใต้การดูแลของ \nผศ. ดร. ชุติเมษฏ์ ศรีนิลทา  ภายใต้โครงการ ระบบตรวจสอบใบหน้าเพื่อยืนยันตัวบุคคล \nซึ่งสนับสนุนโดย ศูนย์เทคโนโลยีอิเล็กทรอนิกส์และคอมพิวเตอร์แห่งชาติ โดยมีวัตถุประสงค์เพื่อส่งเสริมให้นักเรียนและนักศึกษาได้เรียนรู้และฝึกทักษะในการพัฒนาซอฟต์แวร์ ลิขสิทธิ์ของซอฟต์แวร์นี้จึงเป็นของผู้พัฒนา ซึ่งผู้พัฒนาได้อนุญาตให้ศูนย์เทคโนโลยีอิเล็กทรอนิกส์และคอมพิวเตอร์แห่งชาติ เผยแพร่ซอฟต์แวร์นี้ตาม “ต้นฉบับ” โดยไม่มีการแก้ไขดัดแปลงใดๆ ทั้งสิ้น ให้แก่บุคคลทั่วไปได้ใช้เพื่อประโยชน์ส่วนบุคคลหรือประโยชน์ทางการศึกษาที่ไม่มีวัตถุประสงค์ในเชิงพาณิชย์ โดยไม่คิดค่าตอบแทนการใช้ซอฟต์แวร์ ดังนั้น ศูนย์เทคโนโลยีอิเล็กทรอนิกส์และคอมพิวเตอร์แห่งชาติ จึงไม่มีหน้าที่ในการดูแล บำรุงรักษา จัดการอบรมการใช้งาน หรือพัฒนาประสิทธิภาพซอฟต์แวร์ รวมทั้งไม่รับรองความถูกต้องหรือประสิทธิภาพการทำงานของซอฟต์แวร์ ตลอดจนไม่รับประกันความเสียหายต่างๆ อันเกิดจากการใช้ซอฟต์แวร์นี้ทั้งสิ้น";
            MessageBox.Show(agreementText);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog b = new FolderBrowserDialog();
            DialogResult r = b.ShowDialog();
            if (r == DialogResult.OK) // Test result.
            {
                folderPath = b.SelectedPath;
                Console.WriteLine(folderPath);
                File.WriteAllText(@"setting.txt", folderPath);
                MessageBox.Show("Path is at "+folderPath);
            }
        }

        
                                           
    }
}
