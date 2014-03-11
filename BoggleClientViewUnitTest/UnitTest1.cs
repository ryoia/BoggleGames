using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using BoggleServer;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using BoggleClient;

namespace BoggleClientViewUnitTest
{
   
        [TestClass]
        public class BoggleClientViewTest
        {
            BoggleClientModel model;
            BoggleClientModel model2;
            BoggleServer.BoggleServer server;

            [TestInitialize]
            public void Initialize()
            {
                model = new BoggleClientModel();
                model2 = new BoggleClientModel();
                server = new BoggleServer.BoggleServer(2000, 2, "..\\..\\..\\dictionary", null);
                model.Connect(2000, "localhost");
                model2.Connect(2000, "localhost");
                //Assert.IsTrue(model.IsConnected);
            }

            [TestCleanup]
            public void CleanUp()
            {
                model.Disconnect();
                model2.Disconnect();
                //Assert.IsFalse(model.IsConnected);
                
            }

            [TestMethod]
            public void PlayTest()
            {
                Assert.IsTrue(model.IsConnected);
                Assert.IsTrue(model2.IsConnected);
                bool modelReceiveEventA = false;
                bool model2ReceiveEventA = false;
                bool modelReceiveEventB = false;
                bool model2ReceiveEventB = false;
                model.IncomingLineEvent += s =>
                    {
                        if (!modelReceiveEventA) modelReceiveEventA = s.StartsWith("START ");
                    };
                model2.IncomingLineEvent += s =>
                    {
                        if (!model2ReceiveEventA) model2ReceiveEventA = s.StartsWith("START ");
                    };
                model.Play("Julia");
                model2.Play("Michael");

                model.SendMessage("sdf ");
                model2.SendMessage("sdf ");

                DateTime startTime = DateTime.UtcNow;
                while (!modelReceiveEventA || !model2ReceiveEventA)
                {
                    Assert.IsTrue((DateTime.UtcNow - startTime) < TimeSpan.FromSeconds(10));
                }
                model.IncomingLineEvent += s =>
                    {
                        if (!modelReceiveEventB) modelReceiveEventB = s.StartsWith("STOP ");
                    };
                model2.IncomingLineEvent += s =>
                    {
                        if (!model2ReceiveEventB) model2ReceiveEventB = s.StartsWith("STOP ");
                    };

                startTime = DateTime.UtcNow;
                while (!modelReceiveEventB || !model2ReceiveEventB)
                {
                    Assert.IsTrue((DateTime.UtcNow - startTime) < TimeSpan.FromSeconds(10));
                }
            }
        }
    
}
