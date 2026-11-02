using TMPro;
using UnityEngine;

public class SkillTooltipStageView : MonoBehaviour
{
    [NotNull][SerializeField] private TMP_Text _headerText;
    [NotNull][SerializeField] private TMP_Text _nameText;
    [NotNull][SerializeField] private TMP_Text _descriptionText;

    public void Show(in SkillTooltipStage stage)
    {
        gameObject.SetActive(true);
        _headerText.text = stage.Header;
        _nameText.text = stage.Name;
        _descriptionText.text = stage.Description;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
