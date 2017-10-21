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
            // Args is an array of strings, separated by spaces
            // Example in lecture
            ushort serverPort = 40001;
            // TODO: get the server's IP from the command line
            string serverIP = "127.0.0.1";
            // TODO: get the directory name from the command line
            string directoryName = "foo";
            if (args.Length > 0)
                directoryName = args[0];

            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(serverIP), serverPort);
            Console.WriteLine("Connected to server");

            // make reader writers
            NetworkStream socketNetworkStream = new NetworkStream(sock);
            StreamReader socketReader = new StreamReader(socketNetworkStream);
            StreamWriter socketwriter = new StreamWriter(socketNetworkStream);

            // create the local directory
            Directory.CreateDirectory(directoryName);

            // TODO this
            socketwriter.WriteLine("get");
            socketwriter.WriteLine(directoryName);
            socketwriter.Flush();
            Console.WriteLine("Sent get: " + directoryName);


            // Download the files that the server says are in the directory
            bool done = false;
            while(!done)
            {
                string cmdString = socketReader.ReadLine();

                //if (cmdString.Substring(0,4) == "done")
                if(cmdString == "done")
                {
                    // Server is done!
                    done = true;
                }
                else
                {
                    string filename = cmdString;
                    string lengthstring = socketReader.ReadLine();
                    int filelength = System.Convert.ToInt32(lengthstring);

                    char[] buffer = new char[filelength];
                    socketReader.Read(buffer, 0, filelength);
                    string fileContents = new string(buffer);
                    File.WriteAllText(Path.Combine(directoryName, filename), fileContents);
                }
            }

            // TODO: Make sure you get here
            // disconnect from the server and close socket
            Console.WriteLine("Disconnecting from server");
            sock.Disconnect(false);
            socketReader.Close();
            socketwriter.Close();
            socketNetworkStream.Close();
            sock.Close();
            Console.WriteLine("Disconnected from server");
        }
    }
}
