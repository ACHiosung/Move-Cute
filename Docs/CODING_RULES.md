# 전투 코드 규칙

여러 명이 각자 개발하기 위한 약속. 이 문서와 코드가 다르면 코드 기준으로 이 문서를 고친다.

## 1. 구조

```
BattleManager (씬 진입점, MonoBehaviour)
 ├─ RunState           런 전체 동안 유지
 └─ BattleContext      전투 1회의 모든 상태 + 계약 함수 + 사이클
     ├─ BattleEvents   유물·상태가 구독하는 이벤트 (await됨)
     ├─ IBattlePresenter  연출 (UI 담당 구현)
     └─ IPlayerInput      플레이어 선택 (UI 담당 구현)
```

- 게임 상태는 **`BattleContext`의 public 함수로만** 바꾼다. 효과·적·유물·장애물은 필드를 직접 수정하지 않는다.
- `*Data`(ScriptableObject)는 원본이다. 런타임에 수정하지 않는다.

## 2. 담당과 소유 파일

자기 파일만 수정한다. 다른 사람 파일을 고쳐야 하면 먼저 담당자와 이야기한다.

| 담당 | 소유 파일 |
|---|---|
| 전투 흐름 | `Battle/BattleContext.cs`, `Battle/BattleContext.Input.cs`, `Battle/BattleEvents.cs`, `Battle/BattleManager.cs` |
| 효과/상태 | `Effects/*`, `Battle/BattleContext.Units.cs`, `Units/Units.cs` |
| 보드/이동 | `Board/*`, `Battle/BattleContext.Board.cs`, `Units/Obstacle.cs` |
| 카드/덱 | `Cards/*`, `Battle/BattleContext.Cards.cs` |
| 적 | `Units/Enemy.cs`, 적별 하위 클래스 |
| UI/연출 | `Presentation/*` (IBattlePresenter·IPlayerInput 구현), `UI/*` |

- 씬(.unity)·프리팹은 머지가 거의 불가능하다. **테스트는 각자 자기 씬에서** 하고, 공용 씬은 한 사람만 수정한다.

## 3. 계약 변경

- `BattleContext`의 public 함수, `Effect.Execute`, `Enemy`의 virtual 함수, `IBattlePresenter`, `IPlayerInput`의 **이름·인자는 합의 없이 바꾸지 않는다.**
- 함수 **추가**는 담당자가 자유롭게 한다. 기존 함수의 내부 구현도 담당자 재량이다.

## 4. async 규칙

로직은 `Awaitable`(Unity 6 내장) 기반이다. **함수가 반환되면 그 결과(연출·연쇄 반응 포함)가 전부 끝난 상태**라는 것이 핵심 약속이다.

1. **ctx 함수는 반드시 `await`한다.** 빠뜨리면 컴파일 에러(CS4014)가 난다 (`Assets/csc.rsp`).
2. **`async void` 금지.** 예외: UI 버튼처럼 최상위에서 기다릴 곳이 없으면 `.Forget()`을 쓴다 (예외를 콘솔에 남겨 줌). 로직 코드 안에서는 `.Forget()`도 쓰지 않는다.
3. **같은 `Awaitable`을 두 번 await하지 않는다.** Unity가 객체를 재사용한다. 변수에 담아 두지 말고 바로 await한다.
4. **전투 종료 체크는 신경 쓰지 않는다.** 전투가 끝난 뒤의 ctx 상태 변경 함수는 아무것도 하지 않는다. 효과 목록은 `ctx.RunEffects`로 실행하면 종료 시 알아서 멈춘다.
5. **상태를 바꾸는 계약 함수는 전부 `Awaitable`을 반환한다.** 지금 기다릴 게 없어도 그렇게 둔다 (나중에 연출이 붙어도 시그니처가 안 바뀌게). 판정·계산만 하는 함수(`CanFullyMove`, `ModifyDamage`, 조회)는 동기.
6. **연출은 ctx만 호출한다.** 효과·적이 `Presenter`를 직접 부르지 않는다. 필요한 연출 시점이 있으면 ctx 함수에 추가해 달라고 요청한다.
7. **이벤트 구독은 `BattleEvents`의 `Add`로 한다.** 반응은 `Awaitable`을 반환하고, 등록 순서대로 하나씩 끝까지 기다린다.

## 5. 자주 하는 작업

### 새 효과
```csharp
[Serializable]
public class HealEffect : Effect
{
    public int amount = 3;

    public override async Awaitable Execute(BattleContext ctx, EffectSource src, EffectTarget target)
    {
        await ctx.SomeApi(src, amount);
    }
}
```
`[Serializable]` + 기본 생성자가 있으면 인스펙터 드롭다운(SubclassPicker)에 자동으로 나온다.

### 효과 도중 플레이어 선택
```csharp
var card = await ctx.ChooseCard(ctx.Piles.Hand.ToList(), "버릴 카드 선택");
if (card == null) return;   // 취소 또는 후보 없음
await ctx.DiscardCard(card);
```
예시: `Effects/BasicEffects.cs`의 `DiscardChosenCardEffect`.

### 특수한 적
`Enemy`를 상속해 `DeclareIntent` / `OnTurnEnd`를 `async Awaitable`로 재정의한다.

### 연출 (UI 담당)
`IBattlePresenter`를 구현한 MonoBehaviour를 만들어 `BattleManager.presenter`에 넣는다. 필요한 함수만 구현하면 되고 나머지는 즉시 끝난다.
- 애니메이션이 끝날 때 완료되는 `Awaitable`을 반환하면 로직이 그만큼 기다린다 (`AwaitableCompletionSource` 활용).
- **메서드는 public이고 시그니처가 정확히 같아야 한다.** 다르면 경고 없이 기본 구현이 쓰인다.
- 호출 시점에 상태는 이미 바뀌어 있다. ctx를 읽어서 그리기만 하고, ctx의 상태 변경 함수는 부르지 않는다.

### 연출에서 DOTween 쓰기

DOTween(`Assets/Plugins/Demigiant`)이 설치되어 있다. 트윈을 `.ToAwaitable()`로 바꿔 반환하면 로직이 트윈이 끝날 때까지 기다린다 (`Presentation/TweenAwaitable.cs`).

```csharp
public Awaitable Damaged(Unit target, int amount) =>
    ViewOf(target).DOPunchScale(Vector3.one * 0.2f, 0.25f).ToAwaitable();

public async Awaitable PlayerStepped(Vector2Int from, Vector2Int to)
{
    await playerMarker.DOMove(TilePos(to), 0.15f).ToAwaitable();
    await tileOf[to].DOPunchScale(Vector3.one * 0.1f, 0.1f).ToAwaitable();   // 순서대로
}
```

- 여러 트윈을 동시에 돌리고 전부 기다리려면 `DOTween.Sequence()`에 `Join`으로 묶어서 시퀀스 하나를 `ToAwaitable()` 한다.
- 트윈이 `Kill`되어도 완료로 처리되므로 로직이 멈춰 버리지 않는다. 대신 **`SetLoops(-1)` 같은 무한 트윈은 반환하지 않는다** (영원히 기다리게 됨).
- 뷰 오브젝트가 파괴될 수 있으면 `SetLink(gameObject)`를 붙인다 (파괴 시 Kill → 완료).
- 연출이 없는 함수는 구현하지 않으면 된다 (기본 구현 = 즉시 완료).

## 6. UI 없이 테스트

- `BattleContext`를 presenter·input 없이 만들면 연출은 즉시 끝나고(`NullBattlePresenter`), 선택은 첫 후보로 자동 결정된다(`AutoPlayerInput`). 이 상태에서는 전투 전체가 한 프레임에 끝까지 돈다.
- `BattleManager`의 OnGUI 디버그 패널은 `DebugPlayerInput`을 써서 선택을 버튼으로 받는다.
