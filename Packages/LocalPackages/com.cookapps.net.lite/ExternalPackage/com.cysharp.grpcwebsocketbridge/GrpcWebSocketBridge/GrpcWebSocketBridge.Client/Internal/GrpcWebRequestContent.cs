using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GrpcWebSocketBridge.Client.Internal
{
    internal class GrpcWebRequestContent : HttpContent
    {
    	// 서버와 text/plain 방식으로 처리하기로 함
        public const string GrpcWebContentType = "text/plain";
        // public const string GrpcWebContentType = "application/grpc-web";

        private readonly HttpContent _inner;
        public GrpcWebRequestContent(HttpContent innerContent)
        {
            _inner = innerContent;

            foreach (var header in innerContent.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            Headers.ContentType = new MediaTypeHeaderValue(GrpcWebContentType);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await _inner.CopyToAsync(stream).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
