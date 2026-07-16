"""
Cataclysm-BN JSON → Project_Pictureless 정제 JSON 변환기

사용법:
    python convert.py --bn-path "Z:/Work/Project/Cataclysm-BN" --output "../../Assets/StreamingAssets/BNData"

CC BY-SA 3.0 라이선스 준수:
    출력 JSON은 Cataclysm: Bright Nights 의 파생 저작물이며,
    동일한 CC BY-SA 3.0 라이선스가 적용됩니다.
    https://creativecommons.org/licenses/by-sa/3.0/
"""

import argparse
import copy
import json
import os
import sys
from pathlib import Path
from typing import Any


# ── Helpers ─────────────────────────────────────────────────────────

def load_all_json(directory: Path) -> list[dict]:
    """디렉토리 내 모든 .json 을 재귀적으로 읽어 리스트로 반환."""
    entries: list[dict] = []
    for path in sorted(directory.rglob("*.json")):
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
        except (json.JSONDecodeError, UnicodeDecodeError) as e:
            print(f"  [WARN] skip {path}: {e}", file=sys.stderr)
            continue
        if isinstance(data, list):
            entries.extend(data)
        elif isinstance(data, dict):
            entries.append(data)
    return entries


def deep_merge(base: dict, override: dict) -> dict:
    """base 위에 override 를 얹는 deep merge. 배열은 override가 덮어씀."""
    result = copy.deepcopy(base)
    for key, val in override.items():
        if key in ("copy-from", "abstract", "looks_like"):
            continue
        if key == "extend":
            for ek, ev in val.items():
                if ek in result and isinstance(result[ek], list):
                    if isinstance(ev, list):
                        result[ek] = result[ek] + ev
                    else:
                        result[ek].append(ev)
                else:
                    result[ek] = ev if isinstance(ev, list) else [ev]
            continue
        if key == "proportional":
            for pk, pv in val.items():
                if pk in result and isinstance(result[pk], (int, float)):
                    result[pk] = result[pk] * pv
            continue
        if key == "relative":
            for rk, rv in val.items():
                if rk in result and isinstance(result[rk], (int, float)):
                    result[rk] = result[rk] + rv
            continue
        if key == "delete":
            for dk, dv in val.items():
                if dk in result and isinstance(result[dk], list):
                    result[dk] = [x for x in result[dk] if x not in dv]
            continue
        result[key] = copy.deepcopy(val)
    return result


def parse_time_to_minutes(t) -> float:
    """BN 시간 문자열 → 분(float). 예: '7 h 40 m' → 460.0"""
    if isinstance(t, (int, float)):
        return t / 100.0  # movement points → minutes (rough)
    if not isinstance(t, str):
        return 0.0
    t = t.strip()
    total = 0.0
    parts = t.replace(",", " ").split()
    i = 0
    while i < len(parts):
        try:
            num = float(parts[i])
        except ValueError:
            i += 1
            continue
        unit = parts[i + 1] if i + 1 < len(parts) else "m"
        if unit.startswith("s"):
            total += num / 60.0
        elif unit.startswith("m"):
            total += num
        elif unit.startswith("h"):
            total += num * 60.0
        elif unit.startswith("d"):
            total += num * 1440.0
        else:
            total += num
        i += 2
    return round(total, 2) if total else 0.0


def parse_weight_to_g(w) -> int:
    if isinstance(w, (int, float)):
        return int(w)
    if not isinstance(w, str):
        return 0
    w = w.strip()
    if w.endswith("kg"):
        return int(float(w[:-2].strip()) * 1000)
    if w.endswith("mg"):
        return max(1, int(float(w[:-2].strip()) / 1000))
    if w.endswith("g"):
        return int(float(w[:-1].strip()))
    # "200 g" style with space
    parts = w.split()
    if len(parts) == 2:
        try:
            num = float(parts[0])
            unit = parts[1].lower()
            if unit == "kg":
                return int(num * 1000)
            if unit == "mg":
                return max(1, int(num / 1000))
            if unit == "g":
                return int(num)
        except ValueError:
            pass
    try:
        return int(float(w))
    except ValueError:
        return 0


def parse_volume_to_ml(v) -> int:
    if isinstance(v, (int, float)):
        return int(v) * 250  # legacy: units of 250ml
    if not isinstance(v, str):
        return 0
    v = v.strip()
    if v.endswith("L"):
        return int(float(v[:-1].strip()) * 1000)
    if v.endswith("ml"):
        return int(float(v[:-2].strip()))
    try:
        return int(float(v)) * 250
    except ValueError:
        return 0


def get_item_name(entry: dict) -> str:
    name = entry.get("name", "")
    if isinstance(name, dict):
        return name.get("str", name.get("str_sp", name.get("str_pl", "")))
    return str(name)


# ── Phase: Items ────────────────────────────────────────────────────

def load_items(bn_path: Path) -> dict[str, dict]:
    """모든 아이템 정의를 읽고 copy-from 상속을 해소하여 id→data 맵 반환."""
    items_dir = bn_path / "data" / "json" / "items"
    obsoletion_dir = bn_path / "data" / "json" / "obsoletion"
    print(f"[items] Loading from {items_dir} ...")
    raw = load_all_json(items_dir)
    if obsoletion_dir.exists():
        print(f"[items] Loading from {obsoletion_dir} ...")
        raw.extend(load_all_json(obsoletion_dir))

    # 1차: type별 인덱스
    ITEM_TYPES = {
        "GENERIC", "COMESTIBLE", "ARMOR", "TOOL", "GUN", "AMMO",
        "BOOK", "MAGAZINE", "GUNMOD", "TOOL_ARMOR", "BIONIC_ITEM",
        "PET_ARMOR", "ENGINE",
        # containers/tools that are used as targets by recipes
        "CONTAINER",
        "TOOLMOD",
        # vehicle/obsolete entries that are still referenced by recipes/tools
        "WHEEL",
        "MIGRATION",
    }
    by_id: dict[str, dict] = {}
    abstracts: dict[str, dict] = {}

    for entry in raw:
        t = entry.get("type", "")
        if t not in ITEM_TYPES:
            continue
        eid = entry.get("id") or entry.get("abstract")
        if not eid:
            continue
        if isinstance(eid, list):
            # obsoletion 등에서 id/abstract가 list로 들어오는 경우가 있어 방어
            eid = eid[0] if len(eid) > 0 and isinstance(eid[0], str) else None
        if not isinstance(eid, str):
            continue
        if "abstract" in entry:
            abstracts[eid] = entry
        else:
            by_id[eid] = entry

    # 2차: copy-from 해소
    resolved: dict[str, dict] = {}
    resolve_stack: set[str] = set()

    def resolve(eid: str) -> dict | None:
        if eid in resolved:
            return resolved[eid]
        if eid in resolve_stack:
            return None  # circular
        resolve_stack.add(eid)

        entry = by_id.get(eid) or abstracts.get(eid)
        if entry is None:
            resolve_stack.discard(eid)
            return None

        parent_id = entry.get("copy-from")
        if parent_id and parent_id != eid:
            parent = resolve(parent_id)
            if parent:
                entry = deep_merge(parent, entry)

        if eid in by_id:
            resolved[eid] = entry
        resolve_stack.discard(eid)
        return entry

    for eid in list(by_id.keys()):
        resolve(eid)

    # abstract 중에도 copy-from 체인 필요하므로 resolve하되 출력은 안 함
    for eid in list(abstracts.keys()):
        resolve(eid)

    print(f"[items] Resolved {len(resolved)} items ({len(abstracts)} abstracts skipped)")
    return resolved


def export_items_and_containers(resolved: dict[str, dict]) -> tuple[list[dict], list[dict]]:
    """정제된 아이템 리스트 + CONTAINER 정의 생성."""
    items_out = []
    containers_out = []
    for eid, entry in sorted(resolved.items()):
        materials = entry.get("material", [])
        if isinstance(materials, str):
            materials = [materials]

        flags = entry.get("flags", [])
        if isinstance(flags, str):
            flags = [flags]

        cat = entry.get("category", "other")
        if isinstance(cat, dict):
            cat = cat.get("id", "other")

        item_type = entry.get("type", "GENERIC")
        comestible_type = entry.get("comestible_type", "")
        qualities = entry.get("qualities", [])
        qual_out = []
        for q in qualities:
            if isinstance(q, list) and len(q) >= 2:
                qual_out.append({"id": q[0], "level": q[1]})
            elif isinstance(q, dict):
                qual_out.append({"id": q.get("id", ""), "level": q.get("level", 0)})

        item = {
            "id": eid,
            "name": get_item_name(entry),
            "type": item_type,
            "category": cat,
            "weight_g": parse_weight_to_g(entry.get("weight", 0)),
            "volume_ml": parse_volume_to_ml(entry.get("volume", 0)),
            "materials": materials,
            "flags": flags,
            "qualities": qual_out,
        }
        if comestible_type:
            item["comestible_type"] = comestible_type

        # CONTAINER: nested inventory 용량 정의까지 items.json root로 함께 출력
        if item_type == "CONTAINER":
            # BN: containers.json에서 contains 값으로 용량(부피)을 제공
            contains = entry.get("contains", entry.get("volume", 0))
            max_volume_ml = parse_volume_to_ml(contains)
            # BN 컨버터 스코프에서 max_weight는 근사한다(액체/일반 재료 1g/ml 가정).
            max_weight_g = float(max_volume_ml)

            item["is_container"] = True
            item["container_id"] = eid
            containers_out.append({
                "id": eid,
                "name": get_item_name(entry),
                "max_weight": max_weight_g,
                "max_volume": float(max_volume_ml),
            })

        # BOOK: 해당 책이 가리키는 skill/필요 레벨/상한 레벨 출력
        if item_type == "BOOK":
            item["book_skill"] = entry.get("skill", "")
            item["book_required_level"] = entry.get("required_level", 0)
            item["book_max_level"] = entry.get("max_level", 0)

        items_out.append(item)
    return items_out, containers_out


# ── Requirement LIST expansion helpers ─────────────────────────

MAX_LIST_EXPANSION_DEPTH = 6

def _scale_component_count(base_count, multiplier):
    try:
        return int(base_count) * int(multiplier)
    except Exception:
        return base_count * multiplier


def expand_component_requirement(req_id: str, count_multiplier: int, requirements: dict[str, dict], depth: int = 0) -> list[dict]:
    """requirement가 {"item": <req_id>, "list": true}로 들어오는 경우를 alternatives로 전개."""
    if depth > MAX_LIST_EXPANSION_DEPTH:
        # circular/overly deep structures: stop expanding to keep converter stable
        print(f"[WARN] LIST component expansion depth exceeded: {req_id}")
        return [{"item": req_id, "count": count_multiplier}]

    req = requirements.get(req_id)
    if not req:
        return [{"item": req_id, "count": count_multiplier}]

    comps = req.get("components", []) or []
    if not comps or not isinstance(comps, list):
        return [{"item": req_id, "count": count_multiplier}]

    # 현재 출력 스키마가 component-slot 단위 OR만 표현하므로 첫 슬롯만 사용
    first_slot = comps[0] if isinstance(comps[0], list) else []
    out: list[dict] = []
    for alt in first_slot:
        if not isinstance(alt, list) or len(alt) < 2:
            continue
        target_id = alt[0]
        base_count = alt[1]
        nested_multiplier = _scale_component_count(base_count, count_multiplier)
        if len(alt) >= 3 and alt[2] == "LIST":
            out.extend(expand_component_requirement(target_id, nested_multiplier, requirements, depth + 1))
        else:
            out.append({"item": target_id, "count": nested_multiplier})
    return out


def _scale_tool_charges(base_charges, multiplier):
    # BN conventions: -1 means "unlimited/unknown" and should not be multiplied.
    if isinstance(base_charges, (int, float)) and base_charges > 0:
        return int(base_charges) * int(multiplier)
    return base_charges


def expand_tool_requirement(req_id: str, charges_multiplier: int, requirements: dict[str, dict], depth: int = 0) -> list[dict]:
    """requirement가 tools alternatives에서 list 참조로 들어오는 경우 전개."""
    if depth > MAX_LIST_EXPANSION_DEPTH:
        print(f"[WARN] LIST tool expansion depth exceeded: {req_id}")
        return [{"tool": req_id, "charges": charges_multiplier}]

    req = requirements.get(req_id)
    if not req:
        return [{"tool": req_id, "charges": charges_multiplier}]

    tools = req.get("tools", []) or []
    if not tools or not isinstance(tools, list):
        return [{"tool": req_id, "charges": charges_multiplier}]

    # 현재 출력 스키마가 tool-slot 단위 OR만 표현하므로 첫 슬롯만 사용
    first_slot = tools[0] if isinstance(tools[0], list) else []
    out: list[dict] = []
    for alt in first_slot:
        if not isinstance(alt, list) or len(alt) < 2:
            continue
        target_id = alt[0]
        base_charges = alt[1]
        if len(alt) >= 3 and alt[2] == "LIST":
            nested_multiplier = _scale_tool_charges(base_charges, charges_multiplier)
            out.extend(expand_tool_requirement(target_id, nested_multiplier, requirements, depth + 1))
        else:
            scaled = _scale_tool_charges(base_charges, charges_multiplier)
            out.append({"tool": target_id, "charges": scaled})
    return out


# ── Phase: Requirements ─────────────────────────────────────────────

def load_requirements(bn_path: Path) -> dict[str, dict]:
    """공유 요구사항(using 참조 대상)을 id→data 맵으로 반환."""
    req_dir = bn_path / "data" / "json" / "requirements"
    print(f"[requirements] Loading from {req_dir} ...")
    raw = load_all_json(req_dir)
    reqs: dict[str, dict] = {}
    for entry in raw:
        if entry.get("type") == "requirement":
            rid = entry.get("id", "")
            if rid:
                reqs[rid] = entry
    print(f"[requirements] Loaded {len(reqs)} requirements")
    return reqs


# ── Phase: Materials & Qualities ────────────────────────────────────

def load_materials(bn_path: Path) -> list[dict]:
    mat_file = bn_path / "data" / "json" / "materials.json"
    if not mat_file.exists():
        return []
    with open(mat_file, "r", encoding="utf-8") as f:
        data = json.load(f)
    mats = []
    for entry in data:
        if entry.get("type") == "material":
            name = entry.get("name", "")
            if isinstance(name, dict):
                name = name.get("str", "")
            mats.append({
                "id": entry.get("id", ""),
                "name": name,
            })
    print(f"[materials] Loaded {len(mats)} materials")
    return mats


def load_qualities(bn_path: Path) -> list[dict]:
    q_file = bn_path / "data" / "json" / "tool_qualities.json"
    if not q_file.exists():
        return []
    with open(q_file, "r", encoding="utf-8") as f:
        data = json.load(f)
    quals = []
    for entry in data:
        if entry.get("type") == "tool_quality":
            name = entry.get("name", "")
            if isinstance(name, dict):
                name = name.get("str", "")
            quals.append({
                "id": entry.get("id", ""),
                "name": name,
            })
    print(f"[qualities] Loaded {len(quals)} tool qualities")
    return quals


def load_skills(bn_path: Path) -> list[dict]:
    skills_file = bn_path / "data" / "json" / "skills.json"
    if not skills_file.exists():
        return []
    with open(skills_file, "r", encoding="utf-8") as f:
        data = json.load(f)

    skills_out: list[dict] = []
    if not isinstance(data, list):
        return skills_out

    for entry in data:
        if not isinstance(entry, dict):
            continue
        if entry.get("type") != "skill":
            continue
        sid = entry.get("id", "")
        name = entry.get("name", "")
        if isinstance(name, dict):
            name = name.get("str", "") or name.get("str_pl", "") or ""
        skills_out.append({"id": sid, "name": str(name)})
    print(f"[skills] Loaded {len(skills_out)} skills")
    return skills_out


# ── Phase: Recipes ──────────────────────────────────────────────────

def load_recipes(bn_path: Path, requirements: dict[str, dict]) -> list[dict]:
    """레시피를 로드하고, using 평탄화 + copy-from 해소 + 필터링."""
    recipes_dir = bn_path / "data" / "json" / "recipes"
    print(f"[recipes] Loading from {recipes_dir} ...")
    raw = load_all_json(recipes_dir)

    # 분류
    recipes_raw: list[dict] = []
    uncraft_raw: list[dict] = []
    recipe_by_key: dict[str, dict] = {}

    for entry in raw:
        t = entry.get("type", "")
        if t == "recipe":
            recipes_raw.append(entry)
            result = entry.get("result", "")
            suffix = entry.get("id_suffix", "")
            key = f"{result}:{suffix}" if suffix else result
            recipe_by_key[key] = entry
        elif t == "uncraft":
            uncraft_raw.append(entry)

    print(f"[recipes] Raw: {len(recipes_raw)} recipes, {len(uncraft_raw)} uncraft")

    # copy-from 해소 (레시피 간)
    resolved_recipes: list[dict] = []
    for entry in recipes_raw:
        parent_id = entry.get("copy-from")
        if parent_id and parent_id in recipe_by_key:
            parent = recipe_by_key[parent_id]
            entry = deep_merge(parent, entry)
        resolved_recipes.append(entry)

    # 필터링
    filtered: list[dict] = []
    skip_count = 0
    for entry in resolved_recipes:
        # basecamp 건설 레시피 제외
        if entry.get("construction_blueprint"):
            skip_count += 1
            continue
        cat = entry.get("category", "")
        if cat == "CC_BUILDING":
            skip_count += 1
            continue
        # obsolete 제외
        if entry.get("obsolete"):
            skip_count += 1
            continue
        # never_learn NPC 전용 제외
        if entry.get("never_learn"):
            skip_count += 1
            continue
        # result 없는 항목 제외
        if not entry.get("result"):
            skip_count += 1
            continue
        filtered.append(entry)

    print(f"[recipes] Filtered: {len(filtered)} kept, {skip_count} skipped")

    # using 평탄화 + 출력 포맷
    output: list[dict] = []
    for entry in filtered:
        output.append(flatten_recipe(entry, requirements))

    # uncraft도 별도 키로 포함
    uncraft_out: list[dict] = []
    for entry in uncraft_raw:
        if not entry.get("result"):
            continue
        uncraft_out.append(flatten_recipe(entry, requirements, is_uncraft=True))

    print(f"[recipes] Output: {len(output)} recipes, {len(uncraft_out)} uncraft")
    return output, uncraft_out


def flatten_recipe(entry: dict, requirements: dict[str, dict],
                   is_uncraft: bool = False) -> dict:
    """단일 레시피를 정제 포맷으로 변환. using 참조를 인라인 전개."""

    # components 정규화 (LIST requirement 전개 포함)
    components = normalize_components(entry.get("components", []), requirements)

    # tools 정규화 (LIST requirement 전개 포함)
    tools = normalize_tools(entry.get("tools", []), requirements)

    # qualities 정규화
    qualities = []
    for q in entry.get("qualities", []):
        if isinstance(q, dict):
            qualities.append({"id": q.get("id", ""), "level": q.get("level", 0)})

    # using 전개
    for using_ref in entry.get("using", []):
        if not isinstance(using_ref, list) or len(using_ref) < 2:
            continue
        req_id, multiplier = using_ref[0], using_ref[1]
        req = requirements.get(req_id)
        if not req:
            continue

        # requirement의 components를 multiplier 적용하여 추가
        for comp_slot in req.get("components", []):
            scaled_slot = []
            for alt in comp_slot:
                if isinstance(alt, list) and len(alt) >= 2:
                    item_id = alt[0]
                    count = alt[1] * multiplier
                    if len(alt) >= 3 and alt[2] == "LIST":
                        # LIST alt 는 requirement id 이므로 alternatives로 재귀 전개
                        scaled_slot.extend(expand_component_requirement(item_id, count, requirements))
                    else:
                        scaled_slot.append({"item": item_id, "count": count})
            if scaled_slot:
                components.append({"alternatives": scaled_slot})

        # requirement의 tools 추가
        for tool_slot in req.get("tools", []):
            scaled_slot = []
            for alt in tool_slot:
                if isinstance(alt, list) and len(alt) >= 2:
                    tool_id = alt[0]
                    charges = alt[1] * multiplier if alt[1] > 0 else alt[1]
                    if len(alt) >= 3 and alt[2] == "LIST":
                        scaled_slot.extend(expand_tool_requirement(tool_id, charges, requirements))
                    else:
                        scaled_slot.append({"tool": tool_id, "charges": charges})
            if scaled_slot:
                tools.append({"alternatives": scaled_slot})

        # requirement의 qualities 추가
        for q in req.get("qualities", []):
            if isinstance(q, dict):
                qualities.append({"id": q.get("id", ""), "level": q.get("level", 0)})

    # dedup qualities (같은 id면 높은 level만 유지)
    qual_map: dict[str, int] = {}
    for q in qualities:
        qid = q["id"]
        if qid not in qual_map or q["level"] > qual_map[qid]:
            qual_map[qid] = q["level"]
    qualities = [{"id": k, "level": v} for k, v in sorted(qual_map.items())]

    # autolearn 정규화
    autolearn = entry.get("autolearn", False)
    if isinstance(autolearn, list):
        autolearn_skills = autolearn
        autolearn = True
    else:
        autolearn_skills = []

    # book_learn 정규화
    book_learn = []
    for bl in entry.get("book_learn", []):
        if isinstance(bl, list) and len(bl) >= 2:
            book_learn.append({"book": bl[0], "level": bl[1]})

    # skills_required 정규화
    skills_req = entry.get("skills_required", [])
    skills_out = []
    if skills_req:
        if isinstance(skills_req[0], str):
            skills_out = [{"skill": skills_req[0], "level": skills_req[1] if len(skills_req) > 1 else 0}]
        elif isinstance(skills_req[0], list):
            for sr in skills_req:
                if len(sr) >= 2:
                    skills_out.append({"skill": sr[0], "level": sr[1]})

    result_id = entry.get("result", "")
    suffix = entry.get("id_suffix", "")
    recipe_id = f"{result_id}_{suffix}" if suffix else result_id

    rec = {
        "id": recipe_id,
        "result": result_id,
        "category": entry.get("category", ""),
        "subcategory": entry.get("subcategory", ""),
        "skill_used": entry.get("skill_used", ""),
        "skills_required": skills_out,
        "difficulty": entry.get("difficulty", 0),
        "time_minutes": parse_time_to_minutes(entry.get("time", 0)),
        "reversible": entry.get("reversible", False),
        "autolearn": autolearn,
        "result_count": entry.get("charges", entry.get("result_mult", 1)),
        "qualities_required": qualities,
        "tools": tools,
        "components": components,
    }

    if is_uncraft:
        rec["is_uncraft"] = True

    if book_learn:
        rec["book_learn"] = book_learn

    if entry.get("byproducts"):
        rec["byproducts"] = [
            {"item": bp[0], "count": bp[1] if len(bp) > 1 else 1}
            for bp in entry["byproducts"] if isinstance(bp, list)
        ]

    return rec


def normalize_components(comps: list, requirements: dict[str, dict]) -> list[dict]:
    """components 배열을 JsonUtility 호환 형태로 정규화.
    각 슬롯을 {"alternatives": [...]} 오브젝트로 감싼다."""
    result = []
    for slot in comps:
        if not isinstance(slot, list):
            continue
        alternatives = []
        for alt in slot:
            if isinstance(alt, list) and len(alt) >= 2:
                item_id = alt[0]
                count = alt[1]
                if len(alt) >= 3 and alt[2] == "LIST":
                    alternatives.extend(expand_component_requirement(item_id, count, requirements))
                else:
                    alternatives.append({"item": item_id, "count": count})
        if alternatives:
            result.append({"alternatives": alternatives})
    return result


def normalize_tools(tools: list, requirements: dict[str, dict]) -> list[dict]:
    """tools 배열을 JsonUtility 호환 형태로 정규화.
    각 슬롯을 {"alternatives": [...]} 오브젝트로 감싼다."""
    result = []
    for slot in tools:
        if not isinstance(slot, list):
            continue
        alternatives = []
        for alt in slot:
            if isinstance(alt, list) and len(alt) >= 2:
                tool_id = alt[0]
                charges = alt[1]
                if len(alt) >= 3 and alt[2] == "LIST":
                    alternatives.extend(expand_tool_requirement(tool_id, charges, requirements))
                else:
                    alternatives.append({"tool": tool_id, "charges": charges})
        if alternatives:
            result.append({"alternatives": alternatives})
    return result


# ── Main ────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="BN → Project JSON converter")
    parser.add_argument("--bn-path", required=True, help="Cataclysm-BN 프로젝트 루트")
    parser.add_argument("--output", required=True, help="출력 디렉토리")
    args = parser.parse_args()

    bn_path = Path(args.bn_path)
    out_dir = Path(args.output)
    out_dir.mkdir(parents=True, exist_ok=True)

    if not (bn_path / "data" / "json").exists():
        print(f"[ERROR] BN data path not found: {bn_path / 'data' / 'json'}")
        sys.exit(1)

    # 1) Items
    resolved_items = load_items(bn_path)
    items_out, containers_out = export_items_and_containers(resolved_items)

    # 2) Materials & Qualities
    materials = load_materials(bn_path)
    qualities = load_qualities(bn_path)

    # 2.5) Skills
    skills_out = load_skills(bn_path)

    # 3) Requirements
    requirements = load_requirements(bn_path)

    # 4) Recipes
    recipes_out, uncraft_out = load_recipes(bn_path, requirements)

    # ── Write output ────────────────────────────────────────────────

    items_file = out_dir / "items.json"
    with open(items_file, "w", encoding="utf-8") as f:
        json.dump({
            "_license": "CC BY-SA 3.0 — derived from Cataclysm: Bright Nights",
            "_source": "https://github.com/cataclysmbnteam/Cataclysm-BN",
            "materials": materials,
            "qualities": qualities,
            "skills": skills_out,
            "items": items_out,
            "containers": containers_out,
        }, f, ensure_ascii=False, indent=2)
    print(f"\n[output] {items_file}  ({len(items_out)} items, {len(containers_out)} containers)")

    recipes_file = out_dir / "recipes.json"
    with open(recipes_file, "w", encoding="utf-8") as f:
        json.dump({
            "_license": "CC BY-SA 3.0 — derived from Cataclysm: Bright Nights",
            "_source": "https://github.com/cataclysmbnteam/Cataclysm-BN",
            "recipes": recipes_out,
            "uncraft": uncraft_out,
        }, f, ensure_ascii=False, indent=2)
    print(f"[output] {recipes_file}  ({len(recipes_out)} recipes, {len(uncraft_out)} uncraft)")

    # 통계 요약
    categories = set()
    skills = set()
    for r in recipes_out:
        if r["category"]:
            categories.add(r["category"])
        if r["skill_used"]:
            skills.add(r["skill_used"])
    print(f"\n── Summary ──")
    print(f"  Items:      {len(items_out)}")
    print(f"  Materials:  {len(materials)}")
    print(f"  Qualities:  {len(qualities)}")
    print(f"  Recipes:    {len(recipes_out)}")
    print(f"  Uncraft:    {len(uncraft_out)}")
    print(f"  Categories: {sorted(categories)}")
    print(f"  Skills:     {sorted(skills)}")


if __name__ == "__main__":
    main()
