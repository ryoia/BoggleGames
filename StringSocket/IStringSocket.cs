using System;
namespace CustomNetworking
{
    public interface IStringSocket
    {
        void BeginReceive(StringSocket.ReceiveCallback callback, object payload);
        void BeginSend(string s, StringSocket.SendCallback callback, object payload);
    }
}
