using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using BoggleServer;
using BB;
using System.Text;
using System.Collections.Generic;
using CustomNetworking;
using System.Net;
using System.Threading;

namespace BoggleServerTest
{
    [TestClass]
    public class UnitTest1
    {
        BoggleServer.BoggleServer server;
        TcpClient player1;
        TcpClient player2;
        StringSocket p1;
        StringSocket p2;
        ManualResetEvent mre;
        List<string> receivedWords;

        public void runGame()
        {
            server = new BoggleServer.BoggleServer(2000, 10, "..\\..\\..\\dictionary", "AAAABBBBCCCCDDDD");
            player1 = new TcpClient("localhost", 2000);
            player2 = new TcpClient("localhost", 2000);
            Socket p1socket = player1.Client;
            Socket p2socket = player2.Client;
            p1 = new StringSocket(p1socket, new UTF8Encoding());
            p2 = new StringSocket(p2socket, new UTF8Encoding());
            p1.BeginSend("PLAY player1\n", (e, o) => { }, null);
            p2.BeginSend("PLAY player2\n", (e, o) => { }, null);
            mre = new ManualResetEvent(false);
            receivedWords = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                p1.BeginReceive(callback, p1);
            }
            mre.WaitOne(1000);

            Assert.IsTrue(receivedWords.Contains("START AAAABBBBCCCCDDDD 10 PLAYER2"));
        }

        private void callback(string s, Exception e, object payload)
        {
            if (s == null)
                mre.Set();
            else
            {
                receivedWords.Add(s);
            }
        }


        [TestMethod]
        public void TestMethod1()
        {
            runGame();
        }

    }

    [TestClass]
    public class UnitTest2
    {
        BoggleServer.BoggleServer server;
        TcpClient player1;
        TcpClient player2;
        StringSocket p1;
        StringSocket p2;
        ManualResetEvent mre;
        List<string> receivedWords;
        public void runGame()
        {
            server = new BoggleServer.BoggleServer(2001, 200, "..\\..\\..\\dictionary", null);
            player1 = new TcpClient("localhost", 2001);
            player2 = new TcpClient("localhost", 2001);
            Socket p1socket = player1.Client;
            Socket p2socket = player2.Client;
            p1 = new StringSocket(p1socket, new UTF8Encoding());
            p2 = new StringSocket(p2socket, new UTF8Encoding());
            p1.BeginSend("PLAY player1\n", (e, o) => { }, null);
            p2.BeginSend("PLAY player2\n", (e, o) => { }, null);
            p1.BeginReceive(callback, null);
            p2.BeginReceive(callback, null);
            mre = new ManualResetEvent(false);
            receivedWords = new List<string>();
            for (int i = 0; i < 30; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }
            mre.WaitOne(1000);

            Assert.IsTrue(receivedWords.Contains("TIME 200"));

        }

        private void callback(string s, Exception e, object payload)
        {
            if (s == null)
                mre.Set();
            else
            {
                receivedWords.Add(s);
            }
        }


        [TestMethod]
        public void TestMethod2()
        {
            runGame();
        }

    }

    [TestClass]
    public class UnitTest3
    {
        BoggleServer.BoggleServer server;
        TcpClient player1;
        TcpClient player2;
        StringSocket p1;
        StringSocket p2;
        ManualResetEvent mre;
        List<string> receivedWords;
        public void runGame()
        {
            receivedWords = new List<string>();
            mre = new ManualResetEvent(false);

            server = new BoggleServer.BoggleServer(2002, 200, "..\\..\\..\\dictionary", "AAAABBBBCCCCDDDD");
            player1 = new TcpClient("localhost", 2002);
            player2 = new TcpClient("localhost", 2002);
            Socket p1socket = player1.Client;
            Socket p2socket = player2.Client;
            p1 = new StringSocket(p1socket, new UTF8Encoding());
            p2 = new StringSocket(p2socket, new UTF8Encoding());
            p1.BeginSend("PLAY player1\n", (e, o) => { }, null);
            p1.BeginSend("Please Ignore Me\n", (e, o) => { }, null);

            for (int i = 0; i < 40; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }
            mre.WaitOne(1000);

            p2.BeginSend("PLAY player2\n", (e, o) => { }, null);
            p1.BeginReceive(callback, null);
            p2.BeginReceive(callback, null);
            
            for (int i = 0; i < 40; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }
            mre.WaitOne(1000);

            Assert.IsTrue(receivedWords.Contains("START AAAABBBBCCCCDDDD 200 PLAYER2"));
            Assert.IsTrue(receivedWords.Contains("START AAAABBBBCCCCDDDD 200 PLAYER1"));
            Assert.IsTrue(receivedWords.Contains("IGNORING Please Ignore Me"));

            p1.BeginSend("WORD PleaseDontIgnoreMe\n", (e, o) => { }, null);

            for (int i = 0; i < 40; i++)
            {
                p1.BeginReceive(callback, p1);

            }
            mre.WaitOne(1000);

            Assert.IsTrue(receivedWords.Contains("SCORE -1 0"));
        }

        private void callback(string s, Exception e, object payload)
        {
            if (s == null)
                mre.Set();
            else
            {
                receivedWords.Add(s);
            }
        }


        [TestMethod]
        public void TestMethod3()
        {
            runGame();
        }

    }

    [TestClass]
    public class UnitTest4
    {
        BoggleServer.BoggleServer server;
        TcpClient player1;
        TcpClient player2;
        StringSocket p1;
        StringSocket p2;
        ManualResetEvent mre;
        List<string> receivedWords;
        public void runGame()
        {
            mre = new ManualResetEvent(false);
            receivedWords = new List<string>();

            server = new BoggleServer.BoggleServer(2003, 200, "..\\..\\..\\dictionary", "DEHNKTMBCRENFADT");
            player1 = new TcpClient("localhost", 2003);
            player2 = new TcpClient("localhost", 2003);
            Socket p1socket = player1.Client;
            Socket p2socket = player2.Client;
            p1 = new StringSocket(p1socket, new UTF8Encoding());
            p2 = new StringSocket(p2socket, new UTF8Encoding());
            p1.BeginSend("PLAY player1\n", (e, o) => { }, null);
            p1.BeginSend("Please Ignore Me\n", (e, o) => { }, null);

            for (int i = 0; i < 40; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }
            mre.WaitOne(1000);

            p2.BeginSend("PLAY player2\n", (e, o) => { }, null);
            p1.BeginReceive(callback, null);
            p2.BeginReceive(callback, null);

            for (int i = 0; i < 40; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }
            mre.WaitOne(1000);

            Assert.IsTrue(receivedWords.Contains("START DEHNKTMBCRENFADT 200 PLAYER2"));
            Assert.IsTrue(receivedWords.Contains("START DEHNKTMBCRENFADT 200 PLAYER1"));
            Assert.IsTrue(receivedWords.Contains("IGNORING Please Ignore Me"));

            p2.BeginSend("WORD DunIgnr\n", (e, o) => { }, null); //X -1

            for (int i = 0; i < 60; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }
            mre.WaitOne(1000);

            p1.BeginSend("WORD PleaseDontIgnoreMe\n", (e, o) => { }, null); //-1 X
            p1.BeginSend("WORD racketed\n", (e, o) => { }, null);           //+11 X = 10
            p1.BeginSend("WORD tracked\n", (e, o) => { }, null);            //+5 X = 15
            p1.BeginSend("WORD racket\n", (e, o) => { }, null);             //+3 X = 18
            p1.BeginSend("WORD trend\n", (e, o) => { }, null);              //+2 X = 20
            p1.BeginSend("WORD read\n", (e, o) => { }, null);               //+1 X = 21
            p1.BeginSend("WORD the\n", (e, o) => { }, null);                //+1 X = 22


            for (int i = 0; i < 60; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }
            mre.WaitOne(2000);

            p2.BeginSend("WORD the\n", (e, o) => { }, null);            //-1 -1 = 21

            for (int i = 0; i < 60; i++)
            {
                p1.BeginReceive(callback, p1);
                p2.BeginReceive(callback, p2);
            }

            mre.WaitOne(1000);

            Assert.IsTrue(receivedWords.Contains("SCORE -1 21"));
        }

        private void callback(string s, Exception e, object payload)
        {
            if (s == null)
                mre.Set();
            else
            {
                receivedWords.Add(s);
            }
        }


        [TestMethod]
        public void TestMethod4()
        {
            runGame();
        }

    }
}
