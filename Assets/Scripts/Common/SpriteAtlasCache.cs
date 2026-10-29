using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

// SpriteAtlas.GetSprite는 부를 때마다 스프라이트를 새로 복제하므로 이름별로 한 번만 받아 둔다.
public class SpriteAtlasCache
{
    private readonly SpriteAtlas _atlas;
    private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    public SpriteAtlasCache(SpriteAtlas atlas)
    {
        _atlas = atlas;
    }

    public Sprite GetSprite(string spriteName)
    {
        if (_atlas == null)
            return null;

        if (_sprites.TryGetValue(spriteName, out var sprite))
            return sprite;

        sprite = _atlas.GetSprite(spriteName);
        if (sprite == null)
            Debug.Log($"[SpriteAtlasCache] 아틀라스에 스프라이트가 없습니다: {spriteName}");

        _sprites[spriteName] = sprite;
        return sprite;
    }
}
