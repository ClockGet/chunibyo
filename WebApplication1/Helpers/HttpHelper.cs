using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Replay;

namespace WebApplication1.Helpers
{
    public enum HttpStatus
    {
        Success,
        Timeout,
        Fault,
        Other
    }
    public class ResponseMessage
    {
        public string Content { get; internal set; }
        public HttpStatus Status
        {
            get;
            internal set;
        }
    }
    public class HttpHelper<T> where T : IMusicProvider
    {
        private static HttpClient httpClient;
        private static string s_contentType;
        private static Encoding s_encoding;
        public static void HttpRegister(string baseAddress, TimeSpan timeout, string contentType = null, bool keepAlive = false, Encoding enc = null, Dictionary<string, string> headers = null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseCookies = false;
            handler.UseProxy = false;
#if DEBUG
            handler.UseProxy = true;
#endif
            httpClient = new HttpClient(handler) { Timeout = timeout };
            if (!string.IsNullOrEmpty(baseAddress))
                httpClient.BaseAddress = new Uri(baseAddress);
            s_contentType = contentType;
            s_encoding = enc;
            if (keepAlive)
            {
                httpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            }
            else
            {
                httpClient.DefaultRequestHeaders.Connection.Add("close");
            }
            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    httpClient.DefaultRequestHeaders.Add(kv.Key, kv.Value);
                }
            }
        }
        public static bool HasCharSet
        {
            private get;
            set;
        }
        public static Task<ResponseMessage> PostAsync(string requestUri, string requestContent)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri);
            httpRequestMessage.Content = new StringContent(requestContent, s_encoding, s_contentType);
            if (!HasCharSet)
                httpRequestMessage.Content.Headers.ContentType.CharSet = "";
            return SendAsync(httpRequestMessage);
        }
        public static Task<ResponseMessage> SendAsync(string requestUri)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return SendAsync(httpRequestMessage);
        }
        internal static Task<ResponseMessage> SendAsync(HttpRequestMessage request)
        {
            TaskCompletionSource<ResponseMessage> taskCompletionSource = new TaskCompletionSource<ResponseMessage>();
            ResponseMessage rm = new ResponseMessage();
            httpClient.SendAsync(request).ContinueWithStandard(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion && t.Result.StatusCode == HttpStatusCode.OK)
                {
                    t.Result.Content.ReadAsStreamAsync().ContinueWithStandard(async _t =>
                    {
                        if (_t.Status == TaskStatus.RanToCompletion)
                        {
                            string content = null;
                            if(t.Result.Content.Headers.ContentEncoding.Contains("gzip"))
                            {
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    GZipStream gzip = new GZipStream(_t.Result, CompressionMode.Decompress, true);
                                    int count = 0;
                                    byte[] buffer = new byte[4096];
                                    while ((count = await gzip.ReadAsync(buffer, 0, 4096)) > 0)
                                    {
                                        await stream.WriteAsync(buffer, 0, count);
                                    }
                                    stream.Position = 0;
                                    using (var sr = new StreamReader(stream))
                                    {
                                        content= await sr.ReadToEndAsync();
                                    }
                                }
                            }
                            else
                            {
                                using (StreamReader sr = new StreamReader(_t.Result))
                                {
                                    content = await sr.ReadToEndAsync();
                                }
                            }
                            rm.Content = content;
                            rm.Status = HttpStatus.Success;
                            taskCompletionSource.SetResult(rm);
                        }
                        else if (_t.Status == TaskStatus.Canceled)   //超时被取消
                        {
                            rm.Status = HttpStatus.Timeout;
                            taskCompletionSource.SetResult(rm);
                        }
                        else if (_t.Status == TaskStatus.Faulted)    //未捕获的异常
                        {
                            rm.Status = HttpStatus.Fault;
                            taskCompletionSource.SetResult(rm);
                        }
                        else
                        {
                            rm.Status = HttpStatus.Other;
                            taskCompletionSource.SetResult(rm);
                        }
                    });
                }
                else if (t.Status == TaskStatus.Canceled)   //超时被取消
                {
                    rm.Status = HttpStatus.Timeout;
                    taskCompletionSource.SetResult(rm);
                }
                else if (t.Status == TaskStatus.Faulted)    //未捕获的异常
                {
                    rm.Status = HttpStatus.Fault;
                    taskCompletionSource.SetResult(rm);
                }
                else
                {
                    rm.Status = HttpStatus.Other;
                    taskCompletionSource.SetResult(rm);
                }
            }
            );
            return taskCompletionSource.Task;
        }
    }
    internal static class HttpUtilities
    {
        internal static readonly byte[] EmptyByteArray = new byte[0];

        internal static bool IsHttpUri(Uri uri)
        {
            string scheme = uri.Scheme;
            return string.Compare("http", scheme, StringComparison.OrdinalIgnoreCase) == 0 || string.Compare("https", scheme, StringComparison.OrdinalIgnoreCase) == 0;
        }

        internal static bool HandleFaultsAndCancelation<T>(Task task, TaskCompletionSource<T> tcs)
        {
            if (task.IsFaulted)
            {
                tcs.TrySetException(task.Exception.GetBaseException());
                return true;
            }
            if (task.IsCanceled)
            {
                tcs.TrySetCanceled();
                return true;
            }
            return false;
        }

        internal static Task ContinueWithStandard(this Task task, Action<Task> continuation)
        {
            return task.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        internal static Task ContinueWithStandard<T>(this Task<T> task, Action<Task<T>> continuation)
        {
            return task.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
