using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using System.IO;
using System.Drawing;
namespace EMGUCV
{
    public class DBConn
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;

        public DBConn(){
            Initialize();
        }

        private void Initialize()
        {
            server = "localhost";
            database = "test";
            uid = "root";
            password = "root";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }
        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        Debug.WriteLine("Cannot connect to server.  Contact administrator");
                        
                        break;

                    case 1045:
                        Debug.WriteLine("Invalid username/password, please try again");
                        
                        break;
                }
                return false;
            }
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Debug.WriteLine(ex.Message);
                
                return false;
            }
        }

        //Insert statement
        public void InsertImageTraining(string Labelname,string TrainedImagePath)
        {

            string query = "INSERT INTO test (label, image) VALUES('" + Labelname + "', LOAD_FILE('" + TrainedImagePath + "'))";
            Debug.WriteLine(query);
            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
            }
        }
        public Int32 getImageCount()
        {
            string query = "select count(*) from test";
            int retval;
            Debug.WriteLine(query);
            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);
               
                //Execute command
                MySqlDataReader rdr = cmd.ExecuteReader();

                rdr.Read();
                
                retval = rdr.GetInt32(0);
                

                //close connection
                this.CloseConnection();
                return retval;
            }
            return 0;
        }

        public List<string> getLabelList()
        {
            string query = "select label from test";
            List<string> retval = new List<string>();
            Debug.WriteLine(query);
            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    string a = rdr.GetString(0);
                    retval.Add(a);
                }
                 //close connection
                this.CloseConnection();
                return retval;
            }
            return null;
        }

        public List<int> getLabelNumList()
        {
            string query = "select idtest from test";
            List<int> retval = new List<int>();
            Debug.WriteLine(query);
            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    int a = rdr.GetInt32(0);
                    retval.Add(a);
                }
                //close connection
                this.CloseConnection();
                return retval;
            }
            return null;
        }

        public Image<Gray, byte>[] getTrainedImageList()
        {
            string query = "select image,length(image) as filesize from test";
            List<Image<Gray, byte>> retval = new List<Image<Gray, byte>>();
            Debug.WriteLine(query);
            byte[] temp;
            Int32 Filesize;
            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                MySqlDataReader rdr = cmd.ExecuteReader();
                
                while (rdr.Read())
                {
                    Filesize = rdr.GetInt32("filesize");

                    temp = new byte[Filesize];
                    
                    rdr.GetBytes(rdr.GetOrdinal("image"), 0, temp, 0,Filesize);
                    MemoryStream stream = new MemoryStream(temp);

                    Image imtemp = Image.FromStream(stream);
                    
                    Image<Gray, byte> addimage = new Image<Gray, byte>(new Bitmap(imtemp));

                    retval.Add(addimage);
                }




                //close connection
                this.CloseConnection();
                return retval.ToArray();
            }
            return null;
        }        
    }  
}
