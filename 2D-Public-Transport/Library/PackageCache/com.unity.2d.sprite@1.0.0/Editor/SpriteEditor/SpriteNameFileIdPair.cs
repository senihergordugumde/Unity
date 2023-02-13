using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Data structure to hold Name ID pair.
    /// </summary>
    [Serializable]
    public class SpriteNameFileIdPair : IEquatable<SpriteNameFileIdPair>
    {
        [SerializeField]
        private string m_Name;
        [SerializeField]
        private long m_FileId;
        [SerializeField]
        private GUID m_GUID;

        /// <summary>
        /// Name property.
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// FileId property. This property is obsolete. Please use GetFileGUID and SetFileGUID instead.
        /// </summary>
        [Obsolete("Property obsolete. Please use GetFileGUID and SetFileGUID instead.")]
        public long fileId
        {
            get { return m_FileId; }
            set
            {
                m_FileId = value;
                SetFileGUID(GUID.CreateGUIDFromSInt64(m_FileId));
            }
        }

        /// <summary>
        /// Returns the ID used for the name.
        /// </summary>
        /// <returns>GUID value.</returns>
        public GUID GetFileGUID()
        {
            return m_GUID;
        }

        /// <summary>
        /// Sets the ID used for the name.
        /// </summary>
        /// <param name="value">GUID value to set.</param>
        public void SetFileGUID(GUID value)
        {
            if (value.Empty())
                value = GUID.Generate();
            m_GUID = value;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SpriteNameFileIdPair() {}

        /// <summary>
        /// Constructor to initialize name and fileID.
        /// </summary>
        public SpriteNameFileIdPair(string name, GUID fileId)
        {
            this.name = name;
            SetFileGUID(fileId);
        }

        /// <summary>
        /// Custom hashcode generation.
        /// </summary>
        /// <returns>Int value representing the hash value.</returns>
        public override int GetHashCode()
        {
            return (name ?? string.Empty).GetHashCode() ^ m_GUID.GetHashCode();
        }

        /// <summary>
        /// Override Equal operator.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True if the same. False otherwise.</returns>
        public override bool Equals(object obj)
        {
            var pair = obj as SpriteNameFileIdPair;
            return pair != null && Equals(pair);
        }

        /// <summary>
        /// Override Equal operator.
        /// </summary>
        /// <param name="pair">Object to compare.</param>
        /// <returns>True if the same. False otherwise.</returns>
        public bool Equals(SpriteNameFileIdPair pair)
        {
            return pair != null && name == pair.name && GetFileGUID() == pair.GetFileGUID();
        }
    }
}
