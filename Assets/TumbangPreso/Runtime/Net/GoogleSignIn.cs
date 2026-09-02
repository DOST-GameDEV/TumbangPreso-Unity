using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Signing in with a Google account, on a desktop build, without a platform SDK.
    ///
    /// 🧑 2026-09-01: *"can we add some sort of authentication too? like an option to sign inn
    /// with google acct or connect google acct"*.
    ///
    /// ⚠️⚠️ UGS TAKES A GOOGLE **ID TOKEN** AND HAS NO WAY OF GETTING ONE ON WINDOWS. The
    /// Authentication package ships `SignInWithGoogleAsync(idToken)` and `LinkWithGoogleAsync`,
    /// and every sample obtains that token from Google Play Games or Firebase, neither of which
    /// exists on a Windows player. **So the token is what this file is for and the UGS call is one
    /// line at the end of it.** This is Google's own "OAuth 2.0 for installed applications" flow:
    /// open the system browser, catch the redirect on a loopback listener, and exchange the code.
    ///
    /// ⚠️⚠️ PKCE, AND IT IS NOT OPTIONAL FOR A DESKTOP CLIENT. A native app cannot keep a secret:
    /// anything compiled into this .exe is readable by anybody who has the .exe, which is
    /// everybody. PKCE replaces the secret with a one-shot proof that the machine which asked for
    /// the code is the machine redeeming it, so an attacker who intercepts the redirect on this
    /// machine still cannot exchange it. `code_verifier` is 64 random bytes, base64url, and the
    /// challenge is its SHA-256.
    ///
    /// ⚠️⚠️ THE LOOPBACK PORT IS EPHEMERAL AND THE LISTENER IS BOUND BEFORE THE BROWSER OPENS.
    /// Google allows any port on `127.0.0.1` for an installed app, which is what makes this work
    /// with no fixed redirect registration. Binding after opening the browser is the race: a fast
    /// consent (an already-signed-in Chrome profile takes about a second) redirects to a port
    /// nothing is listening on and the player sees a browser error instead of a signed-in game.
    ///
    /// ⚠️⚠️ AND IT SHIPS DARK. <see cref="IsAvailable"/> is false unless a client id is present,
    /// and every caller hides its button rather than showing one that explains itself. `CLAUDE.md`
    /// § 6.3: *"a control that does something must react to the pointer; one that does nothing
    /// must not look pressable"*, and `docs/TODO.md` § 108's EQUIP button with no listener is what
    /// a visible-but-inert control actually costs.
    ///
    /// ⚠️ THE CLIENT ID IS NOT A SECRET AND IS NOT IN GIT ANYWAY. It identifies the application to
    /// Google and is visible in every consent URL the player's own browser shows; it is kept out
    /// of the repository because it is per-project configuration rather than because it is
    /// confidential, exactly like `BuildBranch.txt`. See <see cref="ClientIdResource"/>.
    /// </summary>
    public static class GoogleSignIn
    {
        /// <summary>
        /// Where the OAuth client id is read from: `Assets/TumbangPreso/Resources/google_oauth.txt`,
        /// first line the client id, optional second line the client secret.
        ///
        /// ⚠️⚠️ A "CLIENT SECRET" FOR AN INSTALLED APP IS NOT A SECRET AND GOOGLE SAYS SO IN ITS
        /// OWN DOCUMENTATION. Desktop clients created in the Google Cloud console are issued one
        /// and it is embedded in every desktop app that uses this flow; the security comes from
        /// PKCE and from the redirect being on this machine's loopback. It is supported here
        /// because Google's token endpoint still REQUIRES it for a client of type "Desktop app",
        /// and omitted cleanly for a client created without one.
        /// </summary>
        public const string ClientIdResource = "google_oauth";

        /// <summary>
        /// ⚠️ THE ENVIRONMENT VARIABLE IS FOR THIS MACHINE ONLY, so a developer can try the flow
        /// without writing a file into `Resources` that a build would then ship.
        /// </summary>
        public const string ClientIdEnvironmentVariable = "TUMP_GOOGLE_CLIENT_ID";

        private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        /// <summary>
        /// ⚠️ `openid` IS THE ONE THAT MATTERS AND THE OTHER TWO ARE WHY THE CONSENT SCREEN IS
        /// READABLE. Without `email` and `profile` Google's consent page says the app wants to
        /// "associate you with your personal info", which reads far worse than what it is doing.
        /// **The game never asks for anything else**: no Drive, no contacts, no offline access,
        /// and no refresh token, because a refresh token is a long-lived credential this game has
        /// no use for and no safe place to keep.
        /// </summary>
        private const string Scopes = "openid email profile";

        /// <summary>How long the player has to finish in the browser before the listener gives
        /// up. ⚠️ Long enough to type a password and answer a 2FA prompt, and short enough that a
        /// player who closed the tab is not stuck on a spinner for ever.</summary>
        private const int ConsentTimeoutSeconds = 180;

        private static string _clientId;
        private static string _clientSecret;
        private static bool _configRead;

        /// <summary>True when this build has been told which Google application it is.</summary>
        public static bool IsAvailable
        {
            get
            {
                ReadConfig();
                return !string.IsNullOrEmpty(_clientId);
            }
        }

        /// <summary>
        /// The one sentence to show when the feature is off, for a screen that wants to explain
        /// rather than hide. ⚠️ It names the file, because "not configured" sends somebody to read
        /// this class and the file name is the whole answer.
        /// </summary>
        public static string UnavailableReason =>
            $"Google sign-in is not configured in this build (Resources/{ClientIdResource}.txt).";

        private static void ReadConfig()
        {
            if (_configRead) return;
            _configRead = true;

            string fromEnvironment = null;
            try { fromEnvironment = Environment.GetEnvironmentVariable(ClientIdEnvironmentVariable); }
            catch { /* a sandboxed player may refuse; the resource is the real path */ }

            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                _clientId = fromEnvironment.Trim();
                return;
            }

            var asset = Resources.Load<TextAsset>(ClientIdResource);
            if (asset == null) return;

            string[] lines = asset.text.Replace("\r", "").Split('\n');
            if (lines.Length > 0) _clientId = lines[0].Trim();
            if (lines.Length > 1) _clientSecret = lines[1].Trim();
        }

        /// <summary>
        /// Runs the whole browser flow and answers with a Google ID token.
        ///
        /// ⚠️ IT THROWS WITH A SENTENCE A PLAYER CAN READ. Every failure here is something that
        /// happened in a browser window the game cannot see, so "the request failed" is useless:
        /// the three real ones are "you closed it", "you pressed cancel" and "this build has no
        /// client id", and each says so.
        /// </summary>
        public static async Task<string> AcquireIdTokenAsync()
        {
            ReadConfig();
            if (string.IsNullOrEmpty(_clientId)) throw new InvalidOperationException(UnavailableReason);

            string verifier = RandomUrlSafe(64);
            string challenge = Challenge(verifier);
            string state = RandomUrlSafe(24);

            // ⚠️ PORT 0 ASKS THE OS FOR A FREE ONE. A fixed port is a port some other program on
            // the player's machine already has, and the failure would be a listener that refuses
            // to start on one machine in twenty with nothing to point at.
            var listener = new HttpListener();
            int port = FreePort();
            string redirect = $"http://127.0.0.1:{port}/";
            listener.Prefixes.Add(redirect);

            try
            {
                listener.Start();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    "This machine would not let the game listen for Google's answer. " +
                    "A firewall or another program may be holding the port. " + e.Message);
            }

            try
            {
                string url = AuthEndpoint
                             + "?client_id=" + Uri.EscapeDataString(_clientId)
                             + "&redirect_uri=" + Uri.EscapeDataString(redirect)
                             + "&response_type=code"
                             + "&scope=" + Uri.EscapeDataString(Scopes)
                             + "&code_challenge=" + challenge
                             + "&code_challenge_method=S256"
                             + "&state=" + state
                             // ⚠️ SELECT AN ACCOUNT EVERY TIME. Without this, a machine with one
                             // signed-in Chrome profile silently reuses it, so a second player on
                             // a shared laptop cannot get their own account onto the game and
                             // there is nothing on screen to tell them why.
                             + "&prompt=select_account";

                Application.OpenURL(url);

                HttpListenerContext context = await WaitForRedirectAsync(listener);
                var request = context.Request;

                string returnedState = request.QueryString["state"];
                string code = request.QueryString["code"];
                string error = request.QueryString["error"];

                Respond(context, error == null && code != null
                    ? "You are signed in. You can close this tab and go back to the game."
                    : "Sign-in was cancelled. You can close this tab and go back to the game.");

                if (!string.IsNullOrEmpty(error))
                    throw new InvalidOperationException(
                        error == "access_denied"
                            ? "Google sign-in was cancelled."
                            : "Google refused the sign-in: " + error);

                // ⚠️⚠️ THE STATE IS COMPARED AND A MISMATCH IS FATAL. It is the only thing that
                // distinguishes Google's redirect from any other program on this machine posting
                // a code at the loopback port while the listener is open.
                if (returnedState != state)
                    throw new InvalidOperationException("That answer did not come from the sign-in that was started.");

                if (string.IsNullOrEmpty(code))
                    throw new InvalidOperationException("Google did not send an authorisation code.");

                return await ExchangeAsync(code, verifier, redirect);
            }
            finally
            {
                try { listener.Stop(); listener.Close(); } catch { }
            }
        }

        /// <summary>
        /// Waits for the browser to come back, or gives up.
        ///
        /// ⚠️⚠️ A FAVICON REQUEST IS NOT THE REDIRECT. Browsers ask for `/favicon.ico` on any page
        /// they render, including this listener's own "you can close this tab" response, so a
        /// listener that takes the FIRST request it gets sometimes takes that one instead and
        /// reports that Google sent no code. The loop keeps waiting until a request actually
        /// carries `code` or `error`.
        /// </summary>
        private static async Task<HttpListenerContext> WaitForRedirectAsync(HttpListener listener)
        {
            var deadline = DateTime.UtcNow.AddSeconds(ConsentTimeoutSeconds);

            while (DateTime.UtcNow < deadline)
            {
                Task<HttpListenerContext> incoming = listener.GetContextAsync();
                var timeout = Task.Delay(TimeSpan.FromSeconds(2));

                if (await Task.WhenAny(incoming, timeout) != incoming) continue;

                var context = await incoming;
                var q = context.Request.QueryString;

                if (q["code"] != null || q["error"] != null) return context;

                Respond(context, "Waiting for Google.");
            }

            throw new TimeoutException("The browser did not come back. Nothing has changed on this account.");
        }

        private static void Respond(HttpListenerContext context, string message)
        {
            try
            {
                // ⚠️ WOOD, CREAM AND AMBER, because this page is the one piece of this game a
                // player sees outside the game and a white browser default reads as a broken
                // redirect rather than as the end of the flow.
                string html =
                    "<!doctype html><html><head><meta charset=\"utf-8\"><title>TUMP</title></head>" +
                    "<body style=\"background:#31190b;color:#f5e6c8;font-family:system-ui,sans-serif;" +
                    "display:flex;align-items:center;justify-content:center;height:100vh;margin:0\">" +
                    "<div style=\"text-align:center\"><h1 style=\"color:#ffba00;letter-spacing:.1em\">TUMP</h1>" +
                    "<p>" + WebUtility.HtmlEncode(message) + "</p></div></body></html>";

                byte[] body = Encoding.UTF8.GetBytes(html);
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = body.Length;
                context.Response.OutputStream.Write(body, 0, body.Length);
                context.Response.OutputStream.Close();
            }
            catch { /* the browser may have gone; the code is already in hand */ }
        }

        private static async Task<string> ExchangeAsync(string code, string verifier, string redirect)
        {
            var fields = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("redirect_uri", redirect),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code_verifier", verifier),
            };

            if (!string.IsNullOrEmpty(_clientSecret))
                fields.Add(new KeyValuePair<string, string>("client_secret", _clientSecret));

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(30);

                var response = await http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(fields));
                string json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        "Google would not complete the sign-in. " + Describe(json));

                var token = JsonUtility.FromJson<TokenResponse>(json);
                if (token == null || string.IsNullOrEmpty(token.id_token))
                    throw new InvalidOperationException("Google's answer carried no identity token.");

                return token.id_token;
            }
        }

        /// <summary>⚠️ THE ERROR IS SHOWN, NOT SWALLOWED, and it is Google's own `error_description`.
        /// The two that actually happen are a redirect URI mismatch and an unconfigured consent
        /// screen, and both are unrecoverable from inside the game: somebody has to fix the
        /// project in the Google console, and they need the sentence.</summary>
        private static string Describe(string json)
        {
            try
            {
                var error = JsonUtility.FromJson<TokenError>(json);
                if (error != null && !string.IsNullOrEmpty(error.error))
                    return error.error + (string.IsNullOrEmpty(error.error_description)
                        ? "" : ": " + error.error_description);
            }
            catch { }

            return "";
        }

        private static int FreePort()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private static string RandomUrlSafe(int bytes)
        {
            var buffer = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(buffer);
            return UrlSafe(buffer);
        }

        private static string Challenge(string verifier)
        {
            using (var sha = SHA256.Create())
                return UrlSafe(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));
        }

        /// <summary>⚠️ BASE64URL, NOT BASE64. `+`, `/` and `=` are all meaningful in a query
        /// string, and a verifier that survives one browser and not another is the worst possible
        /// shape of bug to chase.</summary>
        private static string UrlSafe(byte[] data)
            => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        [Serializable]
        private sealed class TokenResponse
        {
            public string id_token;
            public string access_token;
        }

        [Serializable]
        private sealed class TokenError
        {
            public string error;
            public string error_description;
        }
    }
}
