
namespace appiocache
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class appiocache : IDisposable
    {
        public appiocache(byte[] vs)
        {
            dataStream = new MemoryStream(vs);
        }
        public appiocache(Uri uri, CancellationToken? cancelFetch=null)
        {
            var byteContent = (cancelFetch.HasValue) ? client.GetByteArrayAsync(uri, cancelFetch.Value).Result : client.GetByteArrayAsync(uri).Result;
            dataStream = new MemoryStream(byteContent);
        }
        public static async Task<appiocache> fromUri(Uri uri, CancellationToken? cancellationToken = null)
        {
            var byteContent = (cancellationToken.HasValue) ? await client.GetByteArrayAsync(uri, cancellationToken.Value) : await client.GetByteArrayAsync(uri);
            return new appiocache(byteContent);
        }

        private static readonly HttpClient client = new HttpClient(); // singleton to support recommended reuse
        public string Checksum { get {return this.findHash();} } // hash (sha256) helps uniquely & easily identify the data bytes
        private MemoryStream dataStream; // data is viewed as in-memory stream, feeded by network, persisted to disk, etc

        private string findHash()
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashbytes = sha256.ComputeHash(dataStream.ToArray());
                var sbuilder = new System.Text.StringBuilder();
                for (int i = 0; i < hashbytes.Length; i++)
                {
                    sbuilder.Append(hashbytes[i].ToString("x2"));
                }
                return sbuilder.ToString();
            }
        }

        public void persistToDisk(string filepath)
        {
            using (FileStream fs = File.Create(filepath))
            {
                dataStream.WriteTo(fs);
            }
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dataStream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }


        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~appiocache()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
