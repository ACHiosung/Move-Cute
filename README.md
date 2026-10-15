# MoveCute

카드로 보드 위를 이동하며 싸우는 턴제 덱빌딩 게임.

## 환경

- Unity **6000.3.12f1** (URP 2D)
- DOTween (무료판, `Assets/Plugins/Demigiant`) — 연출용
- Input System

## 실행

`Assets/Scenes/SampleScene.unity`를 열고 Play. UI가 없어도 화면 왼쪽 위 디버그 패널로 전투를 돌려 볼 수 있다.

- 카드 클릭: 사용 / 보드 칸 클릭: 순간이동 (HP 지불) / 턴 종료
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
  Core/          enum, Awaitable 유틸
  Editor/        에디터 도구
```

전투 로직은 `Awaitable` 기반이다. `await ctx.PlayCard(...)`가 반환되면 이동·효과·연출·연쇄 반응이 모두 끝난 상태다.

## 협업

**작업 전에 [Docs/CODING_RULES.md](Docs/CODING_RULES.md)를 읽는다.** 담당별 소유 파일, 계약 변경 규칙, async 규칙, 효과·적·연출 작성 예시가 있다.
