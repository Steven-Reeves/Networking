/*
 * Steven Reeves 
 * 11/12/2017
 * CST 415
 * Assignment #4
 */

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
        Dictionary<string, ulong> sdSessions = new Dictionary<string, ulong>();   // ip adress: sessionid

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
                        SDGet(parts[1], parts[2]);
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

        private void SDGet(string serverIP, string documentName)
        {
            // clear the contents
            contentTextBox.Clear();

            try
            {
                // Lookup serverPort with PRS stub
                PRSCServiceClient prs = new PRSCServiceClient("SD Server", IPAddress.Parse(PRS_ADDRESS), PRS_PORT);
                ushort serverPort = prs.LookupPort("SD Server");

                // connect to the server on it's IP address and port
                Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(IPAddress.Parse(serverIP), serverPort);

                // establish network stream and reader/writers for the socket
                NetworkStream socketNetworkStream = new NetworkStream(sock);
                StreamReader socketreader = new StreamReader(socketNetworkStream);
                StreamWriter socketwriter = new StreamWriter(socketNetworkStream);

                string responseString = null;
                ulong sessionID = 0;
                // open or resume a session
                if (sdSessions.ContainsKey(serverIP))
                {

                    //resume
                    sessionID = sdSessions[serverIP];
                    socketwriter.WriteLine("resume");
                    socketwriter.WriteLine(sessionID.ToString());
                    socketwriter.Flush();

                    // receive accept from server
                    responseString = socketreader.ReadLine();
                    if (responseString == "accepted")
                    {
                        responseString = socketreader.ReadLine();
                        sessionID = System.Convert.ToUInt64(responseString);
                    }
                    // receive reject from server
                    else if (responseString == "rejected")
                    {
                        responseString = socketreader.ReadLine();
                        throw new Exception("Session resume rejected " + responseString);
                    }
                    else
                    {
                        throw new Exception("Received invalid response" + responseString);
                    }
                }
                else
                {
                    // Open a new sesion with the server
                    socketwriter.WriteLine("open");
                    socketwriter.Flush();

                    // recieve accept from server
                    responseString = socketreader.ReadLine();
                    if (responseString == "accepted")
                    {
                        responseString = socketreader.ReadLine();
                        sessionID = System.Convert.ToUInt64(responseString);
                    }
                    else
                    {
                        throw new Exception("Invalid response from server: " + responseString);
                    }
                    sdSessions[serverIP] = sessionID;

                }


                //send get to server
                socketwriter.WriteLine("get");
                socketwriter.WriteLine(documentName);
                socketwriter.Flush();

                // Receive response for GET
                responseString = socketreader.ReadLine();
                if (responseString == "success")
                {
                    responseString = socketreader.ReadLine();
                    if (responseString == documentName)
                    {
                        responseString = socketreader.ReadLine();
                        int length = System.Convert.ToInt32(responseString);

                        char[] buffer = new char[length];
                        int result = socketreader.Read(buffer, 0, length);
                        if (result == length)
                        {
                            string documentContents = new string(buffer);
                            // Add this to the text box
                            contentTextBox.AppendText(documentContents + "\r\n");
                        }
                        else
                            throw new Exception("Error, received wrong number of bytes");
                    }
                    else
                    {
                        throw new Exception("Recieved unexpected docment name!");
                    }

                }
                else if (responseString == "error")
                {
                    responseString = socketreader.ReadLine();
                    throw new Exception("Recieved error from server: " + responseString);
                }
                else
                {
                    throw new Exception("Recieved invalid response" + responseString);
                }
                // Disconnect from server and close socket
                sock.Disconnect(false);
                socketreader.Close();
                socketwriter.Close();
                socketNetworkStream.Close();
                sock.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(IPAddress.Parse(serverIP), serverPort);

                // establish network stream and reader/writers for the socket
                NetworkStream socketNetworkStream = new NetworkStream(sock);
                StreamReader socketReader = new StreamReader(socketNetworkStream);
                StreamWriter socketWriter = new StreamWriter(socketNetworkStream);

                // send "get\n<directoryName>"
                socketWriter.WriteLine("get");
                socketWriter.WriteLine(directoryName);
                socketWriter.Flush();

                // download the files that the server says are in the directory
                bool done = false;
                while (!done)
                {
                    // receive a message from the server
                    string cmdString = socketReader.ReadLine();

                    //if (cmdString.Substring(0, 4) == "done")
                    if (cmdString == "done")
                    {
                        // server is done sending files
                        done = true;
                    }
                    else
                    {
                        // server sent us a file name and file length
                        string filename = cmdString;
                        string lengthstring = socketReader.ReadLine();
                        int filelength = System.Convert.ToInt32(lengthstring);

                        // read the file contents as a string, and write them to the local file
                        char[] buffer = new char[filelength];
                        int result = socketReader.Read(buffer, 0, filelength);
                        if (result == filelength)
                        {

                            string fileContents = new string(buffer);
                            contentTextBox.AppendText(filename + "\r\n");
                            contentTextBox.AppendText(fileContents + "\r\n");
                            contentTextBox.AppendText("\r\n");

                        }
                        else
                        {
                            MessageBox.Show("Error: received " + result.ToString() + " bytes, but expected " + filelength.ToString() + " bytes!");
                        }
                    }
                }

                // disconnect from the server and close socket
                sock.Disconnect(false);
                socketReader.Close();
                socketWriter.Close();
                socketNetworkStream.Close();
                sock.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(sdSessions.Count > 0)
            {
                foreach(KeyValuePair<string, ulong> pair in sdSessions)
                {
                    string serverIP = pair.Key;
                    ulong sessionID = pair.Value;
                    try
                    {
                        // Lookup serverPort with PRS stub
                        PRSCServiceClient prs = new PRSCServiceClient("SD Server", IPAddress.Parse(PRS_ADDRESS), PRS_PORT);
                        ushort serverPort = prs.LookupPort("SD Server");

                        // connect to the server on it's IP address and port
                        Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
                        sock.Connect(IPAddress.Parse(serverIP), serverPort);

                        // establish network stream and reader/writers for the socket
                        NetworkStream socketNetworkStream = new NetworkStream(sock);
                        StreamReader socketreader = new StreamReader(socketNetworkStream);
                        StreamWriter socketwriter = new StreamWriter(socketNetworkStream);

                        // Close session
                        socketwriter.WriteLine("close");
                        socketwriter.WriteLine(sessionID.ToString());
                        socketwriter.Flush();

                        // receive close from server
                        string responseString = socketreader.ReadLine();
                        if (responseString == "closed")
                        {
                            responseString = socketreader.ReadLine();
                            ulong closedSessionId = System.Convert.ToUInt64(responseString);
                        }
                        else
                        {
                            throw new Exception("Received invalid response" + responseString);
                        }

                        // Disconnect from server and close socket
                        sock.Disconnect(false);
                        socketreader.Close();
                        socketwriter.Close();
                        socketNetworkStream.Close();
                        sock.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to close session with server: " + serverIP + " with session id: " + sessionID.ToString() + " error: " + ex.Message);
                    }
                }
            }
        }

        private void htmlBrowserPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void htmlBrowserPanel_LinkClicked(object sender, string target)
        {
            //TODO: decide which get/post to use
            //Get(target);

            //if SD
            //SDGET(target);
            //if FT
            //FTGET(target);

        }
        private void htmlBrowserPanel_FormClicked(object sender,
            HTMLBrowserPanel.FormClickEventArgs e)
        {
            //TODO: implement Get() and Post()
            /*
            if (e.Method.ToLower() == "get")
                Get(e.Target);
            else if (e.Method.ToLower() == "post")
                Post(e.Target, e.FormVariablesString);
            else
                MessageBox.Show("Unrecognized method " + e.Method);
                */
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

