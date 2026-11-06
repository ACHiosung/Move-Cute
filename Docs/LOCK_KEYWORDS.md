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
- 이동이 없는 잠금 카드는 이동으로 자동 해금하지 않는다. 명시적인 해금 부여는 가능하다.
- 해금 카드는 에너지 0, 실제 이동 없음, 이동 경로가 막혀 있어도 효과 발동. 이동 중 타일 효과와 이동 콜백은 발생하지 않는다.
- 선택형 카드와 해금 시 피해 유물은 아직 구현하지 않았다. 이동 생략 유물은 아래와 같이 적용한다.

## 이동 생략 유물

- `IgnoreFirstLockMovesRelicData`: 해금 조건에서 앞쪽 `moveCount`개 이동을 생략한다.
- `IgnoreLastLockMovesRelicData`: 해금 조건에서 뒤쪽 `moveCount`개 이동을 생략한다.
- 기본값은 각각 1이며, `Assets/Data/Relics/Relic_IgnoreFirstLockMoves.asset`과 `Relic_IgnoreLastLockMoves.asset`의 인스펙터에서 변경한다. 장착은 기존 `BattleManager.relics`에 에셋을 추가한다.
- 예: `← ↓ ↑ ↓`에 앞 1개 생략이면 `↓ ↑ ↓`, 뒤 1개 생략이면 `← ↓ ↑`, 둘 다 있으면 `↓ ↑`가 해금 조건이다.
- 효과는 잠금 상태의 해금 비교에만 적용한다. 카드의 원래 `Moves`, 코스트, 에셋은 수정하지 않는다. 재잠금되거나 전투 중 새로 잠금이 부여된 카드에도 적용된다.
- 여러 개를 보유하면 개수를 합산한다. 음수는 0으로 취급한다. 항상 최소 한 개의 이동을 남겨, 이동이 1개인 카드는 그대로 유지한다. 이동이 없는 카드는 자동 해금하지 않는다.
- 앞·뒤 생략 합계가 너무 크면 앞쪽 생략을 먼저 적용하고, 남은 구간에서 최소 한 개를 남기도록 뒤쪽 생략을 제한한다.
- 이벤트 추가나 기존 PM 소유 파일 변경 없이, `BattleContext.Lock.cs`의 비교 구간 계산만 확장했다. 두 유물은 지속 효과라 이벤트를 구독하지 않는다.

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

실제 두 UI의 `CardChanged`를 사용하는 테스트를 추가했다. `← ↓` 이동 후 턴을 종료하고 `↑ ↓`를 이동했을 때 뽑기 더미·묘지의 두 카드가 모두 해금되는 시나리오다.

## 검증

`Assets/Tests/Editor/LockKeywordTests.cs`에서 잠금 사용 차단, 이동 순서·겹침·불일치, 더미/턴 간 진행도 유지, 순간이동/실패 이동 제외, 해금 무이동·무코스트·효과 발동, 특수 카드, 자동 잠금과 질주, 재잠금 진행도 초기화, 원본 분리, 일반 카드 회귀를 검증한다.

현재 검증 결과: Unity 프로젝트의 기존 응답 파일과 Unity 내장 Roslyn으로 런타임 및 Editor/테스트 코드를 컴파일했다. 컴파일 오류 없음. Editor의 기존 `FindFirstObjectByType` 사용에 대한 obsolete 경고 4건은 남아 있다. 실제 EditMode 테스트 실행은 Unity Licensing Client 연결 시간 초과로 완료하지 못했다. 테스트 통과 여부는 Unity 라이선스가 정상인 환경에서 확인해야 한다.
