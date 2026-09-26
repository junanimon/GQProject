# GQProject — 모험가 길드 접수원 게임

판타지 서류 검수·운영 시뮬레이션 (PC/Steam, Unity 6000.6 · URP 2D). 신입 접수원이 밤에 의뢰서 기입란을 채우고, 낮에 창구에서 수주·완료를 심사한다. 서브컬처 취향의 네임드 캐릭터 서사가 핵심.

## 문서 (기획 판단은 여기부터)
- `Assets/Docs/guild_receptionist_design.md` — 원본 기획서 (확정/제안/미정 표기)
- `Assets/Docs/기획_추가안_Claude.md` — Claude가 채운 추가 기획 (세계관·호감도·몬스터·습격·5주 로드맵·밸런스)
- `Assets/Docs/Characters/` — 네임드 6명 캐릭터 기획서 + `00_관계도_인덱스.md`
- `Assets/Docs/작업내역.md` — 지금까지의 작업 기록 (단계별 · 시스템별 · 문제 해결 · 테스트 결과 · 사용자 결정). 작업이 끝나면 갱신
- 새 기획을 추가하면 해당 문서에 [Claude 제안] / [구현됨] / [문서만] 표기로 반영한다

## 씬 (Assets/Scenes)
- `Core.unity` — GuildGame(상태·흐름), AudioHub(효과음·BGM), 카메라, EventSystem, 공통 캔버스(타이틀·상단 바·결과 종이·스토리·전환), 배경 캔버스
- `Night.unity` — 밤 책상 (의뢰서 · 기입 양식 · 도감), 내일 게시판(+상점 팝업), 승급 심사
- `Day.unity` — 낮 창구 (큰 캐릭터 · 대화창 · 책상 · 도장 · 팝업 · 길드 홀 지명 팝업)
- Core가 시작할 때 Night·Day를 Additive로 불러와 화면을 찾아 연결한다 (씬 간 인스펙터 참조 불가)
- 어느 씬에서 Play해도 Core부터 시작 (`Scripts/Prototype/Editor/PlayFromCore.cs`, 메뉴 GQ > Play는 Core에서 시작)
- 캔버스 정렬: 배경 -100 → 낮/밤 0 → Core 공통 10

## 코드 구조 (Assets/Scripts/Prototype, namespace GuildProto, 객체 지향)
- `Domain/` — 규칙을 가진 객체: Adventurer, GuildCard, Quest(+QuestEntry), Assignment, Evidence, Guild, Ledger, Regulations, Forgeries(IForgery)
- `Day/` — 낮 창구: Visit(추상) → ApplicationVisit / ReturnVisit / ChatVisit / UrgentVisit, Counter(하루 영업), Nomination(지명·흥정), DayRecord
- `Night/` — NightShift
- `Services/` — AdventurerRoster, VisitorFactory, QuestResolver, RaidResolver, ReportWriter, Lines(대사), PromotionBoard(승급), FacilityShop(상점), RandomEvents, Expedition, QuestGenerator, Calendar
- `Core/` — GuildGame(조립 + 화면 흐름만), GameConfig(밸런스, 인스펙터 편집), SaveData/SaveSystem(주 시작 자동 저장 · 엔딩 도감), RunStats(누적 기록), AudioHub
- `Views/` — 화면 표시·연출만 (게임 규칙 없음)
- `Data/` — ScriptableObject 정의

## 작업 규칙 (사용자 요청 사항)
- **UI·오브젝트는 코드로 런타임 생성하지 않는다.** 모두 씬/프리팹에 실제 오브젝트로 두어 하이어라키에서 수정 가능하게. 반복 항목(게시판 쪽지, 도감 목차 등)만 프리팹을 Instantiate
- 씬을 대량으로 구성해야 할 때는 `Assets/Editor/Temp*.cs` 임시 에디터 스크립트를 한 번 실행하고 **바로 삭제**한다
- 데이터(몬스터·의뢰·캐릭터·주차)는 `Assets/Data`의 ScriptableObject 에셋. 이미지는 에셋의 Sprite 필드로 교체 가능하게
  - `Data/Weeks/Week1~5` 주차(임시 규칙·해금·고정 의뢰·스토리·습격), `Data/QuestGenerator` 자동 생성 의뢰 재료, `Data/GameDatabase` 전체 묶음
- **MonoBehaviour · ScriptableObject는 반드시 클래스 이름과 같은 파일에 하나씩** (여러 개를 한 파일에 두면 컴포넌트/에셋 연결이 끊긴다)
- 객체 지향: 상태는 private set + 메서드로 변경, 종류별 동작은 다형성(인터페이스/상속)으로
- 사용자와는 한국어로 대화. 게임 내 텍스트도 한국어
- 기존 코드 스타일: 한국어 주석, 짧은 메서드, `new()` 대상 형식 추론 사용

## Unity MCP
- 에디터 조작은 UnityMCP 도구 사용. 인스턴스 이름 `GQProject@...`
- 에디터 창에 포커스가 없으면 플레이 모드 프레임이 멈춘다 → 테스트는 `EditorApplication.isPaused = true` 후 `EditorApplication.Step()`으로 진행
- 씬 수정 전 플레이 모드가 아닌지 확인할 것

## 임시 자원
- 캐릭터·몬스터·배경 그림은 코드로 그린 도트 임시 그림 (`Assets/Art/`). 실제 그림은 데이터 에셋의 Sprite만 바꾸고 Tint를 흰색으로
- 폰트는 윈도우 맑은 고딕 임시 사용 (배포 불가, 교체 예정)
- 사운드는 코드로 합성한 임시 WAV (`Assets/Audio/SFX`, `Assets/Audio/BGM`). AudioHub 인스펙터에서 교체

## 세이브
- 매주 첫날 밤 시작 때 자동 저장: `Application.persistentDataPath/save.json` (한 칸). 엔딩을 보면 삭제
- 엔딩 도감은 PlayerPrefs `GQ.Endings`
- 새 상태를 추가하면 `SaveData`와 `GuildGame.Save()/Setup()`에도 넣을 것
