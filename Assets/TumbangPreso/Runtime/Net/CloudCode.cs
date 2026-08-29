using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The one way this game calls a published Cloud Code script.
    ///
    /// ⚠️⚠️ IT IS ONE COPY BECAUSE THE THIRD ONE WAS ABOUT TO BE WRITTEN. `PlayerAccount` carried
    /// this request by hand, `UgsServicesProbe` carries a deliberate duplicate of it so the probe
    /// can call the endpoint without widening a private method (`docs/TODO.md` § 88.4), and
    /// `CareerStore` needed a third. Two copies with a note saying they must move together is a
    /// cost somebody chose; three is the shape where the game and the probe drift apart and the
    /// probe keeps passing, which § 88.4 already names as the worst outcome available.
    ///
    /// ⚠️⚠️ AND EXTRACTING IT RESOLVES § 88.4'S OWN DILEMMA RATHER THAN IGNORING IT. That entry
    /// kept the probe's duplicate because `PlayerAccount.CallCloudAsync` was private and widening
    /// it *"would put a seam in shipping code for one probe"*, while admitting the cost: *"if the
    /// call shape drifts, the probe passes while the game fails, which is the worst outcome
    /// available."* A shared helper that the GAME calls is not a seam, so the probe can now call
    /// the same code the game does and the drift it was worried about cannot happen. The probe's
    /// hand-written copy is deleted; `TheAccountEndpointAnswersALoad` goes through here.
    ///
    /// ⚠️ THE SDK PACKAGE IS STILL NOT USED. `com.unity.services.cloudcode` is not in the
    /// manifest and this repository's generated PackageManager state cannot currently resolve an
    /// added package, which is why the REST shape is written out. The envelope, the bearer token
    /// and the `params` wrapper are exactly what the SDK sends.
    /// </summary>
    public static class CloudCode
    {
        [Serializable]
        private sealed class Envelope
        {
            public string output;
        }

        /// <summary>
        /// Calls <paramref name="script"/> with <paramref name="parameters"/> and returns the raw
        /// `output` payload as JSON, or throws.
        ///
        /// ⚠️ THE CALLER PARSES ITS OWN SHAPE. A generic helper that also knew about profiles
        /// would be a second place the account and career payloads are described, which is the
        /// duplication this file exists to remove.
        ///
        /// ⚠️ IT THROWS RATHER THAN RETURNING NULL ON A FAILED REQUEST. Every caller here has an
        /// offline path that has to be entered deliberately: a null that reads as "the service
        /// said there is nothing" is indistinguishable from "the service was not reachable", and
        /// those two answers must never be confused by a layer whose whole job is to keep a local
        /// profile when the second one happens.
        /// </summary>
        public static async Task<string> CallAsync(string script, object parameters)
        {
            string projectId = Application.cloudProjectId;
            string accessToken = AuthenticationService.Instance?.AccessToken;

            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(accessToken))
                throw new InvalidOperationException("Cloud Code is unavailable: no project id or no signed-in session.");

            string url = $"https://cloud-code.services.api.unity.com/v1/projects/{projectId}/scripts/{script}";
            string body = JsonConvert.SerializeObject(new { @params = parameters });

            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json, application/problem+json");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"Cloud Code '{script}' failed ({request.responseCode}): {request.error}");

            // ⚠️⚠️ `JsonUtility` CANNOT READ `output` HERE AND `JsonConvert` CAN, WHICH IS WHY THE
            // TWO SERIALISERS BOTH APPEAR IN THIS FILE. `output` is an OBJECT whose shape differs
            // per script, and `JsonUtility` has no representation for "some JSON I will parse
            // later": typing the field as `string` makes it silently read empty. Newtonsoft hands
            // back the sub-document as text, which is what every caller actually wants.
            var envelope = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(
                request.downloadHandler.text);
            var output = envelope?["output"];
            return output?.ToString(Formatting.None) ?? "";
        }
    }
}
