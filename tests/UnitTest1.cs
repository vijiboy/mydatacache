
namespace tests
{
    using NUnit.Framework;
    using appiocache;
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Threading;

    public class Tests
    {
        static readonly Uri staticUri = new System.Uri(@"https://raw.githubusercontent.com/vijiboy/declarative-camera/master/images/toolbutton.png");
        static readonly CancellationToken cancelledAlready = new CancellationToken(canceled: true);
        static readonly CancellationToken cancelNever = new CancellationToken(canceled: false);
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
            Assert.IsTrue(string.Equals(new appiocache(new byte[] {0xff,0xff}).Checksum, 
            "CA2FD00FA001190744C15C317643AB092E7048CE086A243E2BE9437C898DE1BB", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(new appiocache(System.Text.Encoding.UTF8.GetBytes("Hello World!")).Checksum, 
            "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069", System.StringComparison.OrdinalIgnoreCase));
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
            var fromStaticUri = appiocache.fromUri(staticUri).Result;
            var fromDownloadedBytes = new appiocache(tempclient.GetByteArrayAsync(staticUri).Result);
            Assert.AreEqual(fromDownloadedBytes.Checksum, fromStaticUri.Checksum);
        }

        [Test]
        public void dataCache_From_NetworkResource_Asynchronous()
        {
            var asyncDataRequest = appiocache.fromUri(staticUri);
            var syncData= appiocache.fromUri(staticUri).Result;
            asyncDataRequest.Wait();
            Assert.AreEqual(asyncDataRequest.Result.Checksum, syncData.Checksum);
        }

        [Test]
        public void dataCache_AsynchronousIO_Is_Faster()
        {
            List<Task<appiocache>> asyncTasks = new List<Task<appiocache>>();
            List<appiocache> syncResults = new List<appiocache>();
            const int TotalCalls = 100;
            var AsyncCallsTime = System.Diagnostics.Stopwatch.StartNew(); 
            for (int i = 0; i < TotalCalls; i++)
                asyncTasks.Add(appiocache.fromUri(staticUri,cancelNever,BypassCache:true));
            for (int i = 0; i < TotalCalls; i++)
                asyncTasks[i].Wait();
            AsyncCallsTime.Stop();

            var SyncCallsTime = System.Diagnostics.Stopwatch.StartNew(); 
            for (int i = 0; i < TotalCalls; i++)
                syncResults.Add(appiocache.fromUri(staticUri,cancelNever,BypassCache:true).Result);
            SyncCallsTime.Stop();

            for (int i = 0; i < TotalCalls; i++)
                Assert.AreEqual(asyncTasks[i].Result.Checksum, syncResults[i].Checksum);

            Console.WriteLine($"Total Calls {TotalCalls}, AsyncCalls Time: {AsyncCallsTime.Elapsed.TotalSeconds.ToString()}, SyncCalls Time: {SyncCallsTime.Elapsed.TotalSeconds.ToString()}");
            Assert.True(TimeSpan.Compare(AsyncCallsTime.Elapsed, SyncCallsTime.Elapsed) == -1 );
        }

        [Test]
        public void NetworkFetch_CanBeBlocked_UsingCancellationToken()
        {
            appiocache.clearCache();
            Assert.AreEqual(null, appiocache.fromUri(staticUri, cancelledAlready,BypassCache:true).Result);
        }

        [Test]
        public void Cache_NetworkUri_Delivered_From_Cache_IfAvailable()
        {
            appiocache.clearCache();
            Assert.IsNull(appiocache.fromUri(staticUri, new CancellationToken(canceled: true)).Result, "cancel failed: Network blocking must succeed for cache test");
            var NetworkResource = appiocache.fromUri(staticUri).Result;
            Assert.AreEqual(NetworkResource.Checksum, appiocache.getfromCache(staticUri).Checksum, "cache copy corrupt: mismatched with network copy");
            Assert.AreEqual(NetworkResource.Checksum, appiocache.fromUri(staticUri, new CancellationToken(canceled: true), BypassCache:false).Result.Checksum, "unexpected cache-miss"); 
        }


        [Test]
        public void Cache_ForceRefresh_Fetches_NetworkUri()
        {
            appiocache.clearCache();
            var NetworkResource = appiocache.fromUri(staticUri).Result;
            Assert.IsTrue(appiocache.IsCacheAvailable(staticUri));
            var NetworkResourceRepeat = appiocache.fromUri(staticUri, cancelNever, BypassCache:true).Result;
            Assert.True(NetworkResourceRepeat.LastUpdated > NetworkResource.LastUpdated, "expected Cache update to be recent(greater) than first update");
        }

        [Test]
        public void Cache_Is_Even_Faster_Than_AsynchronousIO()
        {
            List<Task<appiocache>> asyncTasks = new List<Task<appiocache>>();
            List<appiocache> syncResults = new List<appiocache>();
            const int TotalCalls = 100;
            var AsyncCallsTime = System.Diagnostics.Stopwatch.StartNew(); 
            for (int i = 0; i < TotalCalls; i++)
                asyncTasks.Add(appiocache.fromUri(staticUri,cancelNever,BypassCache:true));
            for (int i = 0; i < TotalCalls; i++)
                asyncTasks[i].Wait();
            AsyncCallsTime.Stop();

            var SyncCallsTime = System.Diagnostics.Stopwatch.StartNew(); 
            for (int i = 0; i < TotalCalls; i++)
                syncResults.Add(appiocache.fromUri(staticUri,cancelNever,BypassCache:false).Result);
            SyncCallsTime.Stop();

            for (int i = 0; i < TotalCalls; i++)
                Assert.AreEqual(asyncTasks[i].Result.Checksum, syncResults[i].Checksum);

            Console.WriteLine($"Total Calls {TotalCalls}, AsyncCalls Time: {AsyncCallsTime.Elapsed.TotalSeconds.ToString()}, SyncCallsWithCache Time: {SyncCallsTime.Elapsed.TotalSeconds.ToString()}");
            Assert.True(TimeSpan.Compare(AsyncCallsTime.Elapsed, SyncCallsTime.Elapsed) == 1 );
        }


    }

}