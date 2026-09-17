# 치와와 장비 테마 — T513

주인 지정 10등급 × 도적·전사·암살자, 총 30세트. 등급과 직업별 폴더 안에 PNG와 부위별 PSB를 둡니다. 기존 01–09 등급에는 장비 썸네일도 포함됩니다.

| 폴더 | 등급 |
| --- | --- |
| 01_Primitive | 원시 |
| 02_Medieval | 중세 |
| 03_EarlyModern | 근대 |
| 04_Modern | 현대 |
| 05_Cyber | 사이버 |
| 06_Future | 미래 |
| 07_Space | 우주 |
| 08_Immortal | 불멸 |
| 09_Infinite | 무한 |
| 10_Holy | 신성한 |

직업 폴더는 `Thief`(도적), `Warrior`(전사), `Assassin`(암살자)입니다. 전체 경로와 조합은 `catalog.json`, 각 세트의 파일 연결은 `equipment.json`에 있습니다.

| 세트 안 파일 | 용도 |
| --- | --- |
| rigging_original.png | 1155 × 1362 리깅 원본. 파츠가 떨어져 있는 배치 |
| rigging_layers.psb | 같은 캔버스·좌표의 부위별 레이어. 실제 PSB v2 형식 |
| Thumbnails/hat.png | 착용 중인 모자만 따로 만든 썸네일 |
| Thumbnails/armor.png | 원본의 몸통 파츠에서 추출한 썸네일 |
| Thumbnails/ring.png | 반지 썸네일 |
| Thumbnails/necklace.png | 목걸이 썸네일 |
| Thumbnails/earring.png | 귀걸이 한 개 썸네일 |
| Thumbnails/weapon.png | 원본의 무기 파츠에서 추출한 썸네일 |
| layers.json | 레이어 좌표·ID, 원본/PSB 해시, 무기 길이 검사 |
| design.json | 해당 등급·직업의 디자인 명세 |

썸네일은 모두 512 × 512 투명 PNG입니다. 갑옷과 무기는 다시 그리지 않고 해당 리깅 원본에서 추출했습니다. 반지·목걸이·귀걸이는 썸네일 전용입니다.

PSB의 위에서 아래 순서는 모든 파일이 **무기, 머리, 몸통, 팔1, 팔2, 다리1, 다리2**입니다. 요청한 부위 7개만 포함하며 레이어 ID는 기존 1001–1007을 유지합니다. 모자는 머리 파츠에 포함되어 있습니다. PSB 배경은 투명합니다. 01–09의 `rigging_original.png`는 흰 배경이며, 10_Holy의 PNG는 생성된 투명도를 유지한 정렬 완료본입니다.

2026-09-18 신성한 테마 추가: `10_Holy/Thief`, `Warrior`, `Assassin`에 각 1155 × 1362 투명 PNG와 RLE PSB가 있습니다. `Reference/character_base.png`를 내장 이미지 생성에 직접 참조했습니다. 도적은 녹색, 전사는 파란색, 암살자는 보라색 보석을 사용하며, 몸통에는 소매·어깨 돌출·등 장식·날개·망토가 없습니다. 머리·몸통·무기의 외곽과 팔·다리 노출 연결부의 시작/끝 경계를 Reference에 정렬했고 측정 오차는 최대 1픽셀입니다. 이는 그림의 기준점 정렬이며 본·웨이트나 모든 내부 픽셀의 일치를 의미하지 않습니다. 이 추가 요청의 산출물은 리깅 PNG/PSB이며 별도 장신구 썸네일은 포함하지 않습니다.

신성한 테마의 원본 생성 이미지·전체 프롬프트·파일 검증 결과는 `Artifacts/HolyTheme-20260918/`에 보관합니다. `generation-provenance.json`에 3회 내장 생성 호출의 프롬프트가 있으며, `verification.json`에 이미지 해시와 좌표 오차를 기록했습니다. `tools/build_holy_theme.py`는 생성 알파를 보존해 정렬하고 7개 레이어를 패키징합니다. PSB 재읽기·독립 RLE 해독·레이어 합성과 PNG의 픽셀 일치 검사를 통과했습니다. 이후 Unity 6000.3.8f1 클라우드 임포트·리깅 재임포트 검증을 통과했습니다. Photoshop 앱에서의 직접 열기는 수행하지 않았습니다.

2026-09-18 복구: 기존 Unity 로그의 `ZIP stream was not fully decompressed` 오류에 대응하여 27개 PSB의 레이어와 합성 미리보기를 모두 RLE로 다시 저장했습니다. PSB v2의 64비트 길이 필드와 32비트 RLE 행 길이를 사용합니다. 기존 부위별 RGBA 픽셀·좌표·한글 이름·레이어 ID와 Unity `.meta` GUID는 유지했습니다. 합성 미리보기에는 Photoshop 방식의 흰색 매트 RGB와 별도 투명도 채널을 저장합니다. 원본 확인용 숨김 배경 레이어는 제거했습니다.

01–09의 무기는 `Reference/character_base.png`의 몽둥이를 기준으로 대각선 축 길이와 손잡이 끝 위치를 맞췄습니다. 축에 수직인 폭은 유지합니다. 래스터 경계로 생기는 길이 차이는 2픽셀 이내이며 각 `layers.json`에 실측값이 있습니다. 해당 기존 등급의 생성 결과 1픽셀 캔버스 차이는 파츠를 확대·축소하지 않고 흰 여백을 보정했습니다. 10_Holy는 위에 설명한 부위별 좌표 정렬을 적용했습니다.

Unity 6000.3.8f1 프로젝트에 설치된 2D PSD Importer 12.0.1용 `.meta`가 포함됩니다. PSB는 Mosaic·Character Mode와 레이어 ID 매칭을 켰으며 숨긴 배경은 가져오지 않습니다. 30개 장비 PSB의 `.psb.meta`에 Reference와 동일한 11개 본과 레이어별 Auto Geometry·웨이트가 저장되어 있습니다. 본의 이름·GUID·계층·전역 좌표·길이·회전은 Reference와 같습니다. 각 이미지 외곽으로 총 210개 메시를 새로 생성했습니다. 애니메이션 클립과 런타임 장비 연결은 별도 작업입니다.

생성에는 ChatGPT 내장 이미지 생성을 사용했습니다. 디자인 조합과 프롬프트 템플릿은 `tools/chihuahua_art/`에 있습니다. `build_assets.py`는 원본 좌표를 보존하며 레이어·썸네일·Unity 메타를 만들고, `validate_assets.py`는 저장된 27세트의 완전성·투명도·PSB 실제 레이어·합성을 검사합니다.

복구 스크립트는 `tools/repair_chihuahua_psb.py`, 이번 파일 검사 결과는 `Artifacts/PSBRepair-20260918/verification.json`, 교체 전 백업은 같은 폴더의 `before.zip`에 있습니다. 27개 파일을 다시 열어 7개 레이어와 픽셀 일치를 검사하고, 별도로 구현한 RLE 디코더로 모든 레이어 및 합성 데이터와 파일 경계를 검증했습니다. 로컬 Unity 에디터는 실행하거나 제어하지 않았습니다. 이후 Unity 6000.3.8f1 클라우드에서 31개 PSB 임포트와 30개 장비 PSB의 본·메시 저장 및 재임포트를 검증했습니다. Photoshop 앱에서의 직접 열기는 수행하지 않았습니다. 생성 당시 프롬프트·이미지 해시 기록은 기존 `tools/chihuahua_art/generation_provenance.json` 경로를 참조합니다.

클라우드 작업에서도 이 폴더와 `Assets/Art/ChihuahuaGameUI`를 GitHub `main`에서 받을 수 있습니다. 기존 작업은 최신 `origin/main`을 반영하고, sparse checkout을 쓰면 `Assets/Art`를 포함하세요. 본 설정은 `.psb.meta`에 있으므로 PSB와 함께 유지해야 합니다. 상세 안내는 `Documentation/Art/CHIHUAHUA-ASSETS.md`, 검증 결과는 `Documentation/Art/CHIHUAHUA-RIGGING-VERIFICATION.md`에 있습니다.
