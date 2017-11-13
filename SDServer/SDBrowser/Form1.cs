using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PRSProtocolLibrary;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SDBrowser
{
    public partial class Form1 : Form
    {
        static string PRS_ADDRESS = "127.0.0.1";
        static ushort PRS_PORT = 30000;

        public Form1()
        {
            InitializeComponent();
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            if (addressTextBox.Text != null && addressTextBox.Text.Length != 0)
            {
                string address = addressTextBox.Text;
                string[] parts = address.Split(':');
                if (parts.Length == 3)
                {
                    if (parts[0] == "FT")
                    {
                        FTGet(parts[1], parts[2]);
                    }
                    else if (parts[0] == "SD")
                    {
                        // TODO: SD protocl
                    }
                    else
                    {
                        MessageBox.Show("Bad user! Protocol not found!");
                    }
                }
                else
                {
                    MessageBox.Show("Bad user! Invalid Address!");
                }
            }
        }

        private void FTGet(string serverIP, string directoryName)
        {
            // clear the contents
            contentTextBox.Clear();

            try
            {
                // get the server port from the PRS for the "FT Server" service
                PRSCServiceClient prs = new PRSCServiceClient("FT Server", IPAddress.Parse(PRS_ADDRESS), PRS_PORT);
                ushort serverPort = prs.LookupPort("FT Server");

                // connect to the server on it's IP address and port
                //Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
                Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(IPAddress.Parse(serverIP), serverPort);
                //Console.WriteLine("Connected to server");

                // establish network stream and reader/writers for the socket
                NetworkStream socketNetworkStream = new NetworkStream(sock);
                StreamReader socketReader = new StreamReader(socketNetworkStream);
                StreamWriter socketWriter = new StreamWriter(socketNetworkStream);

                // send "get\n<directoryName>"
                socketWriter.WriteLine("get");
                socketWriter.WriteLine(directoryName);
                socketWriter.Flush();
                //Console.WriteLine("Sent get " + directoryName);

                // download the files that the server says are in the directory
                bool done = false;
                while (!done)
                {
                    // receive a message from the server
                    //Console.WriteLine("Waiting for msg from server");
                    string cmdString = socketReader.ReadLine();

                    //if (cmdString.Substring(0, 4) == "done")
                    if (cmdString == "done")
                    {
                        // server is done sending files
                        //Console.WriteLine("Received done");
                        done = true;
                    }
                    else
                    {
                        // server sent us a file name and file length
                        string filename = cmdString;
                        //Console.WriteLine("Received file name from server: " + filename);
                        string lengthstring = socketReader.ReadLine();
                        int filelength = System.Convert.ToInt32(lengthstring);
                        //Console.WriteLine("Received file length from server: " + filelength.ToString());

                        // read the file contents as a string, and write them to the local file
                        char[] buffer = new char[filelength];
                        int result = socketReader.Read(buffer, 0, filelength);
                        if (result == filelength)
                        {

                            string fileContents = new string(buffer);
                            contentTextBox.AppendText(filename + "\r\n");
                            contentTextBox.AppendText(fileContents + "\r\n");
                            contentTextBox.AppendText("\r\n");
                            //File.WriteAllText(Path.Combine(directoryName, filename), fileContents);
                            //Console.WriteLine("Wrote " + result.ToString() + " bytes to " + filename + " in " + directoryName);
                        }
                        else
                        {
                            MessageBox.Show("Error: received " + result.ToString() + " bytes, but expected " + filelength.ToString() + " bytes!");
                        }
                    }
                }

                // disconnect from the server and close socket
                Console.WriteLine("Disconnecting from server");
                sock.Disconnect(false);
                socketReader.Close();
                socketWriter.Close();
                socketNetworkStream.Close();
                sock.Close();
                Console.WriteLine("Disconnected from server");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
    class PRSCServiceClient
    {
        public PRSCServiceClient(string serviceName, IPAddress prsAdress, ushort port)
        {
            // PRSServiceClient.PRSServiceClient()
        }

        public ushort RequestPort()
        {
            // PRSServiceClient.RequestPort()
            // After getting a port
            // this class will keep port alive on a separate thread until closed

            return 40001;
        }

        public ushort LookupPort(string serviceName)
        {
            // Called by client
            // PRSServiceClient.LookupPort()
            return 40001;
        }

        public void ClosePort()
        {
            // PRSServiceClient.ClosePort()
        }

        public void KeepAlive()
        {
            // PRSServiceClient.KeepAlive()
        }


    }
}

