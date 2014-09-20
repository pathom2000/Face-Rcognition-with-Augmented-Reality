using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Emgu.CV.UI;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Drawing.Imaging;
using System.Drawing;

namespace EMGUCV
{
    class TestRecog
    {
        DBConn mydb;
        int totalImage;
        public TestRecog()
        {
            mydb = new DBConn();
            totalImage = mydb.getImageCount();
        }
        public Image<Gray,byte>[] getImage(){
           return mydb.getTrainedImageList();
        }
        public Image<Gray, byte> getAVGface(Image<Gray,byte>[] ImageSet)
        {

            Image<Gray, int> AVGface = new Image<Gray, int>(200, 200);
            //Image<Gray, byte>[] ImageList = getImage();


            foreach (Image<Gray, byte> Image in ImageSet)
            {
                Image.Convert<Gray,int>();
                CvInvoke.cvAdd(AVGface,Image,AVGface,IntPtr.Zero);
            }
            return (AVGface / ImageSet.Length).Convert<Gray, byte>();
        }
    }
}
