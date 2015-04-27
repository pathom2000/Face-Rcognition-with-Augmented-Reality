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

/// <summary>
/// Desingned to remove the training a EigenObjectRecognizer code from the main form
/// </summary>
namespace EMGUCV
{
    [Serializable]
    public class Classifier_Train : IDisposable
    {

        #region Variables

        //Eigen
        MCvTermCriteria termCrit;
        EigenObjectRecognizer recognizer;
        
        //training variables
        Image<Gray, byte>[] trainingImages;//Images
        int[] allname; 
        
        float Eigen_Distance = 0;
        string Eigen_label;
        
        int recognizeTreshold = 6000;
        int maxRecognizeTreshold = 10000;
        //Class Variables
        string Error;
        bool _IsTrained = false;
        DBConn mydb ;
        #endregion

        #region Constructors
        /// <summary>
        /// Default Constructor, Looks in (Application.StartupPath + "\\TrainedFaces") for traing data.
        /// </summary>
        public Classifier_Train()
        {
            
            _IsTrained = LoadTrainingData();
        }

        
        #endregion

        public bool IsTrained
        {
            get { return _IsTrained; }
        }
        public int getRecognizeTreshold
        {
            get { return recognizeTreshold; }
        }
        public Image<Gray, byte>[] getTrainingImage()
        {
            if (_IsTrained)
            {
                return trainingImages;
            }
            else
            {
                return null;
            }
        }
        
        public void reloadData()
        {
            _IsTrained = LoadTrainingData();
        }
        public Image<Gray,float>[] getEigenfaceArray()
        {
            if(_IsTrained){
                return recognizer.EigenImages;
            }
            else
            {
                return null;
            }
        }
       
        public string Recognise(Image<Gray, byte> Input_image, int Eigen_Thresh = -1)
        {
            try
            {
                
                if (_IsTrained)
                {
                    Set_Eigen_Threshold = recognizeTreshold;
                    EigenObjectRecognizer.RecognitionResult ER = recognizer.Recognize(Input_image);
                   
                    if (ER == null)
                    {
                        Eigen_label = "UnknownNull";
                        Eigen_Distance = 0;
                        return Eigen_label + " " + Eigen_Distance.ToString();
                    }
                    else
                    {
                        Eigen_label = ER.Label;
                        Eigen_Distance = ER.Distance;
                        
                        
                        if (Eigen_Distance < recognizeTreshold)
                        {
                            return Eigen_label + " " + Eigen_Distance.ToString();
                        }
                            // 
                        else
                        {
                            return "UnknownFace" + " " + Eigen_Distance.ToString();
                        }
                    }

                }
                else return "";
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return "";
            }
            
        }

        public int Set_Eigen_Threshold
        {
            set
            {
                 recognizer.EigenDistanceThreshold = value;
            }
        }

        public string Get_Eigen_Label
        {
            get
            {
                return Eigen_label;
            }
        }

        public float Get_Eigen_Distance
        {
            get
            {
                //get eigenDistance
                return Eigen_Distance;
            }
        }

        public string Get_Error
        {
            get { return Error; }
        }

        public void Dispose()
        {
            recognizer = null;
            trainingImages = null;
            allname = null;
            Error = null;
            GC.Collect();
        }
        
        
        private bool LoadTrainingData()
        {
            mydb = new DBConn();
            allname = mydb.getAllImageID();
            string[] allname_st = allname.Select(x => x.ToString()).ToArray();
            trainingImages = mydb.getTrainedImageList();
            
            //trainingImages = mydb.getRawTrainedImageList();  
                if (mydb.getImageCount() > 0)
                {
                    
                    if (trainingImages.Length != 0)
                    {
                        //set round and ...
                        //termCrit = new MCvTermCriteria(mydb.getImageCount(), 0.001);
                        termCrit = new MCvTermCriteria(5000, 0.0001);
                         //Eigen face recognizer
                        recognizer = new EigenObjectRecognizer(trainingImages, allname_st, maxRecognizeTreshold, ref termCrit);
                        
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
    }
}
