using System.Xml.Serialization;

namespace CFMessageQueue.Utilities
{
    public static class XmlUtilities
    {
        public static T DeserializeFromString<T>(string input)
        {
            var serializer = new XmlSerializer(typeof(T));
            var item = default(T);
            using var reader = new StringReader(input);
            item = (T)serializer.Deserialize(reader);

            return item;
        }

        public static string SerializeToString<T>(T item)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, item);
            return writer.ToString();
        }
    }
}
