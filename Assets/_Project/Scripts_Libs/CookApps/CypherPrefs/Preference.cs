using System;
using System.Collections.Generic;
using System.Text;
using CookApps.TeamBattle;
using MemoryPack;

namespace CookApps.CypherPrefs
{
    public abstract class Preference
    {
        protected IPreferenceGetterSetter getterSetter;
        public Preference(IPreferenceGetterSetter getterSetter)
        {
            this.getterSetter = getterSetter ?? throw new ArgumentNullException(nameof(getterSetter));
            GlobalCypherPrefs.RegisterPreference(this);
        }

        public abstract string PreferenceKey { get; }

        protected static byte[] Buffer = new byte[1024];
        protected static readonly byte[] ObfuscationKey = {
            0xA3, 0x7F, 0x52, 0xC8, 0x1B, 0xE4, 0x96, 0x3D,
            0x68, 0xF1, 0x25, 0x9C, 0x4E, 0xD7, 0x0A, 0xB3,
            0x5F, 0x84, 0x2C, 0xE9, 0x71, 0xD0, 0x38, 0xBF,
            0x46, 0x9A, 0x13, 0xCD, 0x7E, 0x02, 0xF8, 0x59
        };

        protected string encryptedKey;
        protected virtual string EncryptedKey => encryptedKey ??= Convert.ToBase64String(EncryptData(PreferenceKey));

        protected bool isDirty = false;
        
        protected static void CheckBufferSize(ref byte[] buffer, int requiredSize)
        {
            if (buffer.Length < requiredSize)
            {
                var newSize = buffer.Length * 2;
                while (newSize < requiredSize)
                {
                    newSize *= 2;
                }
                
                Array.Resize(ref buffer, newSize);
            }
        }

        protected static void ObfuscateBytes(Span<byte> data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                data[i] ^= ObfuscationKey[i % ObfuscationKey.Length];
            }
        }

        protected static Span<byte> EncryptData(string data)
        {
            CheckBufferSize(ref Buffer, data.Length + 4);
            Span<byte> bytes;
            if (Convert.TryFromBase64String(data, Buffer, out var bytesWritten))
            {
                bytes = Buffer.AsSpan(0, bytesWritten);
            }
            else
            {
                // sring to bytes
                bytesWritten = Encoding.UTF8.GetBytes(data, Buffer);
                bytes = Buffer.AsSpan(0, bytesWritten);
            }
            ObfuscateBytes(bytes);
            return bytes;
        }
        
        public abstract void Save();
        public abstract void Load();
        public abstract void Delete();
    }
    
    public abstract class Preference<T> : Preference where T : class, new()
    {
        protected Preference(IPreferenceGetterSetter getterSetter) : base(getterSetter)
        {
            Load();
        }

        protected T data = new ();

        protected virtual bool Deserialize(string serializedData)
        {
            if (string.IsNullOrEmpty(serializedData))
                return false;
            
            try
            {
                var bytes = EncryptData(serializedData);
                MemoryPackSerializer.Deserialize(bytes, ref data);
            }
            catch (Exception e)
            {
                CADebug.LogException(e);
                data = new ();
                return false;
            }

            if (data == null)
                data = new ();
            return true;
        }

        protected virtual string Serialize()
        {
            var bytes = MemoryPackSerializer.Serialize(data);
            ObfuscateBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public override void Save()
        {
            if (!isDirty)
                return;

            var serialized = Serialize();
            getterSetter.Set(EncryptedKey, serialized);
            isDirty = false;
        }

        public override void Load()
        {
            var serialized = getterSetter.Get(EncryptedKey, string.Empty);
            if (string.IsNullOrEmpty(serialized))
            {
                data = new ();
                return;
            }
            Deserialize(serialized);
        }

        public override void Delete()
        {
            data = new ();
            getterSetter.Delete(EncryptedKey);
            isDirty = false;
        }
    }

    [MemoryPackable]
    internal partial class PreferenceDict<TKey, TValue>
    {
        public Dictionary<TKey, TValue> datas = new ();
        public void Clear()
        {
            datas.Clear();
        }
    }

     public abstract class Preference<TKey, TValue> : Preference
     {
         private PreferenceDict<TKey, TValue> dict = new();

         protected Preference(IPreferenceGetterSetter getterSetter) : base(getterSetter)
         {
         }

         public void SetIsDirty(bool dirty)
         {
             isDirty = dirty;
         }

         private void Clear()
         {
             dict.Clear();
             isDirty = true;
         }

         private string Serialize()
         {
             var bytes = MemoryPackSerializer.Serialize(dict);
             ObfuscateBytes(bytes);
             return Convert.ToBase64String(bytes);
         }
         
         private bool Deserialize(string data)
         {
             if (string.IsNullOrEmpty(data))
                 return false;
             
             try
             {
                 var bytes = EncryptData(data);
                 MemoryPackSerializer.Deserialize(bytes, ref dict);
             }
             catch (Exception e)
             {
                 CADebug.Log(e.Message);
                 dict = new ();
                 return false;
             }

             return true;
         }

         public void SetData(TKey key, TValue value)
         {
             dict.datas[key] = value;
             isDirty = true;
         }
         
         public TValue GetData(TKey key)
         {
             return dict.datas.GetValueOrDefault(key, default);
         }
         
         public void RemoveData(TKey key)
         {
             if (dict.datas.Remove(key))
             {
                 isDirty = true;
             }
         }
         
         public override void Save()
         {
             if (!isDirty)
                 return;

             var serialized = Serialize();
             getterSetter.Set(EncryptedKey, serialized);
             isDirty = false;
         }

         public override void Load()
         {
             var serialized = getterSetter.Get(EncryptedKey, string.Empty);
             if (string.IsNullOrEmpty(serialized))
                 dict = new ();
             Deserialize(serialized);
         }

         public override void Delete()
         {
             dict = new ();
             getterSetter.Delete(EncryptedKey);
             isDirty = false;
         }
     }
}