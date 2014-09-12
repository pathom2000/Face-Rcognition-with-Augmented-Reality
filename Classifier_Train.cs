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
    public class Classifier_Train : IDisposable
    {

        #region Variables

        //Eigen
        MCvTermCriteria termCrit;
        EigenObjectRecognizer recognizer;

        //training variables
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();//Images
        List<string> allname = new List<string>(); //labels
        int ContTrain, NumLabels;
        float Eigen_Distance = 0;
        string Eigen_label;
        int Eigen_threshold = 0;

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

        #region Public
        /// <summary>
        /// <para>Return(True): If Training data has been located and Eigen Recogniser has been trained</para>
        /// <para>Return(False): If NO Training data has been located of error in training has occured</para>
        /// </summary>
        public bool IsTrained
        {
            get { return _IsTrained; }
        }

        /// <summary>
        /// Recognise a Grayscale Image using the trained Eigen Recogniser
        /// </summary>
        /// <param name="Input_image"></param>
        /// <returns></returns>
        public void reloadData()
        {
            _IsTrained = LoadTrainingData();
        }
        public string Recognise(Image<Gray, byte> Input_image, int Eigen_Thresh = -1)
        {
            try
            {
                if (_IsTrained)
                {
                    EigenObjectRecognizer.RecognitionResult ER = recognizer.Recognize(Input_image);
                    //handle if recognizer.EigenDistanceThreshold is set as a null will be returned
                    //NOTE: This is still not working correctley 
                    if (ER == null)
                    {
                        Eigen_label = "Unknown1";
                        Eigen_Distance = 0;
                        return Eigen_label + Eigen_Distance.ToString();
                    }
                    else
                    {
                        Eigen_label = ER.Label;
                        Eigen_Distance = ER.Distance;

                        if (Eigen_Thresh > -1)
                        {
                            Eigen_threshold = Eigen_Thresh;
                        }
                        if (Eigen_Distance > Eigen_threshold)
                        {
                            return Eigen_label + Eigen_Distance.ToString();
                        }
                        else
                        {
                            return "Unknown2" + Eigen_Distance.ToString();
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

        /// <summary>
        /// Sets the threshold confidence value for string Recognise(Image<Gray, byte> Input_image) to be used.
        /// </summary>
        public int Set_Eigen_Threshold
        {
            set
            {
                //NOTE: This is still not working correctley 
                //recognizer.EigenDistanceThreshold = value;
                Eigen_threshold = value;
            }
        }

        /// <summary>
        /// Returns a string containg the recognised persons name
        /// </summary>
        public string Get_Eigen_Label
        {
            get
            {
                return Eigen_label;
            }
        }

        /// <summary>
        /// Returns a float confidence value for potential false clasifications
        /// </summary>
        public float Get_Eigen_Distance
        {
            get
            {
                //get eigenDistance
                return Eigen_Distance;
            }
        }

        /// <summary>
        /// Returns a string contatining any error that has occured
        /// </summary>
        public string Get_Error
        {
            get { return Error; }
        }

        

        /// <summary>
        /// Dispose of Class call Garbage Collector
        /// </summary>
        public void Dispose()
        {
            recognizer = null;
            trainingImages = null;
            allname = null;
            Error = null;
            GC.Collect();
        }

        #endregion

        #region Private
        /// <summary>
        /// Loads the traing data given a (string) folder location
        /// </summary>
        /// <param name="Folder_location"></param>
        /// <returns></returns>
        private bool LoadTrainingData()
        {
            try
            {
                mydb = new DBConn();
                if (mydb.getImageCount() > 0)
                {
                    allname = mydb.getLabelList();

                    trainingImages = mydb.getTrainedImageList();
                    if (trainingImages.ToArray().Length != 0)
                    {
                        termCrit = new MCvTermCriteria(mydb.getImageCount(), 0.001);

                        //Eigen face recognizer
                        recognizer = new EigenObjectRecognizer(
                           trainingImages.ToArray(),
                           allname.ToArray(),
                           4000,
                           ref termCrit);
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
            catch (Exception ex)
            {
                Error = ex.ToString();
                return false;
            }
            
        }

        #endregion
    }


}
