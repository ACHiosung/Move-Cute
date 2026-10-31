# 전투 화면

현재 표시 컴포넌트 구조, 갱신 시점과 재사용 정책은 [UI_REFACTOR.md](UI_REFACTOR.md)에 정리했다. 각 View의 이미지·글자 참조는 프리팹에 저장하며, 배치 제작 코드는 에디터 전용으로 분리했다.

`Assets/Scenes/BattleUIScreen.unity`를 열고 Play한다. 공용 `SampleScene`과 기존 `BattleUI`는 변경하지 않았다. 생존해서 전투가 끝나면 선물 상자와 보상 화면으로 이어진다. 패배 시에는 다시 시작 버튼을 표시한다.

## 보상

- 전투당 골드 10, HP 회복 2, 카드 3종 중 1장 선택을 한 번 제공한다. 유물은 현재 제외한다.
- 우측 선물 상자를 클릭하면 골드·회복·카드 보상 목록을 연다. 각 항목은 클릭해서 받으며, 받은 항목은 사라지고 남은 항목이 위로 이동한다. 최대 HP를 넘어서 회복하지 않는다.
- 카드 보상은 기존 일러스트 + 방향 아이콘 표시를 사용한다. 마우스를 올리면 왼쪽 패널에서 상세 정보를 확인하고 클릭하면 런 덱에 추가한다.
- 카드 선택 화면에는 돌아가기 버튼 하나만 표시한다. 돌아가기 / 우클릭은 보상을 소모하지 않고 목록으로 돌아간다. 다시 열어도 후보 3종은 유지한다. 카드 보상을 포기하려면 받지 않은 채 다음 전투로 진행한다.
- 손패·뽑을 카드·버린 카드·플레이어 위치는 보상 배경에 남으며 입력을 차단한다. 상자를 클릭하기 전부터 배경·보드·전투 UI의 RGB를 45%로 어둡게 표시하며 원래 렌더 알파와 CanvasGroup 알파를 유지한다. 좌측 패널·패널 연결 사각형·보상 UI는 어둡게 하지 않는다. 상자 개봉 전후의 배경 밝기는 동일하다. 전투 종료로 이어지는 경우 실제 손패를 버리지 않으므로 기존 화면을 그대로 사용한다. 배경용 손패 저장·복원은 하지 않는다.
- 받지 않은 보상이 있어도 다음 전투를 누르면 경고 없이 진행한다. 수령한 보상과 추가한 카드는 다음 전투에도 유지한다.
- 골드 수치는 PlayerPanel/GoldBalance에 표시한다. 보상 중에도 덱 조회를 사용할 수 있다.
- 화면은 BattleScreenUI.Rewards.cs, 실제 지급과 중복 방지는 BattleContext.Rewards.cs가 담당한다. 수치와 후보 풀은 BattleManager의 config.rewards에서 조절한다. 승리 시 Context가 보상을 생성하고 UI는 결과만 표시한다. 전투별 후보는 ctx.Rng로 한 번만 추첨한다.
- RewardChest / RewardOverlay 아래의 목록·후보도 Image 기반 임시 도형으로, 이후 그림을 교체할 수 있다.

## 화면과 조작

- 스크롤 확인용 초기 덱은 기본 카드 4종을 각각 5장씩, 총 20장으로 구성했다. 이 설정은 전용 BattleUIScreen 씬에만 적용한다.

- 손패는 일러스트와 방향 아이콘만 표시한다. 카드 크기와 3:4 비율은 패 수와 무관하게 유지하고 선택한 카드를 26만큼 올린다. 카드에 마우스를 올리면 좌측에 이름과 상세 정보를 표시한다. 카드가 선택돼 있어도 다른 카드나 정보 요소를 살펴볼 수 있으며, 빈 곳으로 마우스를 옮기면 선택한 카드 설명으로 돌아간다. 상세 일러스트에는 방향 아이콘을 합성하지 않는다.
- 카드를 클릭한 뒤 빨간 테두리의 적·타일·카드를 클릭한다. 대상이 하나여도 자동 선택하지 않는다. 다른 손패 카드를 클릭하면 선택을 옮기며, 빈 배경이나 대상이 아닌 타일·적을 클릭하면 취소한다. 우클릭과 취소 버튼도 지원한다.
- 에너지 버튼을 0.55초 누르면 순간이동 목적지 선택에 들어간다. 장애물이 있는 칸과 현재 칸은 제외한다. 에너지 1과 설정된 HP를 소비하며, 에너지가 없으면 사용할 수 없다.
- 에너지가 0이면 모든 손패 일러스트를 흑백 셰이더로 표시한다. HP·사기·에너지·턴 텍스트에는 흰색 글자와 검은 외곽선을 사용해 막대가 줄어도 읽을 수 있다.
- 에너지·턴 종료·카드 더미·적의 마우스 설명은 별도 팝업 없이 좌측 패널에 표시한다. 적 이름은 이 정보 패널에서만 표시한다. 적별 설명은 BattleScreenUI의 enemyDescriptionIds / enemyDescriptions에 대응해서 입력할 수 있다. 나중에 설명 스크립트가 준비되면 BindEnemy의 공급자만 교체하면 된다.
- 적 그림의 세로 영역을 늘리고 사기 바의 하단을 중앙 보드 테두리 하단과 맞췄다.
- 덱 / 뽑을 카드 / 버린 카드 버튼으로 전체 카드 목록을 확인한다. 목록은 페이지 구분 없이 세로 스크롤하며 마우스 휠, 좌클릭 드래그, 우측 스크롤바를 지원한다. 뽑을 카드는 ID와 변형된 이동·키워드 등의 표시 상태로 정렬해 실제 드로우 순서를 공개하지 않는다.
- 휠 입력 시 드래그에서 남은 관성을 즉시 제거한다. 드래그를 놓았을 때의 관성은 유지하되 휠과 섞이지 않는다. BattleCardScrollRect가 담당한다.
- 카드 사용 효과와 반응 효과가 모두 끝났을 때 적 전원의 사기가 0이면 해당 턴을 끝낼 필요 없이 전투가 종료된다.
- PanelSupports의 두 사각형은 천장부터 좌측 패널 사이를 연결하는 임시 이미지다.
- 카드 상세 설명 위에 마우스를 올리면 카드 키워드 설명을 표시한다.
- HP는 현재/최대, 적 사기는 현재/초기 사기로 표시한다. 사기 바는 초기값을 넘으면 오른쪽으로 연장된다. 현재 타일의 해당 적이 부여한 턴 종료 사기 증가 효과는 `(+n)`으로 예고한다.

## 이미지 교체

`Assets/Prefabs/UI/BattleScreen.prefab`을 열어 Image의 Source Image를 교체한다. 배경과 글자, 클릭 영역, 수치 칸은 분리돼 있다. 부모 RectTransform과 Button을 유지하면 기존 입력과 표시가 유지된다.

- PlayerPanel: 패널 배경, Portrait, HpTrack / HpFill, MoneyIcon.
- DetailPanel: 배경, DetailArt, MouseIcon. 카드 일러스트는 실행 중 CardData.art를 사용한다.
- Player: 보드의 플레이어 이미지. Preview/Ghost는 도착 미리보기 이미지다. 둘 다 같은 그림으로 교체한다.
- Energy / EndTurn: 전체 외곽 이미지. 자식 EnergyCells / TurnCells에 남은 칸을 표시한다.
- Templates: 비활성 템플릿 보관 영역. TileTemplate, CardTemplate, EnemyTemplate, EnergyCellTemplate, TurnCellTemplate을 수정하면 실행 시 복제된다. Templates 부모는 비활성 상태로 유지한다.
- 적 일러스트는 EnemyData.sprite, 카드 일러스트는 CardData.art에 넣는다. 이미지가 없으면 도형으로 표시한다.
- 기본 4종은 CardData.art가 비어 있으면 basicCardArt의 임시 그림을 사용한다. 원본 아틀라스와 개별 Sprite는 Assets/Art/UI에 있으며 방향 아이콘은 DirectionLeft/Up/Right/Down.png다.

임시 그림은 built-in ImageGen으로 아틀라스 한 번만 생성했다. 사용 프롬프트: “One simple game placeholder sprite atlas, 2x2 equal panels, no gaps. Each panel portrait 3:4 (overall atlas 3:4). Same cute round-headed toy robot, crude flat doodle, thick black outlines, pastel background. Top left blue: sitting. Top right yellow: jumping. Bottom left green: retreating. Bottom right pink: running. No text, no arrows, no UI, no border. Very rough and minimal, distinguish poses only.” 방향 아이콘은 코드로 그린 PNG 네 개이며 생성 호출을 추가하지 않았다.

새 화면을 처음 생성하는 메뉴는 `MoveCute > UI > Create Battle Screen Scene`이다. 이미 존재하면 씬을 열기만 하므로 수동으로 교체한 이미지를 덮어쓰지 않는다. 공용 씬에 통합할 때는 BattleScreen 프리팹을 배치하고 BattleManager의 presenter와 input을 BattleScreenUI에 연결한다.

UI 문자열은 `Assets/Resources/UI/BattleScreenText.txt`의 `키|한국어|영어` 형식이다. 카드 이름·설명은 기존 Loc와 카드 데이터를 사용한다. 임시 한글 폰트는 설치된 OS 폰트에서 선택한다. 배포 시에는 프로젝트에서 사용할 한국어 폰트를 정해 포함하는 것이 필요하다.

## 전투 담당자와 연결할 항목

기존 계약과 담당 파일을 변경하지 않기 위해 아래 로직은 구현하지 않았다.

- 현재 BattleContext는 4×4 시작 칸을 자동으로 정한다. 첫 손패 전 선택 시점과 상태 변경 API가 필요하다.
- 잠금/해금/질주 등의 실제 처리는 BattleContext에 TODO로 남아 있다. 화면에서 키워드 설명과 잠금 회색 표시는 가능하지만 이동 큐 진행과 해금 여부를 정확히 표시하려면 카드 담당자의 상태 제공이 필요하다.
- 새로운 효과가 생기면 BattleScreenUI.Layout.cs의 DescribeEffect에 설명을 추가한다. 현재 알려진 효과의 수치는 표시하며 알 수 없는 효과는 형식 이름으로 표시한다.

이번 요청에 따라 BattleContext.Teleport에 에너지 1 소비와 부족 시 사용 차단만 추가했다. UI에서 직접 에너지를 보정하지 않으며 그 외 전투 규칙은 유지했다.
