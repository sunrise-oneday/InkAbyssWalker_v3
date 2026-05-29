using TMPro;
using UnityEngine;

namespace StoreAndInventory
{
    public class AttributeStatLineUI : MonoBehaviour
    {
        [SerializeField] StatType statType;
        [SerializeField] TextMeshProUGUI valueText;

        public StatType StatType => statType;

        public void Bind(float baseValue)
        {
            Bind(baseValue, 0f);
        }

        public void Bind(float baseValue, float equipmentBonus)
        {
            if (valueText == null) return;
            var label = StatDisplayUtil.Label(statType);
            var effective = baseValue + equipmentBonus;
            var effectiveText = StatDisplayUtil.FormatValue(statType, effective);

            if (Mathf.Approximately(equipmentBonus, 0f))
            {
                valueText.text = $"{label}: {effectiveText}";
                return;
            }

            var bonusText = StatDisplayUtil.FormatValue(statType, equipmentBonus);
            var sign = equipmentBonus > 0f ? "+" : string.Empty;
            valueText.text = $"{label}: {effectiveText} ({sign}{bonusText})";
        }
    }
}
