using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using PRSProtocolLibrary;

namespace PRSTestClient
{
    class ClientProgram
    {
        static string ADDRESS = "127.0.0.1";
        static int PORT = 30000;

        static void Main(string[] args)
        {
            // create the socket for sending messages to the server
            Socket clientSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Console.WriteLine("Socket created");

            /*
             * Make these functions to test server things
             TestSuccessfullRequestPort();
             TestPortNotAvailable();
             Test other things...();

             */ 


            try
            {
               //Run tests
                //TestCase1(clientSocket);
                //TestCase2
                //TestCase3
                //TestCase4

                //Stop server
                StopServer(clientSocket);


            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception when receiving..." + ex.Message);
            }

            // close the socket and quit
            Console.WriteLine("Closing down");
            clientSocket.Close();
            Console.WriteLine("Closed!");

            Console.ReadKey();
        }

        private static void StopServer(Socket clientSocket)
        {
            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(ADDRESS), PORT);
            PRSCommunicator.SendMessage(clientSocket, endPt, PRSMessage.CreateSTOP());
        }

            private static void TestCase1(Socket clientSocket)
        {
            //Test case 1: FTP server requests port from PRS
            //Should receive port number
            //FTP server sends keep alive
            
            // construct the server's address and port
            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(ADDRESS), PORT);

            string serviceName = "FTP Server";
            ushort allocatedPort = 0;

            //Send REQUEST_PORT
            PRSCommunicator.SendMessage(clientSocket, endPt, PRSMessage.CreateREQUEST_PORT(serviceName));

            //Check and validate SUCCESS
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            PRSMessage statusMsg = PRSCommunicator.ReceiveMessage(clientSocket, ref remoteEP);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase1 Failed! No SUCCESS on Request port.");

            allocatedPort = statusMsg.port;
            Console.WriteLine("Allocated port of " + allocatedPort.ToString());
     
            // send KEEP_ALIVE
            PRSCommunicator.SendMessage(clientSocket, endPt, PRSMessage.CreateKEEP_ALIVE(serviceName, allocatedPort));

            // check status
            statusMsg = PRSCommunicator.ReceiveMessage(clientSocket, ref remoteEP);
            if (statusMsg.status != PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase1 Failed! No SUCCESS on KeepAlive.");

            // send CLOSE_PORT
            PRSCommunicator.SendMessage(clientSocket, endPt, PRSMessage.CreateCLOSE_PORT(serviceName, allocatedPort));
            // Check status
            if (statusMsg.status == PRSMessage.Status.SUCCESS)
                throw new Exception("TestCase1 Failed! No SUCCESS on CLOSE_PORT.");
        }
    }
}
