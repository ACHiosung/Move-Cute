using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoveCute
{
    /// <summary>
    /// 손패에 없는 카드(휴지통·더미 등)를 목록에서 고르는 창. IPlayerInput.PickCardFromList 구현 (재활용 등).
    ///
    /// 덱/더미 보기 창(modal)을 처음 쓸 때 한 번 복제해서 이 파일만 쓰는 창으로 만든다.
    /// 원래 창의 상태(열림·닫힘, 보던 카드 목록)를 건드리지 않고, 기존 UI 코드는 이 파일을 참조하지 않는다.
    /// 이 파일을 지우면 PickCardFromList는 IPlayerInput의 기본 구현(PickCard)으로 돌아간다.
    ///
    /// 고르기 전에는 닫을 수 없다 (닫기 버튼 숨김, 우클릭 무시). 카드를 클릭하면 그 카드로 확정된다.
    /// </summary>
    public partial class BattleScreenUI
    {
        RectTransform listPicker;
        Text listPickerTitle;
        BattleCardCollectionView listPickerView;
        ScrollRect listPickerScroll;
        AwaitableCompletionSource<CardInstance> listPick;

        public async Awaitable<CardInstance> PickCardFromList(IReadOnlyList<CardInstance> candidates, string message)
        {
            if (candidates == null || candidates.Count == 0) return null;
            if (!EnsureListPicker()) return await PickCard(candidates, message);

            ResolveListPick(null);   // 이전 요청이 남아 있으면 정리
            var source = new AwaitableCompletionSource<CardInstance>();
            listPick = source;

            ClearSelection();
            listPickerTitle.text = message;
            listPicker.gameObject.SetActive(true);
            listPicker.SetAsLastSibling();
            listPickerView.Render(candidates, cardPrefab, BindListPickCard);
            FitListPicker(candidates.Count);
            return await source.Awaitable;
        }

        /// <summary>덱/더미 보기 창을 복제해 목록 선택 창을 만든다. 원래 창이 없으면 false.</summary>
        bool EnsureListPicker()
        {
            if (listPicker != null) return true;
            if (modal == null || modalContent == null) return false;

            var copy = Instantiate(modal.gameObject, modal.parent, false);
            copy.name = "CardListPicker";
            copy.SetActive(false);
            listPicker = (RectTransform)copy.transform;

            listPickerTitle = CopyOf(modalTitle) as Text;
            listPickerScroll = CopyOf(cardBrowserScroll) as ScrollRect;
            var content = CopyOf(modalContent) as RectTransform;
            listPickerView = content != null ? content.GetComponent<BattleCardCollectionView>() : null;
            if (listPickerTitle == null || listPickerView == null)
            {
                Destroy(copy);
                listPicker = null;
                return false;
            }

            // 원래 창에 그려져 있던 카드 슬롯까지 복제되므로 비운다. 슬롯은 Render가 새로 만든다.
            foreach (Transform child in content)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            var close = listPicker.Find("Close");
            if (close != null) close.gameObject.SetActive(false);
            return true;
        }

        /// <summary>원래 창 안의 컴포넌트에 대응하는 복제본 쪽 컴포넌트.</summary>
        Component CopyOf(Component original)
        {
            if (original == null) return null;
            var path = new Stack<string>();
            for (var t = original.transform; t != modal; t = t.parent)
            {
                if (t == null) return null;   // 원래 창 밖의 컴포넌트
                path.Push(t.name);
            }
            var found = path.Count == 0 ? listPicker : listPicker.Find(string.Join("/", path));
            return found != null ? found.GetComponent(original.GetType()) : null;
        }

        void BindListPickCard(Button button, CardInstance card)
        {
            CachedCard(button).Bind(card, ResolveListPick,
                (view, c) => BeginInfoHover(view, () => ShowCard(c)), view => EndInfoHover(view));
            CardMoveDisplay.GetRange(card, MoveDisplayContext, out int start, out int count);
            // 고를 수 있는 후보라는 표시로 테두리를 켠다. 흑백은 잠금만 (에너지·이동 가능 여부와 무관하게 고를 수 있음)
            // 무료·해금 카드 테두리는 손패(RefreshCardView)와 같은 기준으로 표시한다
            bool showFreeFrame = card.Has<Keyword_Free>() || card.IsUnlocked;
            CachedCard(button).Render(ArtFor(card), card.IsLocked ? grayscaleMaterial : null,
                card.IsLocked ? Grayed : CardBase, CardMoveDisplay.VisibleMoves(card), directionIcons, true, showFreeFrame, start, count);
        }

        /// <summary>카드 수에 맞춰 스크롤 영역 높이를 맞춘다 (원래 창의 RenderModal과 같은 계산).</summary>
        void FitListPicker(int count)
        {
            var content = (RectTransform)listPickerView.transform;
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null && listPickerScroll != null)
            {
                int rows = (count + grid.constraintCount - 1) / grid.constraintCount;
                float height = rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    Mathf.Max(listPickerScroll.viewport.rect.height, height));
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            if (listPickerScroll == null) return;
            listPickerScroll.StopMovement();
            listPickerScroll.verticalNormalizedPosition = 1;
        }

        /// <summary>선택 확정(또는 정리할 때 null). 창을 닫고 기다리던 쪽에 결과를 넘긴다.</summary>
        void ResolveListPick(CardInstance card)
        {
            var source = listPick;
            if (source == null) return;
            listPick = null;   // 결과 처리 중에 새 요청이 올 수 있으므로 먼저 비운다

            if (hoveredInfoOwner is Button owner && owner.transform.IsChildOf(listPicker)) EndInfoHover(owner);
            listPickerView.Render(System.Array.Empty<CardInstance>(), cardPrefab, BindListPickCard);
            listPicker.gameObject.SetActive(false);
            RestorePanel();
            source.SetResult(card);
        }
    }
}
