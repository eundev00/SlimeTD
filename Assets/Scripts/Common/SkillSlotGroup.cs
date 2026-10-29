using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

// 헌터 상세 팝업과 인게임 타워 정보가 같은 규칙으로 스킬 슬롯을 그리도록 표시 규칙을 여기에 모은다.
public class SkillSlotGroup : MonoBehaviour
{
    [SerializeField] private SkillSlotItem[] _slots;

    private readonly List<SkillSlot> _buffer = new List<SkillSlot>();
    private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
    private SpriteAtlas _atlas;

    // 레벨 0을 넘기면 모든 슬롯이 잠김으로 보인다 (보유하지 않은 헌터).
    public void Show(SkillCatalog catalog, SpriteAtlas atlas, int towerId, int level)
    {
        _buffer.Clear();
        if (catalog != null)
            catalog.GetSlots(towerId, level, _buffer);

        if (_buffer.Count > _slots.Length)
            Debug.Log($"[SkillSlotGroup] 스킬 계열이 {_buffer.Count}개인데 슬롯은 {_slots.Length}개입니다. 슬롯을 추가하세요. 타워: {towerId}", this);

        SetAtlas(atlas);

        for (int i = 0; i < _slots.Length; i++)
        {
            if (i >= _buffer.Count)
            {
                _slots[i].Hide();
                continue;
            }

            var slot = _buffer[i];
            var shown = slot.Current ?? slot.Root;

            _slots[i].Show(GetSprite(shown.IconName), slot.IsLocked, slot.HasNextUpgrade);
        }
    }

    private void SetAtlas(SpriteAtlas atlas)
    {
        if (_atlas == atlas)
            return;

        ClearSprites();
        _atlas = atlas;
    }

    // SpriteAtlas.GetSprite는 부를 때마다 스프라이트를 새로 복제하므로 이름별로 한 번만 받아 둔다.
    private Sprite GetSprite(string spriteName)
    {
        if (_atlas == null)
            return null;

        if (_sprites.TryGetValue(spriteName, out var sprite))
            return sprite;

        sprite = _atlas.GetSprite(spriteName);
        if (sprite == null)
            Debug.Log($"[SkillSlotGroup] 아틀라스에 스킬 아이콘이 없습니다: {spriteName}", this);

        _sprites[spriteName] = sprite;
        return sprite;
    }

    private void ClearSprites()
    {
        foreach (var sprite in _sprites.Values)
        {
            if (sprite != null)
                Destroy(sprite);
        }

        _sprites.Clear();
    }

    private void OnDestroy()
    {
        ClearSprites();
    }
}
