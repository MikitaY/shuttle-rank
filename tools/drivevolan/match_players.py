"""Suggests our player for every drivevolan.by name that data/drivevolan/players.csv doesn't know yet.

    python tools/drivevolan/match_players.py

Reads the exports in data/drivevolan/tournaments/ and the published ratings
(web/public/data/ratings.json), and writes data/drivevolan/players-candidates.csv for review:
drivevolan writes "Фамилия Имя" (sometimes in Latin), we write "Имя Фамилия", so names are
compared as token sets with a phonetic fuzzy fallback. Nothing here is final — a person checks
the candidates, and the confirmed rows are appended to players.csv (column `player` holds our
spelling; a new player gets "Имя Фамилия").
"""
import csv
import difflib
import json
import pathlib
import re
from collections import defaultdict

ROOT = pathlib.Path(__file__).resolve().parents[2]
DV = ROOT / 'data' / 'drivevolan'

TR = dict(zip('абвгдеёжзийклмнопрстуфхцчшщъыьэюя',
              ['a', 'b', 'v', 'g', 'd', 'e', 'e', 'zh', 'z', 'i', 'y', 'k', 'l', 'm', 'n', 'o', 'p', 'r', 's',
               't', 'u', 'f', 'kh', 'ts', 'ch', 'sh', 'shch', '', 'y', '', 'e', 'yu', 'ya']))
# Diminutives the site lets players register with.
DIMINUTIVES = {'костя': 'константин', 'катя': 'екатерина', 'машка': 'мария', 'маша': 'мария', 'юля': 'юлия',
               'саша': 'александр', 'дима': 'дмитрий', 'женя': 'евгений', 'эля': 'элеонора'}


def token(t):
    t = t.lower().replace('ё', 'е')
    return DIMINUTIVES.get(t, t)


def skeleton(s):
    """Coarse Latin skeleton, so Volotovskij ~ Волотовский and Aliaksandra ~ Александра."""
    s = ''.join(TR.get(c, c) for c in s.lower().replace('ё', 'е'))
    for a, b in [('kh', 'h'), ('ij', 'y'), ('iy', 'y'), ('yi', 'y'), ('ii', 'i'), ('j', 'y'), ('ia', 'a'),
                 ('ya', 'a'), ('yu', 'u'), ('iu', 'u'), ('ks', 'x'), ('w', 'v'), ('ff', 'f'), ('th', 't')]:
        s = s.replace(a, b)
    return re.sub(r'[^a-z ]', '', s)


def similarity(a, b):
    """Best order-insensitive pairing of two-token names, 0..1."""
    def r(x, y):
        return difflib.SequenceMatcher(None, skeleton(x), skeleton(y)).ratio()
    if len(a) != 2 or len(b) != 2:
        return difflib.SequenceMatcher(None, ' '.join(sorted(map(skeleton, a))),
                                       ' '.join(sorted(map(skeleton, b)))).ratio()
    return max((r(a[0], b[0]) + r(a[1], b[1])) / 2, (r(a[0], b[1]) + r(a[1], b[0])) / 2)


def main():
    known = set()
    players_csv = DV / 'players.csv'
    if players_csv.exists():
        known = {row['name_drivevolan'] for row in csv.DictReader(open(players_csv, encoding='utf-8-sig'), delimiter=';')}

    seen = {}
    for f in sorted((DV / 'tournaments').glob('*.json')):
        t = json.loads(f.read_text(encoding='utf-8'))
        for p in t['players']:
            if p['name'] not in known:
                seen.setdefault(p['name'], {'ids': set(), 'tournaments': []})
                seen[p['name']]['ids'].add(p['id'])
                seen[p['name']]['tournaments'].append(t['name'])

    ratings = json.loads((ROOT / 'web' / 'public' / 'data' / 'ratings.json').read_text(encoding='utf-8'))
    ours = {p['name']: p for p in ratings['players']}
    by_tokens = defaultdict(list)
    for name in ours:
        by_tokens[frozenset(token(t) for t in name.split())].append(name)

    rows = []
    for name, info in sorted(seen.items()):
        toks = [token(t) for t in name.split()]
        exact = by_tokens.get(frozenset(toks), [])
        if len(exact) == 1:
            match, status, note = exact[0], 'совпадение', ''
        else:
            best = sorted(((similarity(toks, [token(t) for t in n.split()]), n) for n in ours), reverse=True)[:3]
            score, cand = best[0]
            if score >= 0.80:
                match, status = cand, 'проверить'
                note = f'сходство {score:.2f}' + ''.join(f'; или {n} ({s:.2f})' for s, n in best[1:] if s >= 0.75)
            else:
                match, status = '', 'новый'
                note = f'ближайший: {cand} ({score:.2f})' if score >= 0.7 else ''
        p = ours.get(match)
        level = f"{p.get('level') or '?'}, {p.get('elo_matches', {}).get('overall', 0)} матч." if p else ''
        rows.append([', '.join(map(str, sorted(info['ids']))), name, ', '.join(info['tournaments']),
                     match, level, status, note])

    out = DV / 'players-candidates.csv'
    with open(out, 'w', encoding='utf-8-sig', newline='') as f:
        w = csv.writer(f, delimiter=';')
        w.writerow(['drivevolan_id', 'name_drivevolan', 'tournaments', 'player', 'our_level', 'status', 'comment'])
        w.writerows(rows)
    print(f'{len(rows)} unknown names -> {out}')


if __name__ == '__main__':
    main()
