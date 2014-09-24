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
    class FisherClass : IDisposable
    {
        Image<Gray, byte>[] trainingImages;
        IImage[] Itrainingimage;
        int[] imagelabel;
        string[] imageStringlabel;
        bool _IsTrained = false;


        FisherFaceRecognizer f_recognize;
        DBConn mydb;

        public FisherClass()
        {
            _IsTrained = LoadTrainingData();
        }
        private bool LoadTrainingData()
        {
            mydb = new DBConn();
            imagelabel = mydb.getLabelNumList().ToArray();
            imageStringlabel = mydb.getLabelList().ToArray();
            trainingImages = mydb.getTrainedImageList();
            Itrainingimage = trainingImages;
            if (mydb.getImageCount() > 0)
            {

                if (trainingImages.Length != 0)
                {
                    f_recognize = new FisherFaceRecognizer(0, 123.0);
                    f_recognize.Train(Itrainingimage, imagelabel);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }      
            
            
        }
        public string FisherRecognize(IImage testimage)
        {
            try
            {

                if (_IsTrained)
                {
                    
                    FaceRecognizer.PredictionResult FR = f_recognize.Predict(testimage);
                    Console.WriteLine(FR.Distance);
                    
                    return FR.Distance.ToString() ;
                    

                }
                else return "";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "";
            }
        }
        public bool IsTrained
        {
            get { return _IsTrained; }
        }
        public void reloadData()
        {
            _IsTrained = LoadTrainingData();
        }
        public void Dispose()
        {
            f_recognize = null;
            trainingImages = null;
            imagelabel = null;
            imageStringlabel = null;
            GC.Collect();
        }  
    }
}
