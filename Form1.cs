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
        
        private Capture capture;        //takes images from camera as image frames
        
        HaarCascade face;       
        DBConn mydb;
        MCvFont font;
        
        Stopwatch stopWatch = new Stopwatch();
                       
        Classifier_Train Eigen_Recog = new Classifier_Train();             
        double ROImargin = 1.00;
        double widthScale = 1.00;
        int ROIwidth = 150;
        int ROIheight = 165;
        string name = "Processing...";
        List<string> result;
        ImproveRecognize imReg;

        
        CascadeClassifier eyewithglass;
        CascadeClassifier nose;
        CascadeClassifier mouth;
        Size mineye;
        Size maxeye;
        Size minnose;
        Size maxnose;
        Size minmouth;
        Size maxmouth;
        Point[] coord;

        public Form1()
        {
            InitializeComponent();
            imReg = new ImproveRecognize();
            face = new HaarCascade("haarcascade_frontalface_default.xml");            
            mydb = new DBConn();
            result = new List<string>();

            mineye = new Size(10, 10);
            maxeye = new Size(225, 225);
            minnose = new Size(10, 10);
            maxnose = new Size(225, 225);
            minmouth = new Size(10, 10);
            maxmouth = new Size(225, 225);
            font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
            eyewithglass = new CascadeClassifier("haarcascade_eye_tree_eyeglasses.xml");
            nose = new CascadeClassifier("haarcascade_mcs_nose.xml");
            mouth = new CascadeClassifier("haarcascade_mcs_mouth.xml");
            coord = new Point[4];
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
            
            
        }
        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }
        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> ImageFrame = capture.QueryFrame();  //line 1
            Point[] facefeatureCoord = new Point[4];

            if(ImageFrame != null){
                string matchedname;
                Image<Gray, byte> greyimage = ImageFrame.Convert<Gray, byte>();
                Image<Gray, byte> imageroi;
                greyimage._EqualizeHist();
                
                
                stopWatch.Start();
                var faces = face.Detect(greyimage,1.3,6,HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT,new Size(120,120),new Size(200,200));
                
                if (faces.Length == 0)
                {
                    var eyes = eyewithglass.DetectMultiScale(greyimage, 1.3, 6, mineye, maxeye);
                    if(eyes.Length == 2){
                        Console.WriteLine("helper");
                        if(eyes[0].X > eyes[1].X){
                            var temp = eyes[0];
                            eyes[0] = eyes[1];
                            eyes[1] = temp;
                        }
                        int betweeneLength = eyes[1].X - eyes[0].X;
                        int middleposition = eyes[0].X + ((betweeneLength + eyes[1].Width )/ 2);
                        int forheadpeak = (int)(0.8 * betweeneLength);//
                        int forheadpeakpeak = (int)(0.7 * betweeneLength);//
                        int forheadbelowpeak = (int)(0.4 * betweeneLength);
                        int foreheadpoint = (int)(0.6 * betweeneLength);
                        int neareyebrowpoint = (int)(0.2 * betweeneLength);
                        int lefteyebrowpoint = eyes[0].X;//
                        int righteyebrowpoint = eyes[0].X + betweeneLength + eyes[1].Width;//
                        //int nosepoint =
                        int xx = (int)((5.0 / 12.0) * betweeneLength);
                        int xxx = (int)((1.0 / 8.0) * betweeneLength);

                        int x1 = (int)((1.0 / 16.0) * betweeneLength);

                        Gray skincolor = greyimage[middleposition, eyes[0].Y + forheadpeak];
                        Point[] p = new Point[7];
                        p[0] = new Point(middleposition, eyes[0].Y - forheadpeak);

                        p[1] = new Point(eyes[0].X + (eyes[0].Width / 2), eyes[0].Y - forheadpeakpeak);
                        p[2] = new Point(eyes[0].X - x1, eyes[0].Y - forheadbelowpeak);
                        p[3] = new Point(lefteyebrowpoint - xxx, eyes[0].Y + (eyes[0].Height/5));

                        p[4] = new Point(righteyebrowpoint + xxx, eyes[0].Y + (eyes[0].Height /5));
                        p[5] = new Point(righteyebrowpoint + x1, eyes[0].Y - forheadbelowpeak);
                        p[6] = new Point(eyes[1].X + (eyes[1].Width / 2), eyes[0].Y - forheadpeakpeak);

                        //ImageFrame.Draw(new Rectangle(new Point(eyes[0].X, eyes[0].Y), new Size(betweeneLength + eyes[1].Width, eyes[0].Height)), new Bgr(Color.Aqua), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(middleposition,eyes[0].Y+ foreheadpoint), 1), new Bgr(Color.Yellow), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(middleposition,eyes[0].Y - forheadpeak), 1), new Bgr(Color.Yellow), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(middleposition, eyes[0].Y - neareyebrowpoint), 1), new Bgr(Color.Gold), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(lefteyebrowpoint - xxx, eyes[0].Y), 1), new Bgr(Color.AliceBlue), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(righteyebrowpoint + xxx, eyes[0].Y), 1), new Bgr(Color.AliceBlue), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(lefteyebrowpoint, eyes[0].Y - neareyebrowpoint), 1), new Bgr(Color.LimeGreen), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(righteyebrowpoint, eyes[0].Y - neareyebrowpoint), 1), new Bgr(Color.LimeGreen), 2);
                        //ImageFrame.DrawPolyline(p,true, new Bgr(Color.Azure), 2);
                        greyimage.FillConvexPoly(p, skincolor);
                        //ImageFrame.Draw(new CircleF(new PointF(eyes[0].X - x1, eyes[0].Y - forheadbelowpeak), 1), new Bgr(Color.LimeGreen), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(righteyebrowpoint + x1, eyes[0].Y - forheadbelowpeak), 1), new Bgr(Color.LimeGreen), 2);

                        //ImageFrame.Draw(new CircleF(new PointF(eyes[0].X + (eyes[0].Width / 2), eyes[0].Y - forheadpeakpeak), 1), new Bgr(Color.LimeGreen), 2);
                        //ImageFrame.Draw(new CircleF(new PointF(eyes[1].X + (eyes[1].Width / 2), eyes[0].Y - forheadpeakpeak), 1), new Bgr(Color.LimeGreen), 2);
                        var _faces = face.Detect(greyimage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(120, 120), new Size(200, 200));
                        Parallel.ForEach(_faces, facecount =>
                        {
                            try
                            {

                                ImageFrame.Draw(facecount.rect.Width + " " + facecount.rect.Height, ref font, new Point(facecount.rect.X - 25, facecount.rect.Y - 25), new Bgr(Color.Red));
                                ImageFrame.ROI = new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), (int)(facecount.rect.Height * 1.1));
                                imageroi = ImageFrame.Copy().Convert<Gray, byte>();
                                ImageFrame.ROI = new Rectangle();

                                if (Eigen_Recog.IsTrained)
                                {
                                    //imageroi._EqualizeHist();

                                    //find the most relative face
                                    if (result.Count == 21)
                                    {
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
                                        Console.WriteLine("212");
                                    }
                                    else
                                    {
                                        Console.WriteLine("recog2");
                                        matchedname = Eigen_Recog.Recognise(imageroi.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR));
                                        if (!matchedname.Equals("UnknownNull") && !matchedname.Equals("UnknownFace"))
                                        {
                                            Console.WriteLine(matchedname);
                                            result.Add(matchedname);
                                        }

                                        /*facefeatureCoord = imReg.drawFaceNet(imageroi);
                                    
                                    
                                        if (facefeatureCoord != null)
                                        {
                                            for (int i = 0; i < 4; i++)
                                            {
                                                facefeatureCoord[i].X += facecount.rect.X;
                                                facefeatureCoord[i].Y += facecount.rect.Y;
                                            }
                                            ImageFrame.DrawPolyline(facefeatureCoord,true,new Bgr(Color.Wheat),3);
                                        }*/


                                    }

                                    ImageFrame.Draw(name, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.Red));

                                }

                                ImageFrame.Draw(new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), (int)(facecount.rect.Height * 1.1)), new Bgr(Color.LawnGreen), 2);

                            }
                            catch (Exception e)
                            {
                                Console.Write(e);
                            }


                        });
                    }
                    
                    name = "Processing...";
                    result.Clear();
                }
                else 
                { 
                    Parallel.ForEach(faces, facecount =>
                    {
                        try
                        {

                            ImageFrame.Draw(facecount.rect.Width + " " + facecount.rect.Height, ref font, new Point(facecount.rect.X - 25, facecount.rect.Y - 25), new Bgr(Color.Red));
                            ImageFrame.ROI = new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), (int)(facecount.rect.Height*1.1));
                            imageroi = ImageFrame.Copy().Convert<Gray, byte>();
                            ImageFrame.ROI = new Rectangle();

                            if (Eigen_Recog.IsTrained)
                            {
                                //imageroi._EqualizeHist();
                               
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
                                    Console.WriteLine("21");
                                }
                                else
                                {
                                    Console.WriteLine("recog");
                                    matchedname = Eigen_Recog.Recognise(imageroi.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR));
                                    if (!matchedname.Equals("UnknownNull") && !matchedname.Equals("UnknownFace"))
                                    {
                                        Console.WriteLine(matchedname);
                                        result.Add(matchedname);
                                    }

                                    /*facefeatureCoord = imReg.drawFaceNet(imageroi);
                                    
                                    
                                    if (facefeatureCoord != null)
                                    {
                                        for (int i = 0; i < 4; i++)
                                        {
                                            facefeatureCoord[i].X += facecount.rect.X;
                                            facefeatureCoord[i].Y += facecount.rect.Y;
                                        }
                                        ImageFrame.DrawPolyline(facefeatureCoord,true,new Bgr(Color.Wheat),3);
                                    }*/
                                    
                                    
                                }

                                ImageFrame.Draw(name, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.Red));
                                
                            }
                            
                            ImageFrame.Draw(new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), (int)(facecount.rect.Height*1.1)), new Bgr(Color.LawnGreen), 2);

                        }
                        catch(Exception e)
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

                    stopWatch.Reset();
                    CvInvoke.cvSmooth(ImageFrame, ImageFrame, SMOOTH_TYPE.CV_GAUSSIAN, 1, 1, 1, 1);
                    
                //imageBox1.Image = greyimage;//line 2
               imageBox1.Image = ImageFrame;//line 2
            }
            
            
        }
        public Point[] drawFaceNet(Image<Gray, byte> image)
        {
            var eyes = eyewithglass.DetectMultiScale(image, 1.3, 6, mineye, maxeye);
            //if found pair of eye
            if (eyes.Length == 2)
            {
                //find nose
                foreach(var eyee in eyes){
                    
                }
                var noses = nose.DetectMultiScale(image, 1.3, 6, minnose, maxnose);
                // if found nose
                if (noses.Length == 1)
                {

                    //find mouth
                    var mouths = mouth.DetectMultiScale(image, 1.3, 6, minnose, maxnose);
                    //if found mouth
                    if (mouths.Length == 1)
                    {
                        //do the rest
                        //return coordinate
                        for (int i = 0; i < 2; i++)
                        {
                            coord[i].X = eyes[0].X;
                            coord[i].Y = eyes[0].Y;
                        }
                        coord[2].X = noses[0].X;
                        coord[2].Y = noses[0].Y;
                        coord[3].X = mouths[0].X;
                        coord[3].Y = mouths[0].Y;

                        return coord;
                    }
                }
            }
            return null;

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


                    var faces = face.Detect(greyimage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(120, 120), new Size(200, 200));
                    if (faces.Length > 0)
                    {
                        foreach (var facecount in faces)
                        {
                            ImageFrame.Draw(facecount.rect, new Bgr(Color.Red), 2);
                            ImageFrame.Draw(facecount.rect.Height + "," + facecount.rect.Width, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.LightGreen));
                            greyimage.ROI = new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y,(int)(facecount.rect.Width * widthScale),(int)( facecount.rect.Height*1.1));
                            // CropFrame = greyimage.Copy();
                            //pic.Add(CropFrame);
                        }
                        //get bigger face in frame
                        cropimage = greyimage.Resize(ROIwidth,ROIheight, INTER.CV_INTER_LINEAR);
                        if (!cropimage.Equals(darkimage)){
                            cropimage._EqualizeHist();
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
        
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Eigen_Recog.Set_Eigen_Threshold = trackBar1.Value;
        }

        private void button3_Click(object sender, EventArgs e)
        {
           
            
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
