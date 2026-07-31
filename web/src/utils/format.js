// Shared formatting helpers used across components.

// '2026-05-10' -> '10.05.2026'
export function fmtDate(iso) {
  return iso ? iso.split('-').reverse().join('.') : ''
}

// Rating-category tabs shown in the controls bar: disciplines first, then skill levels
// (Masters first among levels — it's the featured bracket, not a footnote after E).
export const CATEGORIES = [
  { key: 'singles', label: 'Одиночки', group: 'discipline' },
  { key: 'doubles', label: 'Пары', group: 'discipline' },
  { key: 'mixed', label: 'Микст', group: 'discipline' },
  { key: 'M', label: 'Masters', group: 'level' },
  { key: 'A', label: 'A', group: 'level' },
  { key: 'B', label: 'B', group: 'level' },
  { key: 'C', label: 'C', group: 'level' },
  { key: 'D', label: 'D', group: 'level' },
  { key: 'E', label: 'E', group: 'level' },
]

// The two rating styles the switcher toggles between: the per-category tables
// (discipline + level) and the single cross-category ladder.
export const RATING_MODES = [
  { key: 'category', label: 'По категориям' },
  { key: 'unified', label: 'Сквозной' },
]

// Category ladder, weakest first: [{ key: 'E', floor: 0 }, { key: 'D', floor: 1200 }, …].
// Comes straight from ratings.json; older snapshots without unified_params fall back to the
// floors the rated players themselves carry, so the thresholds are never duplicated here.
export function categoryLadder(data) {
  if (data?.unified_params?.categories?.length) return data.unified_params.categories

  const floors = new Map()
  for (const p of data?.players ?? []) {
    if (!p.unified) continue
    floors.set(p.unified.category, p.unified.floor)
    if (p.unified.next_category != null) floors.set(p.unified.next_category, p.unified.next_floor)
  }
  return [...floors].map(([key, floor]) => ({ key, floor })).sort((a, b) => a.floor - b.floor)
}

// Category a bare rating falls into — used for pairs, which have no category of their own.
export function categoryOf(rating, ladder) {
  let found = ladder[0]
  for (const band of ladder) if (rating >= band.floor) found = band
  return found?.key ?? ''
}

// Display name of a category: "M" is the league's Masters+ bracket, the rung above A.
const CATEGORY_NAMES = { M: 'М+' }
export function categoryName(key) {
  return CATEGORY_NAMES[key] || key
}

// Badge text: "C" normally, "D → C" for a player within reach of the next category.
export function categoryLabel(u) {
  return u.status === 'promotion' && u.next_category
    ? `${categoryName(u.category)} → ${categoryName(u.next_category)}`
    : categoryName(u.category)
}

// Tooltip spelling out what the badge is showing.
export function categoryHint(u, ladder = [], confirmAt = 10) {
  if (u.provisional) {
    const left = Math.max(0, confirmAt - u.matches)
    return `Категория ещё не присвоена — показана та, в которой игрок начал. `
      + `Осталось ${left} ${plural(left, ['матч', 'матча', 'матчей'])} из ${confirmAt}.`
  }
  if (u.status === 'promotion' && u.next_floor != null) {
    const left = Math.max(0, Math.round(u.next_floor - u.rating))
    return `До категории ${categoryName(u.next_category)} осталось `
      + `${left} ${plural(left, ['очко', 'очка', 'очков'])}.`
  }
  if (u.status === 'demotion' && ladder.length) {
    const at = ladder.findIndex(b => b.key === u.category)
    const below = at > 0 ? ` (ниже — ${categoryName(ladder[at - 1].key)})` : ''
    return `Рейтинг у нижней границы категории ${categoryName(u.category)}${below}.`
  }
  return `Категория ${categoryName(u.category)}: от ${Math.round(u.floor)}`
    + (u.next_floor != null ? ` до ${Math.round(u.next_floor - 1)}.` : ' и выше.')
}

// A pair is not rated as an entity — its strength is the mean of the two players,
// exactly the number the engine uses as the pair's expected level in a doubles match.
export function pairRating(a, b) {
  return (a.unified.rating + b.unified.rating) / 2
}

// Russian plural: plural(2, ['матч', 'матча', 'матчей']) -> 'матча'.
export function plural(n, forms) {
  const mod100 = n % 100
  if (mod100 >= 11 && mod100 <= 14) return forms[2]
  const mod10 = n % 10
  if (mod10 === 1) return forms[0]
  if (mod10 >= 2 && mod10 <= 4) return forms[1]
  return forms[2]
}

// Elo expectation: chance the first rating beats the second.
export function winProbability(ra, rb, scale = 400) {
  return 1 / (1 + Math.pow(10, (rb - ra) / scale))
}

// Combined discipline+level key, e.g. ("singles", "D") -> "singles_D" — matches the
// keys the backend uses for elo/by_combo/points_by_combo when both facets are active.
export function comboKey(discipline, level) {
  return `${discipline}_${level}`
}

// Genitive-case discipline names for the match-history summary (Russian UI text).
export const DISCIPLINE_GENITIVE = {
  singles: 'одиночки', doubles: 'пары', mixed: 'микст',
}

// "Никита Янковский" -> "Янковский Никита". Falls back to the raw name
// for anything that isn't exactly "given name + surname".
export function surnameFirst(name) {
  const parts = name.trim().split(/\s+/)
  return parts.length === 2 ? `${parts[1]} ${parts[0]}` : name
}
