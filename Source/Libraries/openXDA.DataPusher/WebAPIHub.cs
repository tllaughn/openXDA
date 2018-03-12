﻿//******************************************************************************************************
//  WebAPIHub.cs - Gbtc
//
//  Copyright © 2017, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  07/05/2017 - Billy Ernest
//       Generated original version of source code.
//
//******************************************************************************************************

using GSF;
using GSF.Configuration;
using GSF.Net.Security;
using GSF.Security.Model;
using GSF.Web;
using GSF.Web.Model;
using Newtonsoft.Json.Linq;
using openXDA.Model;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace openXDA.DataPusher
{
    public class WebAPIHub
    {
        // Client-side script functionality
        #region [ Static ]

        public static event EventHandler<EventArgs<string>> LogStatusMessageEvent;

        private static void OnLogStatusMessage(string message)
        {
            LogStatusMessageEvent?.Invoke(new object(), new EventArgs<string>(message));
        }

        public static event EventHandler<EventArgs<string>> LogExceptionMessageEvent;

        private static void OnLogExceptionMessage(string message)
        {
            LogExceptionMessageEvent?.Invoke(new object(), new EventArgs<string>(message));
        }

        public static string CompanyName
        {
            get
            {
                using (DataContext dataContext = new DataContext("systemSettings"))
                {
                    return dataContext.Table<Setting>().QueryRecordWhere("Name = 'CompanyName'")?.Value ?? "Synced Remote Devices";
                }
            }
        }


        #endregion

        #region [ RESTful API Handlers ]

        private static string GenerateAntiForgeryToken(string instance, UserAccount userAccount)
        {
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;

                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.GetAsync("api/PQMark/GenerateRequestVerficationToken").Result;
                    if (response.IsSuccessStatusCode)
                        return response.Content.ReadAsStringAsync().Result;
                    else
                        return "";

                }
            }
        }

        public dynamic GetRecordHub(string instance, string tableName, int id, UserAccount userAccount)
        {
            return GetRecord(instance, tableName, id, userAccount);
        }

        public static dynamic GetRecord(string instance, string tableName, int id, UserAccount userAccount)
        {
            dynamic record = new object();
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;

                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.GetAsync("api/PQMark/GetRecord/" + tableName + "/" + id).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        record = response.Content.ReadAsAsync<dynamic>();
                    }
                    return record.Result.ToObject(typeof(openXDA.Model.Meter).Assembly.GetType("openXDA.Model." + tableName));

                }
            }
        }

        public dynamic GetRecordsHub(string instance, string tableName, string ids, UserAccount userAccount)
        {
            return GetRecords(instance, tableName, ids, userAccount);
        }

        public static IEnumerable<dynamic> GetRecords(string instance, string tableName, string ids, UserAccount userAccount)
        {
            dynamic record = new object();
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;               
                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.GetAsync("api/PQMark/GetRecords/" + tableName + "/" + ids).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        record = response.Content.ReadAsAsync<dynamic>();
                    }
                    Type type = typeof(openXDA.Model.Meter).Assembly.GetType("openXDA.Model." + tableName);
                    Type listType = typeof(IEnumerable<>).MakeGenericType(type);
                    return record.Result.ToObject(listType);

                }
            }

        }

        public dynamic GetRecordsWhereHub(string instance, string tableName, string ids, UserAccount userAccount)
        {
            return GetRecordsWhere(instance, tableName, ids, userAccount);
        }

        public static IEnumerable<dynamic> GetRecordsWhere(string instance, string tableName, string ids, UserAccount userAccount)
        {
            dynamic record = new object();
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.GetAsync("api/PQMark/GetRecordsWhere/" + tableName + "/" + ids).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        record = response.Content.ReadAsAsync<dynamic>();
                    }

                    Type type = typeof(openXDA.Model.Meter).Assembly.GetType("openXDA.Model." + tableName);
                    Type listType = typeof(IEnumerable<>).MakeGenericType(type);
                    return record.Result.ToObject(listType);

                }
            }

        }



        public dynamic GetChannelsHub(string instance, string ids, UserAccount userAccount)
        {
            return GetChannels(instance, ids, userAccount);
        }

        public static IEnumerable<ChannelDetail> GetChannels(string instance, string ids, UserAccount userAccount)
        {
            dynamic record = new object();
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;
                using (HttpClient client = new HttpClient(handler))
                {
                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.GetAsync("api/PQMark/GetChannels/channel/" + ids).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        record = response.Content.ReadAsAsync<dynamic>();
                    }

                    return record.Result.ToObject(typeof(IEnumerable<ChannelDetail>));

                }
            }

        }


        public HttpResponseMessage UpdateRecordHub(string instance, string tableName, JObject record, UserAccount userAccount)
        {
            return UpdateRecord(instance, tableName, record, userAccount);
        }

        public static HttpResponseMessage UpdateRecord(string instance, string tableName, JObject record, UserAccount userAccount)
        {
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;

                using (HttpClient client = new HttpClient(handler))
                {
                    string antiForgeryToken = GenerateAntiForgeryToken(instance, userAccount);

                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("X-GSF-Verify", antiForgeryToken);
                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.PutAsJsonAsync("api/PQMark/UpdateRecord/" + tableName, record).Result;
                    return response;
                }
            }

        }

        public int CreateRecordHub(string instance, string tableName, JObject record, UserAccount userAccount)
        {
            return CreateRecord(instance, tableName, record, userAccount);
        }

        public static int CreateRecord(string instance, string tableName, JObject record, UserAccount userAccount)
        {
            dynamic r = new object();
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;

                using (HttpClient client = new HttpClient(handler))
                {
                    string antiForgeryToken = GenerateAntiForgeryToken(instance, userAccount);

                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("X-GSF-Verify", antiForgeryToken);

                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.PostAsJsonAsync("api/PQMark/CreateRecord/" + tableName, record).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        r = response.Content.ReadAsAsync<dynamic>();
                    }

                    return (int)r.Result;
                }
            }
        }

        public int CreateChannelHub(string instance, JObject record, UserAccount userAccount)
        {
            return CreateChannel(instance, record, userAccount);
        }

        public static int CreateChannel(string instance, JObject record, UserAccount userAccount)
        {
            dynamic r = new object();
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;

                using (HttpClient client = new HttpClient(handler))
                {
                    string antiForgeryToken = GenerateAntiForgeryToken(instance, userAccount);

                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("X-GSF-Verify", antiForgeryToken);

                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.PostAsJsonAsync("api/PQMark/CreateChannel", record).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        r = response.Content.ReadAsAsync<dynamic>();
                    }

                    return (int)r.Result;
                }
            }
        }


        public HttpResponseMessage ProcessFileGroupHub(string instance, JObject record, UserAccount userAccount)
        {
            return ProcessFileGroup(instance, record, userAccount);
        }

        public static HttpResponseMessage ProcessFileGroup(string instance, JObject record, UserAccount userAccount)
        {
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;

                using (HttpClient client = new HttpClient(handler))
                {
                    string antiForgeryToken = GenerateAntiForgeryToken(instance, userAccount);


                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("X-GSF-Verify", antiForgeryToken);

                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.PostAsJsonAsync("api/PQMark/ProcessFileGroup", record).Result;
                    return response;
                }
            }
        }


        public HttpResponseMessage DeleteRecordHub(string instance, string tableName, int id, UserAccount userAccount)
        {
            return DeleteRecord(instance, tableName, id, userAccount);
        }

        public static HttpResponseMessage DeleteRecord(string instance, string tableName, int id, UserAccount userAccount)
        {
            using (WebRequestHandler handler = new WebRequestHandler())
            {
                handler.ServerCertificateValidationCallback += HandleCertificateValidation;

                using (HttpClient client = new HttpClient(handler))
                {
                    string antiForgeryToken = GenerateAntiForgeryToken(instance, userAccount);

                    client.BaseAddress = new Uri(instance);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("X-GSF-Verify", antiForgeryToken);
                    client.AddBasicAuthenticationHeader(userAccount.AccountName, userAccount.Password);
                    HttpResponseMessage response = client.DeleteAsync("api/PQMark/DeleteRecord/" + tableName + "/" + id).Result;
                    return response;
                }
            }
        }

        private static bool HandleCertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {           
            SimpleCertificateChecker simpleCertificateChecker = new SimpleCertificateChecker();

            CategorizedSettingsElementCollection systemSettings = ConfigurationFile.Current.Settings["systemSettings"];

            systemSettings.Add("CertFile", "", "This is a certfile.");
            systemSettings.Add("ValidPolicyErrors", "None", "Password for PQMarkWeb API access.", true);
            systemSettings.Add("ValidChainFlags", "NoError", "Password for PQMarkWeb API access.", true);


            try
            {
                simpleCertificateChecker.ValidPolicyErrors = (SslPolicyErrors)Enum.Parse(typeof(SslPolicyErrors), (systemSettings["ValidPolicyErrors"].Value != "All" ? systemSettings["ValidPolicyErrors"].Value : "7"));
                simpleCertificateChecker.ValidChainFlags = (X509ChainStatusFlags)Enum.Parse(typeof(X509ChainStatusFlags), (systemSettings["ValidChainFlags"].Value != "All"? systemSettings["ValidChainFlags"].Value : (~0).ToString()));
                simpleCertificateChecker.TrustedCertificates.Add((!string.IsNullOrEmpty(systemSettings["CertFile"].Value) ? new X509Certificate2(systemSettings["CertFile"].Value) : certificate));
            }
            catch (Exception ex)
            {
                OnLogExceptionMessage(ex.ToString());
            }

            return simpleCertificateChecker.ValidateRemoteCertificate(sender, certificate, chain, sslPolicyErrors);            
        }
        #endregion

    }
}
