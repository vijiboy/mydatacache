
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
    using System.Linq;

    public class Tests
    {
        static readonly Uri staticUri = new Uri(@"https://raw.githubusercontent.com/vijiboy/declarative-camera/master/images/toolbutton.png");
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
            Assert.IsTrue(string.Equals(new appiocache("sample1", new byte[] {0xff,0xff}).Checksum, 
            "CA2FD00FA001190744C15C317643AB092E7048CE086A243E2BE9437C898DE1BB", System.StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(new appiocache("sample2", System.Text.Encoding.UTF8.GetBytes("Hello World!")).Checksum, 
            "7F83B1657FF1FC53B92DC18148A1D65DFC2D4B1FA3D677284ADDD200126D9069", System.StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void dataCache_Can_PersistTo_Disk()
        {
            var a = new appiocache("HelloWorld", System.Text.Encoding.UTF8.GetBytes("Hello World!"));
             a.persistToDisk(@"mytest.txt");
             Assert.AreEqual(File.ReadAllText(@"mytest.txt"), "Hello World!");
        }

        [Test]
        public void dataCache_From_NetworkResource_Synchronous()
        {
            var newUri = new System.Uri(@"https://upload.wikimedia.org/wikipedia/commons/thumb/f/fc/Papio_anubis_%28Serengeti%2C_2009%29.jpg/200px-Papio_anubis_%28Serengeti%2C_2009%29.jpg");
            //var fromStaticUri = appiocache.fromUri(staticUri).Result;
            var fromStaticUri = appiocache.fromUri(newUri).Result;
            var fromDownloadedBytes = new appiocache(newUri.ToString(), tempclient.GetByteArrayAsync(newUri).Result);
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

        [Test]
        public void Simulate_EndUserComputeActivity_WithAsynchronousNetwork_Fetch()
        {
            Task<int> ComputeIntensiveTask_findprimeNumbers = Task.Run(() => Enumerable.Range(2, 3000000).Count(n => Enumerable.Range(2, (int)Math.Sqrt(n) - 1).All(i => n % i > 0)));
            string[] wikipediaUris = new string[] {"https://en.wikipedia.org/wiki/Timeline_of_historic_inventions", "https://en.wikipedia.org/wiki/History_of_aerospace", "https://en.wikipedia.org/wiki/History_of_artificial_intelligence",
            "https://en.wikipedia.org/wiki/History_of_agriculture", "https://en.wikipedia.org/wiki/History_of_agricultural_science", "https://en.wikipedia.org/wiki/History_of_Biotechnology", "https://en.wikipedia.org/wiki/History_of_cartography", 
            "https://en.wikipedia.org/wiki/History_of_chemical_engineering", "https://en.wikipedia.org/wiki/History_of_computing", "https://en.wikipedia.org/wiki/History_of_computing_hardware", "https://en.wikipedia.org/wiki/History_of_the_graphical_user_interface",
            "https://en.wikipedia.org/wiki/Hypertext#History", "https://en.wikipedia.org/wiki/History_of_the_Internet", "https://en.wikipedia.org/wiki/History_of_the_World_Wide_Web", "https://en.wikipedia.org/wiki/History_of_operating_systems", "https://en.wikipedia.org/wiki/History_of_programming_languages",
            "https://en.wikipedia.org/wiki/History_of_software_engineering", "https://en.wikipedia.org/wiki/History_of_electrical_engineering", "https://en.wikipedia.org/wiki/History_of_energy_development", "https://en.wikipedia.org/wiki/Engineering#History",
            "https://en.wikipedia.org/wiki/History_of_industry", "https://en.wikipedia.org/wiki/History_of_library_and_information_science", "https://en.wikipedia.org/wiki/Timeline_of_microscope_technology", "https://en.wikipedia.org/wiki/History_of_manufacturing",
            "https://en.wikipedia.org/wiki/History_of_materials_science"};
            List<Uri> uriRequests = new List<Uri>(100);
            Random next = new Random();
            for (int i = 0; uriRequests.Count < uriRequests.Capacity; i++)
            {
                uriRequests.Add(new Uri(wikipediaUris[next.Next(0,wikipediaUris.Length)]));
            }
            List<Task<appiocache>> nwfetch = new List<Task<appiocache>>();
            foreach (var uri in uriRequests)
            {
                nwfetch.Add(appiocache.fromUri(uri, cancelNever, BypassCache:true));
            }

            for (int i = 0; i < 500; i++)
            {
                Assert.AreEqual(216816, ComputeIntensiveTask_findprimeNumbers.Result);
            }

            foreach (var task in nwfetch)
            {
                task.Wait();
            }
            Assert.IsTrue(nwfetch.All(t => t.IsCompleted == true));
        }
    }
}