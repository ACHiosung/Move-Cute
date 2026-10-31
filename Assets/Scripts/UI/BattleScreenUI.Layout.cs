using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoveCute
{
    public partial class BattleScreenUI
    {
        [Header("교체 가능한 전투 화면 이미지 / 표시")]
        public Font screenFont;
        public Image hpFill;
        public Text hpText, energyText, turnsText, drawText, discardText;
        public RectTransform energyCells, turnCells, previewRoot, modal, modalContent;
        public Image energyCellTemplate, turnCellTemplate;
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
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                var relay = button.GetComponent<BattleScreenPointer>();
                if (!relay) relay = button.gameObject.AddComponent<BattleScreenPointer>();
                relay.RightClick = Back;
            }
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
            closeBrowserButton.onClick.AddListener(CloseCardBrowser);
            var descriptionPointer = detailDescription.GetComponent<BattleScreenPointer>();
            if (!descriptionPointer) descriptionPointer = detailDescription.gameObject.AddComponent<BattleScreenPointer>();
            descriptionPointer.Enter = ShowKeywords;
            descriptionPointer.Exit = () => keywordPanel.gameObject.SetActive(false);
            descriptionPointer.RightClick = Back;
            energyButton.GetComponent<BattleScreenPointer>().Hold = BeginTeleport;
            var back = gameObject.AddComponent<BattleScreenPointer>();
            back.RightClick = Back;
            backgroundObject.AddComponent<BattleScreenPointer>().LeftClick = Back;
            playerMarker.GetComponent<Image>().raycastTarget = true;
            playerMarker.gameObject.AddComponent<BattleScreenPointer>().LeftClick = Back;
            BindTooltip(energyButton, () => $"{T("energyHelp")}\nHP −{ctx?.Config.teleportHpCost}", () => T("energy"));
            BindTooltip(endTurnButton, () => T("turnHelp"), () => T("turnEnd"));
            BindTooltip(drawButton, () => T("drawHelp"), () => T("draw"));
            BindTooltip(discardButton, () => T("discardHelp"), () => T("discard"));
            BindTooltip(deckButton, () => T("deckHelp"), () => T("deck"));
            cancelButton.onClick.AddListener(Back);
            endTurnButton.onClick.AddListener(Back);
            endTurnButton.GetComponent<BattleScreenPointer>().LeftClick = () => { if (pick != null) Back(); };
            energyButton.GetComponent<BattleScreenPointer>().LeftClick = () => { if (pick != null) Back(); };
            ShowHelp();
        }

        void LateUpdate()
        {
            if (ctx == null) return;
            endTurnButton.interactable = M.CanInput && !teleportMode && !modal.gameObject.activeSelf;
            if (teleportMode && !M.CanInput) { teleportMode = false; RefreshTargets(); }
        }

        void BeginTeleport()
        {
            if (pick != null) Back();
            if (!M.CanInput || ctx.Energy <= 0 || modal.gameObject.activeSelf) return;
            ClearSelection();
            teleportMode = true;
            prompt.text = $"{T("teleport")} · HP −{ctx.Config.teleportHpCost}";
            promptRoot.SetActive(true);
            RefreshTargets();
        }

        void Back()
        {
            if (rewardScreenActive) { RewardBack(); return; }
            if (modal.gameObject.activeSelf) { CloseCardBrowser(); return; }
            if (pick != null) Resolve(null);
            ClearSelection();
            RefreshTargets();
        }

        void ClearSelection()
        {
            selectedCard = null;
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
            keywordPanel.gameObject.SetActive(false);
            helpText.gameObject.SetActive(true);
            mouseIcon.SetActive(true);
            helpText.text = T("help").Replace("\\n", "\n");
        }

        void BeginInfoHover(object owner, Action display)
        {
            hoveredInfoOwner = owner;
            hoveredInfoDisplay = display;
            display();
        }

        void EndInfoHover(object owner)
        {
            if (!ReferenceEquals(hoveredInfoOwner, owner)) return;
            hoveredInfoOwner = null;
            hoveredInfoDisplay = null;
            RestorePanel();
        }

        void RestorePanel()
        {
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
            keywordPanel.gameObject.SetActive(false);
            mouseIcon.SetActive(false);
        }

        void ShowCard(CardInstance card)
        {
            inspectedCard = card;
            helpText.gameObject.SetActive(false);
            mouseIcon.SetActive(false);
            detailArt.gameObject.SetActive(true);
            detailArt.sprite = ArtFor(card);
            detailArt.color = detailArt.sprite != null ? Color.white : new Color(0.37f, 0.57f, 0.76f);
            detailName.text = card.Data.DisplayName;
            detailMeta.text = $"{T(card.Data.type.ToString())} · {T(card.Data.rarity.ToString())}";
            detailMoves.text = string.Concat(card.Moves.Select(x => x.ToArrow()));
            detailDescription.text = card.Data.Description + "\n\n" + string.Join(" · ", card.Keywords.Select(k => T(k.ToString())));
            keywordPanel.gameObject.SetActive(false);
        }

        void ShowKeywords()
        {
            if (inspectedCard == null) return;
            var lines = inspectedCard.Keywords.Select(k => $"{T(k.ToString())}\n{T(k + "Desc")}").ToList();
            if (inspectedCard.Data.Description.Contains(T("bomb"))) lines.Add(T("bombDesc"));
            keywordText.text = string.Join("\n\n", lines);
            keywordPanel.gameObject.SetActive(lines.Count > 0);
        }



        void BindTile(Button button, Vector2Int pos)
        {
            var pointer = button.GetComponent<BattleScreenPointer>();
            if (!pointer) pointer = button.gameObject.AddComponent<BattleScreenPointer>();
            pointer.RightClick = Back;
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



        int? shownGold, shownHp, shownMaxHp, shownEnergy, shownMaxEnergy, shownTurns, shownTurnLimit, shownDraw, shownDiscard;
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
                energyStrip.Render(energyCellTemplate, ctx.Energy, Mathf.Max(ctx.MaxEnergy, ctx.Energy));
            }
            int turns = ctx.IsOver ? 0 : Mathf.Max(0, ctx.Config.turnLimit - ctx.Turn + 1);
            if (shownTurns != turns || shownTurnLimit != ctx.Config.turnLimit || languageChanged)
            {
                shownTurns = turns; shownTurnLimit = ctx.Config.turnLimit;
                turnsText.text = $"{T("turnEnd")}  {turns}/{ctx.Config.turnLimit}";
                turnStrip.Render(turnCellTemplate, turns, ctx.Config.turnLimit);
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
        }

        void RefreshCardView(Button view, CardInstance card)
        {
            bool gray = !rewardScreenActive && (card.IsGrayed || card.Has(Keyword.Lock) || ctx != null && ctx.Energy <= 0);
            CachedCard(view).Render(ArtFor(card), gray ? grayscaleMaterial : null,
                gray ? Grayed : CardBase, card.Moves, directionIcons,
                selectedCard == card || pickable != null && pickable.Contains(card));
        }

        void RefreshEnemy(Button button, Enemy enemy)
        {
            var view = unitViews[button];
            int bonus = 0;
            foreach (var tileEffect in ctx.Board.Get(ctx.Player.Pos).Effects)
                if (tileEffect.Trigger == TileTrigger.TurnEndStanding && tileEffect.Source == enemy)
                    foreach (var effect in tileEffect.Effects)
                        if (effect is GainMoraleEffect gain) bonus += gain.amount;
            view.SetMorale(enemy.Hp, bonus, enemy.Data.startMorale);
            if (view.NeedsIntent(enemy.CurrentIntent, Loc.Language))
            {
                var lines = new List<string>();
                var intent = enemy.CurrentIntent;
                if (intent != null)
                {
                    foreach (var tile in intent.tileEffects)
                        lines.Add($"{TriggerName(tile.trigger)} ({tile.pos.x + 1}, {tile.pos.y + 1}) : " + string.Join(" / ", tile.effects.Select(DescribeEffect)));
                    lines.AddRange(intent.turnEndActions.Select(DescribeEffect));
                }
                view.SetIntent(intent, Loc.Language, T("intent") + "\n" + string.Join("\n", lines));
            }
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
            var pos = ctx.Player.Pos;
            foreach (var direction in card.Moves)
            {
                var next = pos + direction.ToOffset();
                if (!ctx.Board.IsPassable(next)) break;
                previewView.AddSegment(tiles[pos].transform.position, tiles[next].transform.position);
                pos = next;
            }
            ghost.gameObject.SetActive(pos != ctx.Player.Pos);
            ghost.rectTransform.position = tiles[pos].transform.position;
            ghost.transform.SetAsFirstSibling();
        }


    }
}
