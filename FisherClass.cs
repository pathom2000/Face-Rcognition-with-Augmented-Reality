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
        public FisherClass()
        {

        }
        public void Dispose()
        {
            
            GC.Collect();
        }  
    }
}
