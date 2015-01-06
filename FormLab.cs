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
    public partial class FormLab : Form
    {

        Classifier_Train Eigen_Recog = new Classifier_Train();
        String result;
        float distance;
        DBConn db;

        public FormLab()
        {
            InitializeComponent();
            db = new DBConn();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            test();
        }

        private void test()
        {

            Image<Gray, byte> testImage = new Image<Gray, byte>("E:/Images/tmp.jpg");
            result = Eigen_Recog.Recognise(testImage);
            distance = Eigen_Recog.Get_Eigen_Distance;
            textBox1.Text = result;
            textBox2.Text = distance.ToString();
            imageBox7.Image = testImage;
            imageBox1.Image = db.manualQuery1row(result);
        }
    }
}
