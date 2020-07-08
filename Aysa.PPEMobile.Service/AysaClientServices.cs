using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aysa.PPEMobile.Service.Helper;
using Aysa.PPEMobile.Service.HttpExceptions;
using Newtonsoft.Json;
using System.IO;

using Aysa.PPEMobile.Model;

namespace Aysa.PPEMobile.Service
{
    public class AysaClientServices
    {
        public delegate string GetTokenDelegate();

        public GetTokenDelegate GetToken;

        public static readonly string AUTHORIZATION_HEADER = "Authorization";
        public static readonly string AUTHORIZATION_TYPE = "Bearer";
        public static readonly int TIMEOUT = 360;

        private static readonly object SyncLock = new object();
        private static AysaClientServices instance;

        private AysaClientServices()
        {
        }

        public static AysaClientServices Instance
        {
            get
            {
                lock (SyncLock)
                {
                    if (instance == null)
                    {
                        instance = new AysaClientServices();
                    }

                    return instance;
                }
            }
        }

        public async Task<T> Get<T>(string url)
        {
            return await this.Get<T>(url, null);
        }

        public async Task<T> Get<T>(string url, Dictionary<string, string> parameters)
        {
            if (!this.CheckNetworkConnection())
            {
                throw new NetworkConnectionException();
            }

            using (var client = this.CreateHttpClient(AysaClient.URL))
            {
                var jsonResponse = string.Empty;
                HttpResponseMessage response = null;


                if (parameters != null)
                {
                    var queryString = new QueryHelper(parameters);
                    response = client.GetAsync(url + "?" + queryString).Result;
                }
                else
                {
                    response = client.GetAsync(url).Result;
                }

                this.ValidateResponse(response);

                jsonResponse = await response.Content.ReadAsStringAsync();


                return JsonConvert.DeserializeObject<T>(jsonResponse,
																 new JsonSerializerSettings
																 {
																	 NullValueHandling = NullValueHandling.Ignore,
																	 MissingMemberHandling = MissingMemberHandling.Ignore
																 });
            }
        }

        public async Task<T> Post<T>(string url)
        {
            return await this.Post<T>(url, null);
        }

        public async Task<T> Post<T>(string url, object data)
        {
            if (!this.CheckNetworkConnection())
            {
                throw new NetworkConnectionException();
            }

            using (var client = this.CreateHttpClient(AysaClient.URL))
            {

				var jsonData = JsonConvert.SerializeObject(data,
							Formatting.None,
							new JsonSerializerSettings
							{
								NullValueHandling = NullValueHandling.Ignore
							});

                HttpResponseMessage response = null;

                response = await client.PostAsync(url, new StringContent(jsonData, Encoding.UTF8, "application/json"));

                this.ValidateResponse(response);

                var jsonResponse = await response.Content.ReadAsStringAsync();

				return JsonConvert.DeserializeObject<T>(jsonResponse,
																 new JsonSerializerSettings
																 {
																	 NullValueHandling = NullValueHandling.Ignore,
																	 MissingMemberHandling = MissingMemberHandling.Ignore
																 });
            }
        }

        public async Task<T> Put<T>(string url)
        {
            return await this.Put<T>(url, null);
        }

        public async Task<T> Put<T>(string url, object data)
        {
            if (!this.CheckNetworkConnection())
            {
                throw new NetworkConnectionException();
            }

            using (var client = this.CreateHttpClient(AysaClient.URL))
            {
				var jsonData = JsonConvert.SerializeObject(data,
							Formatting.None,
							new JsonSerializerSettings
							{
								NullValueHandling = NullValueHandling.Ignore
							});

                HttpResponseMessage response = null;

                response = await client.PutAsync(url, new StringContent(jsonData, Encoding.UTF8, "application/json"));

                this.ValidateResponse(response);

                var jsonResponse = await response.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<T>(jsonResponse,
																 new JsonSerializerSettings
																 {
																	 NullValueHandling = NullValueHandling.Ignore,
																	 MissingMemberHandling = MissingMemberHandling.Ignore
																 });
            }
        }

		public async Task<T>Delete<T>(string url)
		{
			if (!this.CheckNetworkConnection())
			{
				throw new NetworkConnectionException();
			}

			using (var client = this.CreateHttpClient(AysaClient.URL))
			{

				HttpResponseMessage response = null;

                response = await client.DeleteAsync(url);


				this.ValidateResponse(response);

				var jsonResponse = await response.Content.ReadAsStringAsync();
				return JsonConvert.DeserializeObject<T>(jsonResponse,
																 new JsonSerializerSettings
																 {
																	 NullValueHandling = NullValueHandling.Ignore,
																	 MissingMemberHandling = MissingMemberHandling.Ignore
																 });
			}
		}

		public async Task<byte[]>GetFile(string url)
		{
			if (!this.CheckNetworkConnection())
			{
				throw new NetworkConnectionException();
			}

			using (var client = this.CreateHttpClient(AysaClient.URL))
			{
				byte[] byteArray = null;

                byteArray = await client.GetByteArrayAsync(url); 

                return byteArray;
			}
		}

		public async Task<T>UploadFile<T>(string url, byte[] file, string filename)
		{

			if (!this.CheckNetworkConnection())
			{
				throw new NetworkConnectionException();
			}

			using (var client = this.CreateHttpClient(AysaClient.URL))
			{

				using (var content = new MultipartFormDataContent())
				{
					content.Add(new StreamContent(new MemoryStream(file)), "UploadFile", filename);

					using (HttpResponseMessage response = await client.PostAsync(url, content))
					{
                        System.Diagnostics.Debug.WriteLine(response);

                        this.ValidateResponse(response);

						var jsonResponse = await response.Content.ReadAsStringAsync();

						return JsonConvert.DeserializeObject<T>(jsonResponse,
																		 new JsonSerializerSettings
																		 {
																			 NullValueHandling = NullValueHandling.Ignore,
																			 MissingMemberHandling = MissingMemberHandling.Ignore
																		 });
					}
				}
			}
		}



        public bool CheckNetworkConnection()
        {
            // TODO: Implement
            return true;
        }

        private HttpClient CreateHttpClient(string baseUrl)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            };

            client.Timeout = TimeSpan.FromSeconds(TIMEOUT);

            client.DefaultRequestHeaders.Accept.Clear();

            //client.MaxResponseContentBufferSize = 10240000;



			//if (GetToken != null)
			//{
			//    var token = GetToken();

			//    client.DefaultRequestHeaders.Add(AUTHORIZATION_HEADER, string.Format("{0} {1}", AUTHORIZATION_TYPE, token));
			//}

			if (UserSession.Instance.Access_token != null)
			{
                var token = UserSession.Instance.Access_token;

				client.DefaultRequestHeaders.Add(AUTHORIZATION_HEADER, string.Format("{0} {1}", AUTHORIZATION_TYPE, token));
            }

            return client;
        }

        private void ValidateResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        throw new HttpConflict();
                    case HttpStatusCode.Unauthorized:
                        throw new HttpUnauthorized();
                    case HttpStatusCode.InternalServerError:
                        throw new HttpInternalServerError();
                    case HttpStatusCode.BadRequest:
                        throw new HttpBadRequest();
                    case HttpStatusCode.NotFound:
                        throw new HttpNotFound();
                    case HttpStatusCode.Forbidden:
                        throw new HttpForbidden();
                    default:
                        throw new Exception(response.ReasonPhrase);
                }
            }
        }
    }
}