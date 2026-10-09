# 아트 제작 목록 (PixelLab)

GDD §8(아트 파이프라인 — PixelLab 확정)에 따라 생성하는 1차 아트 목록. 37개 — 정기구독 한도(최대 200개) 안에서
MVP에 바로 쓰이는 것만 먼저 뽑는다. 생성 스크립트: [`tools/pixelart/pixellab_generate.py`](../tools/pixelart/pixellab_generate.py)

결과물은 `Assets/_Project/Art/Sprites/{이름}.png`에 저장된다. 아직 Unity 쪽 자동 연결 메뉴는 없어서(2차 작업),
받은 뒤 각 `RetainerDef`·`UnitDef`·`SettlementTierDef`·`FactionDef`의 스프라이트 필드에 수동으로 끌어다 놓으면 된다.

## 1. 인물 초상화 (20개) — `portrait_*`

RetainerDef 샘플 데이터·GDD에 등장한 실존/가상 인물. 32×32, 옆모습 흉상.

| 파일명 | 인물 | 비고 |
|---|---|---|
| portrait_ieok | 김억 | MVP 시나리오 주인공 (가상 인물) |
| portrait_wanggeon | 왕건 | 고려 태조 |
| portrait_yugeumpil | 유금필 | 고려 개국공신, 대장군 |
| portrait_sinsunggyeom | 신숭겸 | 고려 개국공신 |
| portrait_hongyu | 홍유 | 고려 개국공신 |
| portrait_baehyeongyeong | 배현경 | 고려 개국공신 |
| portrait_boksagwi | 복사귀 | 고려 개국공신 |
| portrait_bakjiyun | 박지윤 | 청주 호족, 대상서 |
| portrait_choieung | 최응 | 신라 유학자, 내사령 |
| portrait_gyeonhwon | 견훤 | 후백제 시조 |
| portrait_gungye | 궁예 | 태봉(후고구려) 군주 |
| portrait_ajagae | 아자개 | 상주 호족, 견훤의 아버지 |
| portrait_gimsunsik | 김순식 | 명주(강릉) 호족 |
| portrait_yucheongung | 유천궁 | 패서 호족 |
| portrait_gimchong | 김총 | 나주 호족 |
| portrait_wangmu | 왕무 | 제1후계자(태자) 예시 |
| portrait_wangyo | 왕요 | 제2후계자(대군) 예시 |
| portrait_gimsunsik_daughter | 김순식의 장녀 | 혼사 후보 |
| portrait_bakjiyun_son | 박지윤의 차남 | 혼사 후보 |
| portrait_yucheongung_daughter | 유천궁의 외동딸 | 혼사 후보 |

## 2. 자원 아이콘 (3개) — `res_*`

32×32, 아이템 아이콘.

| 파일명 | 자원 |
|---|---|
| res_grain | 석 (식량) |
| res_coin | 엽전 |
| res_prestige | 권위 |

## 3. 병종 아이콘 (4개) — `unit_*`

32×32, 옆모습. §3.11 출진 화면·§3.9 병영 화면에서 쓴다.

| 파일명 | 병종 |
|---|---|
| unit_infantry | 보병 |
| unit_spear | 창병 |
| unit_archer | 궁병 |
| unit_cavalry | 기병 |

## 4. 거점 등급 건물 (5개) — `tier_*`

96×96, 탑다운. 지도 노드·영지 개요·거점 승격 팝업(§3.13)에서 촌→경 성장이 보이게.

| 파일명 | 등급 |
|---|---|
| tier_village | 촌 — 흙 울타리 작은 마을 |
| tier_township | 현 — 목책 두른 고을 |
| tier_district | 군 — 석성 거점 도시 |
| tier_province | 주 — 대도성 |
| tier_capital | 경 — 궁궐급 수도 |

## 5. 세력 깃발 (5개) — `banner_*`

32×48, 세로 깃발. 지도·상단 바에서 세력 구분용.

| 파일명 | 세력 |
|---|---|
| banner_goryeo | 고려 |
| banner_hubaekje | 후백제 |
| banner_silla | 신라 |
| banner_taebong | 태봉 |
| banner_player | 이서 호족 (플레이어) |
