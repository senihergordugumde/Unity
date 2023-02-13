using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    internal class TextureImporterDataProviderFactory :
        ISpriteDataProviderFactory<Texture2D>,
        ISpriteDataProviderFactory<Sprite>,
        ISpriteDataProviderFactory<TextureImporter>,
        ISpriteDataProviderFactory<GameObject>
    {
        public ISpriteEditorDataProvider CreateDataProvider(Texture2D obj)
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)) as TextureImporter;
            if (ti != null)
                return new TextureImporterDataProvider(ti);
            return null;
        }

        public ISpriteEditorDataProvider CreateDataProvider(Sprite obj)
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj)) as TextureImporter;
            if (ti != null)
                return new TextureImporterDataProvider(ti);
            return null;
        }

        public ISpriteEditorDataProvider CreateDataProvider(GameObject obj)
        {
            var spriteRenderer = obj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var ti = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteRenderer.sprite)) as TextureImporter;
                if (ti != null)
                    return new TextureImporterDataProvider(ti);
            }
            return null;
        }

        public ISpriteEditorDataProvider CreateDataProvider(TextureImporter obj)
        {
            return new TextureImporterDataProvider(obj);
        }

        [SpriteEditorAssetPathProviderAttribute]
        static string GetAssetPathFromSpriteRenderer(UnityEngine.Object obj)
        {
            var go = obj as GameObject;
            if (go != null)
            {
                var spriteRenderer = go.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                    return AssetDatabase.GetAssetPath(spriteRenderer.sprite);
            }
            return null;
        }

        [SpriteObjectProvider]
        static Sprite GetSpriteObjectFromSpriteRenderer(UnityEngine.Object obj)
        {
            var go = obj as GameObject;
            if (go != null)
            {
                var spriteRenderer = go.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    return spriteRenderer.sprite;
            }
            return null;
        }
    }

    internal class TextureImporterDataProvider : ISpriteEditorDataProvider, ISpriteNameFileIdDataProvider
    {
        TextureImporter m_TextureImporter;
        SpriteNameFileIdPairExt[] m_NameFileIdPairs;
        List<SpriteDataExt> m_SpritesMultiple;
        SpriteDataExt m_SpriteSingle;
        SpriteImportMode m_SpriteImportMode = SpriteImportMode.None;
        SecondarySpriteTexture[] m_SecondaryTextureDataTransfer;
        SerializedObject m_CachedSerializedObject;

        internal TextureImporterDataProvider(TextureImporter importer)
        {
            m_TextureImporter = importer;
            if (m_TextureImporter != null)
            {
                m_SpriteImportMode = m_TextureImporter.spriteImportMode;
                m_CachedSerializedObject = new SerializedObject(m_TextureImporter);
            }
        }

        float ISpriteEditorDataProvider.pixelsPerUnit
        {
            get { return m_TextureImporter.spritePixelsPerUnit; }
        }

        UnityEngine.Object ISpriteEditorDataProvider.targetObject
        {
            get { return m_TextureImporter; }
        }

        public SpriteImportMode spriteImportMode
        {
            get { return m_SpriteImportMode; }
        }

        SpriteRect[] ISpriteEditorDataProvider.GetSpriteRects()
        {
            return spriteImportMode == SpriteImportMode.Multiple ? m_SpritesMultiple.Select(x => new SpriteDataExt(x) as SpriteRect).ToArray() : new[] { new SpriteDataExt(m_SpriteSingle) };
        }

        public SerializedObject GetSerializedObject()
        {
            m_CachedSerializedObject?.UpdateIfRequiredOrScript();
            return m_CachedSerializedObject;
        }

        public string assetPath
        {
            get { return m_TextureImporter.assetPath; }
        }

        public void GetWidthAndHeight(ref int width, ref int height)
        {
            m_TextureImporter.GetWidthAndHeight(ref width, ref height);
        }

        void ISpriteEditorDataProvider.SetSpriteRects(SpriteRect[] spriteRects)
        {
            if (spriteImportMode != SpriteImportMode.Multiple && spriteImportMode != SpriteImportMode.None && spriteRects.Length == 1)
            {
                m_SpriteSingle.CopyFromSpriteRect(spriteRects[0]);
            }
            else if (spriteImportMode == SpriteImportMode.Multiple)
            {
                Dictionary<GUID, SpriteRect> newSprites = new Dictionary<GUID, SpriteRect>();
                foreach (var newSprite in spriteRects)
                {
                    newSprites.Add(newSprite.spriteID, newSprite);
                }
                HashSet<long> internalIdUsed = new HashSet<long>();
                for (int i = m_SpritesMultiple.Count - 1; i >= 0; --i)
                {
                    var spriteID = m_SpritesMultiple[i].spriteID;
                    if (newSprites.TryGetValue(spriteID, out SpriteRect smd))
                    {
                        m_SpritesMultiple[i].CopyFromSpriteRect(smd);
                        internalIdUsed.Add(m_SpritesMultiple[i].internalID);
                        newSprites.Remove(spriteID);
                    }
                    else
                    {
                        m_SpritesMultiple.RemoveAt(i);
                    }
                }
                // Add new ones
                var nameIdTable = GetSerializedNameFileIdTable(GetSerializedObject());
                // First pass map by id
                var values = newSprites.Values.ToArray();
                foreach (var newSprite in values)
                {
                    var newSpriteRect = new SpriteDataExt(newSprite);
                    var nameIdPair = nameIdTable.FirstOrDefault(x => x.GetFileGUID() == newSprite.spriteID);
                    if (nameIdPair != null && !internalIdUsed.Contains(nameIdPair.internalID))
                    {
                        newSpriteRect.internalID = nameIdPair.internalID;
                        internalIdUsed.Add(nameIdPair.internalID);
                        m_SpritesMultiple.Add(newSpriteRect);
                        newSprites.Remove(newSprite.spriteID);
                    }
                }
                //Second pass map by name
                foreach (var newSprite in newSprites.Values)
                {
                    var newSpriteRect = new SpriteDataExt(newSprite);
                    var nameIdPair = nameIdTable.FirstOrDefault(x => x.name == newSprite.name);
                    if (nameIdPair != null && !internalIdUsed.Contains(nameIdPair.internalID))
                        newSpriteRect.internalID = nameIdPair.internalID;

                    internalIdUsed.Add(newSpriteRect.internalID);
                    m_SpritesMultiple.Add(newSpriteRect);
                }
            }
        }

        IEnumerable<SpriteNameFileIdPair> ISpriteNameFileIdDataProvider.GetNameFileIdPairs()
        {
            return m_NameFileIdPairs.ToArray<SpriteNameFileIdPair>();
        }

        void ISpriteNameFileIdDataProvider.SetNameFileIdPairs(IEnumerable<SpriteNameFileIdPair> pairs)
        {
            var newFileIdPair = new SpriteNameFileIdPairExt[pairs.Count()];
            var count = 0;
            foreach (var pair in pairs)
            {
                var guid = pair.GetFileGUID();
                var internalId = (long)guid.GetHashCode();
                //check if guid is in current name id table
                var idPair = m_NameFileIdPairs.FirstOrDefault(x => x.GetFileGUID() == guid);
                if (idPair != null)
                    internalId = idPair.internalID;
                else
                {
                    // check if guid is in current sprite list
                    var sr = m_SpritesMultiple.FirstOrDefault(x => x.spriteID == guid);
                    if (sr != null)
                        internalId = sr.internalID;
                }
                newFileIdPair[count] = new SpriteNameFileIdPairExt(pair.name, guid, internalId);
                count++;
            }

            m_NameFileIdPairs = newFileIdPair;
        }

        internal SpriteRect GetSpriteData(GUID guid)
        {
            return spriteImportMode == SpriteImportMode.Multiple ? m_SpritesMultiple.FirstOrDefault(x => x.spriteID == guid) : m_SpriteSingle;
        }

        internal int GetSpriteDataIndex(GUID guid)
        {
            switch (spriteImportMode)
            {
                case SpriteImportMode.Single:
                case SpriteImportMode.Polygon:
                    return 0;
                case SpriteImportMode.Multiple:
                {
                    return m_SpritesMultiple.FindIndex(x => x.spriteID == guid);
                }
                default:
                    throw new InvalidOperationException(string.Format("Sprite with GUID {0} not found", guid));
            }
        }

        void ISpriteEditorDataProvider.Apply()
        {
            var so = GetSerializedObject();
            m_SpriteSingle.Apply(so);
            var spriteSheetSO = so.FindProperty("m_SpriteSheet.m_Sprites");
            Dictionary<GUID, SpriteDataExt> newSprites = new Dictionary<GUID, SpriteDataExt>();
            foreach (var newSprite in m_SpritesMultiple)
            {
                newSprites.Add(newSprite.spriteID, newSprite);
            }

            if (spriteSheetSO.arraySize > 0)
            {
                var validateCurrent = spriteSheetSO.GetArrayElementAtIndex(0);
                for (int i = 0; i < spriteSheetSO.arraySize; ++i)
                {
                    var guid = SpriteRect.GetSpriteIDFromSerializedProperty(validateCurrent);
                    // find the GUID in our sprite list and apply to it;
                    if (newSprites.TryGetValue(guid, out SpriteDataExt smd))
                    {
                        smd.Apply(validateCurrent);
                        newSprites.Remove(guid);
                        validateCurrent.Next(false);
                    }
                    else// we can't find it, it is already deleted
                    {
                        spriteSheetSO.DeleteArrayElementAtIndex(i);
                        --i;
                    }
                }
            }

            // Add new ones
            int newCount = newSprites.Count();
            if (newCount > 0)
            {
                var idx = spriteSheetSO.arraySize;
                spriteSheetSO.arraySize += newCount;
                var addCurrent = spriteSheetSO.GetArrayElementAtIndex(idx);
                foreach (var newSprite in newSprites.Values)
                {
                    newSprite.Apply(addCurrent);
                    addCurrent.Next(false);
                }
            }

            var noOfPairs = m_NameFileIdPairs.Length;
            var pairsSo = so.FindProperty("m_SpriteSheet.m_NameFileIdTable");
            pairsSo.arraySize = noOfPairs;
            if (noOfPairs > 0)
            {
                var element = pairsSo.GetArrayElementAtIndex(0);
                foreach (var pair in m_NameFileIdPairs)
                {
                    pair.Apply(element);
                    element.Next(false);
                }
            }

            SpriteSecondaryTextureDataTransfer.Apply(so, m_SecondaryTextureDataTransfer);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        void ISpriteEditorDataProvider.InitSpriteEditorDataProvider()
        {
            var so = GetSerializedObject();

            var spriteSheetSo = so.FindProperty("m_SpriteSheet.m_Sprites");
            m_SpritesMultiple = new List<SpriteDataExt>();
            m_SpriteSingle = new SpriteDataExt(so);

            if (spriteSheetSo.arraySize > 0)
            {
                var sp = spriteSheetSo.GetArrayElementAtIndex(0);
                for (int i = 0; i < spriteSheetSo.arraySize; ++i)
                {
                    var data = new SpriteDataExt(sp);
                    m_SpritesMultiple.Add(data);
                    sp.Next(false);
                }
            }
            m_SecondaryTextureDataTransfer = SpriteSecondaryTextureDataTransfer.Load(so);

            m_NameFileIdPairs = GetSerializedNameFileIdTable(so);
        }

        SpriteNameFileIdPairExt[] GetSerializedNameFileIdTable(SerializedObject so)
        {
            var nameFileIdTableSo = so.FindProperty("m_SpriteSheet.m_NameFileIdTable");
            var arraySize = nameFileIdTableSo.arraySize;
            var nameFileIdPairs = new SpriteNameFileIdPairExt[arraySize];
            if (nameFileIdPairs.Length > 0)
            {
                var sp = nameFileIdTableSo.GetArrayElementAtIndex(0);
                for (var i = 0; i < nameFileIdTableSo.arraySize; ++i)
                {
                    var spriteNameFileId = SpriteNameFileIdPairExt.GetValue(sp);
                    // check if this internal nid is already in one of the sprite.
                    // We don't check name as changing internal id can cause reference to be lost
                    var spriteRect = m_SpritesMultiple.FirstOrDefault(x => x.internalID == spriteNameFileId.internalID);
                    if (spriteRect != null)
                        spriteNameFileId.SetFileGUID(spriteRect.spriteID);
                    nameFileIdPairs[i] = spriteNameFileId;
                    sp.Next(false);
                }
            }

            return nameFileIdPairs;
        }

        T ISpriteEditorDataProvider.GetDataProvider<T>()
        {
            if (typeof(T) == typeof(ISpriteBoneDataProvider))
            {
                return new SpriteBoneDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ISpriteMeshDataProvider))
            {
                return new SpriteMeshDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ISpriteOutlineDataProvider))
            {
                return new SpriteOutlineDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ISpritePhysicsOutlineDataProvider))
            {
                return new SpritePhysicsOutlineDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ITextureDataProvider))
            {
                return new SpriteTextureDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ISecondaryTextureDataProvider))
            {
                return new SpriteSecondaryTextureDataTransfer(this) as T;
            }
            else
                return this as T;
        }

        bool ISpriteEditorDataProvider.HasDataProvider(Type type)
        {
            if (type == typeof(ISpriteBoneDataProvider) ||
                type == typeof(ISpriteMeshDataProvider) ||
                type == typeof(ISpriteOutlineDataProvider) ||
                type == typeof(ISpritePhysicsOutlineDataProvider) ||
                type == typeof(ITextureDataProvider) ||
                type == typeof(ISecondaryTextureDataProvider))
            {
                return true;
            }
            else
                return type.IsAssignableFrom(GetType());
        }

        public override bool Equals(object a)
        {
            return this == (a as TextureImporterDataProvider);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator!=(TextureImporterDataProvider t1, TextureImporterDataProvider t2)
        {
            return !(t1 == t2);
        }

        public static bool operator==(TextureImporterDataProvider t1, TextureImporterDataProvider t2)
        {
            if (ReferenceEquals(null, t1) && (!ReferenceEquals(null, t2) && t2.m_TextureImporter == null) ||
                ReferenceEquals(null, t2) && (!ReferenceEquals(null, t1) && t1.m_TextureImporter == null))
                return true;

            if (!ReferenceEquals(null, t1) && !ReferenceEquals(null, t2))
            {
                if (t1.m_TextureImporter == null && t2.m_TextureImporter == null ||
                    t1.m_TextureImporter == t2.m_TextureImporter)
                    return true;
            }
            if (ReferenceEquals(t1, t2))
                return true;

            return false;
        }

        public SecondarySpriteTexture[] secondaryTextures
        {
            get { return m_SecondaryTextureDataTransfer; }
            set { m_SecondaryTextureDataTransfer = value; }
        }
    }
}
