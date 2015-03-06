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

        int imageCollectionPosition = 0;
        Image<Gray, byte>[] userImageCollection;

        string userId = "";
        public FormManageData(Form1 frm1)
        {
            InitializeComponent();
            db = new DBConn();
            _form1 = frm1;
            FillData();
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

        
    }
}
