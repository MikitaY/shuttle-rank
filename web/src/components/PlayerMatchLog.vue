<script setup>
import { computed } from 'vue'
import { fmtDate, DISCIPLINE_GENITIVE, CATEGORIES } from '../utils/format.js'

const props = defineProps({
  player: { type: Object, required: true },
  category: { type: String, default: 'overall' },
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

const levelKeys = CATEGORIES.filter(c => c.group === 'level').map(c => c.key)
const disciplineKeys = CATEGORIES.filter(c => c.group === 'discipline' && c.key !== 'overall').map(c => c.key)

// Which facet the active tab filters on — null for the "Общий" tab (no split).
const facetKey = computed(() => {
  if (levelKeys.includes(props.category)) return 'level'
  if (disciplineKeys.includes(props.category)) return 'discipline'
  return null
})

const valueOf = m => (facetKey.value === 'level' ? m.level : m.discipline)

// Matches played in the currently selected category.
const primary = computed(() =>
  facetKey.value ? fullLog.value.filter(m => valueOf(m) === props.category) : fullLog.value)

// Everything else, grouped by the same facet (other levels, or other disciplines).
const otherGroups = computed(() => {
  if (!facetKey.value) return []
  const keys = facetKey.value === 'level' ? levelKeys : disciplineKeys
  const rest = fullLog.value.filter(m => valueOf(m) !== props.category)
  return keys
    .filter(k => k !== props.category)
    .map(k => ({
      key: k,
      label: CATEGORIES.find(c => c.key === k)?.label || k,
      matches: rest.filter(m => valueOf(m) === k),
    }))
    .filter(g => g.matches.length)
})

// One section for the active category, then (if any) a divider and one section per other group.
const sections = computed(() => {
  const list = [{ label: null, matches: primary.value }]
  if (otherGroups.value.length) {
    list.push({ divider: true })
    for (const g of otherGroups.value) list.push({ label: g.label, matches: g.matches })
  }
  return list
})

const score = m => m.games.map(g => g.join(':')).join(', ') || 'w/o'
</script>

<template>
  <b>{{ player.name }}</b> — Elo: общий {{ Math.round(player.elo.overall) }}{{ eloSummary }}
  · очки {{ Math.round(player.points) }}

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
              {{ m.opponents.join(' / ') }}<template v-if="m.teammates.length"> (с {{ m.teammates.join(', ') }})</template>
            </td>
            <td class="score">{{ score(m) }}</td>
          </tr>
        </tbody>
      </table>
    </template>
  </template>
</template>
