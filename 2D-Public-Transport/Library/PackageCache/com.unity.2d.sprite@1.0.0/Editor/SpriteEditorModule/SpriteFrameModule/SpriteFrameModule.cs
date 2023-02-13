using System;
using System.IO;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Text;
using UnityTexture2D = UnityEngine.Texture2D;
using UnityEditor.ShortcutManagement;

namespace UnityEditor.U2D.Sprites
{
    [RequireSpriteDataProvider(typeof(ITextureDataProvider))]
    internal partial class SpriteFrameModule : SpriteFrameModuleBase
    {
        public enum AutoSlicingMethod
        {
            DeleteAll = 0,
            Smart = 1,
            Safe = 2
        }

        private bool[] m_AlphaPixelCache;
        SpriteFrameModuleContext m_SpriteFrameModuleContext;

        private const float kOverlapTolerance = 0.00001f;
        private StringBuilder m_SpriteNameStringBuilder;

        private List<Rect> m_PotentialRects;
        public List<Rect> potentialRects
        {
            set => m_PotentialRects = value;
        }

        public SpriteFrameModule(ISpriteEditor sw, IEventSystem es, IUndoSystem us, IAssetDatabase ad) :
            base("Sprite Editor", sw, es, us, ad)
        {}

        class SpriteFrameModuleContext : IShortcutToolContext
        {
            SpriteFrameModule m_SpriteFrameModule;

            public SpriteFrameModuleContext(SpriteFrameModule spriteFrame)
            {
                m_SpriteFrameModule = spriteFrame;
            }

            public bool active
            {
                get { return true; }
            }
            public SpriteFrameModule spriteFrameModule
            {
                get { return m_SpriteFrameModule; }
            }
        }

        [FormerlyPrefKeyAs("Sprite Editor/Trim", "#t")]
        [Shortcut("Sprite Editor/Trim", typeof(SpriteFrameModuleContext), KeyCode.T, ShortcutModifiers.Shift)]
        static void ShortcutTrim(ShortcutArguments args)
        {
            if (!string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
                return;
            var spriteFrameContext = (SpriteFrameModuleContext)args.context;
            spriteFrameContext.spriteFrameModule.TrimAlpha();
            spriteFrameContext.spriteFrameModule.spriteEditor.RequestRepaint();
        }

        public override void OnModuleActivate()
        {
            base.OnModuleActivate();
            spriteEditor.enableMouseMoveEvent = true;
            m_SpriteFrameModuleContext = new SpriteFrameModuleContext(this);
            ShortcutIntegration.instance.contextManager.RegisterToolContext(m_SpriteFrameModuleContext);
            m_SpriteNameStringBuilder = new StringBuilder(GetSpriteNamePrefix() + "_");
            m_PotentialRects = null;
        }

        public override void OnModuleDeactivate()
        {
            base.OnModuleDeactivate();
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_SpriteFrameModuleContext);
            m_PotentialRects = null;
            m_AlphaPixelCache = null;
        }

        public static SpriteImportMode GetSpriteImportMode(ISpriteEditorDataProvider dataProvider)
        {
            return dataProvider == null ? SpriteImportMode.None : dataProvider.spriteImportMode;
        }

        public override bool CanBeActivated()
        {
            return GetSpriteImportMode(spriteEditor.GetDataProvider<ISpriteEditorDataProvider>()) != SpriteImportMode.Polygon;
        }

        private string GenerateSpriteNameWithIndex(int startIndex)
        {
            int originalLength = m_SpriteNameStringBuilder.Length;
            m_SpriteNameStringBuilder.Append(startIndex);
            var name = m_SpriteNameStringBuilder.ToString();
            m_SpriteNameStringBuilder.Length = originalLength;
            return name;
        }

        // 1. Find top-most rectangle
        // 2. Sweep it vertically to find out all rects from that "row"
        // 3. goto 1.
        // This will give us nicely sorted left->right top->down list of rectangles
        // Works for most sprite sheets pretty nicely
        private List<Rect> SortRects(List<Rect> rects)
        {
            List<Rect> result = new List<Rect>();

            while (rects.Count > 0)
            {
                // Because the slicing algorithm works from bottom-up, the topmost rect is the last one in the array
                Rect r = rects[rects.Count - 1];
                Rect sweepRect = new Rect(0, r.yMin, textureActualWidth, r.height);

                List<Rect> rowRects = RectSweep(rects, sweepRect);

                if (rowRects.Count > 0)
                    result.AddRange(rowRects);
                else
                {
                    // We didn't find any rects, just dump the remaining rects and continue
                    result.AddRange(rects);
                    break;
                }
            }
            return result;
        }

        private List<Rect> RectSweep(List<Rect> rects, Rect sweepRect)
        {
            if (rects == null || rects.Count == 0)
                return new List<Rect>();

            List<Rect> containedRects = new List<Rect>();

            foreach (Rect rect in rects)
            {
                if (rect.Overlaps(sweepRect))
                    containedRects.Add(rect);
            }

            // Remove found rects from original list
            foreach (Rect rect in containedRects)
                rects.Remove(rect);

            // Sort found rects by x position
            containedRects.Sort((a, b) => a.x.CompareTo(b.x));

            return containedRects;
        }

        private int AddSprite(Rect frame, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod, int originalCount, ref int nameIndex)
        {
            int outSprite = -1;
            switch (slicingMethod)
            {
                case AutoSlicingMethod.DeleteAll:
                {
                    while (outSprite == -1)
                    {
                        outSprite = AddSprite(frame, alignment, pivot, GenerateSpriteNameWithIndex(nameIndex++), Vector4.zero);
                    }
                }
                break;
                case AutoSlicingMethod.Smart:
                {
                    outSprite = GetExistingOverlappingSprite(frame, originalCount, true);
                    if (outSprite != -1)
                    {
                        var existingRect = m_RectsCache.spriteRects[outSprite];
                        existingRect.rect = frame;
                        existingRect.alignment = (SpriteAlignment)alignment;
                        existingRect.pivot = pivot;
                    }
                    else
                    {
                        while (outSprite == -1)
                        {
                            outSprite = AddSprite(frame, alignment, pivot, GenerateSpriteNameWithIndex(nameIndex++), Vector4.zero);
                        }
                    }
                }
                break;
                case AutoSlicingMethod.Safe:
                {
                    outSprite = GetExistingOverlappingSprite(frame, originalCount);
                    while (outSprite == -1)
                    {
                        outSprite = AddSprite(frame, alignment, pivot, GenerateSpriteNameWithIndex(nameIndex++), Vector4.zero);
                    }
                }
                break;
            }
            return outSprite;
        }

        private int GetExistingOverlappingSprite(Rect rect, int originalCount, bool bestFit = false)
        {
            var count = Math.Min(originalCount, m_RectsCache.spriteRects.Count);
            int bestRect = -1;
            float rectArea = rect.width * rect.height;
            if (rectArea < kOverlapTolerance)
                return bestRect;

            float bestRatio = float.MaxValue;
            float bestArea = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Rect existingRect = m_RectsCache.spriteRects[i].rect;
                if (existingRect.Overlaps(rect))
                {
                    if (bestFit)
                    {
                        float dx = Math.Min(rect.xMax, existingRect.xMax) - Math.Max(rect.xMin, existingRect.xMin);
                        float dy = Math.Min(rect.yMax, existingRect.yMax) - Math.Max(rect.yMin, existingRect.yMin);
                        float overlapArea = dx * dy;
                        float overlapRatio = Math.Abs((overlapArea / rectArea) - 1.0f);
                        float existingArea = existingRect.width * existingRect.height;
                        if (overlapRatio < bestRatio || (overlapRatio < kOverlapTolerance && existingArea < bestArea))
                        {
                            bestRatio = overlapRatio;
                            if (overlapRatio < kOverlapTolerance)
                                bestArea = existingArea;
                            bestRect = i;
                        }
                    }
                    else
                    {
                        bestRect = i;
                        break;
                    }
                }
            }
            return bestRect;
        }

        private bool PixelHasAlpha(int x, int y, UnityTexture2D texture)
        {
            if (m_AlphaPixelCache == null)
            {
                m_AlphaPixelCache = new bool[texture.width * texture.height];
                Color32[] pixels = texture.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                    m_AlphaPixelCache[i] = pixels[i].a != 0;
            }
            int index = y * (int)texture.width + x;
            return m_AlphaPixelCache[index];
        }

        private int AddSprite(Rect rect, int alignment, Vector2 pivot, string name, Vector4 border)
        {
            if (m_RectsCache.IsNameUsed(name))
                return -1;

            SpriteRect spriteRect = new SpriteRect();
            spriteRect.rect = rect;
            spriteRect.alignment = (SpriteAlignment)alignment;
            spriteRect.pivot = pivot;
            spriteRect.name = name;
            spriteRect.originalName = spriteRect.name;
            spriteRect.border = border;

            spriteRect.spriteID = GUID.Generate();

            m_RectsCache.Add(spriteRect);
            spriteEditor.SetDataModified();

            return m_RectsCache.spriteRects.Count - 1;
        }

        private string GetSpriteNamePrefix()
        {
            return Path.GetFileNameWithoutExtension(spriteAssetPath);
        }

        public void DoAutomaticSlicing(int minimumSpriteSize, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod)
        {
            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Automatic Slicing");

            if (slicingMethod == AutoSlicingMethod.DeleteAll)
                m_RectsCache.Clear();

            var textureToUse = GetTextureToSlice();
            List<Rect> frames = new List<Rect>(InternalSpriteUtility.GenerateAutomaticSpriteRectangles((UnityTexture2D)textureToUse, minimumSpriteSize, 0));
            frames = SortRects(frames);
            int index = 0;
            int originalCount = m_RectsCache.spriteRects.Count;

            foreach (Rect frame in frames)
                AddSprite(frame, alignment, pivot, slicingMethod, originalCount, ref index);

            if (slicingMethod == AutoSlicingMethod.DeleteAll)
                m_RectsCache.ClearUnusedFileID();
            selected = null;
            spriteEditor.SetDataModified();
            Repaint();
        }

        UnityTexture2D GetTextureToSlice()
        {
            int width, height;
            m_TextureDataProvider.GetTextureActualWidthAndHeight(out width, out height);
            var readableTexture = m_TextureDataProvider.GetReadableTexture2D();
            if (readableTexture == null || (readableTexture.width == width && readableTexture.height == height))
                return readableTexture;
            // we want to slice based on the original texture slice. Upscale the imported texture
            var texture = UnityEditor.SpriteUtility.CreateTemporaryDuplicate(readableTexture, width, height);
            return texture;
        }

        public IEnumerable<Rect> GetGridRects(Vector2 size, Vector2 offset, Vector2 padding, bool keepEmptyRects)
        {
            var textureToUse = GetTextureToSlice();
            return InternalSpriteUtility.GenerateGridSpriteRectangles((UnityTexture2D)textureToUse, offset, size, padding, keepEmptyRects);
        }

        public void DoGridSlicing(Vector2 size, Vector2 offset, Vector2 padding, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod, bool keepEmptyRects = false)
        {
            var frames = GetGridRects(size, offset, padding, keepEmptyRects);

            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Grid Slicing");
            if (slicingMethod == AutoSlicingMethod.DeleteAll)
                m_RectsCache.Clear();

            int index = 0;
            int originalCount = m_RectsCache.spriteRects.Count;
            foreach (Rect frame in frames)
                AddSprite(frame, alignment, pivot, slicingMethod, originalCount, ref index);

            if (slicingMethod == AutoSlicingMethod.DeleteAll)
                m_RectsCache.ClearUnusedFileID();
            selected = null;
            spriteEditor.SetDataModified();
            Repaint();
        }

        public IEnumerable<Rect> GetIsometricRects(Vector2 size, Vector2 offset, bool isAlternate, bool keepEmptyRects)
        {
            var textureToUse = GetTextureToSlice();
            var gradient = (size.x / 2) / (size.y / 2);
            bool isAlt = isAlternate;
            float x = offset.x;
            if (isAlt)
                x += size.x / 2;
            float y = textureToUse.height - offset.y;
            while (y - size.y >= 0)
            {
                while (x + size.x <= textureToUse.width)
                {
                    var rect = new Rect(x, y - size.y, size.x, size.y);
                    if (!keepEmptyRects)
                    {
                        int sx = (int)rect.x;
                        int sy = (int)rect.y;
                        int width = (int)size.x;
                        int odd = ((int)size.y) % 2;
                        int topY = ((int)size.y / 2) - 1;
                        int bottomY = topY + odd;
                        int totalPixels = 0;
                        int alphaPixels = 0;
                        {
                            for (int ry = 0; ry <= topY; ry++)
                            {
                                var pixelOffset = Mathf.CeilToInt(gradient * ry);
                                for (int rx = pixelOffset; rx < width - pixelOffset; ++rx)
                                {
                                    if (PixelHasAlpha(sx + rx, sy + topY - ry, textureToUse))
                                        alphaPixels++;
                                    if (PixelHasAlpha(sx + rx, sy + bottomY + ry, textureToUse))
                                        alphaPixels++;
                                    totalPixels += 2;
                                }
                            }
                        }
                        if (odd > 0)
                        {
                            int ry = topY + 1;
                            for (int rx = 0; rx < size.x; ++rx)
                            {
                                if (PixelHasAlpha(sx + rx, sy + ry, textureToUse))
                                    alphaPixels++;
                                totalPixels++;
                            }
                        }
                        if (totalPixels > 0 && ((float)alphaPixels) / totalPixels > 0.01f)
                            yield return rect;
                    }
                    else
                        yield return rect;
                    x += size.x;
                }
                isAlt = !isAlt;
                x = offset.x;
                if (isAlt)
                    x += size.x / 2;
                y -= size.y / 2;
            }
        }

        public void DoIsometricGridSlicing(Vector2 size, Vector2 offset, int alignment, Vector2 pivot, AutoSlicingMethod slicingMethod, bool keepEmptyRects = false, bool isAlternate = false)
        {
            var frames = GetIsometricRects(size, offset, isAlternate, keepEmptyRects);

            List<Vector2[]> outlines = new List<Vector2[]>(4);
            outlines.Add(new[] { new Vector2(0.0f, -size.y / 2)
                                 , new Vector2(size.x / 2, 0.0f)
                                 , new Vector2(0.0f, size.y / 2)
                                 , new Vector2(-size.x / 2, 0.0f)});

            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Isometric Grid Slicing");
            if (slicingMethod == AutoSlicingMethod.DeleteAll)
                m_RectsCache.Clear();

            int index = 0;
            var spriteRects = m_RectsCache.GetSpriteRects();
            int originalCount = spriteRects.Count;
            foreach (var frame in frames)
            {
                var spriteIndex = AddSprite(frame, alignment, pivot, slicingMethod, originalCount, ref index);
                var outlineRect = new OutlineSpriteRect(spriteRects[spriteIndex]);
                outlineRect.outlines = outlines;
                spriteRects[spriteIndex] = outlineRect;
            }
            if (slicingMethod == AutoSlicingMethod.DeleteAll)
                m_RectsCache.ClearUnusedFileID();
            selected = null;
            spriteEditor.SetDataModified();
            Repaint();
        }

        public void ScaleSpriteRect(Rect r)
        {
            if (selected != null)
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Scale sprite");
                selected.rect = ClampSpriteRect(r, textureActualWidth, textureActualHeight);
                selected.border = ClampSpriteBorderToRect(selected.border, selected.rect);
                spriteEditor.SetDataModified();
            }
        }

        public void TrimAlpha()
        {
            var texture = GetTextureToSlice();
            if (texture == null)
                return;

            Rect rect = selected.rect;

            int xMin = (int)rect.xMax;
            int xMax = (int)rect.xMin;
            int yMin = (int)rect.yMax;
            int yMax = (int)rect.yMin;

            for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
            {
                for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
                {
                    if (PixelHasAlpha(x, y, texture))
                    {
                        xMin = Mathf.Min(xMin, x);
                        xMax = Mathf.Max(xMax, x);
                        yMin = Mathf.Min(yMin, y);
                        yMax = Mathf.Max(yMax, y);
                    }
                }
            }
            // Case 582309: Return an empty rectangle if no pixel has an alpha
            if (xMin > xMax || yMin > yMax)
                rect = new Rect(0, 0, 0, 0);
            else
                rect = new Rect(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);

            if (rect.width <= 0 && rect.height <= 0)
            {
                m_RectsCache.Remove(selected);
                spriteEditor.SetDataModified();
                selected = null;
            }
            else
            {
                rect = ClampSpriteRect(rect, texture.width, texture.height);
                if (selected.rect != rect)
                    spriteEditor.SetDataModified();

                selected.rect = rect;
                PopulateSpriteFrameInspectorField();
            }
        }

        public void DuplicateSprite()
        {
            if (selected != null)
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Duplicate sprite");
                var index = 0;
                var createdIndex = -1;
                while (createdIndex == -1)
                {
                    createdIndex = AddSprite(selected.rect, (int)selected.alignment, selected.pivot, GenerateSpriteNameWithIndex(index++), selected.border);
                }
                selected = m_RectsCache.spriteRects[createdIndex];
            }
        }

        public void CreateSprite(Rect rect)
        {
            rect = ClampSpriteRect(rect, textureActualWidth, textureActualHeight);
            undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Create sprite");
            var index = 0;
            var createdIndex = -1;
            while (createdIndex == -1)
            {
                createdIndex = AddSprite(rect, 0, Vector2.zero, GenerateSpriteNameWithIndex(index++), Vector4.zero);
            }
            selected = m_RectsCache.spriteRects[createdIndex];
        }

        public void DeleteSprite()
        {
            if (selected != null)
            {
                undoSystem.RegisterCompleteObjectUndo(m_RectsCache, "Delete sprite");
                m_RectsCache.Remove(selected);
                selected = null;
                spriteEditor.SetDataModified();
            }
        }
    }
}
