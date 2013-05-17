#region Copyright

// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2012
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Caching;
using System.Xml;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Services.Cache;

namespace DotNetNuke.Providers.CachingProviders.DummySerializationCachingProvider
{
    public class DummySerializationCachingProvider: CachingProvider 
    {
        #region Private Members

        private const string ProviderName = "DummySerializationCachingProvider";
        private const bool DefaultUseCompression = false;


        private string GetProviderConfigAttribute(string attributeName, string defaultValue = "")
        {
            var provider = Config.GetProvider("caching", ProviderName);

            if (provider != null && provider.Attributes.AllKeys.Contains(attributeName))
                return provider.Attributes[attributeName];
            return defaultValue;
        }

        private bool UseCompression
        {
            get { return bool.Parse(GetProviderConfigAttribute("useCompression", DefaultUseCompression.ToString(CultureInfo.InvariantCulture))); }
        }

        #endregion

        #region Abstract implementation
        public override void Insert(string key, object value, DNNCacheDependency dependency, DateTime absoluteExpiration, TimeSpan slidingExpiration, CacheItemPriority priority,
                                    CacheItemRemovedCallback onRemoveCallback)
        {        
            try
            {
                // Simply serialize the object before inserting into memory. Optional: use compression
                if (UseCompression)
                {
                    base.Insert(key, CompressData(value), dependency, absoluteExpiration, slidingExpiration,
                                priority, onRemoveCallback);
                }
                else
                {
                    base.Insert(key, Serialize(value), dependency, absoluteExpiration, slidingExpiration,
                                priority, onRemoveCallback);                    
                }
            }
            catch (Exception serialEx)
            {
                // If the object is not serializable, do something
                if (bool.Parse(GetProviderConfigAttribute("silentMode", "false")))
                {
                    // Write on the cache for debugging purposes
                    base.Insert("SERIALIZATION_ERROR_" + key,
                                "An exception was thrown during the serialization of this object" +
                                string.Format("{0}", value), dependency, Cache.NoAbsoluteExpiration,
                                new TimeSpan(0, 0, int.Parse(GetProviderConfigAttribute("defaultCacheTimeout", "300"))));
                    try
                    {
                        throw new SerializationException(
                            string.Format("Error while trying to cache key {0} (Object type: {1}): {2}", key,
                                          value.GetType(), serialEx));
                    }
                    catch (Exception ex)
                    {
                        DotNetNuke.Services.Exceptions.Exceptions.LogException(ex);
                    }
                }
                else
                    throw;

            }

        }

        public override bool IsWebFarm()
        {
            bool _IsWebFarm = Null.NullBoolean;
            if (!string.IsNullOrEmpty(Config.GetSetting("IsWebFarm")))
            {
                _IsWebFarm = bool.Parse(Config.GetSetting("IsWebFarm"));
            }
            return _IsWebFarm;
        }

        public override object GetItem(string key)
        {
            var value = base.GetItem(key);
            if (value != null)
            {
                value = UseCompression ? DecompressData((byte[])value) : Deserialize<object>((string)value);
            }
            return value;
        }

        #endregion


        #region Private methods
        public static string Serialize(object source)
        {
            IFormatter formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, source);
            return Convert.ToBase64String(stream.ToArray());
        }

        public static T Deserialize<T>(string base64String)
        {
            var stream = new MemoryStream(Convert.FromBase64String(base64String));
            IFormatter formatter = new BinaryFormatter();
            stream.Position = 0;
            return (T)formatter.Deserialize(stream);
        }

        public static byte[] SerializeXmlBinary(object obj)
        {
            using (var ms = new MemoryStream())
            {
                using (var wtr = XmlDictionaryWriter.CreateBinaryWriter(ms))
                {
                    var serializer = new NetDataContractSerializer();
                    serializer.WriteObject(wtr, obj);
                    ms.Flush();
                }
                return ms.ToArray();
            }
        }
        public static object DeSerializeXmlBinary(byte[] bytes)
        {
            using (var rdr = XmlDictionaryReader.CreateBinaryReader(bytes, XmlDictionaryReaderQuotas.Max))
            {
                var serializer = new NetDataContractSerializer { AssemblyFormat = FormatterAssemblyStyle.Simple };
                return serializer.ReadObject(rdr);
            }
        }
        public static byte[] CompressData(object obj)
        {
            byte[] inb = SerializeXmlBinary(obj);
            byte[] outb;
            using (var ostream = new MemoryStream())
            {
                using (var df = new DeflateStream(ostream, CompressionMode.Compress, true))
                {
                    df.Write(inb, 0, inb.Length);
                } outb = ostream.ToArray();
            } return outb;
        }

        public static object DecompressData(byte[] inb)
        {
            byte[] outb;
            using (var istream = new MemoryStream(inb))
            {
                using (var ostream = new MemoryStream())
                {
                    using (var sr =
                        new DeflateStream(istream, CompressionMode.Decompress))
                    {
                        sr.CopyTo(ostream);
                    } outb = ostream.ToArray();
                }
            } return DeSerializeXmlBinary(outb);
        }

        #endregion

    }
}
