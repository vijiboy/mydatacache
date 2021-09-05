
namespace tests
{
    using NUnit.Framework;
    using appiocache;
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class Tests
    {
        static HttpClient tempclient;
        [OneTimeSetUp]
        public static void Setup()
        {
            tempclient = new HttpClient();
        }

        [OneTimeTearDown]
        public static void TearDown()
        {
            tempclient.Dispose();
        }

        [Test]
        public void dataCache_FromBytes_ChecksumVerified()
        {
            Assert.IsTrue(string.Equals(new appiocache(new byte[] {0xff,0xff}).Checksum, "CA2FD00FA001190744C15C317643AB092E7048CE086A243E2BE9437C898DE1BB", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(new appiocache(System.Text.Encoding.UTF8.GetBytes("Hello World!")).Checksum, "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069", System.StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void dataCache_Can_PersistTo_Disk()
        {
            var a = new appiocache(System.Text.Encoding.UTF8.GetBytes("Hello World!"));
             a.persistToDisk(@"mytest.txt");
             Assert.AreEqual(File.ReadAllText(@"mytest.txt"), "Hello World!");
        }

        [Test]
        public void dataCache_From_NetworkResource_Synchronous()
        {
            var fromStaticUri = new appiocache(new System.Uri(@"https://raw.githubusercontent.com/vijiboy/declarative-camera/master/images/toolbutton.png"));
            var fromDownloadedBytes = new appiocache(tempclient.GetByteArrayAsync("https://raw.githubusercontent.com/vijiboy/declarative-camera/master/images/toolbutton.png").Result);
            Assert.AreEqual(fromDownloadedBytes.Checksum, fromStaticUri.Checksum);
        }

        [Test]
        public void dataCache_From_NetworkResource_Asynchronous()
        {
            var asyncDataRequest = appiocache.fromUri(new System.Uri(@"https://raw.githubusercontent.com/vijiboy/declarative-camera/master/images/toolbutton.png"));
            var syncData= new appiocache(new System.Uri(@"https://raw.githubusercontent.com/vijiboy/declarative-camera/master/images/toolbutton.png"));
            asyncDataRequest.Wait();
            Assert.AreEqual(asyncDataRequest.Result.Checksum, syncData.Checksum);
        }

        [Test]
        public void dataCache_AsynchronousIO_Is_Faster()
        {
            var sameUri = new System.Uri(@"https://raw.githubusercontent.com/vijiboy/declarative-camera/master/images/toolbutton.png");
            List<Task<appiocache>> asyncTasks = new List<Task<appiocache>>();
            List<appiocache> syncResults = new List<appiocache>();
            const int TotalCalls = 200;
            var AsyncCallsTime = System.Diagnostics.Stopwatch.StartNew(); 
            for (int i = 0; i < TotalCalls; i++)
                asyncTasks.Add(appiocache.fromUri(sameUri));
            for (int i = 0; i < TotalCalls; i++)
                asyncTasks[i].Wait();
            AsyncCallsTime.Stop();

            var SyncCallsTime = System.Diagnostics.Stopwatch.StartNew(); 
            for (int i = 0; i < TotalCalls; i++)
                syncResults.Add(new appiocache(sameUri));
            SyncCallsTime.Stop();

            for (int i = 0; i < TotalCalls; i++)
                Assert.AreEqual(asyncTasks[i].Result.Checksum, syncResults[i].Checksum);

            Console.WriteLine($"Total Calls {TotalCalls}, AsyncCalls Time: {AsyncCallsTime.Elapsed.TotalSeconds.ToString()}, SyncCalls Time: {SyncCallsTime.Elapsed.TotalSeconds.ToString()}");
            Assert.True(TimeSpan.Compare(AsyncCallsTime.Elapsed, SyncCallsTime.Elapsed) == -1 );
        }
    }
}