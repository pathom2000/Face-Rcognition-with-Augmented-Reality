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
        Image<Gray, byte> AvgFace;
        Image<Gray, byte>[] trainImageArr;
        List<Image<Gray, byte>> diffFaceList;
        Image<Gray, byte>[] diffFaceArr;
        public TestRecog()
        {
            mydb = new DBConn();
            totalImage = mydb.getImageCount();
            trainImageArr = mydb.getTrainedImageList();
            diffFaceList = new List<Image<Gray, byte>>();
            AvgFace = getAVGface(trainImageArr);
            diffFaceArr = getDiffFace();
        }
        public Image<Gray,byte>[] getImage{
            get{
                return mydb.getTrainedImageList();
            }
            
           
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
        public Image<Gray, byte>[] getDiffFace()
        {

            foreach(Image<Gray, byte> image in trainImageArr){

                diffFaceList.Add(image.Sub(AvgFace));
            }
            return diffFaceList.ToArray();
        }
    }
}
