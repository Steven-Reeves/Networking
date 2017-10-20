using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTClient
{
    class ClientProgram
    {
        static void Main(string[] args)
        {
            // TODO: get the server port from the PRS for the "FT Server" service
            ushort serverPort = 40001;
            // TODO: get the server's IP from the command line
            string serverIP = "127.0.0.1";
            // TODO: get the directory name from the command line
            string directoryName = "foo";
            if (args.Length > 1)
                directoryName = args[1];

            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(serverIP), serverPort);
            Console.WriteLine("Connected to server");

            // TODO: use these!
            // Byte streams over socket? YES!
            //NetworkStream ns = new NetworkStream(sock);
            //StreamReader sr = new StreamReader(ns);
            //sr.ReadLine();


            // create the local directory
            Directory.CreateDirectory(directoryName);

            // send "get \n<directoryName>"
            string msg = "get\n" + directoryName + "\n";
            Console.WriteLine("Sending to server: " + msg);
            byte[] buffer = ASCIIEncoding.UTF8.GetBytes(msg);
            int length = sock.Send(buffer);
            Console.WriteLine("Sent " + length.ToString() + " bytes to server");


            // Download the files that the server says are in the directory
            bool done = false;
            while(!done)
            {
                // Recieve a message from the server
                buffer = new byte[256];
                length = sock.Receive(buffer);
                string cmdString = new string(ASCIIEncoding.UTF8.GetChars(buffer));
                cmdString = cmdString.TrimEnd('\0');
                Console.WriteLine("Recieved " + length.ToString() + " bytes from client: " + cmdString);

                if(cmdString.Substring(0,4) == "done")
                {
                    // Server is done!
                    done = true;
                }
                else
                {
                    // Server sent filename and length
                    string filename = cmdString.Substring(0, cmdString.IndexOf('\n'));
                    string lengthstring = cmdString.Substring(cmdString.IndexOf('\n') + 1);
                    lengthstring = lengthstring.TrimEnd('\n');
                    int filelength = System.Convert.ToInt32(lengthstring);

                    // Read the file bytes and write them out
                    buffer = new byte[filelength];
                    length = sock.Receive(buffer);
                    if(length != filelength)
                    {
                        // Error!
                        Console.WriteLine("Received wrong number of bytes for the file");
                    }
                    else
                    {
                        // Write out the file
                        File.WriteAllBytes(directoryName + "\\" + filename, buffer);
                    }

                }
            }

            // TODO: Make sure you get here
            // disconnect from the server and close socket
            Console.WriteLine("Disconnecting from server");
            sock.Disconnect(false);
            sock.Close();
            Console.WriteLine("Disconnected from server");
        }
    }
}
