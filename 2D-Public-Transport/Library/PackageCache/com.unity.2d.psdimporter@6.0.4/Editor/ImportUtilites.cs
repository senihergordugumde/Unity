using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UnityEditor.U2D.PSD
{
    class UniqueNameGenerator
    {
        List<int> m_NameHash = new List<int>();

        public bool ContainHash(int i)
        {
            return m_NameHash.Contains(i);
        }

        public void AddHash(int i)
        {
            m_NameHash.Add(i);
        }

        public void AddHash(string name)
        {
            m_NameHash.Add(GetStringHash(name));
        }
        
        public string GetUniqueName(string name, bool logNewNameGenerated = false, UnityEngine.Object context = null)
        {
            return GetUniqueName(name, m_NameHash);
        }
        
        static string GetUniqueName(string name, List<int> stringHash, bool logNewNameGenerated = false, UnityEngine.Object context = null)
        {
            var sanitizedName = string.Copy(SanitizeName(name));
            string uniqueName = sanitizedName;
            int index = 1;
            while (true)
            {
                var hash = GetStringHash(uniqueName);
                if (!stringHash.Contains(hash))
                {
                    stringHash.Add(hash);
                    if (logNewNameGenerated && sanitizedName != uniqueName)
                        Debug.Log(string.Format("Asset name {0} is changed to {1} to ensure uniqueness", name, uniqueName), context);
                    return uniqueName;
                }
                uniqueName = string.Format("{0}_{1}", sanitizedName, index);
                ++index;
            }
        }
        
        static string SanitizeName(string name)
        {
            name = name.Replace('\0', ' ');
            string newName = null;
            // We can't create asset name with these name.
            if ((name.Length == 2 && name[0] == '.' && name[1] == '.')
                || (name.Length == 1 && name[0] == '.')
                || (name.Length == 1 && name[0] == '/'))
                newName += name + "_";

            if (!string.IsNullOrEmpty(newName))
            {
                Debug.LogWarning(string.Format("File contains layer with invalid name for generating asset. {0} is renamed to {1}", name, newName));
                return newName;
            }
            return name;
        }

        static int GetStringHash(string str)
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToInt32(hashed, 0);
        }
    }
        
    class GameObjectCreationFactory : UniqueNameGenerator
    {

        public GameObject CreateGameObject(string name, params System.Type[] components)
        {
            var newName = GetUniqueName(name);
            return new GameObject(newName, components);
        }
    }

}
