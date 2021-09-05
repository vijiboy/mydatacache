
namespace appiocache
{
    using System;
    using System.Collections.Generic;
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
            LastUpdated = DateTime.UtcNow;
        }
        public static async Task<appiocache> fromUri(Uri uri, CancellationToken? cancellationToken = null, bool BypassCache = false)
        {
            if (!BypassCache && IsCacheAvailable(uri))
                return theCache[uri];

            byte[] byteContent;
            try
            {
                byteContent = (cancellationToken.HasValue) ? await client.GetByteArrayAsync(uri, cancellationToken.Value) : await client.GetByteArrayAsync(uri);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            if (!BypassCache)
                updateCache(uri, ref byteContent);

            return new appiocache(byteContent);
        }

        private static Dictionary<Uri, appiocache> theCache = new Dictionary<Uri, appiocache>();
        public static void clearCache()
        {
            theCache.Clear();
        }
        public static bool IsCacheAvailable(Uri uri)
        {
            return theCache.ContainsKey(uri);
        }
        public static appiocache getfromCache(Uri staticUri)
        {
            if (theCache.ContainsKey(staticUri))
                return theCache[staticUri];
            return null;
        }
        private static void updateCache(Uri uri, ref byte[] byteContent)
        {
            theCache[uri] = new appiocache(byteContent);
        }
        public DateTime LastUpdated { get; private set; } //latest cache update time

        private static readonly HttpClient client = new HttpClient(); // singleton to support recommended reuse
        public string Checksum { get { return this.findHash(); } } // hash (sha256) helps uniquely & easily identify the data bytes
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
