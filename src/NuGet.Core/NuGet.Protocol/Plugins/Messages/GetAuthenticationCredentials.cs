// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace NuGet.Protocol.Plugins.Messages
{


    public class GetAuthenticationCredentialsRequest
    {
        [JsonRequired]
        public Uri Uri { get; private set; }

        [JsonRequired]
        public bool IsRetry { get; private set; }

        [JsonRequired]
        public bool NonInteractive { get; private set; }

        [JsonConstructor]
        public GetAuthenticationCredentialsRequest(Uri uri, bool isRetry, bool nonInteractive)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            IsRetry = isRetry;
            NonInteractive = nonInteractive;
        }
    }
}
