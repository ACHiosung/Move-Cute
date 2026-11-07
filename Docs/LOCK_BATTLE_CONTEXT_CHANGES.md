# 잠금/해금 — BattleContext·Event 변경 설명

## 비교 기준과 범위

- 기준: 로컬 `main`의 `d6ae921` → `feature/lock`의 구현 커밋 `4746590`.
- 중간 커밋의 구현 과정보다 최종 코드의 동작을 설명한다. 줄 번호는 `4746590` 기준이며, 병합 후에는 함수 이름으로 찾는다.
- 기존 파일의 함수 이름·인자·반환형은 유지했다. 핵심 새 로직은 `BattleContext.Lock.cs` partial 파일에 분리했다.
- `BattleEvents.cs`와 `IBattlePresenter.cs`는 변경하지 않았다. 새 이벤트도 추가하지 않았다.
- UI 쪽 상세 변경은 [LOCK_UI_CHANGES.md](LOCK_UI_CHANGES.md)를 참고한다.

## 기존 파일 변경 목록

| 기존 파일 | 함수 | 변경 목적 |
|---|---|---|
| `Assets/Scripts/Battle/BattleContext.cs` | `PlayCard` | 잠금 사용 거부, 질주 반복 완료 후 자동 잠금 |
| 같은 파일 | `RefreshPlayable` | 잠금 상태를 손패 흑백 판정에 명시적으로 반영 |
| `Assets/Scripts/Battle/BattleContext.Board.cs` | `MovePlayer` | 해금 카드의 이동 차단, 성공한 한 칸마다 잠금 이동 검사 |
| 같은 파일 | 콜백을 받는 `MovePlayerAlong` | 해금 카드의 이동·이동 콜백·도착 효과 전체 생략 |
| 같은 파일 | `CanFullyMove` | 잠금은 false, 해금은 경로와 무관하게 true |
| `Assets/Scripts/Battle/BattleContext.Cards.cs` | `AddKeyword` | 통합 키워드의 상태 전환, null 방어, 즉시 흑백 상태 갱신 |
| `Assets/Scripts/Battle/BattleManager.cs` | `PlayCard` | 대상 선택 전에 잠금 카드 거부 |

## 1. BattleContext.cs

### PlayCard(CardInstance card, EffectTarget target) — L142

#### 사용 전 잠금 검사 — L145

기존 손패·행동 가능 여부 검사와 에너지 검사 사이에 다음을 추가했다.

```csharp
if (!CanAct || !Piles.Hand.Contains(card)) return false;
if (card.IsLocked) return false; // 추가
if (Energy < card.PlayCost(this)) { ... }
```

잠금 카드는 에너지 지불, 손패 제거, `PlayingCard` 지정, 카드 연출·효과 실행 전에 거부한다. UI를 거치지 않고 이 함수를 호출해도 같은 규칙을 적용한다. `false` 반환은 사용이 시작되지 않았다는 의미다.

**병합 시 주의:** 잠금 검사를 `SpendEnergy`나 `Piles.Hand.Remove` 뒤로 옮기지 않는다. 거부된 카드가 비용을 내거나 손패에서 빠지면 안 된다. 기존 행동 단계 잠금 및 try/finally 구조는 유지한다.

#### 질주 반복 후 자동 잠금 — L164

```csharp
// 기존 질주 반복 for문 종료 후
if (card.AutoLock) await LockCard(card); // 추가
await Events.CardPlayed.Raise(card);
```

`AutoLock`은 통합 `Keyword_Lock.autoLock`을 읽는다. 카드 전체에 별도 bool 필드를 추가한 구조가 아니다. 자동 잠금은 최초 사용과 질주 재사용이 모두 끝난 뒤 한 번 적용한다. `LockCard`는 유물 때문에 해금에 필요한 이동이 0이면 결과를 즉시 해금으로 정규화한다.

이 시점에는 카드의 `Finish`가 이미 수행되어 묘지나 휴지통에 들어갔을 수 있다. 손패 UI가 없어도 상태 변경 통지가 가능해야 한다. UI의 `TryGetValue` 수정이 함께 필요한 이유다.

**이벤트 순서:** 기존 `CardReused`는 각 재사용 후, 자동 잠금은 전체 반복 후, 기존 `CardPlayed`는 그 뒤다. `CardPlayed` 구독자는 자동 잠금까지 반영된 최종 상태를 읽는다. 전투가 반복 도중 끝나 기존 조기 반환을 통과한 경우에는 종료 후 상태 변경을 추가로 수행하지 않는다.

**병합 시 주의:** 자동 잠금을 반복문 안으로 옮기면 해금 질주의 후속 재사용에 영향을 준다. 기존 재사용 횟수 계산, 승패 판정, 입력 복구 코드를 통째로 교체하지 않고 이 연결 위치만 유지한다.

### RefreshPlayable() — L280

```csharp
// main
foreach (var c in Piles.Hand) c.IsGrayed = !c.CanPlayFully(this);

// 최종
foreach (var c in Piles.Hand) c.IsGrayed = c.IsLocked || !c.CanPlayFully(this);
```

특수 카드가 `CanPlayFully`를 재정의해 true를 반환하더라도 잠금 카드의 손패 흑백 상태를 보장한다. 에너지 부족 여부는 별도 UI 조건이며 여기서 추가하지 않는다.

**병합 시 주의:** 이 값은 카드의 캐시된 표시 상태다. 잠금 상태 전환 직후의 갱신은 새 partial 파일에서도 수행하고, 행동 종료 후에는 이 함수가 손패 전체를 다시 갱신한다. UI의 잠금 검사만 남기고 이 갱신을 제거하면 다른 표시 구현과 불일치할 수 있다.

## 2. BattleContext.Board.cs

### MovePlayer(EffectSource src, Direction dir) — L12

#### 이동 주체 검사 — L15

```csharp
if (IsOver) return false;
if (src.Owner is CardInstance card && card.IsUnlocked) return false; // 추가
```

이동 주체가 해금 카드이면 위치 계산·변경 전에 종료한다. 현재 구현은 카드의 기본 이동뿐 아니라 그 카드가 같은 `EffectSource`로 발생시키는 `MovePlayer` 호출에도 적용된다. 카드 외 주체의 이동은 이 조건으로 차단하지 않는다.

이 함수의 false는 이동하지 않았다는 의미다. 카드 사용 전체의 실패를 뜻하지 않으며, 해금 카드의 효과 실행 여부는 `CanPlayFully`/`CanFullyMove`의 성공 판정으로 유지한다.

#### 성공한 이동을 잠금 검사에 전달 — L22

```csharp
Player.Pos = next;
await Presenter.PlayerStepped(from, next);
await TrackLockMovement(dir); // 추가
await Events.PlayerMoved.Raise(from, next);
await TriggerTile(next, TileTrigger.Pass);
```

실제 위치 변경과 이동 연출 이후, 기존 `PlayerMoved` 이벤트보다 먼저 잠금 조건을 검사한다. 한 카드의 이동이 3개 모두 성공하면 이 검사도 3번 수행된다. 카드 사용이 끝날 때까지 기다리는 방식이 아니다.

- 막힌 이동은 이전 검사에서 반환하므로 기록하지 않는다.
- 해금 카드의 생략된 이동도 기록하지 않는다.
- `Teleport`는 기존 `PlayerMoved`를 발생시키지만 이 함수를 통과하지 않으므로 HP 소모 순간이동은 기록하지 않는다.
- 진행도는 턴 종료 때 초기화하지 않는다. 여러 카드·여러 턴에 걸쳐 연속 방향이 일치할 수 있다.
- 해금과 그 UI 갱신까지 await한 뒤 기존 이동 이벤트와 Pass 타일 효과로 진행한다.

**병합 시 주의:** 이 호출을 카드 종료 처리로 옮기거나 `PlayerMoved` 구독으로 대체하면 한 칸 단위 검사 또는 순간이동 제외 규칙이 달라진다. 이동 주체·실제 방향·효과 실행 순서를 함께 확인한다. 새 await의 예외도 기존 호출자에게 전파되므로 손패 밖 카드의 UI 갱신 방어를 유지한다.

### MovePlayerAlong(..., Func<Vector2Int, Vector2Int, Awaitable> onStep) — L39

```csharp
if (src.Owner is CardInstance card && card.IsUnlocked) return; // 추가
```

이동 반복 전에 해금 카드를 종료한다. 위치 변경뿐 아니라 `onStep` 콜백과 마지막 Arrive 타일 효과까지 생략한다. 따라서 떠난 칸에 장애물을 남기는 카드도 해금 상태에서는 이동에 따른 설치를 수행하지 않는다. 카드 자체의 `Data.effects`까지 생략하는 코드는 아니다.

콜백 없는 두 인자 오버로드는 원래부터 이 오버로드에 위임하며, 이번 변경에서도 그 구조를 유지했다.

**병합 시 주의:** `MovePlayer`의 차단만으로 대체하지 않는다. 경로 전체를 생략한다는 의도가 이 위치에 있다. 해금 카드에도 콜백이나 도착 효과를 별도로 발생시키는 분기를 추가하지 않는다.

### CanFullyMove(CardInstance card) — L58

기존 경로 시뮬레이션 앞에 다음을 추가했다.

```csharp
if (card.IsLocked) return false;
if (card.IsUnlocked) return true;
```

잠금은 전체 효과 발동 불가, 해금은 경로가 막혀 있어도 효과 발동 가능으로 판정한다. 해금의 true는 실제로 이동했다는 뜻이 아니라 이동 조건을 면제받았다는 뜻이다. 그 외 카드는 기존 보드 통과 가능 검사를 수행한다.

이 함수는 비용 계산, 카드 연출의 `fullyMoved`, 질주 반복 조건에도 사용되므로 단순 UI 함수로 취급하지 않는다. 잠금 카드의 실제 사용 거부는 별도 `PlayCard` 검사가 보장한다.

## 3. BattleContext.Cards.cs

### AddKeyword(CardInstance card, CardKeyword kw) — L38

#### 통합 키워드 분기

```csharp
if (kw is Keyword_Lock lockKeyword)
{
    await SetLockState(card, lockKeyword.isUnlocked, lockKeyword.autoLock);
    return;
}
```

main에서는 같은 키워드 타입이 이미 있으면 부여를 무시했다. 이제 잠금과 해금이 같은 타입이므로 타입 중복 여부로 반환하면 상태를 전환할 수 없다. 이 분기를 일반 중복 검사 앞에 두어, 기존 객체의 `isUnlocked`와 명시적으로 전달된 `autoLock`을 반영한다. 없을 때만 새 객체를 만들고, 전달받은 키워드 객체를 카드와 공유하지 않는다.

새 partial의 내부 처리에서 전투 종료/null 검사, 이동 기록 정리, 전부 생략된 조건의 즉시 해금, `IsGrayed` 갱신, `CardChanged` 통지까지 완료한다. 따라서 이 분기 뒤에는 return이 필요하다.

#### 다른 키워드의 기존 경로

```csharp
if (IsOver || card == null || kw == null || card.Has(kw)) return;
card.Keywords.Add(kw.Clone());
card.IsGrayed = card.IsLocked || !card.CanPlayFully(this);
await Presenter.CardChanged(card);
```

`card == null` 방어와 표시 상태 재계산을 추가했다. 다른 키워드는 기존처럼 복사본을 추가하고 중복 타입을 거부한다. UI 통지 전 상태를 확정해 표시가 과거 값을 읽지 않도록 한다.

**병합 시 주의:** 일반 중복 검사를 통합 키워드 분기 앞으로 옮기지 않는다. 키워드 에셋의 객체를 직접 추가하거나, 통합 키워드를 지웠다가 다시 추가해 `autoLock` 등 설정을 잃지 않도록 한다.

## 4. BattleManager.cs

### PlayCard(CardInstance card) — L79

```csharp
if (!CanInput) return;
if (card == null || card.IsLocked) return; // 추가
```

기존 `ChooseTarget` 진입 전에 잠금 카드를 거부한다. 사용할 수 없는 카드가 적·타일 선택 UI를 열지 않도록 하는 입력 측 방어다. BattleContext의 사용 차단과 역할이 다르므로 둘 다 유지한다. `choosingTarget` 플래그와 취소 처리, 전투 문맥 변경 검사 등 이후 코드는 유지했다.

## 5. Event와 Presenter 계약

| 항목 | 최종 상태와 의미 |
|---|---|
| `BattleEvents.cs` | 변경 없음. 새 이벤트 필드나 Raise 위치를 추가하지 않음 |
| `PlayerMoved` | 기존 이벤트 유지. 잠금 추적은 여기에 구독하지 않고 성공 이동 함수에서 직접 호출 |
| `BattleStart` | 기존 이벤트에 새 유물의 Bind가 `UnlockCompletedLockCards`를 구독. 첫 드로우 전에 전부 생략된 카드 해금 |
| `CardReused` / `CardPlayed` | 기존 이벤트 유지. 자동 잠금 호출을 두 이벤트 흐름 사이의 전체 반복 종료 위치에 추가 |
| `IBattlePresenter.CardChanged` | 시그니처 변경 없음. 새 잠금 처리에서도 호출하므로 손패 밖 카드까지 인자로 올 수 있음 |
| 전용 해금 이벤트 | 현재 없음. 추후 해금 시 피해 유물을 구현할 때 별도 계약 검토 필요 |

`StartBattle` 함수 자체는 main 대비 수정하지 않았다. 기존 순서인 덱 복사 → 유물 Bind → Presenter 초기화 → BattleStart → StartTurn/드로우를 이용한다. 여러 이동 생략 유물을 보유하면 같은 검사 함수가 여러 번 구독될 수 있으나, 이미 해금된 카드는 다시 처리하지 않는다.

## 6. 새 파일과 연관 카드 변경 — 개요

- `BattleContext.Lock.cs`: 카드별 이동 기록, 잠금/해금 전환, 판정/UI 공용 구간 조회, 이동 없이 해금 가능한 카드 일괄 처리를 담당한다. 기존 public `SetLockState(card, bool)`도 유지했다.
- `IgnoreFirstLockMovesRelicData.cs` / `IgnoreLastLockMovesRelicData.cs`: 설정 가능한 `moveCount`를 제공하고 기존 BattleStart에 초기 해금 검사를 연결한다. 앞·뒤/중복 효과 합산이 이동 개수 이상이면 즉시 해금한다.
- 전투 중 유물 획득 기능에서 목록을 바꾸는 경우, 호출자가 `await ctx.UnlockCompletedLockCards()`로 재평가해야 한다. 임의 리스트 변경이나 인스펙터 숫자 변경을 자동 관찰하는 기능은 없다.
- 카드 상태는 `IsLocked` / `IsUnlocked`로 조회한다. `Has<Keyword_Lock>()`는 해금 상태에서도 true다.
- 기존 `Cards.cs`는 해금의 유효 비용을 0으로 계산하고 기본 `CanPlayFully`에 잠금 차단을 추가했다. 원래 Cost/Moves는 보존한다. `ObstacleTrailCard`와 `ExampleComplicateCard`의 자체 판정도 키워드를 반영한다.

## 7. 병합 후 확인할 시나리오

아래는 확인해야 할 기대 동작이며 실행 완료를 뜻하지 않는다.

1. 잠금 카드 선택 시 대상 선택이 뜨지 않고 에너지·손패가 유지된다.
2. `← ↓ ↑ ↓`를 턴을 나누어 성공하면 해금된다. 실패 이동과 HP 순간이동은 진행도를 올리지 않는다.
3. 한 카드의 중간 이동에서도 조건이 완성되는 즉시 해금된다.
4. 묘지/뽑기 더미의 카드가 먼저 해금되어도 UI 예외 없이 다른 카드까지 처리된다.
5. 해금 카드가 에너지 0, 막힌 경로에서 이동 없이 효과를 실행한다.
6. 자동 잠금은 질주 반복 후 적용된다. 유물로 남은 이동이 0이면 다시 즉시 해금된다.
7. 생략 개수가 이동 개수와 같거나 더 많으면 첫 드로우 전에 해금된다. 앞뒤 합산도 포함한다.
8. 카드 사용과 턴 종료 후 입력 잠금이 정상적으로 해제된다.

키워드 테스트 파일과 대응 메타 파일은 사용자 요청으로 삭제했다. 런타임 컴파일은 통과했으나 Unity 라이선스 초기화 문제로 실제 EditMode/플레이 실행 검증은 완료하지 못했다.
