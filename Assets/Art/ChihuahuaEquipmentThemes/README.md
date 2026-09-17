# 치와와 장비 테마 — T513

주인 지정 9등급 × 도적·전사·암살자, 총 27세트. 등급과 직업별 폴더 안에 원본 PNG, 부위별 PSB, 장비 썸네일을 함께 둡니다.

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

PSB의 위에서 아래 순서는 모든 파일이 **무기, 머리, 몸통, 팔1, 팔2, 다리1, 다리2, 배경**입니다. 레이어 ID도 1001–1008로 동일합니다. 모자는 머리 파츠에 포함되어 있습니다. 배경은 원본 확인용으로 보관하고 기본으로 숨겼습니다. 배경을 켜서 합성하면 해당 원본 PNG를 채널 오차 1 이내로 복원합니다.

무기는 `Reference/character_base.png`의 몽둥이를 기준으로 대각선 축 길이와 손잡이 끝 위치를 맞췄습니다. 축에 수직인 폭은 유지합니다. 래스터 경계로 생기는 길이 차이는 2픽셀 이내이며 각 `layers.json`에 실측값이 있습니다. 생성 결과의 1픽셀 캔버스 차이는 파츠를 확대·축소하지 않고 흰 여백을 보정했습니다.

Unity 6000.3.8f1 프로젝트에 설치된 2D PSD Importer 12.0.1용 `.meta`가 포함됩니다. PSB는 Mosaic·Character Mode와 레이어 ID 매칭을 켰으며 숨긴 배경은 가져오지 않습니다. Sprite Editor의 Skinning Editor에서 본과 웨이트를 설정할 수 있는 **리깅 준비용 소스**입니다. 본·애니메이션·런타임 장비 연결은 이 아트 파일에 포함되지 않습니다.

생성에는 ChatGPT 내장 이미지 생성을 사용했습니다. 디자인 조합과 프롬프트 템플릿은 `tools/chihuahua_art/`에 있습니다. `build_assets.py`는 원본 좌표를 보존하며 레이어·썸네일·Unity 메타를 만들고, `validate_assets.py`는 저장된 27세트의 완전성·투명도·PSB 실제 레이어·합성을 검사합니다.

최종 파일 검사 결과는 `tools/chihuahua_art/validation_report.json`, 생성 프롬프트와 생성 이미지 해시는 `tools/chihuahua_art/generation_provenance.json`에 보관했습니다. Unity 에디터에서의 직접 임포트 검증은 아직 수행하지 않았습니다.
