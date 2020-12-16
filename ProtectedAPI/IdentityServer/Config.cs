// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.Models;
using System.Collections.Generic;

namespace IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("scope1"),
                new ApiScope("scope2"),
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                // m2m client credentials flow client
                new Client
                {
                    ClientId = "m2m.client",
                    ClientName = "Client Credentials Client",

                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    ClientSecrets = { new Secret("511536EF-F270-4058-80CA-1C89C192F69A".Sha256()) },

                    AllowedScopes = { "scope1" }
                },

                // interactive client using code flow + pkce
                new Client
                {
                    ClientId = "interactive",
                    ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },
                    
                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,

                    RedirectUris = { "https://localhost:44300/signin-oidc" },
                    FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                    PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                    AllowOfflineAccess = true,
                    AllowedScopes = { "openid", "profile", "scope2" }
                },

                // m2m client credentials flow client
                new Client
                {
                    ClientId = "vouch-proxy",
                    ClientName = "Vouch Proxy for Authorization of Clients at NGINX",

                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    ClientSecrets = { new Secret("b1612bd0-9bbf-4157-99e3-0e1a65bdad92".Sha256()) },

                    RedirectUris = { "https://vouch.proxy.localhost/auth" },
                    FrontChannelLogoutUri = "https://vouch.proxy.localhost/signout-oidc",
                    PostLogoutRedirectUris = { "https://vouch.proxy.localhost/signout-callback-oidc" },

                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Sliding,

                    AllowedCorsOrigins =
                    {
                        "https://vouch.proxy.localhost",
                        "https://reverse.proxy.localhost"
                    },

                    RequireClientSecret = true,
                    RequirePkce = false,

                    AllowedScopes = { "openid", "profile", "scope1" }
                },

                // m2m client credentials flow client
                new Client
                {
                    ClientId = "openresty-proxy",
                    ClientName = "OpenResty NGINX Proxy for Authorization of Clients at NGINX",

                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    ClientSecrets = { new Secret("438c3e42-5f87-4fad-83a5-a30130943521".Sha256()) },

                    RedirectUris = { "https://reverse.proxy.localhost/secure/callback" },
                    FrontChannelLogoutUri = "https://reverse.proxy.localhost/signout-oidc",
                    PostLogoutRedirectUris = { "https://reverse.proxy.localhost/" },

                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Sliding,

                    AllowedCorsOrigins =
                    {
                        "https://reverse.proxy.localhost"
                    },

                    AllowedScopes = { "openid", "profile", "scope1" }
                },
                
                // m2m client credentials flow client
                new Client
                {
                    ClientId = "oauth2-proxy",
                    ClientName = "OAuth2-Proxy for Authorization of Clients at NGINX",

                    AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                    ClientSecrets = { new Secret("0edc6d7b-633c-42d6-9b60-fbb2f10c49dc".Sha256()) },

                    RedirectUris = { "https://reverse.proxy.localhost/oauth2/callback" },
                    FrontChannelLogoutUri = "https://reverse.proxy.localhost/signout-oidc",
                    PostLogoutRedirectUris = { "https://reverse.proxy.localhost/" },

                    RequireClientSecret = true,
                    RequirePkce = false,

                    AlwaysIncludeUserClaimsInIdToken = true,

                    AllowOfflineAccess = true,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    RefreshTokenExpiration = TokenExpiration.Sliding,

                    AllowedCorsOrigins =
                    {
                        "https://reverse.proxy.localhost"
                    },

                    AllowedScopes = { "openid", "profile", "scope1" }
                },
            };
    }
}