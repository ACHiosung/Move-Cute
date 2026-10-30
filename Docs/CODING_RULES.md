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
- 효과는 **그 효과를 가진 쪽이 직접 발동한다.** 카드(`CardInstance.Play`)·타일(`TileEffect.Fire`)·유물(`Relic.Fire`)·적(`Enemy.OnTurnEnd`)·장애물(`Obstacle.OnDestroyed` / `OnLastTurnEnd`)이 `effects.ExecuteAll(ctx, src, target)`로 실행하고, `BattleContext`는 언제 발동할지(이동·턴 종료·파괴 등)만 알린다.
- 무작위는 `UnityEngine.Random` 대신 **`ctx.Rng`**(런 시드 RNG)를 쓴다 → 같은 시드면 같은 런이 재현된다. 보조 함수는 `ctx.Rng.Pick(list)` / `.Shuffled(list)` / `.Range(a, b)` (`Core/RandomExtensions.cs`). 시드 고정은 `BattleManager.seed`(0 = 무작위)
- `*Data`(ScriptableObject)는 원본이다. 런타임에 수정하지 않는다.
- ScriptableObject 클래스는 **이름이 같은 파일**에 둔다 (`RelicData` → `RelicData.cs`). 다르면 Unity가 스크립트를 못 찾아 에셋이 깨진다.

## 2. 담당과 소유 파일

자기 파일만 수정한다. 다른 사람 파일을 고쳐야 하면 먼저 담당자와 이야기한다.

| 담당 | 소유 파일 |
|---|---|
| 전투 흐름 | `Battle/BattleContext.cs`, `Battle/BattleContext.Input.cs`, `Battle/BattleEvents.cs`, `Battle/BattleManager.cs` |
| 효과/상태 | `Effects/*`, `Battle/BattleContext.Units.cs`, `Units/Units.cs` |
| 보드/이동 | `Board/*`, `Battle/BattleContext.Board.cs`, `Units/Obstacle.cs`, `Units/ObstacleData.cs` |
| 카드/덱 | `Cards/*`, `Battle/BattleContext.Cards.cs` |
| 적 | `Units/Enemy.cs`, `Units/EnemyData.cs`, `Enemies/*` (적별 파일) |
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
4. **전투 종료 체크는 신경 쓰지 않는다.** 전투가 끝난 뒤의 ctx 상태 변경 함수는 아무것도 하지 않는다. 효과 목록은 `effects.ExecuteAll(ctx, src, target)`로 실행하면 종료 시 알아서 멈춘다.
5. **상태를 바꾸는 계약 함수는 전부 `Awaitable`을 반환한다.** 지금 기다릴 게 없어도 그렇게 둔다 (나중에 연출이 붙어도 시그니처가 안 바뀌게). 판정·계산만 하는 함수(`CanFullyMove`, `ModifyDamage`, 조회)는 동기.
6. **연출은 ctx만 호출한다.** 효과·적이 `Presenter`를 직접 부르지 않는다. 필요한 연출 시점이 있으면 ctx 함수에 추가해 달라고 요청한다.
7. **이벤트 구독은 `BattleEvents`의 `Add`로 한다.** 반응은 `Awaitable`을 반환하고, 등록 순서대로 하나씩 끝까지 기다린다.

## 5. 자주 하는 작업

### 새 카드
카드 한 종류 = `CardData` 에셋 한 개. 예시: `Assets/Data/Cards/Card_Sit.asset` (앉기: ↓, 피해 5). 기본 덱은 씬의 `BattleManager.startingDeck`에 넣는다.

1. Project 창 `Assets/Data/Cards`에서 우클릭 → `Create > MoveCute > Card > Basic`. 파일 이름은 `Card_<Id>`.
2. 인스펙터 입력
   - `id`: 소문자 영문 (`sit`). `nameKey` = `card.<id>.name`, `descKey` = `card.<id>.desc`
   - `moves`: 이동 방향 (순서대로 한 칸씩). 카드에는 화살표로 표시되므로 설명문에 쓰지 않는다
   - `effects`: 드롭다운에서 효과 선택 후 값 입력. 모든 이동이 성공했을 때 발동한다
3. `Assets/Resources/Localization/Strings.csv`에 이름·설명 줄 추가
   ```
   card.sit.name,앉기,Sit
   card.sit.desc,적에게 피해를 {damage} 줍니다.,Deal {damage} damage to an enemy.
   ```
   - `{damage}`처럼 중괄호 안에 **효과의 필드 이름**을 쓰면 그 값이 들어간다. 숫자를 문장에 직접 쓰지 않는다 (값을 바꾸면 설명이 틀려짐)
   - 효과 안에 담긴 효과의 값은 점으로 잇는다: `{onBreak.damage}` (이름이 같으면 앞의 효과가 먼저 쓰인다)
   - 쉼표가 들어간 칸은 `"..."`로 감싼다. 엑셀로 저장할 때는 반드시 `CSV UTF-8(쉼표로 분리)` 형식으로 저장한다. 그냥 `CSV`로 저장하면 한글이 깨진다 (콘솔에 에러가 뜬다)
   - 표에 없는 키는 콘솔에 경고가 뜨고, 이름은 에셋 이름으로 대신 보인다
4. 코드에서는 `card.Data.DisplayName` / `card.Data.Description`으로 현재 언어 텍스트를 얻는다. 언어 전환은 메뉴 `MoveCute > Language`.

카드를 낼 때 하는 일(에너지 지불·이동·부가 효과·묘지/휴지통)은 **`CardInstance.Play`**(`Cards/Cards.cs`)에 있다. `BattleContext.PlayCard`는 낼 수 있는지 확인하고 입력을 잠근 뒤 이걸 호출한다. 키워드의 사용 시 동작(질주·잠금 등)은 `Play`에 넣는다.

#### 특수 카드 (이동·효과 순서가 기본과 다른 카드)
`CardInstance`를 상속한 `[Serializable]` 클래스를 만들고, 카드 에셋의 `behavior` 드롭다운에서 고른다. 에셋은 그대로 `Create > MoveCute > Card > Basic`으로 만든다 (`[CreateAssetMenu]` 필요 없음). 참고용 예시: **`Cards/ExampleComplicateCard.cs`** (이중 충격파: 이동 없이 충격파 2번) + 에셋 `Assets/Data/Cards/Card_ExampleComplicate.asset`.
- `Play` 재정의: 에너지 지불(`ctx.SpendEnergy(PlayCost(ctx))`) → `ctx.BeginCardPlay` → 카드가 하는 일 → `Finish(ctx)` 순서는 지킨다
- 흑백 판정을 바꾸려면 `CanPlayFully` 재정의 (기본은 전체 이동 가능 여부)
- public 필드는 에셋의 `behavior` 아래에서 조정하고, 설명문에 `{repeat}`, `{shockwave.damage}`처럼 쓸 수 있다
- 전투 중 상태 필드에는 `[NonSerialized]`를 붙인다. 카드 한 장마다 복사되므로 다른 카드와 공유되지 않는다. 단, 복사는 얕은 복사라 효과 같은 참조 필드는 공유된다 (효과에 상태를 두지 않는 규칙과 같음)
- 카드는 `new CardInstance`로 만들지 않고 `cardData.CreateCard()`로 만든다 (behavior 하위 클래스로 만들어진다)

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

**참고용 예시: `Effects/ShockwaveEffect.cs`** (충격파). 새 효과는 이 파일을 복사해서 시작한다. 주석에 정리된 것:
- 숫자는 public 필드 → 인스펙터에서 조정, 설명문에 자동 반영
- 다른 효과를 담는 필드(`[SerializeReference, SubclassPicker] List<Effect>`)로 "무엇을 할지"를 기획자가 조합
- 대상 목록은 `ToList()`로 복사해 두고 돈다 (await하는 동안 원본이 바뀜)
- await 뒤에는 상황이 바뀌었을 수 있으니 다시 확인한다 (연쇄 폭발로 이미 부서진 장애물 등)
- **효과 필드에 전투 중 상태를 저장하지 않는다.** 효과 객체는 에셋에 하나뿐이라 모든 복사본·전투가 공유한다

### 새 유물
- **단순한 유물** (시점 + 효과): 코드 없이 `Create > MoveCute > Relic > Basic` 에셋에서 `trigger`와 `effects`만 고른다.
- **조건·누적 상태가 필요한 유물**: `RelicData`와 `Relic`을 함께 상속한다. 참고용 예시: **`Relics/PedometerRelicData.cs`** (만보기: 한 턴에 N칸 이동하면 발동)
  - `RelicData` 하위 클래스: 조정할 숫자 필드 + `CreateRelic()` 재정의. 메뉴는 `[CreateAssetMenu(menuName = "MoveCute/Relic/<이름>")]`로 `Relic` 아래에 둔다
  - `Relic` 하위 클래스: `Bind`에서 `ctx.Events` 구독, 상태 보관. Relic은 런 내내 유지되므로 전투마다 초기화할 값은 `Bind`에서 초기화
  - 발동 시 할 일은 에셋의 `effects`를 `Fire(ctx)`로 실행 → 기획자가 조정 가능
- 장착은 씬의 `BattleManager.relics`. 이름·설명 키는 `relic.<id>.name` / `relic.<id>.desc`이고, 설명문에는 유물 에셋의 필드(`{steps}`)와 effects 안 필드(`{count}`)를 쓸 수 있다.

### 새 적
적은 **코드로 만든다.** 적 한 종류 = `Enemies/` 아래 파일 하나. 참고용 예시: **`Enemies/GravekeeperData.cs`** (묘지기) + 에셋 `Assets/Data/Enemies/Enemy_Gravekeeper.asset`.

한 파일에 두 클래스를 만든다:
- `XxxData : EnemyData` — 기획자가 조정할 숫자·참조(피해량, 장애물 에셋 등)를 필드로 둔다. `CreateEnemy()`에서 아래 클래스를 돌려준다. **파일 이름은 이 클래스 이름과 같아야 한다**
- `Xxx : Enemy` — 전투 중 행동. 필수는 `PlanIntent` 하나

```csharp
protected override IntentData PlanIntent(BattleContext ctx)
{
    var intent = new IntentData();
    intent.AddTile(ctx.Player.Pos, TileTrigger.TurnEndStanding, new GainMoraleEffect { amount = data.prayMorale });
    if (ctx.Turn % 2 == 1) intent.AddTurnEnd(new PlaceObstacleEffect { obstacle = data.tombstone });
    return intent;
}
```
- `PlanIntent`는 턴 시작에 이번 턴 예고를 정한다. 보드·플레이어 위치·사기·턴 수를 읽어 판단만 하고 상태는 바꾸지 않는다. 타일 배치·UI 공개·턴 종료 실행은 `Enemy` 기본 구현이 한다
- 좌표를 박아 두지 말고 `ctx.Board`, `ctx.Player.Pos`에서 계산한다 (보드 크기가 막마다 다르다). 보드 좌표는 (0,0)이 왼쪽 아래, y가 위로 증가
- 타일 `trigger`: `Pass` 지나갈 때(도착 포함) / `Arrive` 이동이 끝나 도착할 때 / `TurnEndStanding` 그 칸에 서 있는 채로 턴이 끝날 때
- 예고와 별개로 전투 상황에 반응하려면 `Bind`에서 `ctx.Events`를 구독한다 (묘지기: 묘비가 부서지면 사기 저하)
- 예고로 표현이 안 되는 행동은 `DeclareIntent` / `OnTurnEnd`를 재정의한다. 단, 플레이어가 미리 볼 수 있게 가능하면 예고(`IntentData`)에 담는다
- 효과가 부족하면 [새 효과](#새-효과)를 만든다

에셋 만들기: `Assets/Data/Enemies`에서 우클릭 → `Create > MoveCute > Enemy > (적 이름)`. 파일 이름은 `Enemy_<Id>`. `id`는 소문자 영문, `nameKey` = `enemy.<id>.name`, `startMorale`은 시작 사기 (카드 피해로 깎고, 제한 턴이 끝나면 남은 사기만큼 플레이어가 피해를 입는다). `Strings.csv`에 이름 줄을 추가하고, 전투에 넣는 곳은 씬의 `BattleManager.enemy`.

### 새 장애물
장애물 한 종류 = `ObstacleData` 에셋 한 개. 예시 두 개:
- `Assets/Data/Obstacles/Obstacle_Tombstone.asset` (묘비: 효과 없이 칸만 막는 가장 단순한 형태)
- `Assets/Data/Obstacles/Obstacle_Bomb.asset` (폭탄: 파괴될 때와 마지막 턴에 남아 있을 때 효과가 나간다)

1. Project 창 `Assets/Data/Obstacles`에서 우클릭 → `Create > MoveCute > Obstacle > Basic`. 파일 이름은 `Obstacle_<Id>`.
2. 인스펙터 입력
   - `id`: 소문자 영문 (`bomb`). `nameKey` = `obstacle.<id>.name`
   - `hp`: 이 값만큼 피해를 받으면 부서진다
   - `onDestroyed`: 부서질 때 실행할 효과. 효과의 대상 타일은 장애물이 있던 칸이다
   - `onLastTurnEnd`: 제한 턴의 마지막 턴이 끝났을 때 아직 남아 있으면 실행할 효과
3. `Strings.csv`에 이름 줄 추가
   ```
   obstacle.bomb.name,폭탄,Bomb
   ```
4. 장애물은 `PlaceObstacleEffect`로 놓는다.
   - 적: 적 코드에서 `PlaceObstacleEffect`로 놓고, 적 Data 에셋의 장애물 필드에 이 에셋을 끌어다 놓는다 (묘지기의 `tombstone`)
   - 카드: 카드 `target`을 `OtherTile`로 하고 `effects`에 `PlaceObstacleEffect`를 넣는다 (`Assets/Data/Cards/Card_Bomb.asset`: 고른 칸에 폭탄 설치)

   장애물은 칸을 막고, `ShockwaveEffect` 같은 효과로 피해를 줘서 부순다. 폭탄의 효과는 `DamageEffect`라서 대상 유닛이 없으면 무작위 적에게 피해를 준다.

### 효과 도중 플레이어 선택
```csharp
var card = await ctx.ChooseCard(ctx.Piles.Hand.ToList(), "버릴 카드 선택");
if (card == null) return;   // 취소 또는 후보 없음
await ctx.DiscardCard(card);
```
예시: `Effects/BasicEffects.cs`의 `DiscardChosenCardEffect`.

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
