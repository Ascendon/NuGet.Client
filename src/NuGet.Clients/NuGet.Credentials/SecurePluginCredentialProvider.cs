using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Protocol.Plugins.Messages;

namespace NuGet.Credentials
{
    public class SecurePluginCredentialProvider : ICredentialProvider
    {

        private PluginDiscoveryResult DiscoveredPlugin { get; set; }
        private Common.ILogger Logger { get; set; }

        public SecurePluginCredentialProvider(PluginDiscoveryResult plugin, Common.ILogger logger)
        {
            DiscoveredPlugin = plugin;
            Logger = logger;
            Id = $"{nameof(SecurePluginCredentialProvider)}_{plugin.PluginFile}";
        }

        /// <summary>
        /// Unique identifier of this credential provider
        /// </summary>
        public string Id { get; }

        /// <param name="uri">The uri of a web resource for which credentials are needed.</param>
        /// <param name="proxy">Ignored.  Proxy information will not be passed to plugins.</param>
        /// <param name="type">
        /// The type of credential request that is being made. Note that this implementation of
        /// <see cref="ICredentialProvider"/> does not support providing proxy credenitials and treats
        /// all other types the same.
        /// </param>
        /// <param name="isRetry">If true, credentials were previously supplied by this
        /// provider for the same uri.</param>
        /// <param name="message">A message provided by NuGet to show to the user when prompting.</param>
        /// <param name="nonInteractive">If true, the plugin must not prompt for credentials.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A credential object.  If </returns>

        public async Task<CredentialResponse> GetAsync(Uri uri, IWebProxy proxy, CredentialRequestType type, string message, bool isRetry, bool nonInteractive, CancellationToken cancellationToken)
        {
            CredentialResponse taskResponse = null;
            if (type == CredentialRequestType.Proxy)
            {
                taskResponse = new CredentialResponse(CredentialStatus.ProviderNotApplicable);
                return taskResponse;
            }

            var request = new GetAuthenticationCredentialsRequest(uri, isRetry, nonInteractive);
            var plugin = await PluginManager.Instance.CreatePluginAsync(DiscoveredPlugin, cancellationToken);
            var credentialResponse = await plugin.Plugin.Connection.SendRequestAndReceiveResponseAsync<GetAuthenticationCredentialsRequest, GetAuthenticationCredentialsResponse>(
                MessageMethod.GetAuthCredentials,
                request,
                cancellationToken);
            taskResponse = GetCredentialResponseToCredentiaResponse(credentialResponse);

            // Dispose of plugin - currently we don't consider the case that the plugin might be used for another operation.
            await PluginManager.Instance.DisposeOfPlugin(plugin.Plugin);

            return taskResponse;
        }

        private static CredentialResponse GetCredentialResponseToCredentiaResponse(GetAuthenticationCredentialsResponse credentialResponse)
        {
            CredentialResponse taskResponse;
            if (credentialResponse.IsValid)
            {
                ICredentials result = new NetworkCredential(credentialResponse.Username, credentialResponse.Password);
                if (credentialResponse.AuthTypes != null)
                {
                    result = new AuthTypeFilteredCredentials(result, credentialResponse.AuthTypes);
                }

                taskResponse = new CredentialResponse(result);
            }
            else
            {
                taskResponse = new CredentialResponse(CredentialStatus.ProviderNotApplicable);
            }

            return taskResponse;
        }
    }
}
