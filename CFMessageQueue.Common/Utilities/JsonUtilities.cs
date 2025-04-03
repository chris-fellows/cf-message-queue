using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace CFMessageQueue.Utilities
{
    public static class JsonUtilities
    {
        public static JsonSerializerOptions DefaultJsonSerializerOptions
        {
            get
            {
                var jsonSerializerOptions = new JsonSerializerOptions();
                //jsonSerializerOptions.Converters.Add(new TimeSpanToStringConverter());
                jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                jsonSerializerOptions.WriteIndented = true;
                jsonSerializerOptions.PropertyNameCaseInsensitive = true;
                return jsonSerializerOptions;
            }
        }

        public static string SerializeToString(object item, Type objectType, JsonSerializerOptions options)
        {
            return JsonSerializer.Serialize(item, options);
        }

        public static object DeserializeFromString(string json, Type objectType, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize(json, objectType, options)!;
        }

        public static string SerializeToString<T>(T item, JsonSerializerOptions options)
        {
            return JsonSerializer.Serialize(item, options);
        }

        public static T DeserializeFromString<T>(string json, JsonSerializerOptions options)
        {
            return (T)JsonSerializer.Deserialize(json, typeof(T), options)!;
        }

        public static string SerializeToBase64String<T>(T item, JsonSerializerOptions options)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(item, options)));
        }

        public static T DeserializeFromBase64String<T>(string json, JsonSerializerOptions options)
        {
            return (T)JsonSerializer.Deserialize(Encoding.UTF8.GetString(Convert.FromBase64String(json)), typeof(T), options)!;
        }
    }
}
