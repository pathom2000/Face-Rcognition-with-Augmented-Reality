using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Threading;


namespace EMGUCV
{
    class ImproveRecognize
    {
        
        CascadeClassifier eyewithglass;
        CascadeClassifier nose;
        CascadeClassifier mouth;
        Size mineye;
        Size maxeye;
        Size minnose;
        Size maxnose;
        Size minmouth;
        Size maxmouth;
        Point[] coord;
        float disEl_Er;  //lefteye-righteye
        float disEl_No;  //lefteye-nose
        float disEl_Mo;  //lefteye-mouth
        float disNo_Er;  //nose-righteye
        float disMo_Er;  //mouth-righteye
        float disNo_Mo;  //nose-mouth
        public ImproveRecognize()
        {
            mineye = new Size(10,10);
            maxeye = new Size(50, 50);
            minnose = new Size(10, 10);
            maxnose = new Size(50, 50);
            minmouth = new Size(10, 10);
            maxmouth = new Size(50, 100);
            
            eyewithglass = new CascadeClassifier("haarcascade_eye_tree_eyeglasses.xml");
            nose = new CascadeClassifier("haarcascade_mcs_nose.xml");
            mouth = new CascadeClassifier("haarcascade_mcs_mouth.xml");
            coord = new Point[4];
            
        }
        public Point[] drawFaceNet(Image<Gray, byte> image)
        {
            var eyes = eyewithglass.DetectMultiScale(image, 1.3, 6, mineye, maxeye);
            //if found pair of eye
            if(eyes.Length == 2){
                //find nose
                var noses = nose.DetectMultiScale(image, 1.3, 6, minnose,maxnose);
                // if found nose
                if(noses.Length == 1){
                    
                    //find mouth
                    var mouths = mouth.DetectMultiScale(image, 1.3, 6, minnose, maxnose);
                    //if found mouth
                    if (mouths.Length == 1)
                    {
                        //do the rest
                        //return coordinate
                        for (int i = 0; i<2;i++ )
                        {
                            coord[i].X = eyes[i].X;
                            coord[i].Y = eyes[i].Y;
                        }
                        coord[2].X = noses[0].X;
                        coord[2].Y = noses[0].Y;
                        coord[3].X = mouths[0].X;
                        coord[3].Y = mouths[0].Y;
                        
                        return coord;
                    }
                }
            }
            return null;

        }
        public void calculateVariance()
        {
            if(coord != null){

            }
        }
    }
}
