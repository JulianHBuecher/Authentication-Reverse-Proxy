# Authentication at Proxy Level

## Table of Contenxt
- [Infrastructure](#infrastructure)
- [Target](#target)
- [Possibilities](#possibilities)
    - [Functions out of the Box](#out-of-the-box)
        - [Basic Authentication](#basic-authentication)
        - [External Authentication Provider](#external-authentication-provider)
        - [JWT Authentication](#jwt-authentication)
    - [External Providers](#external-providers)
        - [Vouch Proxy](#vouch-proxy)
        - [lua-resty-openidc](#lua-resty-openidc)
        - [OAuth2-Proxy](#oauth2-proxy)

## Infrastructure
The test environment for this project includes two differenct container services. One part is a NGINX Webserver with two static files as content for protection. Plus a NGINX Reverse Proxy with Authentication at Proxy Level to protected the services behind it. <br>

## Target
Target of this test is to evaluate different authentication schemes to secure the webserver without additional security configuration at website level.

## Possibilities
For tackling these tasks NGINX serves a range of standard functionalities. <br>
Following the different authentication schemes to protect access to resources: <br><br>
For references see the according [documentation](https://docs.nginx.com/nginx/admin-guide/security-controls/).

### Out of the box

#### Basic Authentication
First of all the standard of all authentication, Basic Authentication. Via NGINX it is possible to restrict access to your website or some parts of it by implementing a username/password authentication. These credentials are taken from a file created and populated by a password creation tool, for example `apache2-utils` (Debian, Ubuntu) or `httpd-tools` (RHEL/CentOS/Oracle Linux). <br>

To create a password file everything you have to do is to set this command...
```bash
sudo htpasswd -c /etc/apache2/.htpasswd user
``` 
... and enter a password in the prompt. It's possible to insert more than one user, so access could be granted to multiple users. <br><br>

To inspect the entries in the generated file, use:
```bash
cat /etc/apache2/.htpasswd
```
To enable the Basic Authentication in NGINX, you have to specify the locations, which you want to protect. <br>
Simplest form of securing an endpoint is:
```Nginx
location /endpoint {
    auth_basic "Administrator's Area";
    auth_basic_user_file /etc/appache2/.htpasswd;
}
```
Additionally you could enable Basic Authentication at the server level and disable it on single endpoints:
```Nginx
server {
    ...
    auth_basic "Administrator's Area";
    auth_basic_user_file conf/htpasswd;

    location /public {
        auth_basic off;
    }
}
```
Another feature is Access Restriction by IP Address. Configuration Parameters could be enabled at location level...
```Nginx
location /protected {
    ...
    deny 192.168.1.2;
    allow 192.168.1.1/24;
    allow 127.0.0.1;
    deny all;
}
```
or combined with Basic Authentication:
```Nginx
location /protected {
    satisfy all;
    
    deny 192.168.1.2;
    allow 192.168.1.1/24;
    allow 127.0.0.1;
    deny all;
    
    auth_basic "Administrator's Area";
    auth_basic_user_file conf/htpasswd;
}
```
Now you have to login when you want access to your page. If the credentials do not match, you get a 401 (Authorization Required) error.

#### External Authentication Provider
Another Option is so called "Authentication Based on Subrequest Result" or Authentication via external authentication provider.
It's possible to authenticate each request to your website with an external server or service. To perform authentication, NGINX makes an HTTP subrequest to an external server where the subrequest is verified. If the subrequest returns a `2xx` status code, the access is allowd, if it returns `401` or `403`, the access is denied. <br><br>

For using this feature you have to make sure, your NGINX is compiled with the with-http_auth_request_module configuration option. To verify this you have to run the following command:
```bash
nginx -V 2>&1 | grep -- 'http_auth_request_module'
```
If the output includes `--with-http_auth_request_module` everything is fine! <br><br>

Next you have to specify the location, which requires request authentication. For getting authentification, specify the `auth_request` directive with an internal location where an authorization subrequest will be forwarded to: <br>
(As an option, you can set a variable basing on the result of the subrequest with the `auth_request_set` direcive)
```Nginx
location /private {
    auth_request /auth;
    ...
    auth_request_set $auth_status $upstream_status;
}
```
Another option is to proxy authentication subrequests to an authentication server or service. As the request body is discarded for authentication subrequests, you will need to set the `proxy_pass_request_body` directive to `off` and also set the `Content-Length` header to a null string.
Additionally you could pass thhe full original reqquuet URI with `proxy_set_header` directive:
```Nginx
location = /auth {
    # Locations marked as internal could not be accessed via external clients
    internal;
    proxy_pass http://auth-server;
    proxy_set_header Content-Length "";
    proxy_set_header X-Original-URI $request_uri;
}
```

#### JWT Authentication
Finally NGINX could handle authentication via JSON Web Tokens (JWT) or Opaque Tokens. The following illustration shows the flow for this specific authentication:
<div style="vertical-align: top; text-align: center;width: 500px;margin-left: auto;margin-right: auto;margin-top: 10px;">
<img src="https://www.nginx.com/wp-content/uploads/2019/05/OAuth-2.0-access-tokens_NGINX-validates.png" width=500 height=350 style="display: block;" alt="Secure Infrastructure with NGINX Reverse Proxy" />
<span style="display: block;text-align: center;"><i>Src: https://www.nginx.com/wp-content/uploads/2019/05/OAuth-2.0-access-tokens_NGINX-validates.png</i></span>
</div>

**This flow works as follows:** <br>
After authentication, a client presents its access token with each HTTP request to gain access to protected resources. Validation of the access token is required to ensure that it was indeed issued by a trusted identity provider (in our case: IdentityServer4 IS4) and that it has not exxpired. Because IS4 cryptographically sign the JWTs they issue, JWTs can be validated "offline" without runtime dependency on the IS4. Typically, a JWT also includes an expiry date, which can also be checked. <br><br>
To enable this behaviour in NGINX we need the NGINX `auth_request` Module to validate Tokens. This avoids code duplication in the backend on the one side and on the other side we have a single point of failure, if the token is not as expected. <br><br>

The following example configuration describes the Token Introspection of NGINX via IS4. For this kind of request a client has to present it's access token to the NGINX, which forward it to the IS4. The IS4 validates it and responds with `204` or a `4xx` status code (possible solution for API Calls).
```Nginx
server {
    listen 80;

    location / {
        auth_request /_oauth2_token_introspection;
        proxy_pass http://backend;
    }

    location = /_oauth2_token_introspection {
        # Locations marked as internal could not be accessed via external clients
        internal;
        proxy_method      POST;
        proxy_set_header  Authorization "Bearer SecretForOAuthServer";
        proxy_set_header  Content-Type "application/x-www-form-urlencoded";
        proxy_set_body    "token=$http_apikey&token_hint=access_token";
        proxy_pass        http://identityserver/oauth/token;
    }
}
```
The additional configuration in the token-introspection endpoint configures the request to be conform with the token introspection request format. In this case access tokens will be supplied by the client in the `apikey` request header. But any other HTTP header could be used here. <br><br>

All of the configuration to construct the token introspection request is contained within the **/_oauth2_send_request** location. Authentication (Authorization), the access token itself and the URL for the token introspection endpoint are typically the only necessary configuration items. <br><br>

The `auth_request` module like this, is not a complete solution. The OAuth 2.0 token introspection responses encode success or failure in a JSON object, and return HTTP status code `200` in both cases. <br>
We need an additional JSON parser to convert the IS4 introspection response to the appropriate HTTP status code, so we could interpret that response correctly.
```Json
{
    "active": true
}
```
 We need here the *NGINX JavaScript module (njs)*. So instead of defining a location block to perform the token introspection request, we define a JavaScript function. <br><br>

The following example uses the JavaScript-Function introspectAccessToken():
```Nginx
js_include oauth2.js; # Location of JavaScript code

server {
    listen 80;

    location / {
        auth_request /_oauth2_token_introspection;
        proxy_pass http://my_backend;
    }

    location = /_oauth2_token_introspection {
        internal;
        js_content introspectAccessToken;
    }
}
```
According to this implementation in `oauth2.js`:
```JavaScript
function introspectAccessToken(r) {
    r.subrequest("/_oauth2_send_request",
        function(reply) {
            if (reply.status == 200) {
                var response = JSON.parse(reply.responseBody);
                if (response.active == true) {
                    r.return(204); // Token is valid, return success code
                } else {
                    r.return(403); // Token is invalid, return forbidden code
                }
            } else {
                r.return(401); // Unexpected response, return 'auth required'
            }
        }
    );
}
```
This solution handles the proper exchange of status codes between the IS4 and the NGINX.

#### Challenge
Because of introspection this method adds in general minor latency to each and every HTTP request. This can becaome a significant issue. To solve this, we could enable **Caching by NGINX**. <br><br>

NGINX can be configured to cache a copy of the introspection response for each access token so that the next time the same access token is presented, NGINX serves the cached introspection response instead of making an API call to the IS4. This improves overall latency for subsequent requests. A definition for how long the token will be cached is possible too. <br><br>

To enable caching we have to set a `proxy_cache_path` directive, which allocates the necessary storage (**/var/cache/nginx/oauth**) for the introspection response and a memory zone (**token_response**) for the keys. It is configured in the `http` context and appears outside the `server` and `location` blocks. <br><br>

Caching itself is enabled inside the `location` block where the token instrospection responses are processed:
```Nginx
    ...
    
    location /_oauth2_send_request {
        internal;
        js_content introspectAccessToken;

        proxy_cache           token_responses; # Enable caching
        proxy_cache_key       $http_apikey;    # Cache for each access token
        proxy_cache_lock      on;              # Duplicate tokens must wait
        proxy_cache_valid     200 10s;         # How long to use each response
        proxy_ignore_headers  Cache-Control Expires Set-Cookie;
    }

    ...
```
Caching is now enabled for this location. By default NGINX caches based on the URI but in our case we want to cache the response based on the access token presented in the `apikey` request header. <br><br>

We use the `proxy_cache_lock` directive to tell NGINX that if concurrent requests arrive with the same cache key, it needs to wait until the first request has populated the cache before responding to the others. The `proxy_cached_valid` directive tells NGINX how long to cache the introspection response. Without this directive NGINX determines the caching time from the cache-control headers sent by the IS4. These headers are not always reliable, which is why we also tell NGINX to **ignore headers** that would otherwise affect how we cache responses. <br><br>

With caching now enabled, a client presenting an access token suffers only the latency cost of making the token intropsection request once every 10 seconds.  

### External Providers
Beside the build in functions for subrequests there are additional frameworks, which handle authentication at NGINX. The following three are open source projects and are used in the sample repository above.

#### Vouch Proxy
Vouch Proxy is a relativly new community driven project. It is implemented in Golang and is an additional service in your network. It takes unauthorized requests from NGINX validates them and send them to your identity provider. If the user authentication is successfull access to your protected resources will be granted. <br><br>

For information about the project, see the [GitHub Repo](https://github.com/vouch/vouch-proxy).

#### lua-resty-openidc
lua-resty-openidc is like a extension to NGINX. It depends on the auth_request modul build in in NGINX and handles user authentication. To use this Lua module, you have to install it in your NGINX server first and configure your protected endpoint in your .conf-files. <br>
An working example is placed in my repo in `OpenResty_Reverse_Proxy` folder. <br><br>

For information about the project, see the [GitHub Repo](https://github.com/zmartzone/lua-resty-openidc).

#### OAuth2-Proxy
OAuth2-Proxy is like Vouch Proxy a community driven project. It is implemented in Golang and is similiar to Vouch Proxy an additional service in your network. A standalone variant is possible too. It takes unauthorized requests from NGINX validates them and send them to your identity provider. If the user authentication is successfull access to your protected resources will be granted. 
<br><br>

For information about the project, see the [GitHub Repo](https://github.com/oauth2-proxy/oauth2-proxy).