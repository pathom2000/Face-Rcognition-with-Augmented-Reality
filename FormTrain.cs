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
        private string tempPath = "\\tmp.jpg";
        private string tempPath2 = "\\tmp2.jpg";
        private string folderPath = "";
        private DBConn mydb;
        private Classifier_Train eigenRecog;
        private MCvFont font;

        private Point facePosition;
        private Rectangle faceRectangle;
        private Size faceRectangleSize;

        private Point realFacePosition;
        private Rectangle realfaceRectangle;
        private Size realfaceRectangleSize;
        private Image<Bgr, byte> showImage;
        
        private Size frameSize = new Size(400, 400);
        private Point framePoint = new Point(30, 30);
        private Point frameTextPoint = new Point(30, 30);
        private Int32 newid;
        private int imageCount = 1;
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
            if (File.ReadAllText("setting.txt") != null)
            {
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
            initializeCombobox();
            Application.Idle += new EventHandler(runningCamera);
            comboBox4.SelectedIndex = 0;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            if(comboBox4.SelectedIndex == 0){
                string dateTemp = dateTimePicker1.Value.ToString("s");


                if (mydb.checkUserProfile(textBox1.Text, textBox2.Text))
                {
                    mydb.InsertUserData(textBox1.Text, textBox2.Text, dateTemp, comboBox1.Text, comboBox2.Text);
                }

                newid = mydb.getUserId(textBox1.Text, textBox2.Text, dateTemp, comboBox1.Text);
                if (newid != 0)
                {
                    TrainFrame(newid);
                }
            }else if(comboBox4.SelectedIndex == 1){
                TrainFrame2Image();
                if(File.Exists("E:/ImagetestSet/" + textBox1.Text + comboBox1.Text + imageCount + ".jpg")){
                    imageCount++;
                }
                
            }
            
            

            
            
        }
        private void initializeCombobox()
        {
            int[] allUserID = mydb.getAllUserID();
            if (allUserID != null)
            {
                foreach(int item in allUserID){
                comboBox3.Items.Add(item.ToString());
                }
            }
            
            
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

            if (imageFrameT != null)
            {
                Image<Gray, byte> greyImage = imageFrameT.Copy().Convert<Gray, Byte>();
                showImage = imageFrameT.Copy();

                var faces = face.Detect(greyImage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(120, 120), new Size(200, 200));
                if (faces.Length > 0)
                {
                    label6.ForeColor = Color.Chocolate;
                    label6.Text = "Tracking Face";
                    foreach (var facecount in faces)
                    {
                        facePosition = new Point(facecount.rect.X, facecount.rect.Y);
                        faceRectangleSize = new Size(facecount.rect.Width, facecount.rect.Height);
                        faceRectangle = new Rectangle(facePosition, faceRectangleSize);
                        greyImage.ROI = faceRectangle;
                        var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                        greyImage.ROI = Rectangle.Empty;
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
                            int righteyebrowpoint = eyeObjects[1].X + eyeObjects[1].Width;//
                            int margin = (int)((1.5 / 8.0) * betweeneLength);
                            int neareyebrowpoint = (int)(0.2 * betweeneLength);
                            int faceheight = (int)(2.3 * betweeneLength);

                            realFacePosition = new Point(facePosition.X + lefteyebrowpoint - margin, facePosition.Y + eyeObjects[0].Y - neareyebrowpoint);
                            realfaceRectangleSize = new Size((righteyebrowpoint + margin) - (lefteyebrowpoint - margin), faceheight);
                            realfaceRectangle = new Rectangle(realFacePosition, realfaceRectangleSize);

                            greyImage.ROI = realfaceRectangle;

                            showImage.Draw(realfaceRectangle, new Bgr(Color.LimeGreen), 2);

                        }
                    }
                }
                else
                {
                    label6.ForeColor = Color.DeepSkyBlue;
                    label6.Text = "Idle";
                }

            }
            imageBox1.Image = showImage;
        }
        
       
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.Idle -= runningCamera;
            if (captureT != null)
            {
                captureT.Dispose();
            }
            base.OnFormClosing(e);
            
            if (e.CloseReason == CloseReason.WindowsShutDown) return;
           
            _form1.Show();
            
        }
        private void TrainFrame(int newid)
        {
            try
            {

                
                Image<Gray, byte> darkimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                Image<Gray, byte> cropimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                Image<Gray, byte> plainimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                //ArrayList pic = new ArrayList();
                if (imageFrameT != null)
                {
                    Image<Gray, byte> greyImage = imageFrameT.Copy().Convert<Gray, Byte>();
                    

                    var faces = face.Detect(greyImage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(120, 120), new Size(300, 300));
                    if (faces.Length > 0)
                    {
                        foreach (var facecount in faces)
                        {
                            facePosition = new Point(facecount.rect.X, facecount.rect.Y);
                            faceRectangleSize = new Size(facecount.rect.Width, facecount.rect.Height);
                            faceRectangle = new Rectangle(facePosition, faceRectangleSize);
                            greyImage.ROI = faceRectangle;
                            var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                            greyImage.ROI = Rectangle.Empty;
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
                                int righteyebrowpoint = eyeObjects[1].X + eyeObjects[1].Width;//
                                int margin = (int)((1.5 / 8.0) * betweeneLength);
                                int neareyebrowpoint = (int)(0.2 * betweeneLength);
                                int faceheight = (int)(2.3 * betweeneLength);

                                realFacePosition = new Point(facePosition.X + lefteyebrowpoint - margin, facePosition.Y + eyeObjects[0].Y - neareyebrowpoint);
                                realfaceRectangleSize = new Size((righteyebrowpoint + margin) - (lefteyebrowpoint - margin), faceheight);
                                realfaceRectangle = new Rectangle(realFacePosition, realfaceRectangleSize);

                                greyImage.ROI = realfaceRectangle;
                                
                                //get bigger face in frame
                                cropimage = greyImage.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR);
                                
                                if (!cropimage.Equals(darkimage))
                                {
                                    cropimage._EqualizeHist();

                                    CvInvoke.cvFastNlMeansDenoising(cropimage, cropimage,3,7,21);
                                    plainimage = cropimage.Copy();
                                    Point[] pL = new Point[3];
                                    Point[] pR = new Point[3];
                                    int y0 = 105;
                                    int y1 = 174;
                                    int x0 = 0;
                                    int x1 = 34;
                                    int x2 = 105;
                                    int x3 = 139;
                                    pL[0] = new Point(x0, y0);
                                    pL[1] = new Point(x0, y1);
                                    pL[2] = new Point(x1, y1);
                                    pR[0] = new Point(x3, y0);
                                    pR[1] = new Point(x3, y1);
                                    pR[2] = new Point(x2, y1);
                                    cropimage.FillConvexPoly(pL,new Gray(128));
                                    cropimage.FillConvexPoly(pR, new Gray(128));
                                    //cropimage = cropimage.SmoothMedian(3);
                                    imageBox7.Image = cropimage;     //line 2
                                    cropimage.Save(folderPath + tempPath);
                                    string dbPath = (folderPath + tempPath).Replace("\\", "/");
                                    plainimage.Save(folderPath + tempPath2);
                                    string dbPathPlain = (folderPath + tempPath2).Replace("\\", "/");
                                    mydb.InsertImageTraining(newid,dbPathPlain, dbPath, true);
                                    label6.ForeColor = Color.ForestGreen;
                                    label6.Text = "Success";
                                    //File.Delete(tempPath);
                                    eigenRecog.reloadData();
                                    imageBox7.Image = cropimage;
                                }
                                else
                                {
                                    label6.ForeColor = Color.Red;
                                    label6.Text = "Fail";
                                }

                            }
                            
                        }
                    }
                }
                else
                {
                    mydb.DeleteUser(newid);
                }
            }
            catch
            {
                // MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void TrainFrame2Image()
        {
            try
            {


                Image<Gray, byte> darkimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                Image<Gray, byte> cropimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                Image<Gray, byte> plainimage = new Image<Gray, byte>(ROIwidth, ROIheight);
                //ArrayList pic = new ArrayList();
                if (imageFrameT != null)
                {
                    Image<Gray, byte> greyImage = imageFrameT.Copy().Convert<Gray, Byte>();


                    var faces = face.Detect(greyImage, 1.3, 6, HAAR_DETECTION_TYPE.FIND_BIGGEST_OBJECT, new Size(120, 120), new Size(300, 300));
                    if (faces.Length > 0)
                    {
                        foreach (var facecount in faces)
                        {
                            facePosition = new Point(facecount.rect.X, facecount.rect.Y);
                            faceRectangleSize = new Size(facecount.rect.Width, facecount.rect.Height);
                            faceRectangle = new Rectangle(facePosition, faceRectangleSize);
                            greyImage.ROI = faceRectangle;
                            var eyeObjects = eyeWithGlass.DetectMultiScale(greyImage, 1.3, 6, minEye, maxEye);
                            greyImage.ROI = Rectangle.Empty;
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
                                int righteyebrowpoint = eyeObjects[1].X + eyeObjects[1].Width;//
                                int margin = (int)((1.5 / 8.0) * betweeneLength);
                                int neareyebrowpoint = (int)(0.2 * betweeneLength);
                                int faceheight = (int)(2.3 * betweeneLength);

                                realFacePosition = new Point(facePosition.X + lefteyebrowpoint - margin, facePosition.Y + eyeObjects[0].Y - neareyebrowpoint);
                                realfaceRectangleSize = new Size((righteyebrowpoint + margin) - (lefteyebrowpoint - margin), faceheight);
                                realfaceRectangle = new Rectangle(realFacePosition, realfaceRectangleSize);

                                greyImage.ROI = realfaceRectangle;

                                //get bigger face in frame
                                cropimage = greyImage.Resize(ROIwidth, ROIheight, INTER.CV_INTER_LINEAR);

                                if (!cropimage.Equals(darkimage))
                                {
                                    cropimage._EqualizeHist();

                                    CvInvoke.cvFastNlMeansDenoising(cropimage, cropimage, 3, 7, 21);
                                    plainimage = cropimage.Copy();
                                    Point[] pL = new Point[3];
                                    Point[] pR = new Point[3];
                                    int y0 = 105;
                                    int y1 = 174;
                                    int x0 = 0;
                                    int x1 = 34;
                                    int x2 = 105;
                                    int x3 = 139;
                                    pL[0] = new Point(x0, y0);
                                    pL[1] = new Point(x0, y1);
                                    pL[2] = new Point(x1, y1);
                                    pR[0] = new Point(x3, y0);
                                    pR[1] = new Point(x3, y1);
                                    pR[2] = new Point(x2, y1);
                                    cropimage.FillConvexPoly(pL, new Gray(128));
                                    cropimage.FillConvexPoly(pR, new Gray(128));
                                    //cropimage = cropimage.SmoothMedian(3);
                                    imageBox7.Image = cropimage;     //line 2
                                    cropimage.Save("E:/ImagetestSet/" + textBox1.Text + comboBox1.Text + imageCount + ".jpg");
                                    
                                    label6.ForeColor = Color.ForestGreen;
                                    label6.Text = "Success";
                                    //File.Delete(tempPath);
                                    
                                    imageBox7.Image = cropimage;
                                }
                                else
                                {
                                    label6.ForeColor = Color.Red;
                                    label6.Text = "Fail";
                                }

                            }

                        }
                    }
                }
                
            }
            catch
            {
                // MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectData = mydb.getUserData(comboBox3.Text);
            string[] splitData = selectData.Split(' ');
            textBox1.Text = splitData[1];
            textBox2.Text = splitData[2];
            dateTimePicker1.Text = splitData[3];
            comboBox1.Text = splitData[5];
            comboBox2.Text = splitData[6];
        }

        
    }
}
