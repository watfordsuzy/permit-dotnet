﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using PermitDotnet.Models;
using System.Net.Http.Headers;
using System.Text;

public class PermissionCheckException : Exception
{
    public PermissionCheckException() { }

    public PermissionCheckException(string message) : base(message) { }
}

public class CreateResourceException : Exception
{
    public CreateResourceException() { }

    public CreateResourceException(string message) : base(message) { }
}

namespace PermitDotnet
{
    public class PermitCheckData
    {
        public bool allow { get; }

        private PermitCheckData() { }
    }

    public class PermitCheck
    {
        public bool allow { get; }

        private PermitCheck() { }
    }

    public interface IResource
    {
        public string type { get; }
        public string id { get; }
        public string tenant { get; }
        public IDictionary<string, string> attributes { get; }
        public IDictionary<string, string> context { get; }
    }

    /// <summary>
    /// Implements Permit Enforcer using API checks against PDP sidecar.
    /// </summary>
    public class Enforcer
    {
        string Url;
        string CheckURI = "/allowed";
        Config Config;
        HttpClient Client = new HttpClient();
        public JsonSerializerOptions options { get; private set; }

        public Enforcer(Config config, string url = Permit.DEFAULT_PDP_URL)
        {
            this.Url = url;
            this.Config = config;
            Client.BaseAddress = new Uri(url);
            Client.DefaultRequestHeaders.Add(
                "Authorization",
                string.Format("Bearer {0}", config.Token)
            );
            Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
            this.options = new JsonSerializerOptions();
            this.options.IgnoreNullValues = true;
        }

        /// <inheritdoc />
        public async Task<bool> Check(
            IUserKey user,
            string action,
            ResourceInput resource,
            Dictionary<string, string> context = null
        )
        {
            var normalizedResource = ResourceInput.Normalize(resource, Config);
            var parameters = new Dictionary<string, object>
            {
                { "user", user.key },
                { "action", action },
                { "resource", normalizedResource },
                { "context", context }
            };
            var serializedResources = JsonSerializer.Serialize(parameters, options);
            var httpContent = new StringContent(
                serializedResources,
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await Client
                    .PostAsync(Url + CheckURI, httpContent)
                    .ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseContent = await response.Content
                        .ReadAsStringAsync()
                        .ConfigureAwait(false);
                    if (Config.DebugMode)
                    {
                        Console.Write(
                            string.Format(
                                "Checking permission for {0} to perform {1} on {2}",
                                user,
                                action,
                                resource.type
                            )
                        );
                    }
                    bool decision =
                        JsonSerializer.Deserialize<PermitCheck>(responseContent).allow || false;
                    return decision;
                }
                else
                {
                    //throw new PermissionCheckException("Permission check failed");
                    Console.Write(
                        string.Format(
                            "Error while checking permission for {0} to perform {1} on {2}",
                            user,
                            action,
                            resource.type
                        )
                    );
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.Write(
                    string.Format(
                        "Error while checking permission for {0} to perform {1} on {2}",
                        user,
                        action,
                        resource.type
                    )
                );
                return false;
            }
        }

        public async Task<bool> Check(
            string user,
            string action,
            string resource,
            Dictionary<string, string> context = null
        )
        {
            var resourceObject = ResourceInput.ResourceFromString(resource);
            var userKey = new UserKey(user);
            return await Check(userKey, action, resourceObject, context);
        }
    }
}
