using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace HouseOfCode.Helpers
{
    class SerializationHelper
    {
        public static string DataContractSerializeObject<T>(T objectToSerialize)
        {
            using (MemoryStream memStm = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(memStm, objectToSerialize);

                memStm.Seek(0, SeekOrigin.Begin);

                using (var streamReader = new StreamReader(memStm))
                {
                    string result = streamReader.ReadToEnd();
                    return result;
                }
            }
        }

        /// <summary>
        /// De-serialize non-nullable struct type as nullable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static T? DataContractDeserializeStruct<T>(string serialized) where T : struct
        {
            if (String.IsNullOrWhiteSpace(serialized))
            {
                return null;
            }

            using (MemoryStream m = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                try
                {
                    return serializer.ReadObject(new MemoryStream(Encoding.Unicode.GetBytes(serialized))) as T?;
                }
                catch (SerializationException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// De-serialize nullable class type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serialized"></param>
        /// <returns></returns>
        public static T DataContractDeserializeClass<T>(string serialized) where T : class
        {
            if (String.IsNullOrWhiteSpace(serialized))
            {
                return null;
            }

            using (MemoryStream m = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                try
                {
                    return serializer.ReadObject(new MemoryStream(Encoding.Unicode.GetBytes(serialized))) as T;
                }
                catch (SerializationException)
                {
                    return null;
                }
            }
        }
    }
}
