using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KeyboardEscapeGameplay
{
    public sealed class World1ShopView : MonoBehaviour
    {
        readonly List<Row> rows = new List<Row>();
        World1Session session;
        RectTransform canvas;
        CanvasGroup speedSliderInput;
        GameObject window;
        Transform trailPanel, auraPanel;
        UnityEngine.UI.Image trailTab, auraTab;
        Sprite inactiveTab;
        TMP_Text balance, notice;
        World1ShopPreview preview;
        Row selected;

        internal void Initialize(World1Session owner, RectTransform hudCanvas, CanvasGroup sliderInput)
        {
            session = owner;
            canvas = hudCanvas;
            speedSliderInput = sliderInput;
        }

        void BuildWindow()
        {
            window = Instantiate(Resources.Load<GameObject>("World1ShopView"), canvas, false);
            window.name = "Shop";
            var root = window.transform.Find("Root");
            root.Find("Background").GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            BindButton(root.Find("Title/Btn_Back"), Close);
            balance = root.Find("CurrencyBar_Cup/Bar/TextCount").GetComponent<TMP_Text>();
            notice = CreateNotice(root, root.Find("Title/Text_Title").GetComponent<TMP_Text>());
            var tabs = root.Find("SafeArea/ScrollRect_Tab/Viewport/Content");
            var rowPrefab = Resources.Load<GameObject>("World1ShopRow");
            var tabText = rowPrefab.transform.Find("Text_Name").GetComponent<TMP_Text>();
            for (int i = 0; i < tabs.childCount; i++)
            {
                var tab = (RectTransform)tabs.GetChild(i);
                tab.anchorMin = tab.anchorMax = tab.pivot = new Vector2(0, 1);
                tab.anchoredPosition = new Vector2(43, -12 - i * 103);
                tab.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 222);
                tab.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 82);
                var label = tab.Find("Text").GetComponent<TMP_Text>();
                label.font = tabText.font;
                label.fontSharedMaterial = tabText.fontSharedMaterial;
                label.enableAutoSizing = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.fontSize = tab.name == "Tab_Jackpot" ? 26 : 30;
                if (tab.name == "Tab_Skin") label.text = "SKIN";
                for (int child = 2; child < tab.childCount; child++)
                    if (tab.GetChild(child).name == "Icon") tab.GetChild(child).gameObject.SetActive(false);
                if (tab.name == "Tab_Trails") trailTab = BindButton(tab, () => SelectTab(false));
                if (tab.name == "Tab_Aura") auraTab = BindButton(tab, () => SelectTab(true));
            }
            inactiveTab = trailTab.sprite;
            trailPanel = root.Find("SafeArea/Shop_Trails/Root");
            auraPanel = root.Find("SafeArea/Shop_Aura/Root");
            preview = new GameObject("Shop preview stage").AddComponent<World1ShopPreview>();
            preview.transform.SetParent(transform, false);
            preview.Initialize(session.player, session.CosmeticMaterials);
            BuildList(trailPanel, "Trails", false, rowPrefab);
            BuildList(auraPanel, "Auras", true, rowPrefab);
            window.SetActive(false);
        }

        public void Open()
        {
            if (session.ShopOpen) return;
            if (window == null) BuildWindow();
            session.ShopOpen = true;
            speedSliderInput.blocksRaycasts = false;
            window.SetActive(true);
            preview.SetOpen(true);
            SelectTab(false);
        }

        public void Close()
        {
            session.ShopOpen = false;
            speedSliderInput.blocksRaycasts = true;
            window.SetActive(false);
            preview.SetOpen(false);
        }

        void Update()
        {
            if (session == null || !session.ShopOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        void SelectTab(bool aura)
        {
            trailPanel.gameObject.SetActive(!aura);
            auraPanel.gameObject.SetActive(aura);
            trailTab.sprite = aura ? inactiveTab : session.Catalog.activeTab;
            auraTab.sprite = aura ? session.Catalog.activeTab : inactiveTab;
            notice.text = "";
            var next = rows.Find(row => row.item.aura == aura && session.IsEquipped(row.item))
                ?? rows.Find(row => row.item.aura == aura);
            Select(next);
        }

        void Select(Row row)
        {
            if (row == null) return;
            if (selected != row)
            {
                if (selected != null) selected.outline.SetActive(false);
                selected = row;
                selected.outline.SetActive(true);
                preview.Show(row.item);
            }
            RefreshRows();
        }

        void Use(Row row)
        {
            Select(row);
            notice.text = session.UseCosmetic(row.item) ?? "";
            RefreshRows();
        }

        void RefreshRows()
        {
            balance.text = World1Hud.Compact(session.Entropy);
            foreach (var row in rows)
            {
                bool owned = session.Owns(row.item);
                bool equipped = session.IsEquipped(row.item);
                row.purchase.SetActive(!owned && !row.item.daily && !row.item.iap);
                row.daily.SetActive(!owned && row.item.daily);
                row.iap.SetActive(!owned && row.item.iap);
                row.equip.SetActive(owned && !equipped);
                row.equipped.SetActive(equipped);
                row.price.color = session.Entropy >= row.item.price ? Color.white : Color.red;
            }
        }

        void BuildList(Transform panel, string listName, bool aura, GameObject rowPrefab)
        {
            var scrollRoot = panel.Find(listName + "/ScrollRect");
            var viewport = (RectTransform)scrollRoot.Find("Viewport");
            var content = (RectTransform)viewport.Find("Content");
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.GetComponent<UnityEngine.UI.Image>() ?? viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.raycastTarget = true;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38;
            var bar = scrollRoot.Find("Scrollbar Vertical");
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = (RectTransform)bar.Find("Sliding Area/Handle");
            scrollbar.targetGraphic = scrollbar.handleRect.GetComponent<UnityEngine.UI.Image>();
            scrollbar.targetGraphic.raycastTarget = true;
            bar.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            int index = 0;
            foreach (var item in session.Catalog.items)
            {
                if (item.aura != aura) continue;
                var card = (RectTransform)Instantiate(rowPrefab, content, false).transform;
                card.name = item.id;
                card.anchorMin = new Vector2(0, 1);
                card.anchorMax = Vector2.one;
                card.pivot = new Vector2(.5f, 1);
                card.anchoredPosition = new Vector2(0, -12 - index * 171);
                card.sizeDelta = new Vector2(-32, 148);
                var row = new Row
                {
                    item = item, outline = card.Find("Selected").gameObject,
                    purchase = card.Find("Btn_Unlock_Entrophy").gameObject,
                    daily = card.Find("Btn_Get").gameObject, iap = card.Find("Btn_Unlock_IAP").gameObject,
                    equip = card.Find("Btn_Equip").gameObject, equipped = card.Find("Btn_Equipped").gameObject,
                    price = card.Find("Btn_Unlock_Entrophy/Layout/Text_Entrophy").GetComponent<TMP_Text>()
                };
                card.Find("Text_Name").GetComponent<TMP_Text>().text = item.title;
                card.Find("Text_Boost").GetComponent<TMP_Text>().text = "x" + World1Hud.Compact(item.multiplier) + " SpeedBoost";
                card.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite = item.icon;
                row.price.text = World1Hud.Compact(item.price);
                row.price.alignment = TextAlignmentOptions.Center;
                row.iap.transform.Find("Text_Entrophy").GetComponent<TMP_Text>().text = "IAP";
                SetPricePosition((RectTransform)row.price.transform, 24, 130);
                SetPricePosition((RectTransform)row.purchase.transform.Find("Layout/Icon"), -65, 56);
                row.outline.SetActive(false);
                BindButton(card, () => Select(row)).sprite = item.frame;
                foreach (var action in new[] { row.purchase, row.daily, row.iap, row.equip, row.equipped })
                    BindButton(action.transform, () => Use(row));
                rows.Add(row);
                index++;
            }
            content.sizeDelta = new Vector2(0, 24 + index * 171);
            scroll.verticalNormalizedPosition = 1;
            var raw = panel.Find("Preview/Preview_" + (aura ? "Aura" : "Trail")).gameObject.AddComponent<RawImage>();
            raw.texture = preview.Texture;
            raw.raycastTarget = false;
            raw.rectTransform.sizeDelta = new Vector2(-6, -6);
        }

        static void SetPricePosition(RectTransform rect, float x, float width)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(width, 65);
        }

        static TMP_Text CreateNotice(Transform root, TMP_Text template)
        {
            var label = Instantiate(template, root, false);
            label.name = "Shop status";
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 36);
            rect.sizeDelta = new Vector2(900, 52);
            label.enableAutoSizing = false;
            label.fontSize = 28;
            label.text = "";
            return label;
        }

        internal static UnityEngine.UI.Image BindButton(Transform target, UnityEngine.Events.UnityAction action)
        {
            var image = target.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
            {
                image = target.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = Color.clear;
            }
            image.raycastTarget = true;
            var button = target.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            return image;
        }

        sealed class Row
        {
            public World1ShopCatalog.Item item;
            public GameObject outline, purchase, daily, iap, equip, equipped;
            public TMP_Text price;
        }
    }
}
