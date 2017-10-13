/*
 * Steven Reeves 
 * 10/12/2017
 * CST 415
 * Assignment #1
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace PRSProtocolLibrary
{
    public class PRSCommunicator
    {
        public static void SendMessage(Socket sock, IPEndPoint endPt, PRSMessage msg)
        {
            Console.WriteLine("Sending message......");
            byte[] buffer = msg.Serialize();
            int result = sock.SendTo(buffer, endPt);
            // This is for debugging
            //Console.WriteLine("Sent " + result.ToString() + " bytes: " + new string(ASCIIEncoding.UTF8.GetChars(buffer)));
        }

        public static PRSMessage ReceiveMessage(Socket sock, ref IPEndPoint remoteIPEP)
        {
            Console.WriteLine("Waiting for message..........");
            byte[] buffer = new byte[PRSMessage.SIZE];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            int result = sock.ReceiveFrom(buffer, ref remoteEP);
            remoteIPEP = (IPEndPoint)remoteEP;
            // This is for debugging
            //Console.WriteLine("Received " + result.ToString() + " bytes: " + new string(ASCIIEncoding.UTF8.GetChars(buffer)));

            // deserialize and handle the message
            PRSMessage msg = PRSMessage.Deserialize(buffer);

            return msg;
        }
    }
}
