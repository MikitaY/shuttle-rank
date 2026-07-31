<script setup>
import { computed } from 'vue'
import { fmtDate, DISCIPLINE_GENITIVE, CATEGORIES, surnameFirst, comboKey } from '../utils/format.js'

const props = defineProps({
  player: { type: Object, required: true },
  discipline: { type: String, default: '' },
  level: { type: String, default: null },
  // Cross-category view: one chronological list with the rating change of every match,
  // instead of sections split by discipline and level.
  unified: { type: Boolean, default: false },
  confirmAt: { type: Number, default: 10 },
})

// Elo summary for disciplines where the player has at least one match.
const eloSummary = computed(() => {
  const p = props.player
  const parts = ['singles', 'doubles', 'mixed']
    .filter(d => p.elo_matches[d])
    .map(d => `${DISCIPLINE_GENITIVE[d]} ${Math.round(p.elo[d])}`)
  return parts.length ? ', ' + parts.join(', ') : ''
})

// Matches from most recent to oldest.
const fullLog = computed(() => props.player.match_log.slice().reverse())

const disciplineKeys = CATEGORIES.filter(c => c.group === 'discipline').map(c => c.key)
const levelKeys = CATEGORIES.filter(c => c.group === 'level').map(c => c.key)
const labelOf = key => CATEGORIES.find(c => c.key === key)?.label || key

// A match belongs to the active view if it's the right discipline, and (when a level
// is also picked) the right level too — e.g. "singles" + "D" only matches singles-D.
const isPrimary = m => m.discipline === props.discipline && (!props.level || m.level === props.level)

const primary = computed(() => fullLog.value.filter(isPrimary))

// Everything else. With no level picked, group by discipline (as before). With a level
// picked, the discipline+level pair is already pinned, so group by the exact combo
// each remaining match belongs to (e.g. "Пары · C").
const otherGroups = computed(() => {
  const rest = fullLog.value.filter(m => !isPrimary(m))
  if (!rest.length) return []

  if (!props.level) {
    return disciplineKeys
      .filter(d => d !== props.discipline)
      .map(d => ({ key: d, label: labelOf(d), matches: rest.filter(m => m.discipline === d) }))
      .filter(g => g.matches.length)
  }

  const groups = new Map()
  for (const m of rest) {
    const key = comboKey(m.discipline, m.level)
    if (!groups.has(key)) groups.set(key, { key, label: `${labelOf(m.discipline)} · ${labelOf(m.level)}`, matches: [] })
    groups.get(key).matches.push(m)
  }
  return [...groups.values()].sort((x, y) => {
    const [da, la] = x.key.split('_')
    const [db, lb] = y.key.split('_')
    return disciplineKeys.indexOf(da) - disciplineKeys.indexOf(db) || levelKeys.indexOf(la) - levelKeys.indexOf(lb)
  })
})

// One section for the active category, then (if any) a divider and one section per other group.
const sections = computed(() => {
  if (props.unified) return [{ label: null, matches: fullLog.value }]

  const list = [{ label: null, matches: primary.value }]
  if (otherGroups.value.length) {
    list.push({ divider: true })
    for (const g of otherGroups.value) list.push({ label: g.label, matches: g.matches })
  }
  return list
})

const score = m => m.games.map(g => g.join(':')).join(', ') || 'w/o'

// "+12.4" / "−8.1" — the rating change this match produced.
const delta = m => m.unified_delta == null
  ? ''
  : (m.unified_delta >= 0 ? '+' : '−') + Math.abs(m.unified_delta).toFixed(1)
</script>

<template>
  <template v-if="unified">
    <b>{{ surnameFirst(player.name) }}</b> — сквозной рейтинг {{ Math.round(player.unified.rating) }},
    старт {{ Math.round(player.unified.seed) }}<template v-if="player.unified.seed_level">
    ({{ player.unified.seed_level }})</template>, пик {{ Math.round(player.unified.peak) }}
    · категория {{ player.unified.category }}<template v-if="player.unified.provisional">
    (предварительная, {{ player.unified.matches }} из {{ confirmAt }} матчей)</template><template
    v-else-if="player.unified.category_since"> с {{ fmtDate(player.unified.category_since) }}</template>
  </template>
  <template v-else>
    <b>{{ surnameFirst(player.name) }}</b> — Elo: общий {{ Math.round(player.elo.overall) }}{{ eloSummary }}
    · очки {{ Math.round(player.points) }}
  </template>

  <template v-for="(s, si) in sections" :key="si">
    <hr v-if="s.divider" class="log-divider" />
    <template v-else>
      <p v-if="s.label" class="log-group-label">{{ s.label }}</p>
      <table class="log">
        <tbody>
          <tr v-for="(m, i) in s.matches" :key="i">
            <td>{{ fmtDate(m.date) }}</td>
            <td>{{ m.event }}</td>
            <td>{{ m.round || '' }}</td>
            <td :class="m.won ? 'res-W' : 'res-L'">{{ m.won ? 'победа' : 'поражение' }}</td>
            <td>
              {{ m.opponents.map(surnameFirst).join(' / ') }}<template v-if="m.teammates.length"> (с {{ m.teammates.map(surnameFirst).join(', ') }})</template>
            </td>
            <td class="score">{{ score(m) }}</td>
            <td v-if="unified" class="num" :class="m.unified_delta >= 0 ? 'res-W' : 'res-L'">{{ delta(m) }}</td>
            <td v-if="unified" class="num">{{ m.unified_after != null ? Math.round(m.unified_after) : '' }}</td>
          </tr>
        </tbody>
      </table>
    </template>
  </template>
</template>
