using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace EMGUCV
{
    public partial class FormManageData : Form
    {
        DBConn db;
        private Form1 _form1;

        Classifier_Train eigenRecog;

        int imageCollectionPosition = 0;
        Image<Gray, byte>[] userImageCollection;
        OpenFileDialog browseImage;
        string userId = "";
        public FormManageData(Form1 frm1)
        {
            InitializeComponent();
            db = new DBConn();
            _form1 = frm1;
            FillData();
            eigenRecog = new Classifier_Train();
            browseImage = new OpenFileDialog();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;
            
            _form1.Show();

        }
        void FillData()
        {
            // 1
            // Open connection
            string dataSQL = "SELECT userid,concat(name,' ',surname)as Fullname,birthdate,bloodtype,gender,registertime FROM userprofile;";
            using (MySqlConnection connection = new MySqlConnection(db.getConnectionString))
            {
                connection.Open();
                // 2
                // Create new DataAdapter
                using (MySqlDataAdapter dataAdaptor = new MySqlDataAdapter(dataSQL, connection))
                {
                    // 3
                    // Use DataAdapter to fill DataTable
                    DataTable t = new DataTable();
                    dataAdaptor.Fill(t);
                    // 4
                    // Render data onto the screen
                    dataGridView1.DataSource = t;
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int selectedRow = e.RowIndex;
            DataGridViewRow row = dataGridView1.Rows[selectedRow];
            userId = row.Cells[0].Value.ToString();
            userImageCollection = db.getUserImage(userId);
            if (userImageCollection.Length > 0)
            {
                imageCollectionPosition = 0;
                imageBox7.Image = userImageCollection[imageCollectionPosition];
                if (userImageCollection.Length > 1)
                {
                    button4.Enabled = true;
                    button3.Enabled = true;
                    button2.Enabled = true;
                    button1.Enabled = false;
                    label1.Text = "1";
                    label3.Text = userImageCollection.Length.ToString();
                }
                else
                {
                    button4.Enabled = true;
                    button3.Enabled = true;
                    button1.Enabled = false;
                    button2.Enabled = false;
                    label1.Text = "1";
                    label3.Text = "1";
                }
                
            }
            

                
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (imageCollectionPosition > 0)
            {
                button2.Enabled = true;
                imageBox7.Image = userImageCollection[--imageCollectionPosition];
            }
            if (imageCollectionPosition == 0)
            {
                button1.Enabled = false;
            }
            label1.Text = (imageCollectionPosition + 1).ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (imageCollectionPosition < userImageCollection.Length)
            {
                button1.Enabled = true;
                imageBox7.Image = userImageCollection[++imageCollectionPosition];
            }
            if (imageCollectionPosition == userImageCollection.Length-1)
            {
                button2.Enabled = false;
            }
            label1.Text = (imageCollectionPosition + 1).ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            db.DeleteUserImage(userId, imageCollectionPosition);
            if (imageCollectionPosition > 0)
            {
                 imageBox7.Image = userImageCollection[--imageCollectionPosition];
            }
            else
            {
                imageBox7.Image = userImageCollection[imageCollectionPosition];
            }
           
            
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Delete user.All profile and picture will dissapear.", "Caution", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                db.DeleteUser(Int32.Parse(userId));
                db.DeleteAllUserImage(Int32.Parse(userId));
                FillData();
                imageBox7.Image = null;
            }
            else if (dialogResult == DialogResult.No)
            {
                //do something else
            }
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Image<Gray,byte>[] selfCheckImageList = db.getTrainedImageList();
            int[] selfCheckLabel = db.getAllImageID();
            int count = 0;
            foreach(var image in selfCheckImageList){

                string[] matchedData = imageCheckRecognize(image);
                db.updateSelfChecking(matchedData[0], matchedData[1], selfCheckLabel[count].ToString());
                count++;
            }
            MessageBox.Show("Self Checking Finished.");
        }
        private string[] imageCheckRecognize(Image<Gray,byte> image)
        {
            string matchedResult = eigenRecog.Recognise(image);
            string[] matchedData = matchedResult.Split(' ');
            return matchedData;
        }
        private void button6_Click(object sender, EventArgs e)
        {
            string folderPath = "";
            browseImage.Title = "Please select 140*175 pixel Image";
            DialogResult result = browseImage.ShowDialog();
            Image<Gray, byte> loadImage;
            if (result == DialogResult.OK) // Test result.
            {
                folderPath = browseImage.FileName;
                Console.WriteLine(folderPath);
                Image<Gray, byte> receiveImage = new Image<Gray, byte>(folderPath);
                if (receiveImage.Width <= 640 && receiveImage.Height <= 480)
                {
                    loadImage = receiveImage;
                    
                    imageBox7.Image = loadImage;
                    
                    
                    //tomorrow
                    string[] matchedData = imageCheckRecognize(loadImage);
                    label7.Text = matchedData[0];//result
                    label5.Text = matchedData[1];//distance
                }
                else
                {
                    MessageBox.Show("Please select image that smaller than 640x480 pixel");
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {                    
            FormImageRetrieve frmManData = new FormImageRetrieve(this);
            frmManData.Show();
            //button1.Enabled = true;
            this.Hide();
           
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int[] allName = db.getAllImageID();
            Image<Gray, byte>[] allImage = db.getTrainedImageList();
            string tempPath = "\\tmpp.jpg";
            string folderPath = "E:\\Visual2013\\Project\\EMGUCV\\EMGUCV\\bin\\x64\\Release";
            Point[] pL = new Point[3];
            Point[] pR = new Point[3];
            int y0 = 105;
            int y1 = 174;
            int x0 = 0;
            int x1 = 34;
            int x2 = 105;
            int x3 = 139;
            pL[0] = new Point(x0, y0);
            pL[1] = new Point(x0, y1);
            pL[2] = new Point(x1, y1);
            pR[0] = new Point(x3, y0);
            pR[1] = new Point(x3, y1);
            pR[2] = new Point(x2, y1);
            int count = 0;
            foreach(var id in allName){

                
                allImage[count].FillConvexPoly(pL, new Gray(128));
                allImage[count].FillConvexPoly(pR, new Gray(128));
                allImage[count].Save(folderPath + tempPath);
                string dbPath = (folderPath + tempPath).Replace("\\", "/");
                db.updateImagePreprocessing(allName[count].ToString(), dbPath);
                count++;
            }
            MessageBox.Show("Image trans finish.");

        }  
    }
}
