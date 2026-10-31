#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
namespace MoveCute
{
    public partial class BattleScreenUI
    {
        // Editor builder calls this once. All Images are serialized into the prefab,
        // so artists replace sprites directly without changing runtime code.
        public void BuildLayout(Font font)
        {
            screenFont = font;
            var canvas = gameObject.GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (!scaler) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)transform;
            Box("Background", root, 0, 0, 1920, 1080, new Color(0.96f, 0.97f, 0.99f)).raycastTarget = true;
            var status = Box("PlayerPanel", root, 70, 40, 410, 235, Panel).rectTransform;
            Box("HpTrack", status, 24, 22, 342, 28, Ink);
            hpFill = Box("HpFill", status, 24, 22, 342, 28, Accent);
            hpText = TextAt("HpText", status, "HP", 24, 22, 342, 28, 22);
            Contrast(hpText);
            Box("Portrait", status, 25, 76, 126, 130, new Color(0.35f, 0.58f, 0.78f));
            TextAt("PortraitLabel", status, T("player"), 25, 120, 126, 40, 24);
            var deck = MakeButton("Deck", status, T("deck"), 180, 77, 160, 52);
            deck.onClick.AddListener(() => OpenCards(T("deck"), M.Run.Deck));
            Box("MoneyIcon", status, 185, 160, 38, 38, new Color(0.94f, 0.71f, 0.25f));
            var detail = Box("DetailPanel", root, 70, 300, 410, 540, Panel).rectTransform;
            detailName = TextAt("DetailName", detail, "", 20, 20, 370, 50, 28);
            detailArt = Box("DetailArt", detail, 24, 87, 122, 138, Color.white);
            detailArt.preserveAspect = true;
            detailMeta = TextAt("DetailMeta", detail, "", 160, 85, 225, 60, 21);
            detailMoves = TextAt("DetailMoves", detail, "", 155, 160, 230, 64, 36);
            detailDescription = TextAt("DetailDescription", detail, "", 25, 250, 360, 245, 23);
            detailDescription.alignment = TextAnchor.UpperLeft;
            var hover = detailDescription.gameObject.AddComponent<BattleScreenPointer>();
            detailDescription.raycastTarget = true;
            hover.Enter = ShowKeywords;
            hover.Exit = () => keywordPanel.gameObject.SetActive(false);
            helpText = TextAt("Help", detail, "", 35, 135, 340, 340, 25);
            helpText.alignment = TextAnchor.UpperLeft;
            mouseIcon = Box("MouseIcon", detail, 30, 82, 25, 42, Ink).gameObject;
            var boardRoot = Box("BoardFrame", root, 610, 180, 676, 676, Ink).rectTransform;
            board = NewRect("Board", boardRoot, 8, 8, 660, 660).gameObject.AddComponent<GridLayoutGroup>();
            board.spacing = new Vector2(5, 5);
            board.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            board.constraintCount = 3;
            board.cellSize = Vector2.one * 216.66f;
            previewRoot = NewRect("Preview", root, 0, 0, 1920, 1080);
            ghost = Box("Ghost", previewRoot, 0, 0, 100, 120, new Color(0.15f, 0.52f, 0.85f, 0.38f));
            ghost.gameObject.SetActive(false);
            var player = Box("Player", root, 0, 0, 100, 120, new Color(0.12f, 0.42f, 0.68f));
            playerMarker = player.rectTransform;
            playerMarker.pivot = Vector2.one * 0.5f;
            ghost.rectTransform.pivot = Vector2.one * 0.5f;
            TextAt("PlayerLabel", playerMarker, T("player"), 0, 40, 100, 40, 21);
            enemies = NewRect("Enemies", root, 1400, 140, 390, 700);
            var enemyLayout = enemies.gameObject.AddComponent<HorizontalLayoutGroup>();
            enemyLayout.childControlWidth = enemyLayout.childControlHeight = true;
            enemyLayout.childForceExpandWidth = enemyLayout.childForceExpandHeight = true;
            enemyLayout.spacing = 20;
            hand = NewRect("Hand", root, 510, 890, 870, 175);
            var handLayout = hand.gameObject.AddComponent<HorizontalLayoutGroup>();
            handLayout.childControlWidth = handLayout.childControlHeight = true;
            handLayout.childForceExpandWidth = handLayout.childForceExpandHeight = true;
            handLayout.spacing = 12;
            var draw = MakeButton("DrawPile", root, "", 30, 930, 115, 115);
            drawText = Label(draw);
            draw.onClick.AddListener(() => OpenCards(T("draw"), ctx.Piles.Draw, true));
            var discard = MakeButton("DiscardPile", root, "", 1775, 930, 115, 115);
            discardText = Label(discard);
            discard.onClick.AddListener(() => OpenCards(T("discard"), ctx.Piles.Discard));
            energyButton = MakeButton("Energy", root, "", 180, 895, 280, 70);
            energyButton.image.color = Ink;
            energyCells = NewRect("EnergyCells", energyButton.transform, 15, 12, 250, 45);
            energyText = Label(energyButton);
            energyText.transform.SetAsLastSibling();
            endTurnButton = MakeButton("EndTurn", root, "", 180, 985, 280, 70);
            endTurnButton.image.color = Ink;
            turnCells = NewRect("TurnCells", endTurnButton.transform, 15, 12, 250, 45);
            turnsText = Label(endTurnButton);
            turnsText.transform.SetAsLastSibling();
            Contrast(energyText);
            Contrast(turnsText);
            hud = TextAt("UnusedHud", root, "", 0, 0, 1, 1, 1);
            log = TextAt("UnusedLog", root, "", 0, 0, 1, 1, 1);
            promptRoot = Box("PromptRoot", root, 610, 80, 676, 65, Pale).gameObject;
            prompt = TextAt("Prompt", promptRoot.transform, "", 12, 5, 540, 55, 23);
            cancelButton = MakeButton("Cancel", promptRoot.transform, T("cancel"), 550, 10, 110, 45);
            banner = TextAt("Banner", root, "", 610, 360, 676, 110, 48);
            var keywords = Box("KeywordPanel", root, 490, 300, 300, 490, Pale);
            keywordPanel = keywords.rectTransform;
            keywordText = TextAt("KeywordText", keywordPanel, "", 20, 20, 260, 450, 21);
            keywordText.alignment = TextAnchor.UpperLeft;
            resultPanel = Box("Result", root, 600, 370, 720, 300, Pale).gameObject;
            resultText = TextAt("ResultText", resultPanel.transform, "", 20, 25, 680, 100, 35);
            nextBattleButton = MakeButton("NextBattle", resultPanel.transform, T("nextBattle"), 55, 180, 280, 70);
            newRunButton = MakeButton("NewRun", resultPanel.transform, T("newRun"), 385, 180, 280, 70);
            modal = Box("CardBrowser", root, 490, 60, 1050, 1000, new Color(0.10f, 0.16f, 0.23f, 0.98f)).rectTransform;
            modal.GetComponent<Image>().raycastTarget = true;
            modalTitle = TextAt("Title", modal, "", 35, 20, 800, 60, 30);
            modalTitle.color = Color.white;
            var close = MakeButton("Close", modal, T("close"), 890, 25, 130, 50);
            close.onClick.AddListener(() => { modal.gameObject.SetActive(false); RefreshAll(); });
            modalContent = NewRect("Cards", modal, 35, 110, 980, 740);
            var grid = modalContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(180, 240);
            grid.spacing = new Vector2(16, 25);
            ConfigureCardBrowserScroll();
            var templates = NewRect("Templates", root, 0, 0, 1, 1);
            tilePrefab = MakeButton("TileTemplate", templates, "", 0, 0, 100, 100);
            ((RectTransform)tilePrefab.transform).pivot = Vector2.one * 0.5f;
            Label(tilePrefab).fontSize = 20;
            AddFrame(tilePrefab);
            cardPrefab = MakeButton("CardTemplate", templates, "", 0, 0, 130, 175);
            Label(cardPrefab).gameObject.SetActive(false);
            CardChildren(cardPrefab.transform);
            AddFrame(cardPrefab);
            enemyPrefab = MakeButton("EnemyTemplate", templates, "", 0, 0, 390, 700);
            Label(enemyPrefab).gameObject.SetActive(false);
            EnemyChildren(enemyPrefab.transform);
            enemyPrefab.image.color = Color.clear;
            AddFrame(enemyPrefab);
            var targetFrame = enemyPrefab.transform.Find("TargetFrame");
            targetFrame.SetParent(enemyPrefab.transform.Find("Art"), false);
            Stretch((RectTransform)targetFrame, 0, 0, 1, 1);
            energyCellTemplate = Box("EnergyCellTemplate", templates, 0, 0, 40, 45, new Color(0.25f, 0.88f, 0.64f));
            turnCellTemplate = Box("TurnCellTemplate", templates, 0, 0, 40, 45, new Color(0.98f, 0.74f, 0.3f));
            templates.gameObject.SetActive(false);
            modal.gameObject.SetActive(false);
            resultPanel.SetActive(false);
            keywordPanel.gameObject.SetActive(false);
            promptRoot.SetActive(false);
            banner.gameObject.SetActive(false);
            ShowHelp();
            BuildRewardLayout();
            AddPanelSupports();
            UpgradeViewComponents();
        }

        void CardChildren(Transform root)
        {
            RelativeBox("Art", root, 0, 0, 1, 1, Color.white).preserveAspect = true;
            var icons = NewRect("DirectionIcons", root, 0, 0, 130, 40);
            Stretch(icons, 0, 0, 1, 1);
        }

        void EnemyChildren(Transform root)
        {
            RelativeBox("Art", root, 0.10f, 0.035f, 0.80f, 0.72f, new Color(0.45f, 0.57f, 0.69f)).preserveAspect = true;
            var track = RelativeBox("Bar", root, 0.06f, -16f / 700f, 0.88f, 0.045f, Ink);
            Box("Fill", track.transform, 0, 0, 342, 32, Accent);
            Contrast(RelativeText("Morale", root, "", 0.06f, -16f / 700f, 0.88f, 0.045f, 23));
            RelativeText("Statuses", root, "", 0.06f, -0.10f, 0.88f, 0.06f, 20);
            var intent = RelativeText("Intent", root, "", 0.06f, 0.77f, 0.88f, 0.23f, 18);
            intent.alignment = TextAnchor.UpperLeft;
        }

        static RectTransform NewRect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        static void Contrast(Text text)
        {
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        static Image Box(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = NewRect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        Text TextAt(string name, Transform parent, string text, float x, float y, float w, float h, int size)
        {
            var label = NewRect(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = screenFont;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Min(13, size);
            label.resizeTextMaxSize = size;
            label.color = Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        Button MakeButton(string name, Transform parent, string text, float x, float y, float w, float h)
        {
            var image = Box(name, parent, x, y, w, h, Panel);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = TextAt("Label", image.transform, text, 0, 0, w, h, 24);
            Stretch(label.rectTransform, 0, 0, 1, 1);
            return button;
        }

        static void Stretch(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(x + w, y + h);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Image RelativeBox(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Box(name, parent, 0, 0, 1, 1, color);
            Stretch(image.rectTransform, x, y, w, h);
            return image;
        }

        Text RelativeText(string name, Transform parent, string value, float x, float y, float w, float h, int size)
        {
            var text = TextAt(name, parent, value, 0, 0, 1, 1, size);
            Stretch(text.rectTransform, x, y, w, h);
            return text;
        }

        static void AddFrame(Button button)
        {
            var root = NewRect("TargetFrame", button.transform, 0, 0, 1, 1);
            Stretch(root, 0, 0, 1, 1);
            RelativeBox("Top", root, 0, 0.975f, 1, 0.025f, Accent);
            RelativeBox("Bottom", root, 0, 0, 1, 0.025f, Accent);
            RelativeBox("Left", root, 0, 0, 0.025f, 1, Accent);
            RelativeBox("Right", root, 0.975f, 0, 0.025f, 1, Accent);
            root.gameObject.SetActive(false);
        }
        public void BuildRewardLayout()
        {
            if (rewardOverlay != null) return;
            goldBalance = TextAt("GoldBalance", transform.Find("PlayerPanel"), "0", 235, 155, 120, 48, 24);
            rewardChest = MakeButton("RewardChest", transform, T("openGift"), 1400, 520, 390, 290);
            rewardChest.image.color = new Color(.72f, .49f, .29f);
            Box("Ribbon", rewardChest.transform, 175, 0, 40, 290, new Color(.96f, .76f, .35f));
            Box("Lid", rewardChest.transform, -8, -15, 406, 48, new Color(.85f, .66f, .43f));
            Label(rewardChest).transform.SetAsLastSibling();
            rewardOverlay = Box("RewardOverlay", transform, 0, 0, 1920, 1080, Color.clear).gameObject;
            rewardOverlay.GetComponent<Image>().raycastTarget = true;
            rewardOverlay.AddComponent<BattleScreenPointer>().RightClick = RewardBack;
            var list = Box("RewardList", rewardOverlay.transform, 1390, 240, 420, 550, Pale).rectTransform;
            rewardListPanel = list.gameObject;
            TextAt("Title", list, T("rewards"), 20, 20, 380, 65, 32);
            rewardGoldButton = MakeButton("Gold", list, "", 30, 110, 360, 80);
            rewardHealButton = MakeButton("Healing", list, "", 30, 215, 360, 80);
            rewardCardButton = MakeButton("Card", list, T("cardReward"), 30, 320, 360, 80);
            rewardNextButton = MakeButton("Continue", list, T("nextBattle"), 50, 445, 320, 70);
            var choices = NewRect("CardRewards", rewardOverlay.transform, 550, 200, 790, 600);
            rewardCardsPanel = choices.gameObject;
            Contrast(TextAt("Title", choices, T("chooseReward"), 0, 0, 790, 80, 32));
            rewardCandidates = NewRect("Candidates", choices, 55, 120, 680, 267);
            var grid = rewardCandidates.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(200, 266.6667f);
            grid.spacing = new Vector2(40, 0);
            rewardBackButton = MakeButton("Back", choices, T("back"), 265, 450, 260, 70);
            rewardChest.gameObject.SetActive(false);
            rewardOverlay.SetActive(false);
        }
    }
}
#endif
