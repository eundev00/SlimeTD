using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TmpFontTrimmer
{
    private const string FontFolder = "Assets/Fonts/TMP";
    private const string CharsetPath = "Doc/korean_charset.txt";
    private const int MinAtlasSize = 512;
    private const int MaxAtlasSize = 4096;

    [MenuItem("SlimeTD/Data/Trim TMP Fonts To Used Characters")]
    public static void Trim()
    {
        KoreanCharsetBuilder.Build();

        string charsetFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", CharsetPath));
        string charset = File.ReadAllText(charsetFullPath);

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { FontFolder });
        if (guids.Length == 0)
        {
            Debug.Log($"[TmpFontTrimmer] 폰트 에셋을 찾지 못했습니다: {FontFolder}");
            return;
        }

        if (!EditorUtility.DisplayDialog("TMP 폰트 정리",
                $"{FontFolder}의 폰트 에셋 {guids.Length}개를 사용 글자({charset.Length}자)만 남기고 다시 생성합니다.\n되돌릴 수 없으니 먼저 커밋하세요.",
                "진행", "취소"))
            return;

        Debug.Log($"[TmpFontTrimmer] 시작: 에셋 {guids.Length}개, 문자셋 {charset.Length}자");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fontAsset == null)
            {
                Debug.Log($"[TmpFontTrimmer] 폰트 에셋으로 로드하지 못했습니다: {path}");
                continue;
            }

            try
            {
                TrimFont(fontAsset, path, charset);
            }
            catch (System.Exception e)
            {
                Debug.Log($"[TmpFontTrimmer] 처리 중 예외로 건너뜁니다: {path}\n{e}", fontAsset);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TmpFontTrimmer] 완료");
    }

    private static void TrimFont(TMP_FontAsset fontAsset, string path, string charset)
    {
        var serialized = new SerializedObject(fontAsset);

        // 정적 에셋은 런타임 소스 폰트 참조가 비어 있어 GUID로 직접 찾는다
        string sourceGuid = serialized.FindProperty("m_SourceFontFileGUID").stringValue;
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(sourceGuid));
        if (sourceFont == null)
        {
            Debug.Log($"[TmpFontTrimmer] 원본 폰트를 찾지 못해 건너뜁니다: {path}", fontAsset);
            return;
        }

        if (HasExactCharacters(fontAsset, charset))
        {
            Debug.Log($"[TmpFontTrimmer] 이미 정리된 에셋이라 건너뜁니다: {path} ({fontAsset.characterTable.Count}자)", fontAsset);
            return;
        }

        int before = fontAsset.characterTable.Count;
        int atlasSize = EstimateAtlasSize(charset.Length, fontAsset);

        serialized.FindProperty("m_SourceFontFile").objectReferenceValue = sourceFont;
        serialized.FindProperty("m_AtlasWidth").intValue = atlasSize;
        serialized.FindProperty("m_AtlasHeight").intValue = atlasSize;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 정적 에셋에는 글자를 추가할 수 없어 Dynamic으로 바꿔 채운 뒤 되돌린다
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.ClearFontAssetData(true);
        fontAsset.TryAddCharacters(charset, out string missing, true);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        serialized.Update();
        serialized.FindProperty("m_SourceFontFile").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (fontAsset.material != null)
        {
            fontAsset.material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasSize);
            fontAsset.material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasSize);
        }

        EditorUtility.SetDirty(fontAsset);
        if (fontAsset.material != null)
            EditorUtility.SetDirty(fontAsset.material);

        foreach (Texture2D atlas in fontAsset.atlasTextures)
        {
            if (atlas != null)
                EditorUtility.SetDirty(atlas);
        }

        string missingInfo = string.IsNullOrEmpty(missing) ? string.Empty : $", 누락 {missing.Length}자: {missing}";
        Debug.Log($"[TmpFontTrimmer] {path}: {before}자 -> {fontAsset.characterTable.Count}자, 아틀라스 {atlasSize}{missingInfo}", fontAsset);
    }

    private static bool HasExactCharacters(TMP_FontAsset fontAsset, string charset)
    {
        var existing = new HashSet<uint>(fontAsset.characterTable.Select(c => c.unicode));
        var wanted = new HashSet<uint>(charset.Select(c => (uint)c));
        return existing.SetEquals(wanted);
    }

    // 글리프 한 칸 = 포인트 크기 + 양쪽 패딩, 배치 손실을 15% 잡는다
    private static int EstimateAtlasSize(int glyphCount, TMP_FontAsset fontAsset)
    {
        float cell = fontAsset.faceInfo.pointSize + fontAsset.atlasPadding * 2;
        float side = Mathf.Sqrt(glyphCount) * cell * 1.15f;

        int size = MinAtlasSize;
        while (size < side && size < MaxAtlasSize)
            size *= 2;

        return size;
    }
}
