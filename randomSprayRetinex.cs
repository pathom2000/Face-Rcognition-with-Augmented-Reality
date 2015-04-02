using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace EMGUCV
{
   	
    class randomSprayRetinex
    {
        int _N = 1;
        int _n = 250;
        int _inputKernelSize = 25;
        int _intensityChangeKernelSize = 25;
        int _r = 1;
        int _c = 1;
        double _upperBound = 255.0;
        bool _normalizeIntensityChange = false;
        Image<Bgr, byte> _result;
        public randomSprayRetinex()
        {

        }

        public randomSprayRetinex(int numspray,int spraysize,int kernelSize,int intensitykern,int rowstep,int columnstep,double maxintensity,bool normalizeChange)
        {
            _N = numspray;
            _n = spraysize;
            _inputKernelSize = kernelSize;
            _intensityChangeKernelSize = intensitykern;
            _r = rowstep;
            _c = columnstep;
            _upperBound = maxintensity;
            _normalizeIntensityChange = normalizeChange;
        }

        private void Image<Bgr,byte> PerformRandomSpraysRetinex(Image<Bgr,byte> source,int N,int n,double upperBound,int rowsStep,int colsStep){
	
	        int rows= source.Height;
	        int cols= source.Width;

	        int R=sqrt((double)(rows*rows+cols*cols))+0.5;

	        int spraysCount=1000*N;
	        cv::Point2i **sprays=CreateSprays(spraysCount, n, R);
	
	        cv::Mat normalized;
	        source.convertTo(normalized, CV_64FC3);

	        int outputRows=rows/rowsStep;
	        int outputCols=cols/colsStep;
	        destination=cv::Mat(outputRows, outputCols, CV_64FC3);

	        cv::Vec3d *input=(cv::Vec3d *)normalized.data;
	        cv::Vec3d *inputPoint=input;
	        cv::Vec3d *output=(cv::Vec3d *)destination.data;
	        cv::Vec3d *outputPoint=output;

	        cv::RNG random;

	        cv::Mat certainity=cv::Mat::zeros(rows, cols, CV_64FC1);

	        for (int outputRow=0;outputRow<outputRows;++outputRow){
		        for (int outputCol=0;outputCol<outputCols;++outputCol){
			
			        int row=outputRow*rowsStep;
			        int col=outputCol*colsStep;

			        inputPoint=input+row*cols+col;
			        outputPoint=output+outputRow*outputCols+outputCol;
			
			        cv::Vec3d &currentPoint=*inputPoint;
			        cv::Vec3d &finalPoint=*outputPoint;
			        finalPoint=cv::Vec3d(0, 0, 0);

			        for (int i=0;i<N;++i){
				
				        int selectedSpray=random.uniform(0, spraysCount);
				        cv::Vec3d max=cv::Vec3d(0, 0, 0);

				        for (int j=0;j<n;++j){
					
					        int newRow=row+sprays[selectedSpray][j].y;
					        int newCol=col+sprays[selectedSpray][j].x;

					        if (newRow>=0 && newRow<rows && newCol>=0 && newCol<cols){
						
						        cv::Vec3d &newPoint=input[newRow*cols+newCol];

						        for (int k=0;k<3;++k){
							        if (max[k]<newPoint[k]){
								        max[k]=newPoint[k];
							        }
						        }
					        }
					
				        }

				        for (int k=0;k<3;++k){
					        finalPoint[k]+=currentPoint[k]/max[k];
				        }

			        }
			
			        finalPoint/=N;

			        for (int i=0;i<3;++i){
				        if (finalPoint[i]>1){
					        finalPoint[i]=1;
				        }
			        }

		        }
	        }

	        double scaleFactor=upperBound;
	
	        if (rowsStep>1 || colsStep>1){
		        resize(destination, destination, source.size());
	        }

	        destination=destination*scaleFactor-1;

	        destination.convertTo(destination, source.type());

	        DeleteSprays(sprays, spraysCount);
	
        }

        public void Image<Bgr,byte> PerformLightRandomSpraysRetinex(Image<Bgr,byte> source, int N, int n, int inputKernelSize, double inputSigma, int intensityChangeKernelSize, double intensityChangeSigma, int rowsStep, int colsStep, bool normalizeIntensityChange, double upperBound){
	
	        Image<Bgr,byte> inputSource;
	        Image<Bgr,byte> inputRetinex;
	        Image<Bgr,byte> retinex;

	        PerformRandomSpraysRetinex(source, retinex, N, n, upperBound, rowsStep, colsStep);

	        source.convertTo(inputSource, CV_64FC3);
	        retinex.convertTo(inputRetinex, CV_64FC3);

	        if (normalizeIntensityChange){
		        Image<Bgr,byte> illuminant;
        
		        cvdivide(inputSource, inputRetinex, illuminant);
		        std::vector<cv::Mat> illuminantChannels;
	
		        split(illuminant, illuminantChannels);
		        cv::Mat illuminantAverage=(illuminantChannels[0]+illuminantChannels[1]+illuminantChannels[2])/3;
		        for (int i=0;i<3;++i){
			        cv::divide(illuminantChannels[i], illuminantAverage, illuminantChannels[i]);
		        }
		        cv::merge(illuminantChannels, illuminant);
		
		        inputSource=inputRetinex.mul(illuminant);
	        }

	        if (inputKernelSize>1){
		        if (inputSigma==0.0){
			        cv::Mat averaging=cv::Mat::ones(inputKernelSize, inputKernelSize, CV_64FC1)/(double)(inputKernelSize*inputKernelSize);
			        Filter64F(inputSource, inputSource, inputKernelSize);
			        Filter64F(inputRetinex, inputRetinex, inputKernelSize);
		        } else{
			        GaussianBlur(inputSource, inputSource, cv::Size(inputKernelSize, inputKernelSize), inputSigma);
			        GaussianBlur(inputRetinex, inputRetinex, cv::Size(inputKernelSize, inputKernelSize), inputSigma);
		        }
	        }
	
	        cv::Mat illuminant;
	        divide(inputSource, inputRetinex, illuminant);
	        std::vector<cv::Mat> illuminantChannels;
	
	        if (intensityChangeKernelSize>1){
		        if (intensityChangeSigma==0.0){
			        cv::Mat averaging=cv::Mat::ones(intensityChangeKernelSize, intensityChangeKernelSize, CV_64FC1)/(double)(intensityChangeKernelSize*intensityChangeKernelSize);
			        Filter64F(illuminant, illuminant, intensityChangeKernelSize);
		        } else{
			        GaussianBlur(illuminant, illuminant, cv::Size(intensityChangeKernelSize, intensityChangeKernelSize), intensityChangeSigma);
		        }
	        }

	        std::vector<cv::Mat> destinationChannels;
	        split(source, destinationChannels);
	        split(illuminant, illuminantChannels);
	        for (int i=0;i<(int)destinationChannels.size();++i){
		        destinationChannels[i].convertTo(destinationChannels[i], CV_64FC1);
		        cv::divide(destinationChannels[i], illuminantChannels[i], destinationChannels[i]);
	        }
	
	        cv::merge(destinationChannels, destination);
	
	        double *check=(double *)destination.data;
	        for (int i=0;i<destination.rows*destination.cols*3;++i){
		        if (check[i]>=upperBound){
			        check[i]=upperBound-1;
		        }
	        }
	
	        destination.convertTo(destination, source.type());

        }
    }
}
