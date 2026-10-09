"""
PixelLab API로 docs/ART_LIST.md 의 에셋을 일괄 생성한다.

    # 인증: 클라우드 환경의 API credentials(api.pixellab.ai)에 키를 등록하면 프록시가 자동으로 붙여준다.
    #       로컬에서는 export PIXELLAB_API_KEY=... 로 직접 넣어도 된다.
    python3 tools/pixelart/pixellab_generate.py --dry-run     # 요청 목록만 출력 (사용량 소모 X)
    python3 tools/pixelart/pixellab_generate.py --balance     # 선불 잔액 확인 (정기구독이면 보통 0)
    python3 tools/pixelart/pixellab_generate.py               # 없는 파일만 생성
    python3 tools/pixelart/pixellab_generate.py --only portrait_wanggeon tier_village --force

결과는 Assets/_Project/Art/Sprites/{이름}.png — 아직 Unity 자동 연결 메뉴는 없어서(2차 작업),
각 Def 에셋의 스프라이트 필드에 수동으로 끌어다 놓으면 된다.

표준 라이브러리만 사용한다.
"""
import argparse
import base64
import json
import os
import sys
import time
import urllib.error
import urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Sprites")
API = os.environ.get("PIXELLAB_API_BASE", "https://api.pixellab.ai/v1")

COMMON = "late Three Kingdoms of Korea, 10th century Korean historical, pixel art, muted earthy palette with crimson and gold accents, dark outline"

# 이름, 가로, 세로, 시점, 프롬프트  (docs/ART_LIST.md 와 같은 목록)
ASSETS = [
    # 인물 초상화 — 옆모습 흉상
    ("portrait_ieok",               32, 32, "side", "young Korean minor lord, plain brown hanbok, topknot, modest headband, determined expression"),
    ("portrait_wanggeon",           32, 32, "side", "Korean king, red and gold royal robe, black gwanmo crown hat, dignified beard"),
    ("portrait_yugeumpil",          32, 32, "side", "Korean general, iron lamellar armor, horned helmet, stern face, scar"),
    ("portrait_sinsunggyeom",       32, 32, "side", "Korean general, bronze armor, thick beard, loyal stoic expression"),
    ("portrait_hongyu",             32, 32, "side", "Korean general, leather and iron armor, short beard, alert eyes"),
    ("portrait_baehyeongyeong",     32, 32, "side", "Korean general, iron armor, topknot under helmet, calm confident face"),
    ("portrait_boksagwi",           32, 32, "side", "Korean general, simple armor, weathered face, long moustache"),
    ("portrait_bakjiyun",           32, 32, "side", "Korean court official, blue silk hanbok, black gat hat, scholarly expression"),
    ("portrait_choieung",           32, 32, "side", "Korean scholar official, white and green robe, horsehair gat hat, wise thin face"),
    ("portrait_gyeonhwon",          32, 32, "side", "Korean warlord king, dark red armor, fierce bearded face, battle scar"),
    ("portrait_gungye",             32, 32, "side", "Korean self-proclaimed monk king, golden robe, eyepatch, wild intense eyes"),
    ("portrait_ajagae",             32, 32, "side", "old Korean regional lord, rough armor, grey beard, weathered farmer-warrior face"),
    ("portrait_gimsunsik",          32, 32, "side", "Korean coastal lord, layered hanbok with fur collar, composed proud face"),
    ("portrait_yucheongung",        32, 32, "side", "Korean regional lord, dark hanbok, black gat hat, shrewd middle-aged face"),
    ("portrait_gimchong",           32, 32, "side", "Korean southern lord, green hanbok, short beard, confident smile"),
    ("portrait_wangmu",             32, 32, "side", "young Korean crown prince, martial build, red and black robe, firm expression"),
    ("portrait_wangyo",             32, 32, "side", "young Korean prince, scholarly robe, book in hand, intelligent gentle face"),
    ("portrait_gimsunsik_daughter", 32, 32, "side", "young Korean noblewoman, pale green hanbok, jade hairpin, gentle face"),
    ("portrait_bakjiyun_son",       32, 32, "side", "young Korean nobleman, red silk hanbok, black gat hat, proud young face"),
    ("portrait_yucheongung_daughter", 32, 32, "side", "young Korean noblewoman, pink and gold hanbok, ornate headdress, elegant face"),
    # 자원 아이콘
    ("res_grain",   32, 32, "side", "game item icon, straw sack of rice grain"),
    ("res_coin",    32, 32, "side", "game item icon, stack of old Korean bronze coins with square holes"),
    ("res_prestige", 32, 32, "side", "game item icon, ornate royal jade seal stamp, red ribbon"),
    # 병종 아이콘
    ("unit_infantry", 32, 32, "side", "Korean foot soldier, round shield, short sword, brown tunic armor, facing right"),
    ("unit_spear",     32, 32, "side", "Korean spearman, long spear, round shield, iron helmet, facing right"),
    ("unit_archer",    32, 32, "side", "Korean archer, traditional bow drawn, leather armor, quiver on back, facing right"),
    ("unit_cavalry",   32, 32, "side", "Korean mounted warrior on horseback, lance, lamellar armor, facing right"),
    # 거점 등급 건물 — 탑다운
    ("tier_village",   96, 96, "low top-down", "small Korean village, thatched roof hanok huts, low wooden fence, dirt paths, isolated object, whole village visible"),
    ("tier_township",  96, 96, "low top-down", "Korean township, wooden palisade wall, tiled roof hanok buildings, small gatehouse, isolated object, whole settlement visible"),
    ("tier_district",  96, 96, "low top-down", "Korean fortified district town, grey stone wall, watchtower, tiled roof government building, banners, isolated object, whole settlement visible"),
    ("tier_province",  96, 96, "low top-down", "Korean provincial capital, tall stone fortress wall, large tiled palace hall, multiple towers, banners, isolated object, whole settlement visible"),
    ("tier_capital",   96, 96, "low top-down", "grand Korean royal capital, palace complex with ornate tiled roofs, tall stone walls, many towers, golden banners, isolated object, whole settlement visible"),
    # 세력 깃발
    ("banner_goryeo",   32, 48, "side", "tall Korean war banner, blue and white cloth, black Chinese character, wooden pole"),
    ("banner_hubaekje", 32, 48, "side", "tall Korean war banner, red and yellow cloth, black Chinese character, wooden pole"),
    ("banner_silla",    32, 48, "side", "tall Korean war banner, gold and white cloth, black Chinese character, wooden pole"),
    ("banner_taebong",  32, 48, "side", "tall Korean war banner, dark purple and gold cloth, black Chinese character, wooden pole"),
    ("banner_player",   32, 48, "side", "tall Korean war banner, brown and crimson cloth, simple clan emblem, wooden pole"),
]


def call(method, path, body=None):
    headers = {"Content-Type": "application/json"}
    key = os.environ.get("PIXELLAB_API_KEY")
    if key:  # 없으면 환경의 API credentials가 프록시에서 붙여준다
        headers["Authorization"] = f"Bearer {key}"
    req = urllib.request.Request(
        f"{API}{path}",
        data=json.dumps(body).encode() if body is not None else None,
        method=method,
        headers=headers,
    )
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req, timeout=180) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            detail = e.read().decode(errors="replace")[:500]
            if e.code in (401, 403):
                sys.exit(f"PixelLab 인증 실패 ({e.code}). API 키를 확인하세요: {detail}")
            if e.code in (429, 500, 502, 503) and attempt < 3:
                wait = 2 ** (attempt + 2)
                print(f"  HTTP {e.code}, {wait}초 후 재시도… {detail}")
                time.sleep(wait)
                continue
            sys.exit(f"PixelLab API 오류 {e.code} ({path}): {detail}")
        except urllib.error.URLError as e:
            sys.exit(f"PixelLab에 연결할 수 없습니다 ({e.reason}). 네트워크에서 api.pixellab.ai 허용 여부를 확인하세요.")


def extract_png(resp):
    """응답에서 base64 이미지 추출. {"image": {"base64": ...}} 형태."""
    img = resp.get("image") or {}
    data = img.get("base64") if isinstance(img, dict) else img
    if not data:
        sys.exit(f"응답에 이미지가 없습니다: {json.dumps(resp)[:300]}")
    if "," in data[:64]:  # data:image/png;base64,... 형태 대비
        data = data.split(",", 1)[1]
    return base64.b64decode(data)


def common_params(w, h, view, prompt):
    return {
        "description": f"{prompt}, {COMMON}",
        "image_size": {"width": w, "height": h},
        "view": view,
        "outline": "single color outline",
        "shading": "basic shading",
        "detail": "medium detail",
        "no_background": True,
    }


def generate(name, w, h, view, prompt):
    body = common_params(w, h, view, prompt)
    resp = call("POST", "/generate-image-pixflux", body)
    png = extract_png(resp)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"{name}.png")
    with open(path, "wb") as f:
        f.write(png)
    usage = resp.get("usage")
    print(f"  ✓ {name}.png ({w}×{h})" + (f"  사용량 {usage}" if usage else ""))
    return png


def main():
    ap = argparse.ArgumentParser(description="PixelLab으로 후삼국통일 에셋 생성")
    ap.add_argument("--only", nargs="+", help="이 이름들만 생성 (예: portrait_wanggeon tier_village)")
    ap.add_argument("--force", action="store_true", help="이미 있는 파일도 다시 생성")
    ap.add_argument("--dry-run", action="store_true", help="요청 목록만 출력")
    ap.add_argument("--balance", action="store_true", help="선불 잔액 확인")
    args = ap.parse_args()

    if args.balance:
        print(json.dumps(call("GET", "/balance"), ensure_ascii=False, indent=2))
        return

    targets = [a for a in ASSETS if not args.only or a[0] in args.only]
    unknown = set(args.only or []) - {a[0] for a in ASSETS}
    if unknown:
        sys.exit(f"알 수 없는 이름: {', '.join(sorted(unknown))}")
    todo = [a for a in targets if args.force or not os.path.exists(os.path.join(OUT, f"{a[0]}.png"))]

    print(f"생성 대상 {len(todo)}개 (전체 {len(ASSETS)}개 중)")
    if args.dry_run:
        for name, w, h, view, prompt in todo:
            print(f"  {name:28} {w}×{h}  [{view}]  {prompt}")
        return

    for name, w, h, view, prompt in todo:
        generate(name, w, h, view, prompt)

    print("완료!")


if __name__ == "__main__":
    main()
