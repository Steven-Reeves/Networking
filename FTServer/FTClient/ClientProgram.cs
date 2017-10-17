using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

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

            // connect to the server on it's IP address and port
            Console.WriteLine("Connecting to server at " + serverIP + ":" + serverPort.ToString());
            Socket sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(IPAddress.Parse(serverIP), serverPort);
            Console.WriteLine("Connected to server");

            // send "get <directoryName>"
            string msg = "get " + directoryName;
            Console.WriteLine("Sending to server: " + msg);
            byte[] buffer = ASCIIEncoding.UTF8.GetBytes(msg);
            int length = sock.Send(buffer);
            Console.WriteLine("Sent " + length.ToString() + " bytes to server");


            // TODO Download the files that the server says are in the directory

            // disconnect from the server and close socket
            Console.WriteLine("Disconnecting from server");
            sock.Disconnect(false);
            sock.Close();
            Console.WriteLine("Disconnected from server");
        }
    }
}
