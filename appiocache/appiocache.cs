
namespace appiocache
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class appiocache : IDisposable
    {
        public appiocache(string id, byte[] vs, bool BypassCache=false)
        {
            if (string.IsNullOrEmpty(id) || vs == null) return;
            dataStream = new MemoryStream(vs);            
            LastUpdated = DateTime.UtcNow;
            if (!BypassCache) theCache[id] = this;
        }
        public static async Task<appiocache> fromUri(Uri uri, CancellationToken? cancellationToken = null, bool BypassCache = false)
        {
            if (!BypassCache && IsCacheAvailable(uri))
                return theCache[uri.ToString()];

            byte[] byteContent = null;
            try
            {
                //byteContent = (cancellationToken.HasValue) ? await client.GetByteArrayAsync(uri, cancellationToken.Value) : await client.GetByteArrayAsync(uri);
                var httpresponse = (cancellationToken.HasValue) ? await client.GetAsync(uri, cancellationToken.Value).ConfigureAwait(false) : await client.GetAsync(uri).ConfigureAwait(false);
                if (httpresponse.IsSuccessStatusCode)
                    byteContent = await httpresponse.Content.ReadAsByteArrayAsync();
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch(System.AggregateException aex)
            {
                //if (aex.InnerExceptions.Count > 1 && aex.InnerExceptions.Any(e => e.Message.Contains("Java.Unknown.HostExcepton")))
                    return null;
            }

            return new appiocache(uri.ToString(), byteContent, BypassCache);
        }

        private static Dictionary<string, appiocache> theCache = new Dictionary<string, appiocache>();
        public static void clearCache()
        {
            theCache.Clear();
        }
        public static bool IsCacheAvailable(Uri uri)
        {
            return theCache.ContainsKey(uri.ToString());
        }
        public static appiocache getfromCache(Uri staticUri)
        {
            if (theCache.ContainsKey(staticUri.ToString()))
                return theCache[staticUri.ToString()];
            return null;
        }

        public DateTime LastUpdated { get; private set; } //latest cache update time

        private static readonly HttpClient client = new HttpClient(); // singleton to support recommended reuse
        public string Checksum { get { return this.findHash(); } } // hash (sha256) helps uniquely & easily identify the data bytes
        public MemoryStream dataStream {get; private set;}  // data is viewed as in-memory stream of bytes, feeded by network, persisted to disk, etc
        public string cacheId { get; private set; } // id of the cached data 
        public Stream createStream() { return new MemoryStream(dataStream.ToArray()); }
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
