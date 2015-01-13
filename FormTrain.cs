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
    public partial class FormTrain : Form
    {
        private Form1 _form1;
        private Capture captureT;
        private Image<Bgr, Byte> imageFrameT;
        private HaarCascade face;
        private CascadeClassifier eyeWithGlass;
        private int ROIwidth = 140;
        private int ROIheight = 175;
        private Size minEye;
        private Size maxEye;
        private string tempPath = "E:/Images/tmp.jpg";
        private DBConn mydb;
        private Classifier_Train eigenRecog;
        private MCvFont font;  

        public FormTrain(Form1 frm1)
        {
            InitializeComponent();
            _form1 = frm1;

            eigenRecog = new Classifier_Train();
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            eyeWithGlass = new CascadeClassifier("haarcascade_eye_tree_eyeglasses.xml");
            mydb = new DBConn();
            minEye = new Size(10, 10);
            maxEye = new Size(225, 225);
            font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);    

            captureT = new Capture();
            Application.Idle += new EventHandler(runningCamera);
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TrainFrame();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Idle -= runningCamera;
            if (captureT != null)
            {
                captureT.Dispose();
            }
                
            _form1.Show();
            this.Close();
        }
        private void runningCamera(object sender, EventArgs e)
        {
            imageFrameT = captureT.QueryFrame();
            imageBox1.Image = imageFrameT;
        }
       
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;
            Application.Idle -= runningCamera;
            if (captureT != null)
            {
                captureT.Dispose();
            }
            _form1.Show();
            
        }
        private void TrainFrame()
        {
            try
            {

                
                Image<Gray, byte> darkimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                Image<Gray, byte> cropimage = new Image<Gray, byte>(ROIwidth, ROIheight);

                //ArrayList pic = new ArrayList();
                if (imageFrameT != null)
                {
                    Image<Gray, byte> greyImage = imageFrameT.Convert<Gray, byte>();
                    

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

                                //imageFrameT.Draw(facecount.rect, new Bgr(Color.Red), 2);
                                //imageFrameT.Draw(facecount.rect.Height + "," + facecount.rect.Width, ref font, new Point(facecount.rect.X - 2, facecount.rect.Y - 2), new Bgr(Color.LightGreen));
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
                            imageBox7.Image = cropimage;
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
