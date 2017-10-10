using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace PRSProtocolLibrary
{
    public class PRSMessage
    {
        /*
            typedef struct
            {
                int8_t msg_type;
                char service_name[50];
                uint16_t port;
                int8_t status;
            } request_t;
        */

        public const int SIZE = 54;

        public enum MsgType
        {
            REQUEST_PORT = 1,
            LOOKUP_PORT = 2,
            KEEP_ALIVE = 3,
            CLOSE_PORT = 4,
            RESPONSE = 5,
            PORT_DEAD = 6,
            STOP = 7
        }

        public enum Status
        {
            SUCCESS = 0,
            SERVICE_IN_USE = 1,
            SERVICE_NOT_FOUND = 2,
            ALL_PORTS_BUSY = 3,
            INVALID_ARG = 4,
            UNDEFINED_ERROR = 5
        }

        private const int MSG_TYPE_INDEX = 0;
        private const int SERVICE_NAME_INDEX = 1;
        private const int MAX_SERVICE_NAME = 50;
        private const int PORT_INDEX = 51;
        private const int STATUS_INDEX = 53;

        public MsgType msgType;
        public string serviceName;
        public ushort port;
        public Status status;

        public static PRSMessage CreateSTOP()
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.STOP;
            return msg;
        }

        public static PRSMessage CreateREQUEST_PORT(string serviceName)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.REQUEST_PORT;
            msg.serviceName = serviceName;
            return msg;
        }

        public static PRSMessage CreateLOOKUP_PORT(string serviceName)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.LOOKUP_PORT;
            msg.serviceName = serviceName;
            return msg;
        }

        public static PRSMessage CreateKEEP_ALIVE(string serviceName, ushort port)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.KEEP_ALIVE;
            msg.serviceName = serviceName;
            msg.port = port;
            return msg;
        }

        public static PRSMessage CreateCLOSE_PORT(string serviceName, ushort port)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.CLOSE_PORT;
            msg.serviceName = serviceName;
            msg.port = port;
            return msg;
        }

        public static PRSMessage CreatePORT_DEAD(string serviceName, ushort port)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.PORT_DEAD;
            msg.serviceName = serviceName;
            msg.port = port;
            return msg;
        }
        public static PRSMessage CreateRESPONSE(string serviceName, ushort port, Status status)
        {
            PRSMessage msg = new PRSMessage();
            msg.msgType = MsgType.RESPONSE;
            msg.serviceName = serviceName;
            msg.port = port;
            msg.status = status;
            return msg;
        }

            public static PRSMessage Deserialize(byte[] buf)
        {
            PRSMessage msg = new PRSMessage();

            // first turn bytes into integral values
            msg.msgType = (PRSMessage.MsgType)buf[MSG_TYPE_INDEX];
            msg.serviceName = new string(ASCIIEncoding.UTF8.GetChars(buf, SERVICE_NAME_INDEX, MAX_SERVICE_NAME-1)); // leave room for null terminator
            msg.port = BitConverter.ToUInt16(buf, PORT_INDEX);
            msg.status = (PRSMessage.Status)buf[STATUS_INDEX];

            // then translate values from network to host byte order
            msg.port = (ushort)IPAddress.NetworkToHostOrder((short)msg.port);

            return msg;
        }

        public byte[] Serialize()
        {
            // first translate values into network byte order
            // MsgType msgType;     1-byte
            // string serviceName;  up to 50 1-byte values (null terminated string)
            // ushort port;         2-byte <-- translate!
            // Status status;       1-byte
            ushort shortPort = (ushort)IPAddress.HostToNetworkOrder((short)port);

            // then translate each integral value into bytes in a byte array
            byte[] buf = new byte[SIZE];

            // copy in msg type
            buf[MSG_TYPE_INDEX] = (byte)msgType;

            // copy in the service name
            if (serviceName != null)
            {
                byte[] nameAsBytes = ASCIIEncoding.UTF8.GetBytes(serviceName);
                nameAsBytes.CopyTo(buf, SERVICE_NAME_INDEX);
            }

            // copy in the port
            BitConverter.GetBytes(shortPort).CopyTo(buf, PORT_INDEX);

            // copy in the status
            buf[STATUS_INDEX] = (byte)status;

            return buf;
        }
    }
}
