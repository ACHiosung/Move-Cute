# 잠금/해금 — UI 변경 설명과 병합 주의점

## 비교 기준과 최종 표시 규칙

- 기준: 로컬 `main`의 `d6ae921` → `feature/lock`의 구현 커밋 `4746590`.
- 줄 번호는 `4746590` 기준이다. 후속 병합에서는 함수 이름을 함께 확인한다.
- 핵심 BattleContext 변경은 [LOCK_BATTLE_CONTEXT_CHANGES.md](LOCK_BATTLE_CONTEXT_CHANGES.md)를 참고한다.
- 기존 공용 씬·UI 파일과 겹치는 변경을 우선 설명하고, 새 파일은 역할만 요약한다.

| 상태 | 카드 아이콘 / 상세 이동 목록 | 보드 경로 미리보기 |
|---|---|---|
| 일반 카드 | 원래 이동 표시 | 기존 방식 유지 |
| 잠금 카드 | 원래 순서 유지, 유물로 생략된 이동만 `#292929` | 기존 경로 미리보기 유지 |
| 해금 카드 | 이동 표시 숨김 | 숨김 |
| 자동 잠금 후 | 잠금 상태가 실제로 남으면 표시 복원 | 다음 미리보기 호출에서 현재 상태 반영 |
| 전부 생략되는 카드 | 즉시 해금되므로 이동 표시 숨김 | 숨김 |

보상 화면/전투 종료 후에는 현재 전투 유물의 생략 색상을 적용하지 않는다. 카드 자체가 해금 상태이면 이동 숨김 규칙은 유지한다. 카드의 원래 Moves를 지우는 변경은 없다.

## 기존 파일 변경 목록

| 기존 파일 | 변경 함수 / 항목 | 충돌 가능 지점 |
|---|---|---|
| `Assets/Scripts/UI/BattleScreenUI.Layout.cs` | `ShowCard`, `MoveDisplayContext`, `RefreshCardView`, `Preview` | 상세 패널, 카드 렌더 호출, 비용·흑백 판정, 미리보기 |
| `Assets/Scripts/UI/BattleCardView.cs` | `Render` | 렌더 함수 인자, 아이콘 캐시, 오브젝트 풀 재사용 |
| `Assets/Scripts/UI/BattleScreenUI.cs` | `RefreshCard` | 손패 뷰 Dictionary 조회 |
| `Assets/Scripts/UI/TempBattleUI.cs` | `RefreshCard` | 손패 뷰 조회, 문자열 포맷 |
| `Assets/Resources/UI/BattleScreenText.txt` | `IgnoredMovesDesc` | 3열 로컬라이즈 키 추가 |
| `Assets/Scenes/BattleUIScreen.unity` | `startingDeck`, `relics`의 직렬화 값 | YAML 참조와 테스트 설정 |

## 1. BattleScreenUI.Layout.cs

### ShowCard(CardInstance card) — L193

main에서는 원래 이동을 바로 문자열로 만들었다.

```csharp
detailMoves.text = string.Concat(card.Moves.Select(x => x.ToArrow()));
```

최종 코드의 L203~205는 다음과 같다.

```csharp
detailMoves.supportRichText = true;
detailMoves.text = CardMoveDisplay.FormatArrows(card, MoveDisplayContext);
detailMoves.gameObject.SetActive(!card.IsUnlocked && card.Moves.Count > 0);
```

1. `supportRichText`를 켜서 `<color>` 태그를 실제 색상으로 해석한다.
2. 공통 포맷 함수를 통해 해금 여부와 생략 구간을 반영한다. 해금은 빈 문자열, 잠금은 생략된 이동만 색상 태그를 붙인 문자열이다.
3. 해금/이동 없는 카드의 상세 이동 오브젝트를 숨긴다. 다음에 잠금/일반 카드를 표시할 때 조건을 다시 계산해 복원한다.

기존 이름·종류·희귀도·아트·카드 설명·키워드 목록 생성은 유지했다. 설명을 만든 직후 L207~209에 다음을 추가했다.

```csharp
CardMoveDisplay.GetRange(card, MoveDisplayContext, out _, out int moveCount);
if (card.IsLocked && moveCount < card.Moves.Count)
    detailDescription.text += "\n" + T("IgnoredMovesDesc");
```

시작 인덱스는 버리고 남은 이동 개수만 비교한다. 실제로 이동이 생략된 잠금 카드에만 색상 안내를 덧붙인다. 기존 설명을 다시 생성한 뒤 추가하므로 `ShowCard` 재호출 시 문구가 중복 누적되지 않는다.

**병합 시 주의:** 원래 `Moves`의 단순 문자열 출력으로 되돌리면 상세 정보만 다시 전체 기본 색상으로 표시된다. Rich Text를 끄면 태그가 문자로 노출된다. `SetActive(false)`만 남기고 복원 조건을 제거하면 다른 카드의 상세 이동도 계속 숨겨진다. 상세 패널을 TMP 등으로 바꾼다면 태그 지원과 표시/숨김을 새 위젯에 맞춰 함께 옮긴다.

### MoveDisplayContext — L314, 기존 파일에 추가한 조회 속성

```csharp
BattleContext MoveDisplayContext =>
    !rewardScreenActive && ctx != null && !ctx.IsOver ? ctx : null;
```

현재 진행 중인 전투에서만 유물의 이동 생략 효과를 시각화한다. 보상 화면 또는 전투 종료 후에는 null을 넘겨, 직전 전투의 유물 설정이 보상 카드에 적용된 것처럼 보이지 않게 한다.

**병합 시 주의:** `ctx`가 존재하는지만 검사하면 전투가 끝난 보상 화면에도 생략 색상이 남을 수 있다. 보상·전투 단계 상태 변수를 리팩터링하면 이 조건도 새 흐름에 맞춰 반영한다.

### RefreshCardView(Button view, CardInstance card) — L316

#### 카드 전체 흑백 조건 — L318

```csharp
// main
card.IsGrayed || card.Has<Keyword_Lock>() || ctx != null && ctx.Energy <= 0

// 최종 (바깥 !rewardScreenActive 조건은 유지)
card.IsGrayed || card.IsLocked || ctx != null && ctx.Energy < card.PlayCost(ctx)
```

통합 키워드는 해금 상태에도 존재하므로 `Has<Keyword_Lock>()`를 상태 검사로 사용하면 해금 카드도 흑백이 된다. `IsLocked`로 바꿔 실제 잠금만 반영했다.

에너지가 0이라는 이유만으로 흑백 처리하지 않고 실제 사용 코스트와 비교한다. 해금과 무료 카드는 0 에너지에서도 표시상 사용 가능하다. 이 비용 비교 변경은 일반 무료 카드에도 적용된다.

#### 아이콘 렌더 데이터 — L319~322

```csharp
CardMoveDisplay.GetRange(card, MoveDisplayContext, out int start, out int count);
CachedCard(view).Render(ArtFor(card), gray ? grayscaleMaterial : null,
    gray ? Grayed : CardBase, CardMoveDisplay.VisibleMoves(card), directionIcons,
    selectedCard == card || pickable != null && pickable.Contains(card), start, count);
```

- `VisibleMoves`: 해금이면 빈 목록, 그 외에는 원래 이동 목록을 전달한다.
- `start/count`: 정상 색상으로 그릴 해금 필요 구간을 전달한다. 나머지는 어둡게 표시한다.
- 선택 테두리, 대상 선택 후보 표시, 카드 아트와 배경색 처리는 유지했다.
- 기존 `BindBrowserCard`와 `BindRewardCard`도 이 함수를 호출하므로 더미 조회·보상 카드의 공통 렌더 경로에 적용된다. 해당 Bind 함수 자체는 수정하지 않았다.

**병합 시 주의:** 아이콘에 `card.Moves`를 그대로 전달하면 해금 상태에서도 화살표가 남는다. `start/count`를 누락하면 잠금 카드의 생략 색상이 사라진다. 새 렌더 인자와 호출부를 함께 병합한다. 이동 배열을 실제로 잘라서 전달하면 생략된 이동의 위치를 보여줄 수 없으므로, 잠금 상태에서는 전체 목록을 유지한다.

### Preview(CardInstance card) — L375

```csharp
ClearPreview();
if (card.IsUnlocked) return;
```

기존 미리보기를 먼저 지운 다음 해금 카드에서는 새 경로·도착 고스트를 만들지 않는다. 이 변경은 카드 아이콘 숨김과 별개다.

**병합 시 주의:** return을 `ClearPreview` 앞으로 옮기면 직전에 본 다른 카드의 경로가 남는다. 잠금 카드의 경로 미리보기 방식을 해금 필요 구간으로 바꾸는 별도 변경은 이번 범위에 없다.

## 2. BattleCardView.cs

### Render(...) — L76

기존 인자 뒤에 선택 인자 두 개를 추가했다.

```csharp
int activeMoveStart = 0, int activeMoveCount = int.MaxValue
```

둘은 정상 색상으로 표시할 구간이다. 기본값은 전체 정상 표시이므로 기존 인자를 사용하는 일반 호출은 소스 수준에서 그대로 호출할 수 있다. 메서드 그룹·리플렉션 등 정확한 인자 수에 의존하는 외부 사용처가 있다면 별도 확인이 필요하다.

기존 `displayedMoves` 캐시는 이동 개수/방향이 바뀌었을 때만 아이콘 생성, Sprite·크기·좌표·활성 상태를 갱신한다. 이 구조를 유지하고, 그 바깥 L119~122에 다음을 추가했다.

```csharp
directionRoot.gameObject.SetActive(moves.Count > 0);
for (int i = 0; i < moves.Count; i++)
    icons[i].color = i >= activeMoveStart && i - activeMoveStart < activeMoveCount
        ? Color.white : CardMoveDisplay.IgnoredColor;
```

빈 목록이면 아이콘 묶음을 숨긴다. 정상 구간은 흰색 틴트로 원래 이미지 색을 유지하고, 생략 구간은 `#292929` 틴트를 적용한다. 같은 방향 목록이어도 유물/상태가 바뀔 수 있으므로 색상은 매 렌더마다 갱신한다.

**병합 시 주의:** 이 코드를 `if (changed)` 안으로 넣으면 이동 목록이 같은 카드의 색상이 갱신되지 않는다. 풀에서 쓰던 뷰를 다른 카드에 재사용할 때 생략 색상이 남을 수 있다. 정상 구간의 `Color.white` 복원도 반드시 남긴다. 화살표를 숨긴 뒤 재잠금할 때 `directionRoot`와 개별 아이콘의 재활성화가 함께 작동해야 한다. 프리팹·씬의 참조 구조 변경은 하지 않았다.

## 3. BattleScreenUI.cs

### RefreshCard(CardInstance card) — L451

```csharp
// main
var view = cards[card];

// 최종
if (!cards.TryGetValue(card, out var view)) return;
```

`cards`는 현재 손패에 표시된 뷰의 대응표다. 잠금 추적은 뽑기 더미·묘지까지 처리하므로 `CardChanged`의 카드가 반드시 이 표에 있다고 가정할 수 없다.

기존 코드에서는 손패 밖 카드가 먼저 해금될 때 KeyNotFoundException이 발생했다. 상태를 바꾼 뒤 UI에서 예외가 발생해, 나머지 카드의 해금 검사와 현재 이동 해결까지 중단될 수 있었다. 최종 코드는 뷰가 없으면 손패 렌더만 생략한다. 상태 변경 자체는 BattleContext에서 이미 끝났다.

**유지한 기존 처리:** `CardChanged` 함수 자체는 수정하지 않았다. 그 함수는 `RefreshCard` 이후, 해당 카드가 상세 정보의 `inspectedCard`라면 `ShowCard`와 열려 있던 키워드 설명을 다시 갱신한다. 따라서 손패 뷰가 없다는 이유로 `CardChanged` 전체를 조기 반환하도록 바꾸지 않는다. 상세 정보 갱신이 누락될 수 있다.

**병합 시 주의:** Dictionary 인덱서 접근을 복원하지 않는다. 브라우저 카드나 묘지 카드에도 변경 알림이 올 수 있다는 Presenter 계약을 유지한다. `CardChanged`나 `IBattlePresenter`의 시그니처 변경은 필요하지 않다.

## 4. TempBattleUI.cs

### RefreshCard(CardInstance card) — L383

정식 UI와 동일하게 손패 뷰 조회를 `TryGetValue`로 변경했다. 뷰가 없으면 해당 손패 표시만 생략한다. 이 파일의 `CardChanged` 호출 구조도 유지했다.

이동 문자열 출력은 L392에서 다음처럼 변경했다.

```csharp
(card.IsUnlocked ? "" :
    $"<size=36>{CardMoveDisplay.FormatArrows(card, ctx)}</size>\n")
```

해금이면 화살표와 그 줄의 줄바꿈을 넣지 않는다. 다른 카드에는 공통 포맷을 적용하여 생략된 이동을 어둡게 표시한다. 에너지·이름·설명·대상 선택 배경 등의 기존 출력은 유지했다.

**병합 시 주의:** 텍스트 기반 UI이므로 정식 이미지 아이콘의 Render를 호출하는 방식으로 옮기지 않는다. 기존 Rich Text 출력과 함께 색상 태그를 해석할 수 있어야 한다. 이 임시 UI는 정식 UI의 보상 화면 조건 속성을 쓰지 않고 기존 `ctx`를 직접 전달한다.

## 5. 리소스·씬 등 충돌 가능한 데이터

### Assets/Resources/UI/BattleScreenText.txt — L61

다음 키를 추가했다. 나머지 기존 키는 유지했다.

```text
IgnoredMovesDesc|어두운 이동은 해금 조건에서 제외됩니다.|Darkened moves are excluded from the unlock sequence.
```

기존 파서는 `|` 기준 키/한국어/영어 세 칸을 읽는다. CSV 형식으로 바꾸거나 문구에 구분자를 추가하지 않는다. 동일 키가 다른 브랜치에도 추가되었다면 하나로 합친다.

`Assets/Resources/Localization/Strings.csv`에는 새 두 유물의 이름·설명을 추가했다. 설명은 `{moveCount}`를 사용하고, 전부 생략되면 즉시 해금한다는 최종 규칙을 반영한다. 이동을 최소 한 개 남긴다는 과거 문구를 되살리지 않는다.

### Assets/Scenes/BattleUIScreen.unity

직렬화된 테스트 설정에 main 대비 다음 차이가 있다.

- `startingDeck`: main에서 기존 카드였던 네 항목이 `Card_Lock` 참조로 바뀌어 현재 잠금 카드가 네 장 들어 있다.
- `relics`: main의 빈 목록에서 `Relic_IgnoreLastLockMoves` 한 개를 장착하는 설정으로 바뀌었다.
- 해당 유물 에셋의 현재 `moveCount`는 테스트용 5다. 코드의 필드 기본값은 1이지만 저장된 에셋 값이 우선한다.

이 설정들은 작업 중 사용자가 변경한 내용을 보존해 커밋한 것이다. 이번 UI 구현 때문에 씬의 레이아웃·컴포넌트 배치·프리팹 구조를 변경한 것은 아니다.

**병합 시 주의:** 씬 전체를 한쪽 버전으로 교체하지 않는다. UI 담당자의 레이아웃 변경은 보존하고 BattleManager의 카드/유물 참조를 별도 검토한다. 위 테스트 덱과 유물 값이 공용 씬에 필요한지는 PM이 결정할 수 있다. 새 에셋의 `.meta` GUID도 함께 병합해야 참조가 유지된다.

## 6. 새 파일 — 개요

`Assets/Scripts/UI/CardMoveDisplay.cs`는 아래 표시 규칙만 모은 정적 도우미다.

- `VisibleMoves`: 해금이면 빈 목록, 그 외 원래 이동 목록.
- `GetRange`: 잠금+전투 문맥이 있으면 BattleContext의 실제 판정 구간을 조회.
- `FormatArrows`: 생략된 이동을 어둡게 표시하는 Rich Text 생성.
- `IgnoredColor/Hex`: 이미지 아이콘과 텍스트의 공통 색상값.

게임 상태나 원본 이동 목록을 변경하지 않는다. UI에서 유물 효과를 별도 계산하지 않고 `GetLockMoveRange`에 위임하므로 실제 판정과 표시가 같은 기준을 사용한다. 새 메타 파일은 스크립트 GUID만 추가한다.

## 7. 병합 후 화면 확인 항목

아래 항목은 확인할 기대 동작이며 실제 실행 완료를 뜻하지 않는다.

1. 일반 카드는 전체 이동이 기존 색상으로 보인다.
2. 앞 생략/뒤 생략/동시 장착 시 생략된 화살표만 어두워진다. 카드와 상세 정보가 일치한다.
3. 해금되면 손패·더미 조회의 카드 화살표, 상세 이동 목록, 보드 경로가 숨겨진다.
4. 다시 잠기면 원래 화살표와 생략 색상이 복원된다. 전부 생략이면 해금이 유지된다.
5. 같은 이동 배열의 다른 카드로 뷰를 재사용해도 어두운 색이나 숨김 상태가 남지 않는다.
6. 보상 화면에는 직전 전투 유물의 색상 효과가 남지 않는다.
7. 0 에너지의 해금 카드는 에너지 부족 사유로 흑백 처리되지 않는다.
8. 손패 밖 카드가 해금되어도 예외가 발생하지 않고, 보고 있던 상세 정보는 갱신된다.

키워드 테스트 파일은 사용자 요청으로 삭제했다. 런타임 컴파일은 통과했으나 Unity 라이선스 초기화 문제로 실제 화면/플레이 실행 검증은 완료하지 못했다.
