using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace E_Sport.Extensions
{
    public static class SessionExtensions
    {
        // Set object vào session
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        // Get object từ session
        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
