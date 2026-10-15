# MoveCute

카드로 보드 위를 이동하며 싸우는 턴제 덱빌딩 게임.

## 환경

- Unity **6000.3.12f1** (URP 2D)
- DOTween (무료판, `Assets/Plugins/Demigiant`) — 연출용
- Input System

## 실행

`Assets/Scenes/SampleScene.unity`를 열고 Play. 임시 UI(`BattleUI` 프리팹, `UI/TempBattleUI.cs`)로 전투를 진행한다.

- 카드 클릭: 사용 / 보드 칸 클릭: 순간이동 (HP 지불) / 턴 종료 버튼
- 선택이 필요하면 후보가 초록색으로 표시된다. 클릭해서 고르거나 취소
- `BattleManager`의 `presenter`·`input`을 비우고 `showDebugGui`를 켜면 예전 OnGUI 디버그 패널로 돌아간다
- 테스트 데이터는 `SampleContent`가 메모리에서 만든다. 실제 에셋을 `BattleManager` 인스펙터에 넣으면 대체된다.
- 메뉴 `MoveCute > Create Sample Assets`: 샘플 데이터를 에셋으로 저장 (작성 예시)

## 코드 구조

```
Assets/Scripts/
  Battle/        BattleManager(씬 진입점), BattleContext(전투 상태·계약·사이클), BattleEvents
  Board/         보드, 타일, 타일 효과
  Cards/         CardData(원본) / CardInstance(덱의 카드) / CardPiles
  Units/         Unit, PlayerUnit, Enemy, Obstacle
  Effects/       Effect(모든 효과의 기반) + 예시 효과
  Relics/        유물
  Run/           RunState(런 전체 상태)
  Presentation/  연출·입력 계약(IBattlePresenter, IPlayerInput), DOTween 연결
  UI/            임시 전투 UI (TempBattleUI — 연출·선택을 한 파일에서 구현)
  Core/          enum, Awaitable 유틸
  Editor/        에디터 도구
Assets/Prefabs/UI/  BattleUI(캔버스 배치), TileView, CardView, EnemyView
```

전투 로직은 `Awaitable` 기반이다. `await ctx.PlayCard(...)`가 반환되면 이동·효과·연출·연쇄 반응이 모두 끝난 상태다.

## 협업

**작업 전에 [Docs/CODING_RULES.md](Docs/CODING_RULES.md)를 읽는다.** 담당별 소유 파일, 계약 변경 규칙, async 규칙, 효과·적·연출 작성 예시가 있다.
