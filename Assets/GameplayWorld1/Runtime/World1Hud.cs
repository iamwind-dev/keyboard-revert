using UnityEngine;

namespace KeyboardEscapeGameplay
{
    [DefaultExecutionOrder(200)]
    public sealed class World1Hud : MonoBehaviour
    {
        static readonly Color Ink = new Color32(15, 21, 40, 244);
        static readonly Color Muted = new Color32(188, 204, 233, 255);
        static readonly Color Gold = new Color32(255, 216, 94, 255);
        const float GainLifetime = .8f;

        readonly Gain[] gains = new Gain[8];
        World1Session session;
        Camera view;
        RectTransform canvasRect;
        RectTransform trainingPanel, messagePanel, respawnPanel;
        UnityEngine.UI.Text shoes, speed, checkpoint, training, message;
        UnityEngine.UI.Image fill;
        TMPro.TextMeshProUGUI level, progress, cups;
        TMPro.TextMeshProUGUI speedMaximum, speedCurrent;
        CanvasGroup speedCurrentGroup;
        CanvasGroup speedSliderInput;
        Font font;
        int nextGain;
        float nextRefresh;

        internal void Initialize(World1Session source)
        {
            session = source;
            view = Camera.main;
            font = session.hudFont != null ? session.hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvasRect = Element("Gameplay HUD", transform, Vector2.zero, Vector2.zero, Vector2.zero);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = view != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = view;
            canvas.planeDistance = .2f;
            canvas.sortingOrder = 20;
            canvasRect.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var events = new GameObject("Gameplay UI events", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                events.transform.SetParent(transform, false);
            }
            var scaler = canvasRect.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0;

            Vector2 topCenter = new Vector2(.5f, 1);
            Vector2 bottomCenter = new Vector2(.5f, 0);

            var original = Instantiate(Resources.Load<GameObject>("World1OriginalHud"), canvasRect, false);
            original.name = "SpeedKeyboard_GUI_View";
            var root = original.transform.Find("Root");
            var shop = gameObject.AddComponent<World1ShopView>();
            World1ShopView.BindButton(root.Find("UI_Button_Shop"), shop.Open);
            var levelBar = root.Find("Level_Progression/Fill");
            fill = levelBar.GetComponent<UnityEngine.UI.Image>();
            level = levelBar.Find("Text_Level").GetComponent<TMPro.TextMeshProUGUI>();
            progress = levelBar.Find("Text_Progress").GetComponent<TMPro.TextMeshProUGUI>();
            cups = root.Find("CurrencyBar_Entrophy/Bar/TextCount").GetComponent<TMPro.TextMeshProUGUI>();
            var boost = root.Find("Btn_SpeedBoost");
            boost.Find("Text_SpeedBoost_Remaining").gameObject.SetActive(false);
            boost.Find("Text_BoxBossChase").gameObject.SetActive(false);
            InitializeSpeedSlider();

            var stats = Panel("Local progress", canvasRect, topCenter, new Vector2(0, -137), new Vector2(423, 40), Ink);
            Icon("Shoe", stats, new Vector2(0, .5f), new Vector2(10, 0), new Vector2(31, 31), session.shoeIcon);
            shoes = Label("Total shoes", stats, new Vector2(0, .5f), new Vector2(48, 0), new Vector2(95, 32), 23, Gold);
            speed = Label("Speed", stats, new Vector2(.5f, .5f), new Vector2(-3, 0), new Vector2(110, 32), 19, Color.white, TextAnchor.MiddleCenter);
            checkpoint = Label("Checkpoint", stats, new Vector2(1, .5f), new Vector2(-11, 0), new Vector2(155, 32), 17, Muted, TextAnchor.MiddleRight);

            trainingPanel = Panel("Treadmill status", canvasRect, topCenter, new Vector2(0, -184), new Vector2(375, 52), Ink);
            training = Label("Training", trainingPanel, topCenter, new Vector2(0, -7), new Vector2(353, 36), 23, Gold, TextAnchor.MiddleCenter);
            trainingPanel.gameObject.SetActive(false);

            messagePanel = Panel("Gameplay notice", canvasRect, bottomCenter, new Vector2(0, 56), new Vector2(430, 38), Ink);
            message = Label("Notice", messagePanel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(412, 30), 15, Color.white, TextAnchor.MiddleCenter);
            messagePanel.gameObject.SetActive(false);

            respawnPanel = Panel("Respawn status", canvasRect, new Vector2(.5f, .35f), Vector2.zero, new Vector2(322, 52), Ink);
            var respawnImage = respawnPanel.GetComponent<UnityEngine.UI.Image>();
            respawnImage.raycastTarget = true;
            var respawnButton = respawnPanel.gameObject.AddComponent<UnityEngine.UI.Button>();
            respawnButton.targetGraphic = respawnImage;
            respawnButton.onClick.AddListener(session.RespawnPlayer);
            Label("Respawn prompt", respawnPanel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(302, 40), 19, Color.white, TextAnchor.MiddleCenter).text =
                "Bấm R để hồi tại checkpoint";
            respawnPanel.gameObject.SetActive(false);

            for (int i = 0; i < gains.Length; i++)
            {
                var gainRoot = Element("Floating shoes", canvasRect, new Vector2(.5f, .5f), Vector2.zero, new Vector2(142, 40));
                var group = gainRoot.gameObject.AddComponent<CanvasGroup>();
                Icon("Shoe", gainRoot, Vector2.zero, new Vector2(2, 2), new Vector2(34, 34), session.shoeIcon);
                var amount = Label("Shoes gained", gainRoot, Vector2.zero, new Vector2(39, 0), new Vector2(97, 38), 28, Gold);
                var outline = amount.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Ink;
                outline.effectDistance = new Vector2(1.4f, -1.4f);
                gains[i] = new Gain { root = gainRoot, group = group, amount = amount, start = float.NegativeInfinity };
                gainRoot.gameObject.SetActive(false);
            }
            shop.Initialize(session, canvasRect, speedSliderInput);
            Refresh();
        }

        void InitializeSpeedSlider()
        {
            var sliderView = Instantiate(Resources.Load<GameObject>("World1SpeedSlider"), canvasRect, false);
            sliderView.name = "SpeedSliderFeedback";
            speedSliderInput = sliderView.AddComponent<CanvasGroup>();
            var handle = sliderView.transform.Find("Handle Slide Area/Handle");
            var slider = sliderView.AddComponent<UnityEngine.UI.Slider>();
            slider.direction = UnityEngine.UI.Slider.Direction.BottomToTop;
            slider.fillRect = (RectTransform)sliderView.transform.Find("Fill Area/Fill");
            slider.handleRect = (RectTransform)handle;
            var navigation = slider.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            slider.navigation = navigation;
            var hit = sliderView.transform.Find("Background").GetComponent<UnityEngine.UI.Image>();
            hit.raycastTarget = true;
            handle.Find("Icon").GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            slider.SetValueWithoutNotify(session.SpeedLimit);
            slider.onValueChanged.AddListener(value => { session.SetSpeedLimit(value); RefreshSpeed(); });
            speedMaximum = sliderView.transform.Find("Text_SpeedMax").GetComponent<TMPro.TextMeshProUGUI>();
            speedCurrent = handle.Find("Text_Speed").GetComponent<TMPro.TextMeshProUGUI>();
            speedCurrentGroup = speedCurrent.GetComponent<CanvasGroup>();
        }

        internal void ShowGain(double amount)
        {
            if (view == null)
                return;
            var gain = gains[nextGain];
            gain.start = Time.time;
            gain.offset = (nextGain % 3 - 1) * 34;
            gain.amount.text = "+" + Compact(amount);
            nextGain = (nextGain + 1) % gains.Length;
        }

        void LateUpdate()
        {
            if (session == null)
                return;
            if (Time.unscaledTime >= nextRefresh)
            {
                Refresh();
                nextRefresh = Time.unscaledTime + .1f;
            }
            if (view == null)
                return;
            Vector3 head = session.player.transform.position + Vector3.up * (session.player.motor.height + .35f);
            Vector3 viewport = view.WorldToViewportPoint(head);
            foreach (var gain in gains)
            {
                float age = (Time.time - gain.start) / GainLifetime;
                bool show = age < 1 && viewport.z > 0 && !session.player.IsDead;
                gain.root.gameObject.SetActive(show);
                if (!show)
                    continue;
                var rect = canvasRect.rect;
                gain.root.anchoredPosition = new Vector2((viewport.x - .5f) * rect.width + gain.offset, (viewport.y - .5f) * rect.height + age * 57);
                gain.group.alpha = Mathf.Clamp01((1 - age) * 2.5f);
            }
        }

        void Refresh()
        {
            shoes.text = Compact(session.Shoes);
            level.text = "Level " + session.Level;
            RefreshSpeed();
            double required = World1Rules.GetPointToNextLevel(session.Level);
            progress.text = Compact(session.Experience) + "/" + Compact(required);
            fill.fillAmount = Mathf.Clamp01((float)(session.Experience / required));
            checkpoint.text = "CHECKPOINT  " + session.CurrentCheckpoint + " / " + (session.checkpoints.Length - 1);
            cups.text = Compact(session.Entropy);
            var treadmill = session.Treadmill;
            trainingPanel.gameObject.SetActive(treadmill != null);
            if (treadmill != null)
                training.text = "MÁY CHẠY ×" + Compact(treadmill.valueA) + "  •  Space để rời";
            messagePanel.gameObject.SetActive(!string.IsNullOrEmpty(session.Notice) && !session.player.IsDead);
            message.text = session.Notice ?? "";
            respawnPanel.gameObject.SetActive(session.player.IsDead && session.player.DeadElapsed >= 2);
        }

        void RefreshSpeed()
        {
            string current = World1Rules.RealSpeedToDisplay(session.PlayerSpeed).ToString("0.#");
            speed.text = "TỐC ĐỘ " + current;
            speedMaximum.text = World1Rules.RealSpeedToDisplay(World1Rules.MoveSpeedAtLevel(session.Level)).ToString("0.#");
            speedCurrent.text = session.SpeedLimit < .999f ? current : "";
            speedCurrentGroup.alpha = session.SpeedLimit < .999f ? 1 : 0;
        }

        internal static string Compact(double amount)
        {
            if (amount < 1e3) return amount.ToString("0.##");
            if (amount < 1e6) return (amount / 1e3).ToString("0.##") + "K";
            if (amount < 1e9) return (amount / 1e6).ToString("0.##") + "M";
            if (amount < 1e12) return (amount / 1e9).ToString("0.##") + "B";
            if (amount < 1e15) return (amount / 1e12).ToString("0.##") + "T";
            return amount.ToString("0.##e+0");
        }

        static RectTransform Element(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var element = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            element.SetParent(parent, false);
            element.anchorMin = element.anchorMax = element.pivot = anchor;
            element.anchoredPosition = position;
            element.sizeDelta = size;
            return element;
        }

        static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var rect = Element(name, parent, anchor, position, size);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static void Icon(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Texture2D texture)
        {
            var icon = Element(name, parent, anchor, position, size).gameObject.AddComponent<UnityEngine.UI.RawImage>();
            icon.texture = texture;
            icon.raycastTarget = false;
            icon.enabled = texture != null;
        }

        UnityEngine.UI.Text Label(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size,
            int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = Element(name, parent, anchor, position, size).gameObject.AddComponent<UnityEngine.UI.Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        sealed class Gain
        {
            public RectTransform root;
            public CanvasGroup group;
            public UnityEngine.UI.Text amount;
            public float start, offset;
        }
    }
}
