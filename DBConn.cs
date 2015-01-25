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
using System.Windows.Forms;
namespace EMGUCV
{
    public class DBConn
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;
        private string connectionString;
        public DBConn(){
            
            Initialize();      
        }

        private void Initialize()
        {
            server = "localhost";
            database = "facerecog";
            uid = "root";
            password = "root";
            
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
        }
        public bool IsServerConnected()
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return true;
                }
                catch (MySqlException)
                {
                    return false;
                }
            }
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
        public void InsertImageTraining(int Labelname,string TrainedImagePath,bool IsOriginal)
        {
            char originalFlag;
            if(IsOriginal){
                originalFlag = 'Y';
            }else{
                originalFlag = 'N';
            }
            string query = "INSERT INTO faceimage (userid, image,original) VALUES(" + Labelname + ", LOAD_FILE('" + TrainedImagePath + "'),'" + originalFlag + "')";
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
        public void InsertUserData(string userName, string userSurname, string birthDate,string bloodType)
        {

            string query = "INSERT INTO userprofile (name,surname,birthday,bloodtype) VALUES('" + userName + "','" + userSurname + "','" + birthDate + "','" + bloodType + "');";
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
        public Int32 getUserId(string userName, string userSurname, string birthDate, string bloodType)
        {
            string query = "select userid from userprofile where name = '" + userName + "' and surname = '" + userSurname + "' and birthday = '" + birthDate + "' and bloodtype = '" + bloodType +"';";
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
        public void DeleteUser(int userID)
        {

            string query = "DELETE from userprofile WHERE userid ='" + userID + "';";
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
            string query = "select count(*) from faceimage";
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

        public void DeleteOldestImage(string name)
        {

            string query = "DELETE from faceimage WHERE userid ='"+name+"' AND original != 'Y' ORDER BY timestamp LIMIT 1";
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

        public Int32 getSpecifyImageCount(string name)
        {
            string query = "select count(*) from faceimage where userid = '" + name + "' AND original != 'Y'";
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
            string query = "select userid from faceimage";
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
        public string getUserData(string id)//if not have data ret ""
        {
            string query = "select * from userprofile where userid ="+ id;
            string result = "";
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
                    for (int i = 0;i<5 ;i++ )
                    {
                        if (i != 4)
                        {
                            result += rdr.GetString(i) + " ";
                        }
                        else
                        {
                            result += rdr.GetString(i);
                        }
                        
                    }
                    
                    
                }
                //close connection
                this.CloseConnection();
                return result;
            }
            return null;
        }
        
        public Image<Gray, byte> getResultImage(string res)
        {

            Image<Gray, byte> addimage;
            string query = "select image,length(image) as filesize from faceimage where userid = '"+ res+"'";
            Image<Gray, byte> retval = new Image<Gray, byte>(140,175);
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

                    rdr.GetBytes(rdr.GetOrdinal("image"), 0, temp, 0, Filesize);
                    MemoryStream stream = new MemoryStream(temp);

                    Image imtemp = Image.FromStream(stream);

                    addimage = new Image<Gray, byte>(new Bitmap(imtemp));

                    retval = addimage;
                }




                //close connection
                this.CloseConnection();
                return retval;
            }
            return null;
        }
        
        public Image<Gray, byte>[] getTrainedImageList()
        {
            Image<Gray, byte> addimage;
            string query = "select image,length(image) as filesize from faceimage";
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
                    
                    addimage = new Image<Gray, byte>(new Bitmap(imtemp));

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
