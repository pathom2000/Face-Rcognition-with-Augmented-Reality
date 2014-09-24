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
        DBConn mydb;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        
        Stopwatch stopWatch = new Stopwatch();
        Image<Gray, byte>[] allimage;
        
        int count = 0;
        Classifier_Train Eigen_Recog = new Classifier_Train();
        FisherClass Fish_Recog = new FisherClass();
       
        Image<Gray, float>[] EigenimageARR;
        TestRecog t;
        double ROImargin = 1;
        double widthScale = 1;
        int ROIwidth = 200;
        int ROIheight = 200;
        public Form1()
        {
            InitializeComponent();
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            mydb = new DBConn();
            t = new TestRecog();
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
                imageBox7.Image = t.getAVGface(mydb.getTrainedImageList());
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
                Image<Hsv, byte> hsv = ImageFrame.Convert<Hsv, byte>();
                
                
                
                stopWatch.Start();
                var faces = face.Detect(greyimage,1.3,6,HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT,new Size(100,100),new Size(300,300));
                
                    Parallel.ForEach(faces, facecount =>
                    {
                        try
                        {
                            //if (Fish_Recog.IsTrained)
                            if (Eigen_Recog.IsTrained)
                            {
                                ImageFrame.ROI = new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), facecount.rect.Height);
                                Image<Gray, byte> imageroi = ImageFrame.Copy().Convert<Gray, byte>().Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR);
                                ImageFrame.ROI = new Rectangle();
                                imageroi._EqualizeHist();
                                
                                matchedname = Eigen_Recog.Recognise(imageroi);
                                //matchedname = Fish_Recog.FisherRecognize(imageroi);
                                ImageFrame.Draw(matchedname, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.Red));
                            }
                            ImageFrame.Draw(new Rectangle((int)(facecount.rect.X * ROImargin), facecount.rect.Y, (int)(facecount.rect.Width * widthScale), facecount.rect.Height), new Bgr(Color.LawnGreen), 2);
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
                            cropimage._EqualizeHist();
                            CvInvoke.cvSmooth(cropimage, cropimage, SMOOTH_TYPE.CV_GAUSSIAN, 1, 1, 1, 1);
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
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
                resultimage = t.getAVGface(ImageList.ToArray());
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
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Eigen_Recog.Set_Eigen_Threshold = trackBar1.Value;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            TestRecog test = new TestRecog();
            //imageBox7.Image = test.getAVGface();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SpecialTrainFrame();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Image<Gray, byte>[] dif = t.getDiffFace();
            imageBox9.Image = dif[0];
        }
       
                        
    }
}
