using System;
using System.Collections;
using UnityEngine.Networking;

namespace Prg.Util
{
    public static class HttpUtil
    {
        public static void Get(string address, Action<UnityWebRequest> callback)
        {
            Get(address, null, callback);
        }

        public static void Get(string address, Tuple<string, string> accessToken, Action<UnityWebRequest> callback)
        {
            CoroutineHost.Instance.StartCoroutine(DoGet());
            return;

            IEnumerator DoGet()
            {
                using var request = UnityWebRequest.Get(address);
                if (accessToken != null)
                {
                    request.SetRequestHeader(accessToken.Item1, accessToken.Item2);
                }
                yield return request.SendWebRequest();
                callback?.Invoke(request);
            }
        }
    }
}
