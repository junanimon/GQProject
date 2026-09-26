using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuildProto
{
    // 밤 상점 (팝업): 길드 자금으로 편의 시설을 산다
    public class ShopView : MonoBehaviour
    {
        public GameObject panel;
        public RectTransform list;
        public ListRowView rowPrefab;
        public Text fundsText;
        public Text descriptionText;
        public Button buyButton;
        public Text buyLabel;
        public Button closeButton;

        FacilityShop shop;
        Guild guild;
        int week;
        Facility selected;
        Action changed;

        void Awake()
        {
            buyButton.onClick.AddListener(Buy);
            closeButton.onClick.AddListener(() => panel.SetActive(false));
        }

        public void Open(FacilityShop facilityShop, Guild g, int currentWeek, Action onChanged)
        {
            shop = facilityShop;
            guild = g;
            week = currentWeek;
            changed = onChanged;
            selected = null;
            panel.SetActive(true);
            Refresh();
        }

        void Refresh()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            foreach (var f in shop.Catalog)
            {
                if (!shop.IsOnSale(f, week)) continue;
                var row = Instantiate(rowPrefab, list);
                string owned = f.maxCount > 1 ? $" ({shop.Count(f.kind)}/{f.maxCount})" : "";
                string side = shop.SoldOut(f) ? "<color=#2f7a3e>보유</color>" : $"{f.price}G";
                row.Bind(f.name + owned + (f.weekly ? " <color=#7a6a5a>· 이번 주</color>" : ""), f.description, side, () => Select(f));
                row.SetState(f == selected, true);
            }
            fundsText.text = $"길드 자금 <b>{guild.Funds}G</b>";
            if (selected == null)
            {
                descriptionText.text = "물건을 고르세요.";
                buyButton.interactable = false;
                buyLabel.text = "구입";
                return;
            }
            descriptionText.text = $"<b>{selected.name}</b>\n{selected.description}";
            buyButton.interactable = shop.CanBuy(selected, week, guild);
            buyLabel.text = shop.SoldOut(selected) ? "보유 중" : guild.Funds < selected.price ? "자금 부족" : $"구입 ({selected.price}G)";
        }

        void Select(Facility f)
        {
            selected = f;
            AudioHub.Play(Sfx.Paper);
            Refresh();
        }

        void Buy()
        {
            if (selected == null || !shop.CanBuy(selected, week, guild)) return;
            shop.Buy(selected, guild);
            AudioHub.Play(Sfx.Coin);
            changed?.Invoke();
            Refresh();
        }
    }
}
