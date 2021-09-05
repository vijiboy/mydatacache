
namespace tests
{
    using NUnit.Framework;
    using appiocache;
    using System.IO;
    using System;
    using System.Net.Http;

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
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
        [Ignore("TODO checksum does not match exactly as last few bytes are updated by anti-virus program")]
        public void dataCache_From_NetworkResource()
        {
            var fromUri = new appiocache(new System.Uri(@"https://www.ietf.org/about/who/"), new System.Threading.CancellationToken());
            fromUri.persistToDisk("fromUri.html");
        }


        [Test]
        [Ignore("TODO")]
        public void uri_synchronous_api()
        {
            //var a = new appiocache();
            //file myfile = fetchUrl("http://myserver.com/file1.img");
            //Assert.AreEqual(myfile.checksum, a.geturi("http://myserver.com/file1.img").checksum);
        }
    }
}