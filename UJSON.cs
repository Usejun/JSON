using System;
using System.Text;
using System.Collections.Generic;

namespace UJSON
{
    public enum JType
    {
        Number,
        String,
        Boolean,
        Array,
        Null,
        Object
    }

    public enum JAccess
    {
        Immutable,
        Value,
        All
    }

    public class JObject
    {
        private class JsonParser
        {
            private readonly int len;
            private int index;
            private readonly string json;
            private readonly JAccess access;

            public JsonParser(string json, JAccess access)
            {
                this.json = json ?? throw new JSONNullException();
                len = json.Length;
                index = 0;
                this.access = access;
            }

            public JObject GetJObject()
            {
                return Parse("");
            }

            private JObject Parse(string key)
            {
                SkipWhitespace();

                if (json[index] == '{') return ParseObject(key);
                else if (json[index] == '[') return ParseArray(key);
                else if (json[index] == '"') return ParseString(key);
                else if (char.IsDigit(json[index]) || json[index] == '-') return ParseNumber(key);
                else if (json[index] == 't' || json[index] == 'f') return ParseBoolean(key);
                else if (json[index] == 'n') return ParseNull(key);

                throw new JSONParsingException("Type error");
            }

            private void SkipWhitespace()
            {
                while (index < len && char.IsWhiteSpace(json[index])) index++;
            }

            private JObject ParseObject(string _key)
            {
                JObject obj = new JObject(_key, JAccess.All);
                index++;

                while (json[index] != '}')
                {
                    SkipWhitespace();
                    JString jString = ParseString(_key);
                    string key = jString.Value;
                    SkipWhitespace();

                    if (json[index] != ':')
                    {
                        throw new JSONParsingException("Not object type");
                    }
                    index++;
                    SkipWhitespace();

                    JObject value = Parse(key);
                    value.access = access;
                    obj[key] = value;

                    SkipWhitespace();
                    if (json[index] == ',') index++;
                }

                obj.access = access;

                index++;
                return obj;
            }

            private JNumber ParseNumber(string _key)
            {
                int start = index;
                if (json[start] == '-') index++;
                while (char.IsDigit(json[index]) || json[index] == '.') index++;

                string numberString = json.Substring(start, index - start);

                if (double.TryParse(numberString, out double result))
                {
                    return new JNumber(_key, result, access);
                }

                throw new JSONParsingException("Not number type");
            }

            private JString ParseString(string _key)
            {
                index++;
                int start = index;
                while (!(json[index] == '"' && json[index - 1] != '\\'))
                    index++;

                string text = json.Substring(start, index - start);
                string convertedText = ConvertUnicode(text);

                JString jString = new JString(_key, convertedText, access);
                index++;

                return jString;
            }

            private JArray ParseArray(string _key)
            {
                JArray array = new JArray(_key, access);
                index++;

                while (json[index] != ']')
                {
                    SkipWhitespace();
                    JObject value = Parse("");
                    array.Values.Add(value);

                    SkipWhitespace();
                    if (json[index] == ',') index++;
                }

                index++;
                return array;
            }

            private JBoolean ParseBoolean(string _key)
            {
                if (json.Substring(index, 4) == "true")
                {
                    index += 4;
                    return new JBoolean(_key, true, access);
                }
                else
                {
                    index += 5;
                    return new JBoolean(_key, false, access);
                }
            }

            private JNull ParseNull(string _key)
            {
                if (json.Substring(index, 4) == "null")
                {
                    index += 4;
                    return new JNull(_key, access);
                }

                throw new JSONParsingException("Type error");
            }

            private string ConvertUnicode(string text)
            {
                StringBuilder convertedText = new StringBuilder();
                int textIndex = 0;

                while (textIndex < text.Length)
                {
                    if (text[textIndex] == '\\' && text[textIndex + 1] == 'u')
                    {
                        convertedText.Append(Convert.ToChar(int.Parse(text.Substring(textIndex + 2, 4), System.Globalization.NumberStyles.HexNumber)));
                        textIndex += 6;
                    }
                    else
                    {
                        convertedText.Append(text[textIndex]);
                        textIndex++;
                    }
                }

                convertedText.Replace("\\/", "/");

                return convertedText.ToString();
            }
        }

        protected const int DEFAULTTEXTHEIGHT = 2;

        public JAccess Access => access;
        public JType Type => type;
        public string Key => key;

        protected JAccess access;
        protected JType type;
        protected string key;
        private readonly List<JObject> values;

        public JObject(string key = "", JAccess access = JAccess.All)
        {
            values = new List<JObject>();
            type = JType.Object;
            this.key = key;
            this.access = access;
        }

        public JObject this[object key]
        {
            get
            {
                if (type == JType.Object && key is string strKey)
                    foreach (JObject jObj in values)
                        if (jObj.key == strKey)
                            return jObj;

                if (type == JType.Array && this is JArray jArray && key is int intKey)
                    return jArray[intKey];

                throw new JSONIndexingException("It's not Indexable type");

            }
            set
            {
                if (access == JAccess.Immutable) throw new JSONAccessException("It's Immutable");

                if (type == JType.Object && key is string strKey)
                {
                    int index = FindKey(strKey);

                    if (index == -1 && access == JAccess.All)
                        Add(strKey, value);
                    else if (index != -1)
                        Update(strKey, value);
                }
                else if (type == JType.Array && this is JArray jArray && key is int intKey)
                {
                    jArray[intKey] = value;
                    return;
                }

                throw new JSONIndexingException("It's not Indexable type");
            }
        }

        public virtual JObject Add(object value)
        {
            if (access == JAccess.Immutable) throw new JSONAccessException("It's is Immutable");

            if (type == JType.Array && this is JArray jArray)
                jArray.Add(value);

            return this;
        }

        public JObject Add(string key, object value)
        {
            if (access != JAccess.All) throw new JSONAccessException("It's is not All Access");
            if (type != JType.Object) throw new JSONTypeException("Unable to add.");

            int index = FindKey(key);

            if (index != -1)
                Update(key, value);
            else
            {
                switch (value)
                {
                    case int i:
                        values.Add(new JNumber(key, i, access));
                        break;
                    case long l:
                        values.Add(new JNumber(key, l, access));
                        break;
                    case float f:
                        values.Add(new JNumber(key, f, access));
                        break;
                    case double d:
                        values.Add(new JNumber(key, d, access));
                        break;
                    case string s:
                        values.Add(new JString(key, s, access));
                        break;
                    case bool b:
                        values.Add(new JBoolean(key, b, access));
                        break;
                    case null:
                        values.Add(new JNull(key, access));
                        break;
                    case List<JObject> li:
                        values.Add(new JArray(key, li, access));
                        break;
                    case JObject j:
                        values.Add(j);
                        break;
                    default: throw new JSONTypeException("The type is not allowed.");
                }
            }
            return this;
        }

        public JObject Remove(string key)
        {
            if (access != JAccess.All) throw new JSONAccessException("It's is not All Access");

            int index = FindKey(key);

            if (index == -1)

                values.RemoveAt(index);

            return this;
        }

        public JObject AddObject(string key)
        {
            if (access != JAccess.All) throw new JSONAccessException("It's is not All Access");

            values.Add(new JObject(key));

            return this;
        }

        public JObject AddObject(string key, params (string key, object value)[] values)
        {
            if (access != JAccess.All) throw new JSONAccessException("It's is not All Access");

            JObject jObject = new JObject(key, access);

            foreach (var data in values)
                jObject.Add(data.key, data.value);

            this.values.Add(jObject);

            return this;
        }

        public JObject AddArray(string key)
        {
            if (access != JAccess.All) throw new JSONAccessException("It's is not All Access");

            values.Add(new JArray(key, access));

            return this;
        }

        public JObject AddArray(string key, params object[] values)
        {
            if (access != JAccess.All) throw new JSONAccessException("It's is not All Access");

            JArray jArray = new JArray(key, access);

            foreach (object value in values)
                jArray.Add(value);

            this.values.Add(jArray);

            return this;
        }

        public JObject Update(string key, object value)
        {
            if (access == JAccess.Immutable) throw new JSONAccessException("it's immutable");
            if (type != JType.Object) 
            {
                this[key].Update(value);
                return this;
            }
            
            switch (value)
            {
                case int i:
                    this[key] = new JNumber(key, i, access);
                    break;
                case long l:
                    this[key] = new JNumber(key, l, access);
                    break;
                case float f:
                    this[key] = new JNumber(key, f, access);
                    break;
                case double d:
                    this[key] = new JNumber(key, d, access);
                    break;
                case bool b:
                    this[key] = new JBoolean(key, b, access);
                    break;
                case string s:
                    this[key] = new JString(key, s, access);
                    break;
                case null:
                    this[key] = new JNull(key, access);
                    break;
                default: break;
            }

            return this;
        }

        public virtual JObject Update(object value)
        {
            if (access == JAccess.Immutable) throw new JSONAccessException("It's Immutable");

            switch (type)
            {
                case JType.Number:
                    if (this is JNumber jNumber)
                        jNumber.Update(value);
                    break;
                case JType.String:
                    if (this is JString jString)
                        jString.Update(value);
                    break;
                case JType.Boolean:
                    if (this is JBoolean jBoolean)
                        jBoolean.Update(value);
                    break;
                default:
                    throw new JSONTypeException("Unable to update");
            }
            return this;
        }

        public virtual string ToJSON()
        {
            StringBuilder sb = new StringBuilder();

            Put(this, 0);

            return sb.ToString();

            void Put(JObject jObject, int depth)
            {
                sb.AppendLine("{");
                for (int i = 0; i < jObject.values.Count; i++)
                {
                    JObject jObj = jObject.values[i];

                    sb.Append(' ', (depth + 1) * DEFAULTTEXTHEIGHT);

                    if (jObj.type == JType.Object)
                    {
                        sb.Append($"\"{jObj.key}\": ");
                        Put(jObj, depth + 1);
                    }
                    else if (jObj.type == JType.Array)
                    {
                        sb.Append($"\"{jObj.key}\": ");
                        sb.Append(jObj.ToJSON(depth + 1));
                    }
                    else
                        sb.Append(jObj.ToJSON());

                    if (i != jObject.values.Count - 1)
                        sb.Append(", ");
                    sb.Append('\n');
                }

                sb.Append(' ', depth * DEFAULTTEXTHEIGHT);
                sb.Append('}');
            }
        }

        public virtual string ToJSON(int height)
        {
            StringBuilder sb = new StringBuilder();

            Put(this, height);

            return sb.ToString();

            void Put(JObject jObject, int depth)
            {
                sb.AppendLine("{");
                for (int i = 0; i < jObject.values.Count; i++)
                {
                    JObject jObj = jObject.values[i];

                    sb.Append(' ', (depth + 1) * DEFAULTTEXTHEIGHT);

                    if (jObj.type == JType.Object)
                    {
                        sb.Append($"\"{jObj.key}\": ");
                        Put(jObj, depth + 1);
                    }
                    else if (jObj.type == JType.Array)
                    {
                        sb.Append($"\"{jObj.key}\": ");
                        sb.Append(jObj.ToJSON(depth + 1));
                    }
                    else
                        sb.Append(jObj.ToJSON());

                    if (i != jObject.values.Count - 1)
                        sb.Append(", ");
                    sb.Append('\n');
                }

                sb.Append(' ', depth * DEFAULTTEXTHEIGHT);
                sb.Append('}');
            }
        }

        public override string ToString()
        {
            return ToJSON();
        }

        public static implicit operator int(JObject jObject)
        {
            if (jObject.type == JType.Number && jObject is JNumber jNumber)
                return (int)jNumber;

            throw new JSONConvertException("Not number type");
        }

        public static implicit operator long(JObject jObject)
        {
            if (jObject.type == JType.Number && jObject is JNumber jNumber)
                return (long)jNumber;

            throw new JSONConvertException("Not number type");
        }

        public static implicit operator float(JObject jObject)
        {
            if (jObject.type == JType.Number && jObject is JNumber jNumber)
                return (float)jNumber;

            throw new JSONConvertException("Not number type");
        }

        public static implicit operator double(JObject jObject)
        {
            if (jObject.type == JType.Number && jObject is JNumber jNumber)
                return (double)jNumber;

            throw new JSONConvertException("Not number type");
        }

        public static implicit operator string(JObject jObject)
        {
            if (jObject.type == JType.String && jObject is JString jString)
                return (string)jString;

            return jObject.ToString();
        }

        public static implicit operator bool(JObject jObject)
        {
            if (jObject.type == JType.Boolean && jObject is JBoolean jBoolean)
                return (bool)jBoolean;

            throw new JSONConvertException("Not boolean type");
        }

        public static implicit operator List<JObject>(JObject jObject)
        {
            if (jObject.type == JType.Array && jObject is JArray jArray)
                return (List<JObject>)jArray;

            throw new JSONConvertException("Not array type");
        }

        private int FindKey(string key)
        {
            for (int i = 0; i < values.Count; i++)
                if (values[i].key == key)
                    return i;

            return -1;
        }

        public static JObject Parse(string json, JAccess access = JAccess.All)
        {
            return new JsonParser(json, access).GetJObject();
        }
    }

    public class JNumber : JObject
    {
        public double Value => value;

        private double value;

        public JNumber(string key = "", double value = 0, JAccess access = JAccess.All)
        {
            this.key = key;
            this.value = value;
            type = JType.Number;
            this.access = access;
        }

        public override JObject Update(object value)
        {
            if (access == JAccess.Immutable) throw new JSONAccessException("It's Immutable");

            switch (value)
            {
                case int i:
                    this.value = i;
                    break;
                case long l:
                    this.value = l;
                    break;
                case float f:
                    this.value = f;
                    break;
                case double d:
                    this.value = d;
                    break;
                case JNumber j:
                    this.value = j;
                    break;
                default: throw new JSONConvertException("not equal type");
            }

            return this;
        }

        public override string ToJSON()
        {
            if (string.IsNullOrEmpty(key)) return $"{value}";
            return $"\"{key}\": {value}";
        }

        public override string ToJSON(int height)
        {
            return ToJSON();
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public static implicit operator int(JNumber jNumber) => (int)jNumber.value;
        public static implicit operator long(JNumber jNumber) => (long)jNumber.value;
        public static implicit operator float(JNumber jNumber) => (float)jNumber.value;
        public static implicit operator double(JNumber jNumber) => jNumber.value;
    }

    public class JString : JObject
    {
        public string Value => value;

        private string value;

        public JString(string key = "", string value = "", JAccess access = JAccess.All)
        {
            this.key = key;
            this.value = value;
            type = JType.String;
            this.access = access;
        }

        public override JObject Update(object value)
        {
            if (access == JAccess.Immutable) throw new JSONAccessException("It's Immutable");

            if (value is string s)
                this.value = s;
            else if (value is JString js)
                this.value = js;
            else
                throw new JSONConvertException("not equal type");

            return this;
        }

        public override string ToJSON()
        {
            if (string.IsNullOrEmpty(key)) return $"\"{value}\"";
            return $"\"{key}\": \"{value}\"";
        }

        public override string ToJSON(int height)
        {
            return ToJSON();
        }

        public static implicit operator string(JString jString) => $"{jString.value}";
    }

    public class JBoolean : JObject
    {
        public bool Value => value;

        private bool value;

        public JBoolean(string key = "", bool value = true, JAccess access = JAccess.All)
        {
            this.key = key;
            this.value = value;
            type = JType.Boolean;
            this.access = access;
        }

        public override JObject Update(object value)
        {
            if (access == JAccess.Immutable) throw new JSONAccessException("It's immutable");

            if (value is bool b)
                this.value = b;
            else if (value is JBoolean jb)
                this.value = jb;
            else
                throw new JSONConvertException("not equal type");

            return this;
        }

        public override string ToJSON()
        {
            if (string.IsNullOrEmpty(key)) return value ? "true" : "false";
            return value ? $"\"{key}\": true" : $"\"{key}\": false";
        }

        public override string ToJSON(int height)
        {
            return ToJSON();
        }

        public override string ToString()
        {
            return ToJSON();
        }

        public static implicit operator bool(JBoolean jBoolean) => jBoolean.value;
    }

    public class JArray : JObject
    {
        public List<JObject> Values => values;

        private readonly List<JObject> values;

        public JArray(string key = "", JAccess access = JAccess.All)
        {
            this.key = key;
            values = new List<JObject>();
            type = JType.Array;
            this.access = access;
        }

        public JArray(string key = "", List<JObject> values = null, JAccess access = JAccess.All)
        {
            this.key = key;
            this.values = values;
            type = JType.Array;
            this.access = access;
        }

        public override JObject Add(object value)
        {
            switch (value)
            {
                case int i:
                    values.Add(new JNumber(value: i, access: access)); ;
                    break;
                case long l:
                    values.Add(new JNumber(value: l, access: access));
                    break;
                case float f:
                    values.Add(new JNumber(value: f, access: access));
                    break;
                case double d:
                    values.Add(new JNumber(value: d, access: access));
                    break;
                case string s:
                    values.Add(new JString(value: s, access: access));
                    break;
                case bool b:
                    values.Add(new JBoolean(value: b, access: access));
                    break;
                case null:
                    values.Add(new JNull(access: access));
                    break;
                case List<JObject> li:
                    values.Add(new JArray(key, li, access));
                    break;
                case JObject j:
                    values.Add(j);
                    break;
                default: throw new JSONTypeException("The type is not allowed.");
            }

            return this;
        }

        private bool OutOfRange(int index)
        {
            if (index < 0 || index >= values.Count)
                return true;
            return false;
        }

        public override string ToJSON()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[");
            if (values.Count != 0) sb.Append('\n');
            for (int i = 0; i < values.Count; i++)
            {
                sb.Append(' ', DEFAULTTEXTHEIGHT);
                if (values[i].Type == JType.Object ||
                    values[i].Type == JType.Array)
                    sb.Append(values[i].ToJSON(1));
                if (i != values.Count - 1)
                    sb.Append(',');
                sb.Append('\n');
            }

            sb.Append(']');

            return sb.ToString();
        }

        public override string ToJSON(int height)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("[");
            if (values.Count != 0) sb.Append('\n');
            for (int i = 0; i < values.Count; i++)
            {
                sb.Append(' ', (height + 1) * DEFAULTTEXTHEIGHT);

                if (values[i].Type == JType.Object ||
                    values[i].Type == JType.Array)
                    sb.Append(values[i].ToJSON(height + 1));
                else
                    sb.Append(values[i].ToJSON());

                if (i != values.Count - 1)
                    sb.Append(',');

                sb.Append('\n');
            }

            if (values.Count != 0) sb.Append(' ', height * DEFAULTTEXTHEIGHT);
            sb.Append(']');

            return sb.ToString();
        }

        public override string ToString()
        {
            return ToJSON();
        }

        public JObject this[int index]
        {
            get
            {
                if (OutOfRange(index))
                    throw new JSONIndexingException("Out of range");

                return values[index];
            }
            set
            {
                if (OutOfRange(index))
                    throw new JSONIndexingException("Out of range");

                values[index] = value;
            }
        }

        public static implicit operator List<JObject>(JArray jArray) => jArray.values;
    }

    public class JNull : JObject
    {
        public object Value => null;

        public JNull(string key = "", JAccess access = JAccess.All)
        {
            this.key = key;
            type = JType.Null;
            this.access = access;
        }

        public override string ToJSON()
        {
            return $"\"{key}\": null";
        }

        public override string ToJSON(int height)
        {
            return ToJSON();
        }

        public override string ToString()
        {
            return "null";
        }

    }

    public class JSONParsingException : Exception
    {
        public JSONParsingException() { }
        public JSONParsingException(string message) : base(message) { }
        public JSONParsingException(string message, Exception inner) : base(message, inner) { }
        protected JSONParsingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class JSONConvertException : Exception
    {
        public JSONConvertException() { }
        public JSONConvertException(string message) : base(message) { }
        public JSONConvertException(string message, Exception inner) : base(message, inner) { }
        protected JSONConvertException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class JSONIndexingException : Exception
    {
        public JSONIndexingException() { }
        public JSONIndexingException(string message) : base(message) { }
        public JSONIndexingException(string message, Exception inner) : base(message, inner) { }
        protected JSONIndexingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class JSONNullException : Exception
    {
        public JSONNullException() { }
        public JSONNullException(string message) : base(message) { }
        public JSONNullException(string message, Exception inner) : base(message, inner) { }
        protected JSONNullException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class JSONAccessException : Exception
    {
        public JSONAccessException() { }
        public JSONAccessException(string message) : base(message) { }
        public JSONAccessException(string message, Exception inner) : base(message, inner) { }
        protected JSONAccessException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class JSONTypeException : Exception
    {
        public JSONTypeException() { }
        public JSONTypeException(string message) : base(message) { }
        public JSONTypeException(string message, Exception inner) : base(message, inner) { }
        protected JSONTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
