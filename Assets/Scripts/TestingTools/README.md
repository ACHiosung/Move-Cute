# 테스트 도구 (TestingTools)

전투를 빠르게 시험해 보기 위한 개발용 도구. 게임 기능이 아니므로 출시 전에 통째로 지울 수 있게 모아 두었다.

## 쓰는 법

| 메뉴 | 하는 일 |
|---|---|
| MoveCute > Debug > Testing Tools | 순서대로 뽑기, 덱 프리셋 사용, 모든 유물 넣기(꺼진 상태), 에너지·턴·내 HP·적 사기 무한, 새 런 시작 |
| MoveCute > Debug > Deck Builder | 덱 프리셋 만들기·편집 (카드를 끌어 놓아 추가·순서 변경, 더블클릭 추가, ×N 묶음, −/+ 장수). 플레이 중엔 카드를 손패·뽑을 카드 맨 위에 바로 넣기 |

- 체크 상태는 이 PC의 EditorPrefs(`MoveCute.Test.*`)에 저장된다. 씬은 건드리지 않는다. 빌드에서는 모두 꺼져 있다.
- 무한 옵션은 플레이 중 바로 적용된다. 순서대로 뽑기는 다음 전투부터, 덱 프리셋·모든 유물 넣기는 새 런부터 적용된다.
- 덱 프리셋 에셋은 `Assets/Data/TestingTools/Decks`에 생긴다. 이름은 Project 창에서 바꾼다.

## 파일

| 위치 | 내용 |
|---|---|
| `Assets/Scripts/TestingTools/BattleTestOptions.cs` | 옵션 값, 새 런 때 덱·유물 바꾸기 (OnNewRun) |
| `Assets/Scripts/TestingTools/DeckPreset.cs` | 덱 프리셋 ScriptableObject |
| `Assets/Scripts/TestingTools/BattleContext.Testing.cs` | 테스트 전용 BattleContext 함수 (ApplyTestOptions, TestAddCardToHand, TestAddCardToDrawTop) |
| `Assets/Scripts/TestingTools/Editor/TestingToolsWindow.cs` | Testing Tools 창, 옵션 저장·불러오기 (TestingToolsPrefs) |
| `Assets/Scripts/TestingTools/Editor/DeckBuilderWindow.cs` | Deck Builder 창 |
| `Assets/Data/TestingTools/` | 덱 프리셋 에셋 |

## 게임 코드에 넣은 줄 (모두 `// [TestingTools]` 표시)

각 줄은 **한 줄을 통째로 지우면 원래 코드로 돌아가도록** 넣었다.

| 파일 | 함수 | 하는 일 |
|---|---|---|
| `Battle/BattleContext.cs` | StartBattle | 순서대로 뽑기 (섞는 대신 뒤집기) |
| `Battle/BattleContext.cs` | Teleport | 에너지 무한 |
| `Battle/BattleContext.cs` | EndTurn | 턴 수 무한 |
| `Battle/BattleContext.Cards.cs` | SpendEnergy | 에너지 무한 |
| `Battle/BattleContext.Units.cs` | ChangeMorale, DealDamage | 적 사기 무한 |
| `Battle/BattleContext.Units.cs` | LosePlayerHp | 내 HP 무한 |
| `Battle/BattleManager.cs` | NewRun | 덱 프리셋·모든 유물 넣기 (OnNewRun) |
| `UI/BattleScreenUI.Layout.cs` | 남은 턴 표시 | 턴 수 무한일 때 ∞ (2줄) |

## 지우는 법

1. 폴더 두 개를 지운다: `Assets/Scripts/TestingTools`, `Assets/Data/TestingTools` (.meta 포함)
2. 게임 코드에서 `[TestingTools]`가 들어간 줄을 모두 지운다.
   ```bash
   grep -rn "\[TestingTools\]" Assets/Scripts
   ```
3. Unity에서 컴파일 에러가 없는지 확인한다.
4. (선택) 각 PC의 EditorPrefs에 `MoveCute.Test.*` 키가 남지만 쓰이지 않으므로 그대로 둬도 된다.
