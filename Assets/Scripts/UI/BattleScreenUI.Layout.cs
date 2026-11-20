using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MoveCute
{
    public partial class BattleScreenUI
    {
        [Header("교체 가능한 전투 화면 이미지 / 표시")]
        public Font screenFont;
        public Image hpFill;
        public Text hpText, energyText, turnsText, drawText, discardText;
        public Text endTurnLabel;
        public RectTransform energyCells, previewRoot, modal, modalContent;
        public Image energyCellTemplate;
        public Button energyButton;
        public Text detailName, detailMeta, detailMoves, detailDescription, helpText, keywordText;
        public Image detailArt;
        public RectTransform keywordPanel;
        public Text modalTitle;
        public ScrollRect cardBrowserScroll;
        public Image ghost;
        [Tooltip("Left, Up, Right, Down")]
        public Sprite[] directionIcons = new Sprite[4];
        [Tooltip("Sit, Jump, Retreat, Dash — UI-only placeholder artwork")]
        public Sprite[] basicCardArt = new Sprite[4];
        public Material grayscaleMaterial;
        [Tooltip("Optional future enemy-specific descriptions, matched by EnemyData.id")]
        public string[] enemyDescriptionIds = Array.Empty<string>();
        [TextArea] public string[] enemyDescriptions = Array.Empty<string>();

        CardInstance selectedCard, inspectedCard;
        RectTransform keywordButtonsRoot;
        readonly List<Button> keywordButtons = new();
        const int KeywordsPerRow = 3;
        const float KeywordInset = 25f, KeywordBottom = 16f, KeywordHeight = 30f, KeywordGap = 6f;
        string openKeyword;
        object hoveredInfoOwner;
        Action hoveredInfoDisplay;
        bool teleportMode;
        List<CardInstance> viewedCards;
        static Dictionary<string, string[]> words;
        static readonly Color Ink = new(0.10f, 0.16f, 0.23f);
        static readonly Color Panel = new(0.61f, 0.76f, 0.94f);
        static readonly Color Pale = new(0.87f, 0.92f, 0.98f);
        static readonly Color Accent = new(0.96f, 0.22f, 0.24f);

        static string T(string key)
        {
            if (words == null)
            {
                words = new Dictionary<string, string[]>();
                var asset = Resources.Load<TextAsset>("UI/BattleScreenText");
                if (asset != null)
                    foreach (var line in asset.text.Split('\n'))
                    {
                        var parts = line.TrimEnd('\r').Split('|');
                        if (parts.Length >= 3) words[parts[0]] = new[] { parts[1], parts[2] };
                    }
            }
            return words.TryGetValue(key, out var text) ? text[Loc.Language == "en" ? 1 : 0].Replace("\\n", "\n") : key;
        }

        void InitializeScreen()
        {
            InitializeViews();
            var fonts = Font.GetOSInstalledFontNames();
            var preferred = new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR" };
            var installed = preferred.FirstOrDefault(f => fonts.Contains(f));
            if (installed != null)
            {
                screenFont = Font.CreateDynamicFontFromOSFont(installed, 24);
                foreach (var label in GetComponentsInChildren<Text>(true)) label.font = screenFont;
            }
            deckButton.onClick.AddListener(() => OpenCards(T("deck"), M.Run.Deck));
            drawButton.onClick.AddListener(() => OpenCards(T("draw"), ctx.Piles.Draw, true));
            discardButton.onClick.AddListener(() => OpenCards(T("discard"), ctx.Piles.Discard));
            trashButton.onClick.AddListener(() =>
            {
                if (ctx != null) OpenCards(T("trash"), ctx.Piles.Trash);
            });
            closeBrowserButton.onClick.AddListener(CloseCardBrowser);
            detailPanelRoot.GetComponent<Image>().raycastTarget = true;
            var detailPointer = detailPanelRoot.gameObject.AddComponent<BattleScreenPointer>();
            detailPointer.LeftClick = CancelFromDetails;
            var descriptionPointer = detailDescription.GetComponent<BattleScreenPointer>();
            if (!descriptionPointer) descriptionPointer = detailDescription.gameObject.AddComponent<BattleScreenPointer>();
            descriptionPointer.LeftClick = CancelFromDetails;
            InitializeKeywords();
            backgroundObject.AddComponent<BattleScreenPointer>().LeftClick = Back;
            playerMarker.GetComponent<Image>().raycastTarget = true;
            // 플레이어 그림이 내 칸을 덮고 있으므로, 내 칸을 고르는 중이면(SelfTile 카드) 그 칸을 고른 것으로 처리한다
            playerMarker.gameObject.AddComponent<BattleScreenPointer>().LeftClick = () =>
            {
                if (ctx == null || !TryPick(ctx.Player.Pos)) Back();
            };
            BindTooltip(energyButton, () => $"{T("energyHelp")}\nHP −{ctx?.TeleportHpCost}", () => T("energy"));
            BindTooltip(endTurnButton, () => T("turnHelp"), () => T("turnEnd"));
            BindTooltip(drawButton, () => T("drawHelp"), () => T("draw"));
            BindTooltip(discardButton, () => T("discardHelp"), () => T("discard"));
            BindTooltip(trashButton, () => T("trashHelp"), () => T("trash"));
            BindTooltip(deckButton, () => T("deckHelp"), () => T("deck"));
            energyButton.GetComponent<BattleScreenPointer>().Hold = BeginTeleport;
            cancelButton.onClick.AddListener(Back);
            endTurnButton.onClick.AddListener(Back);
            endTurnButton.GetComponent<BattleScreenPointer>().LeftClick = () => { if (pick != null) Back(); };
            energyButton.GetComponent<BattleScreenPointer>().LeftClick = () => { if (pick != null) Back(); };
            ShowHelp();
        }

        void LateUpdate()
        {
            if (ctx == null) return;
            var mouse = Mouse.current;
            if (openKeyword != null && mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                var position = mouse.position.ReadValue();
                if (!RectTransformUtility.RectangleContainsScreenPoint(keywordPanel, position) &&
                    !keywordButtons.Any(button => button.gameObject.activeInHierarchy &&
                        RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, position)))
                    CloseKeyword();
            }
            endTurnButton.interactable = M.CanInput && !teleportMode && !modal.gameObject.activeSelf;
            if (teleportMode && !M.CanInput) { teleportMode = false; RefreshTargets(); }
            RefreshRelicStates();
        }

        void BeginTeleport()
        {
            if (pick != null) Back();
            if (!M.CanInput || ctx.Energy <= 0 || modal.gameObject.activeSelf) return;
            ClearSelection();
            teleportMode = true;
            prompt.text = $"{T("teleport")} · HP −{ctx.TeleportHpCost}";
            promptRoot.SetActive(true);
            RefreshTargets();
        }

        void CancelFromDetails()
        {
            if (openKeyword != null || modal.gameObject.activeSelf || pick != null || selectedCard != null || teleportMode)
                Back();
        }

        void Back()
        {
            if (openKeyword != null) { CloseKeyword(); return; }
            if (rewardScreenActive) { RewardBack(); return; }
            if (modal.gameObject.activeSelf) { CloseCardBrowser(); return; }
            if (pick != null) Resolve(null);
            ClearSelection();
            RefreshTargets();
        }

        void ClearSelection()
        {
            CloseKeyword();
            selectedCard = null;
            energyPreviewCard = null;
            RefreshEnergyStrip();
            teleportMode = false;
            ClearPreview();
            if (pick == null) promptRoot.SetActive(false);
            RestorePanel();
            RefreshHandLayout();
            foreach (var entry in cards) SetFrame(entry.Value, false);
        }

        void ShowHelp()
        {
            inspectedCard = null;
            detailName.text = T("inspectTitle");
            detailMeta.text = "";
            detailMoves.text = "";
            detailDescription.text = "";
            detailArt.gameObject.SetActive(false);
            HideKeywords();
            helpText.gameObject.SetActive(true);
            mouseIcon.SetActive(true);
            helpText.text = T("help").Replace("\\n", "\n");
        }

        void BeginInfoHover(object owner, Action display)
        {
            hoveredInfoOwner = owner;
            hoveredInfoDisplay = display;
            if (openKeyword == null) display();
        }

        void EndInfoHover(object owner, bool restore = true)
        {
            if (!ReferenceEquals(hoveredInfoOwner, owner)) return;
            hoveredInfoOwner = null;
            hoveredInfoDisplay = null;
            if (restore) RestorePanel();
        }

        void RestorePanel()
        {
            if (openKeyword != null) return;
            if (hoveredInfoDisplay != null) hoveredInfoDisplay();
            else if (selectedCard != null) ShowCard(selectedCard);
            else if (rewardScreenActive) ShowInformation(T("rewards"), T("rewardHelp"));
            else ShowHelp();
        }

        void ShowInformation(string title, string description)
        {
            inspectedCard = null;
            detailName.text = title;
            detailMeta.text = detailMoves.text = "";
            detailDescription.text = "";
            detailArt.gameObject.SetActive(false);
            helpText.gameObject.SetActive(true);
            helpText.text = description;
            HideKeywords();
            mouseIcon.SetActive(false);
        }

        void ShowCard(CardInstance card)
        {
            if (inspectedCard != card) CloseKeyword();
            inspectedCard = card;
            helpText.gameObject.SetActive(false);
            mouseIcon.SetActive(false);
            detailArt.gameObject.SetActive(true);
            detailArt.sprite = ArtFor(card);
            detailArt.color = detailArt.sprite != null ? Color.white : new Color(0.37f, 0.57f, 0.76f);
            detailName.text = card.Data.DisplayName;
            detailMeta.text = $"{T(card.Data.type.ToString())} · {T(card.Data.rarity.ToString())}";
            detailMoves.supportRichText = true;
            detailMoves.text = CardMoveDisplay.FormatArrows(card, MoveDisplayContext);
            detailMoves.gameObject.SetActive(!card.IsUnlocked && card.Moves.Count > 0);
            detailDescription.text = card.Data.Description;
            CardMoveDisplay.GetRange(card, MoveDisplayContext, out _, out int moveCount);
            if (card.IsLocked && moveCount < card.Moves.Count)
                detailDescription.text += "\n" + T("IgnoredMovesDesc");
            RefreshKeywords(card);
        }

        void InitializeKeywords()
        {
            var detail = (RectTransform)detailPanelRoot;
            keywordButtonsRoot = new GameObject("Keywords", typeof(RectTransform)).GetComponent<RectTransform>();
            keywordButtonsRoot.SetParent(detail, false);
            keywordButtonsRoot.anchorMin = Vector2.zero;
            keywordButtonsRoot.anchorMax = Vector2.right;
            keywordButtonsRoot.offsetMin = new Vector2(KeywordInset, KeywordBottom);
            keywordButtonsRoot.offsetMax = new Vector2(-KeywordInset, KeywordBottom + KeywordHeight);
            keywordPanel.sizeDelta = new Vector2(detail.rect.width - 2 * KeywordInset, 76);
            detailDescription.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 214);
            keywordPanel.GetComponent<Image>().raycastTarget = true;
            keywordText.alignment = TextAnchor.MiddleCenter;
            var textRect = keywordText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12, 8);
            textRect.offsetMax = new Vector2(-12, -8);
        }

        void RefreshKeywords(CardInstance card)
        {
            var keys = card.Keywords.Select(keyword => keyword.ToString()).Distinct().ToList();
            if (!card.Has<Keyword_Bomb>() && card.Data.Description.Contains(T("bomb"))) keys.Add("bomb");
            if (openKeyword != null && !keys.Contains(openKeyword)) CloseKeyword();
            float buttonWidth = (keywordButtonsRoot.rect.width - (KeywordsPerRow - 1) * KeywordGap) / KeywordsPerRow;
            for (int i = 0; i < keys.Count; i++)
            {
                if (i == keywordButtons.Count) keywordButtons.Add(CreateKeywordButton());
                var button = keywordButtons[i];
                string key = keys[i];
                var label = button.GetComponentInChildren<Text>(true);
                label.text = T(key);
                var rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(buttonWidth, KeywordHeight);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ToggleKeyword(key, rect));
                int row = i / KeywordsPerRow;
                int rowEnd = Mathf.Min(keys.Count, (row + 1) * KeywordsPerRow);
                rect.anchoredPosition = new Vector2(
                    -(rowEnd - i - 1) * (buttonWidth + KeywordGap), row * (KeywordHeight + KeywordGap));
                button.gameObject.SetActive(true);
                if (openKeyword == key) PositionKeywordPanel(rect);
            }
            int rowCount = (keys.Count + KeywordsPerRow - 1) / KeywordsPerRow;
            float extraHeight = Mathf.Max(0, rowCount - 1) * (KeywordHeight + KeywordGap);
            detailDescription.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 214 - extraHeight);
            for (int i = keys.Count; i < keywordButtons.Count; i++)
                keywordButtons[i].gameObject.SetActive(false);
            keywordButtonsRoot.gameObject.SetActive(keys.Count > 0);
            if (openKeyword != null) keywordText.text = T(openKeyword + "Desc");
        }

        Button CreateKeywordButton()
        {
            var image = new GameObject("Keyword", typeof(RectTransform), typeof(Image), typeof(Button))
                .GetComponent<Image>();
            var rect = image.rectTransform;
            rect.SetParent(keywordButtonsRoot, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.right;
            image.color = Pale;
            var button = image.GetComponent<Button>();
            button.targetGraphic = image;
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(button.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.offsetMin = new Vector2(5, 0);
            label.rectTransform.offsetMax = new Vector2(-5, 0);
            label.font = screenFont;
            label.fontSize = 22;
            label.fontStyle = FontStyle.Normal;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Ink;
            label.raycastTarget = false;
            return button;
        }

        void PositionKeywordPanel(RectTransform button)
        {
            keywordPanel.position = button.TransformPoint(
                new Vector3(button.rect.xMax + 8, button.rect.center.y + keywordPanel.rect.height / 2));
        }

        void ToggleKeyword(string key, RectTransform button)
        {
            if (openKeyword == key) { CloseKeyword(); return; }
            openKeyword = key;
            keywordText.text = T(key + "Desc");
            keywordPanel.gameObject.SetActive(true);
            PositionKeywordPanel(button);
            keywordPanel.SetAsLastSibling();
        }

        void CloseKeyword()
        {
            openKeyword = null;
            keywordPanel.gameObject.SetActive(false);
        }

        void HideKeywords()
        {
            CloseKeyword();
            if (keywordButtonsRoot != null) keywordButtonsRoot.gameObject.SetActive(false);
        }

        void BindTile(Button button, Vector2Int pos)
        {
            var pointer = button.GetComponent<BattleScreenPointer>();
            if (!pointer) pointer = button.gameObject.AddComponent<BattleScreenPointer>();
            pointer.Enter = () =>
            {
                if (teleportMode)
                {
                    ghost.gameObject.SetActive(ctx.Board.IsPassable(pos) && pos != ctx.Player.Pos);
                    ghost.rectTransform.position = button.transform.position;
                }
            };
            pointer.Exit = () => { if (teleportMode) ghost.gameObject.SetActive(false); };
        }

        void BindEnemy(Button button, Enemy enemy)
        {
            unitViews[button].SetArtwork(enemy.Data.sprite);
            BindTooltip(button, () =>
            {
                int index = Array.IndexOf(enemyDescriptionIds, enemy.Data.id);
                return (index >= 0 && index < enemyDescriptions.Length
                    ? enemyDescriptions[index] : T("enemyHelp"));
            }, () => enemy.DisplayName);
        }

        Sprite ArtFor(CardInstance card)
        {
            if (card.Data.art != null) return card.Data.art;
            int index = card.Data.id switch { "sit" => 0, "jump" => 1, "retreat" => 2, "dash" => 3, _ => -1 };
            return index >= 0 && index < basicCardArt.Length ? basicCardArt[index] : null;
        }

        void BindTooltip(Button button, Func<string> content, Func<string> title)
        {
            var relay = button.GetComponent<BattleScreenPointer>();
            if (!relay) relay = button.gameObject.AddComponent<BattleScreenPointer>();
            relay.Enter = () => BeginInfoHover(button, () => ShowInformation(title(), content()));
            relay.Exit = () => EndInfoHover(button);
        }



        int? shownGold, shownHp, shownMaxHp, shownEnergy, shownMaxEnergy, shownTurns, shownTurnLimit, shownDraw, shownDiscard, shownTrash;
        string hudLanguage;
        float shownHpWidth = -1;

        void RefreshScreenHud()
        {
            bool languageChanged = hudLanguage != Loc.Language;
            hudLanguage = Loc.Language;
            if (shownGold != ctx.Run.Gold)
            {
                shownGold = ctx.Run.Gold;
                if (goldBalance) goldBalance.text = ctx.Run.Gold.ToString();
            }
            float hpWidth = hpText.rectTransform.rect.width;
            if (shownHp != ctx.Player.Hp || shownMaxHp != ctx.Player.MaxHp || shownHpWidth != hpWidth)
            {
                shownHp = ctx.Player.Hp; shownMaxHp = ctx.Player.MaxHp; shownHpWidth = hpWidth;
                hpText.text = $"HP  {ctx.Player.Hp} / {ctx.Player.MaxHp}";
                hpFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    hpWidth * Mathf.Clamp01((float)ctx.Player.Hp / Mathf.Max(1, ctx.Player.MaxHp)));
            }
            if (shownEnergy != ctx.Energy || shownMaxEnergy != ctx.MaxEnergy || languageChanged)
            {
                shownEnergy = ctx.Energy; shownMaxEnergy = ctx.MaxEnergy;
                energyText.text = $"{T("energy")}  {ctx.Energy}/{ctx.MaxEnergy}";
            }
            RefreshEnergyStrip();
            if (languageChanged) endTurnLabel.text = T("turnEnd");
            int turns = ctx.IsOver ? 0 : Mathf.Max(0, ctx.Config.turnLimit - ctx.Turn + 1);
            if (shownTurns != turns || shownTurnLimit != ctx.Config.turnLimit)
            {
                shownTurns = turns; shownTurnLimit = ctx.Config.turnLimit;
                turnsText.text = $"{turns} / {ctx.Config.turnLimit}";
            }
            if (shownDraw != ctx.Piles.Draw.Count || languageChanged)
            {
                shownDraw = ctx.Piles.Draw.Count;
                drawText.text = $"{T("draw")}\n{ctx.Piles.Draw.Count}";
            }
            if (shownDiscard != ctx.Piles.Discard.Count || languageChanged)
            {
                shownDiscard = ctx.Piles.Discard.Count;
                discardText.text = $"{T("discard")}\n{ctx.Piles.Discard.Count}";
            }
            if (shownTrash != ctx.Piles.Trash.Count || languageChanged)
            {
                shownTrash = ctx.Piles.Trash.Count;
                trashText.text = $"{T("trash")}\n{ctx.Piles.Trash.Count}";
            }
        }

        void RefreshEnergyStrip()
        {
            if (ctx == null) return;
            int cost = ctx.CanAct && energyPreviewCard != null && !energyPreviewCard.IsLocked && cards.ContainsKey(energyPreviewCard)
                ? energyPreviewCard.PlayCost(ctx) : 0;
            int pending = cost <= ctx.Energy ? cost : 0;
            energyStrip.Render(energyCellTemplate, ctx.Energy, Mathf.Max(ctx.MaxEnergy, ctx.Energy), pending);
        }

        BattleContext MoveDisplayContext => !rewardScreenActive && ctx != null && !ctx.IsOver ? ctx : null;

        void RefreshCardView(Button view, CardInstance card, bool showPlayability = true)
        {
            bool gray = showPlayability && !rewardScreenActive &&
                (card.IsGrayed || card.IsLocked || ctx != null && ctx.Energy < card.PlayCost(ctx));
            var displayContext = MoveDisplayContext;
            CardMoveDisplay.GetRange(card, displayContext, out int start, out int count);
            int matched = displayContext?.GetMatchedLockMoveCount(card) ?? 0;
            bool showFreeFrame = card.Has<Keyword_Free>() || card.IsUnlocked;
            CachedCard(view).Render(ArtFor(card), gray ? grayscaleMaterial : null,
                gray ? Grayed : CardBase, CardMoveDisplay.VisibleMoves(card), directionIcons,
                selectedCard == card || pickable != null && pickable.Contains(card), showFreeFrame, start, count, matched);
        }

        void RefreshEnemy(Button button, Enemy enemy)
        {
            var view = unitViews[button];
            int bonus = 0;
            foreach (var tileEffect in ctx.Board.Get(ctx.Player.Pos).Effects)
                if (tileEffect.Trigger == TileTrigger.TurnEndStanding && tileEffect.Source == enemy)
                    foreach (var effect in tileEffect.Effects)
                        if (effect is GainMoraleEffect gain) bonus += gain.amount;
            view.SetMorale(enemy.Hp, bonus, enemy.MaxHp);
            view.SetStatuses(enemy.Statuses);
            view.SetTarget(pickable != null && pickable.Contains(enemy));
        }

        static string DescribeEffect(Effect effect) => effect switch
        {
            GainMoraleEffect e => $"{T("morale")} +{e.amount}",
            DamageEffect e => $"{T("damage")} {e.damage}",
            DamagePlayerEffect e => $"HP −{e.amount}",
            PlaceObstacleEffect e => $"{T("place")} {e.obstacle?.DisplayName}",
            GainEnergyEffect e => $"{T("energy")} +{e.amount}",
            DrawCardsEffect e => $"{T("draw")} +{e.count}",
            AddStatusEffect e => $"{e.statusId} +{e.stacks}",
            _ => effect?.GetType().Name.Replace("Effect", "") ?? ""
        };

        void SetFrame(Button button, bool visible)
        {
            if (cardViews.TryGetValue(button, out var card)) card.SetTarget(visible);
            else if (boardViews.TryGetValue(button, out var tile)) tile.SetTarget(visible);
            else if (unitViews.TryGetValue(button, out var enemy)) enemy.SetTarget(visible);
        }

        void ClearPreview()
        {
            previewView.Clear();
            ghost.gameObject.SetActive(false);
        }

        void Preview(CardInstance card)
        {
            ClearPreview();
            if (card.IsUnlocked) return;
            var pos = ctx.Player.Pos;
            foreach (var direction in card.Moves)
            {
                var next = ctx.StepTarget(pos, direction);   // 경계 이동이 켜져 있으면 반대편 끝 칸
                if (!ctx.Board.IsPassable(next)) break;
                AddPathSegment(pos, next, direction);
                pos = next;
            }
            ghost.gameObject.SetActive(pos != ctx.Player.Pos);
            ghost.rectTransform.position = tiles[pos].transform.position;
            ghost.transform.SetAsFirstSibling();
        }

        /// <summary>경로 한 칸. 경계를 넘는 칸은 보드를 가로지르지 않게, 나가는 반 칸과 반대편에서 들어오는 반 칸으로 나눠 그린다.</summary>
        void AddPathSegment(Vector2Int from, Vector2Int to, Direction direction)
        {
            Vector3 a = tiles[from].transform.position, b = tiles[to].transform.position;
            if (to == from + direction.ToOffset())
            {
                previewView.AddSegment(a, b);
                return;
            }
            var half = TileStep(direction) * 0.5f;
            previewView.AddSegment(a, a + half);
            previewView.AddSegment(b - half, b);
        }

        /// <summary>direction 방향으로 한 칸 갈 때의 화면 위치 차이 (보드 칸 간격).</summary>
        Vector3 TileStep(Direction direction)
        {
            var origin = tiles[Vector2Int.zero].transform.position;
            var right = tiles[Vector2Int.right].transform.position - origin;
            var up = tiles[Vector2Int.up].transform.position - origin;
            return direction switch
            {
                Direction.Left => -right,
                Direction.Up => up,
                Direction.Right => right,
                _ => -up,
            };
        }


    }
}
