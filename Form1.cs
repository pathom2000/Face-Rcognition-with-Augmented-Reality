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
        int state = 0;
        EventHandler state_name = null;
        private Capture capture;        //takes images from camera as image frames
        private bool captureInProgress = false; // checks if capture is executing
        HaarCascade face;
        DBConn mydb;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        //HaarCascade eye;
        Stopwatch stopWatch = new Stopwatch();
        List<Image<Gray, byte>> allimage;
        List<string> allname;
        int count = 0;
        Classifier_Train Eigen_Recog = new Classifier_Train();
        MCvTermCriteria termCrit;
        EigenObjectRecognizer recognizer;
        public Form1()
        {
            InitializeComponent();
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            mydb = new DBConn();
            
            
        }

        
        
        private void button1_Click(object sender, EventArgs e)
        {
            
                capture = new Capture();

                capture.QueryFrame();
                
                Application.Idle += new EventHandler(ProcessFrame);
                                               
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
                Image<Hsv, byte> hsv = ImageFrame.Convert<Hsv, byte>();
                
                
                
                stopWatch.Start();
                var faces = face.Detect(greyimage,1.3,6,HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT,new Size(100,100),new Size(300,300));
                
                    Parallel.ForEach(faces, facecount =>
                    {
                        try
                        {

                            if (Eigen_Recog.IsTrained)
                            {
                                ImageFrame.ROI = new Rectangle(facecount.rect.X, facecount.rect.Y, facecount.rect.Width, facecount.rect.Height);
                                Image<Gray, byte> imageroi = ImageFrame.Copy().Convert<Gray, byte>().Resize(128, 128, INTER.CV_INTER_LINEAR);
                                ImageFrame.ROI = new Rectangle();
                                imageroi._EqualizeHist();
                                
                                matchedname = Eigen_Recog.Recognise(imageroi,2000);

                                ImageFrame.Draw(matchedname, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.LightGreen));
                            }
                            ImageFrame.Draw(new CircleF(new PointF(facecount.rect.X + facecount.rect.Width / 2, facecount.rect.Y + facecount.rect.Height / 2), facecount.rect.Width / 2), new Bgr(Color.Green), 3);
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
                
                imageBox1.Image = ImageFrame;//line 2
                
            }
            
            
        }
        private void TrainFrame()
        {
            try
            {
                string tempPath = "E:/Images/tmp.jpg";
                Image<Bgr, Byte> ImageFrame = capture.QueryFrame();  //line 1
                Image<Gray, byte> cropimage = new Image<Gray, byte>(128, 128);

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
                            greyimage.ROI = new Rectangle(facecount.rect.X, facecount.rect.Y, facecount.rect.Width, facecount.rect.Height);
                            // CropFrame = greyimage.Copy();
                            //pic.Add(CropFrame);
                        }
                        //get bigger face in frame
                        cropimage = greyimage.Resize(128, 128, INTER.CV_INTER_LINEAR);
                        cropimage._EqualizeHist();
                        imageBox2.Image = cropimage;     //line 2


                        cropimage.Save(tempPath);
                        mydb.InsertImageTraining(textBox1.Text, tempPath);

                        //File.Delete(tempPath);
                        //Eigen_Recog.reloadData();
                    }
                    imageBox8.Image = cropimage;
                }
            }
            catch
            {
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            
            
            
            
            
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            TrainFrame();
        }
        private void FaceDetection(object sender, EventArgs e)
        //private void FaceDetection()
        {
            
            
            Image<Bgr, byte> ImageFrame = capture.QueryFrame();
            ImageFrame._EqualizeHist();
            Image<Hsv, byte> hsv = ImageFrame.Convert<Hsv, byte>();

            Image<Gray, byte>[] channel = hsv.Split();

            Image<Gray, byte> roi = channel[0].Resize(165, 140, INTER.CV_INTER_CUBIC).ThresholdBinaryInv(new Gray(trackBar4.Value), new Gray(255)).Erode(1).Dilate(1);
            
            //RectangleF rect = PointCollection.;
            imageBox4.Image = channel[0].Resize(165, 140, INTER.CV_INTER_CUBIC).ThresholdBinaryInv(new Gray(trackBar4.Value), new Gray(255)).Erode(1);
            imageBox5.Image = channel[1].Resize(165, 140, INTER.CV_INTER_CUBIC);
            imageBox6.Image = channel[2].Resize(165, 140, INTER.CV_INTER_CUBIC);
            imageBox1.Image = roi;
            }

        private void button3_Click(object sender, EventArgs e)
        {
        if(state == 0){
                capture = new Capture();

                capture.QueryFrame();
                state++;
                state_name = FaceDetection;
                Application.Idle += FaceDetection;
                //eye = new HaarCascade("haarcascade_eye.xml");
                
                    
                
            }else{
                state--;
                Application.Idle -= state_name;
                System.Threading.Thread.Sleep(50);
                capture = new Capture();

                capture.QueryFrame();
                state++;
                state_name = FaceDetection;
                Application.Idle += FaceDetection;
            }  
        }

        

        
    }
}
