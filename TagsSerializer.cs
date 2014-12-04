using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpQuant.ObjectStore
{
    public interface ITagsSerializer
    {
        IDictionary<string, string> Deserialize(string raw);
        string Serialize(IDictionary<string, string> info);
    }

    /// <summary>
    /// Simplistic but fast INI-like serializer. No special characters allowed.
    /// </summary>
    public class INITagsSerializer : ITagsSerializer
    {
        private string _valueSeparator = "]=[";
        private char _itemSeparator = '\n';

        public IDictionary<string, string> Deserialize(string raw)
        {
            var dict = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(raw))
                return dict;

            string[] items = raw.Split(_itemSeparator);
            foreach (var item in items)
            {
                int pos = item.IndexOf(_valueSeparator);
                string key = item.Substring(1, pos - 1);
                string value = item.Substring(pos + 3, item.Length - pos - 4);

                dict.Add(key, value);
            }

            return dict;
        }

        public string Serialize(IDictionary<string, string> info)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in info)
            {
                sb.Append("[");
                sb.Append(item.Key);
                sb.Append(_valueSeparator);
                sb.Append(item.Value);
                sb.Append("]");
                sb.Append(_itemSeparator);
            }
            if (sb.Length > 1) return sb.ToString(0, sb.Length - 1);
            return string.Empty;
        }
    }

    /// <summary>
    /// Simplistic but fast JSON-like serializer. No special characters allowed.
    /// </summary>
    public class JSONTagsSerializer : ITagsSerializer
    {
        public IDictionary<string, string> Deserialize(string raw)
        {
            var dict = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(raw))
                return dict;

            var lines = raw.Split(',');
            int n = lines.Length;

            for (int i = 0; i < n; i++)
            {
                var vt = lines[i].Split(':');
                if (i == 0)
                    vt[0] = vt[0].Substring(1); //remove {
                if (i == n - 1)
                    vt[1] = vt[1].Substring(0, vt[1].Length - 1); //remove }
                dict.Add(vt[0].Substring(1, vt[0].Length - 2), vt[1].Substring(1, vt[1].Length - 2)); //remove "
            }
            return dict;
        }

        public string Serialize(IDictionary<string, string> info)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (var item in info)
            {
                sb.Append("\"");
                sb.Append(item.Key);
                sb.Append("\":\"");
                sb.Append(item.Value);
                sb.Append("\",");
            }
            if (sb.Length > 1) sb.Length = sb.Length - 1; //remove last comma
            sb.Append("}");
            if (sb.Length > 1) return sb.ToString(0, sb.Length - 1);
            return string.Empty;
        }
    }
}
