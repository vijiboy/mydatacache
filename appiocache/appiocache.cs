
namespace appiocache
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    public class appiocache: IDisposable
    {
        public appiocache(byte[] vs)
        {
            dataStream = new MemoryStream(vs);
            Checksum = this.findHash();
        }

        public appiocache(Uri uri, CancellationToken cancelFetch)
        {
            var stringContent = client.GetStringAsync(uri, cancelFetch).Result;
            dataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(stringContent));
            Checksum = this.findHash();
        }
        static readonly HttpClient client = new HttpClient();

        public string Checksum { get;  private set;}
        private MemoryStream dataStream;
        private bool disposedValue;

        private string findHash(){
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
