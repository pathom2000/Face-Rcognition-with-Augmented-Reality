using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
namespace EMGUCV
{
    public partial class FormImageRetrieve : Form
    {
        FormManageData _formMD;
        Image<Bgr, Byte> imageFrame;
        Image<Bgr, Byte> drawFrame;
        Image<Bgr, Byte> snapFrame;
        Capture capture;
        bool captureFlag = true;
        public FormImageRetrieve(FormManageData formMD)
        {
            _formMD = formMD;
            capture = new Capture();
            InitializeComponent();
            Application.Idle += new EventHandler(runningFrame);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Application.Idle -= new EventHandler(runningFrame);
            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            _formMD.Show();

        }
        private void runningFrame(object sender, EventArgs arg)
        {
            if(captureFlag){
                imageFrame = capture.QueryFrame();
                if (imageFrame != null){
                    drawFrame = imageFrame.Copy();
                    imageBox1.Image = drawFrame;
                }
            }
            else
            {
                snapFrame = drawFrame.Copy();
                Rectangle cropArea = new Rectangle(trackBar1.Value,trackBar2.Value,140,175);
                snapFrame.Draw(cropArea, new Bgr(Color.AliceBlue), 2);
                imageBox1.Image = snapFrame;
            }         
        }
        private void imageBox1_Click(object sender, EventArgs e)
        {
            captureFlag = false;
        }
        private String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmss");
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (!captureFlag)
            {
                drawFrame.ROI = new Rectangle(trackBar1.Value, trackBar2.Value, 140, 175);
                drawFrame._EqualizeHist();
                drawFrame.SmoothMedian(3);
                Console.WriteLine(GetTimestamp(DateTime.Now));
                string path = "E:/TestImage/" + GetTimestamp(DateTime.Now) + ".jpg";
                drawFrame.Save(path);
                MessageBox.Show("Save at "+path);
            }
            
        }
    }
}
