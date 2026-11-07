# 잠금 / 해금 구현 및 PM 전달 사항

## 설정과 사용

- 카드 에셋의 `keywords`에 `Keyword_Lock` 하나를 추가한다. `isUnlocked = false`면 잠금, `true`면 해금이다. 별도의 해금 키워드 클래스는 없다.
- `moves`, `cost`, `effects`는 일반 카드처럼 설정한다. 해금 중에도 원래 이동·코스트는 보존한다.
- 자동 잠금 태그 `autoLock`도 `Keyword_Lock` 안에 둔다. 카드 공통 필드에서는 제거했다. `true`이면 카드 사용 완료 후 같은 키워드의 `isUnlocked`를 `false`로 바꾼다. 질주는 전체 재사용이 끝난 뒤 잠근다.
- 전투 중 부여는 `await ctx.LockCard(card)` / `await ctx.UnlockCard(card)`를 사용한다. 기존 `AddKeyword`에 잠금/해금을 전달해도 같은 처리를 한다.
- 잠금/해금 전환 시 키워드를 제거·추가하지 않고 같은 객체의 상태만 바꾼다. 키워드가 없는 카드에 처음 부여할 때만 새 객체를 추가한다. 런 덱과 원본 에셋은 전투 중 변경되지 않는다.
- 상태 조회는 `card.IsLocked` / `card.IsUnlocked`, 자동 잠금 조회는 `card.AutoLock`을 사용한다. `Has<Keyword_Lock>()`는 두 상태 모두에서 `true`이므로 잠금 상태 판정에 사용하지 않는다.

## 판정 범위

- 뽑기 더미·손패·묘지에 있는 잠금 카드가 실제 성공한 한 칸 이동을 추적한다. 휴지통 카드는 제외한다.
- 여러 카드와 여러 턴에 걸쳐 방향 순서가 연속으로 일치하면 해금한다. 다른 방향은 연속 패턴을 끊지만, 겹치는 접두사는 다시 일치할 수 있다.
- HP 소모 순간이동은 진행도를 올리거나 초기화하지 않는다. 막혀서 실패한 이동도 진행도에 영향을 주지 않는다.
- 이동이 없는 잠금 카드는 유효한 이동 생략 유물이 있으면 즉시 해금한다. 유물이 없거나 생략 개수가 0이면 기존처럼 명시적 해금 부여가 필요하다.
- 해금 카드는 에너지 0, 실제 이동 없음, 이동 경로가 막혀 있어도 효과 발동. 이동 중 타일 효과와 이동 콜백은 발생하지 않는다.
- 선택형 카드와 해금 시 피해 유물은 아직 구현하지 않았다. 이동 생략 유물은 아래와 같이 적용한다.

## 이동 생략 유물

- `IgnoreFirstLockMovesRelicData`: 해금 조건에서 앞쪽 `moveCount`개 이동을 생략한다.
- `IgnoreLastLockMovesRelicData`: 해금 조건에서 뒤쪽 `moveCount`개 이동을 생략한다.
- 기본값은 각각 1이며, `Assets/Data/Relics/Relic_IgnoreFirstLockMoves.asset`과 `Relic_IgnoreLastLockMoves.asset`의 인스펙터에서 변경한다. 장착은 기존 `BattleManager.relics`에 에셋을 추가한다.
- 예: `← ↓ ↑ ↓`에 앞 1개 생략이면 `↓ ↑ ↓`, 뒤 1개 생략이면 `← ↓ ↑`, 둘 다 있으면 `↓ ↑`가 해금 조건이다.
- 효과는 잠금 상태의 해금 비교에만 적용한다. 카드의 원래 `Moves`, 코스트, 에셋은 수정하지 않는다. 재잠금되거나 전투 중 새로 잠금이 부여된 카드에도 적용된다.
- 여러 개를 보유하면 개수를 합산한다. 음수는 0으로 취급한다. 생략 개수 합계가 카드 이동 개수 이상이면 이동 없이 즉시 해금한다. 앞·뒤 생략의 합계와 중복 유물도 포함한다.
- 앞쪽 생략을 먼저 적용하고 남은 구간에서 뒤쪽을 생략한다. 남은 개수는 0까지 허용하며 음수가 되지 않는다.
- 새 이벤트나 기존 PM 소유 파일 변경은 없다. 두 유물은 기존 `BattleStart` 이벤트에 `UnlockCompletedLockCards`를 연결해 첫 드로우 전에 해금한다. 전투 중 유물 획득 시에도 이 Awaitable 함수를 await하여 즉시 반영한다. 잠금 부여/자동 재잠금 시에는 같은 조건을 즉시 재평가한다.

## 이동 표시

- 잠금 카드의 원래 화살표 순서는 유지하되, 유물로 해금 조건에서 빠진 이동은 `#292929`로 어둡게 표시한다. 카드 아이콘과 상세 텍스트가 같은 `GetLockMoveRange` 조회 함수를 사용한다.
- 해금 상태에서는 카드 화살표와 상세 이동 목록을 숨긴다. 보드 경로 미리보기도 기존처럼 생략한다. 다시 잠기면 원래 화살표와 유물 생략 표시를 복원한다.
- 일반 카드에는 생략 색상을 적용하지 않는다. 전투 밖/보상 카드 표시에서는 현재 전투 유물을 적용하지 않는다.
- 원본 `Moves`는 바꾸지 않는다. 카드 뷰 재사용과 동일 이동 목록의 색상 변경도 매번 반영한다.
- UI용 `CardMoveDisplay`와 읽기 전용 `GetLockMoveRange`를 공유하며, BattleEvents와 기존 PM 소유 파일은 변경하지 않았다.

## PM 소유 기존 파일 변경 보고

기존 public 함수 이름·인자와 BattleEvents는 변경하지 않았다. 새 상태와 부여/추적 로직은 `BattleContext.Lock.cs`에 분리했다. 통합 후 기존 연결은 `Has<Keyword_Lock/Unlock>()` 대신 `IsLocked` / `IsUnlocked`를 조회한다. 기존 파일에는 다음 연결을 추가했다.

| 파일 / 함수 | 변경 |
|---|---|
| `BattleContext.cs` / `PlayCard` | 잠금 카드 거부, 질주 반복 완료 후 자동 잠금 처리 추가 |
| `BattleContext.Board.cs` / `MovePlayer` | 해금 카드가 발생시키는 이동 차단, 성공한 이동을 잠금 패턴 추적에 전달 |
| `BattleContext.Board.cs` / `CanFullyMove` | 해금 카드는 경로와 무관하게 성공 판정 |
| `BattleContext.Cards.cs` / `AddKeyword` | 잠금/해금 부여를 새 계약 함수로 연결 |
| `BattleManager.cs` / `PlayCard` | 잠금 카드는 대상 선택 전 거부 |

UI에서는 에너지 0인 해금 카드를 사용 가능하게 표시하고, 해금 카드의 이동 미리보기를 숨긴다. 기본 카드와 현재 특수 카드의 흑백 판정도 키워드를 반영한다.

### 손패 밖 카드 해금 시 UI 예외 수정

잠금 진행도는 뽑기 더미·묘지도 추적한다. 그런데 `BattleScreenUI`와 `TempBattleUI`의 `RefreshCard`는 `cards[card]`로 손패 뷰가 반드시 존재한다고 가정했다. 손패 밖 카드가 해금되면 `CardChanged`에서 `KeyNotFoundException`이 발생해 뒤의 카드 해금 검사와 현재 이동 해결이 중단될 수 있었다. 두 UI에서 `TryGetValue`로 뷰가 있는 카드만 갱신하도록 수정했다. BattleContext와 BattleEvents의 추가 변경은 없다.

수정 시 확인 대상으로 삼은 재현 시나리오는 `← ↓` 이동 후 턴을 종료하고 `↑ ↓`를 이동했을 때 뽑기 더미·묘지의 두 카드가 모두 해금되는 경우다.

## 검증

사용자 요청으로 `Assets/Tests/Editor/LockKeywordTests.cs`와 대응 메타 파일, 비게 된 테스트 폴더의 메타 파일을 삭제했다. 현재 키워드 테스트 파일은 포함하지 않는다.

Unity 프로젝트의 기존 응답 파일과 Unity 내장 Roslyn으로 런타임 코드를 컴파일했다. 실제 EditMode 실행 검증은 Unity Licensing Client 연결 시간 초과로 완료하지 못했다. 따라서 컴파일 성공과 실제 플레이 검증을 구분해야 한다.
