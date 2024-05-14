/*
* Copyright (c) CookApps.
* 이진호(jhlee8@cookapps.com)
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// SpriteAtlas 원본 Sprite Uncompressed 설정 및 Platform 설정 초기화
/// </summary>
internal class AtlasImportPostprocessor : AssetPostprocessor
{
    public override int GetPostprocessOrder() => int.MaxValue;

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var list = importedAssets.Where(c => c.Contains(".spriteatlas", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (list.Length <= 0)
            return;

        ProcessAtlas(list);
    }

    private static void ProcessAtlas(IEnumerable<string> list)
    {
        foreach (var path in list)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
                continue;

            if (atlas.spriteCount == 0)
                continue;

            var serializedObject = new SerializedObject(atlas);
            var sprites = serializedObject.FindProperty("m_PackedSprites");
            foreach (SerializedProperty sprite in sprites)
            {
                var s = sprite.objectReferenceValue as Sprite;
                if (s == null)
                    continue;
                NonCompress(s);
            }
        }
    }

    private static readonly string[] Platform = {"Standalone", "iPhone", "Android"};
    private static void NonCompress(Sprite sprite)
    {
        var path = AssetDatabase.GetAssetPath(sprite);
        var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if(textureImporter == null)
            return;

        var dirty = false;
        foreach (var platform in Platform)
        {
            var settings = textureImporter.GetPlatformTextureSettings(platform);
            if(settings is not { overridden: true })
                continue;

            textureImporter.ClearPlatformTextureSettings(platform);
            dirty = true;
        }

        if (textureImporter.textureCompression != TextureImporterCompression.Uncompressed)
        {
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }

        if (dirty)
        {
            textureImporter.SaveAndReimport();
        }
    }
}