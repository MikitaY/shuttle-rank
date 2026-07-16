<script setup>
import { computed } from 'vue'
import { fmtDate, DISCIPLINE_GENITIVE } from '../utils/format.js'

const props = defineProps({ player: { type: Object, required: true } })

// Elo summary for disciplines where the player has at least one match.
const eloSummary = computed(() => {
  const p = props.player
  const parts = ['singles', 'doubles', 'mixed']
    .filter(d => p.elo_matches[d])
    .map(d => `${DISCIPLINE_GENITIVE[d]} ${Math.round(p.elo[d])}`)
  return parts.length ? ', ' + parts.join(', ') : ''
})

// Matches from most recent to oldest.
const log = computed(() => props.player.match_log.slice().reverse())

const score = m => m.games.map(g => g.join(':')).join(', ') || 'w/o'
</script>

<template>
  <b>{{ player.name }}</b> — Elo: общий {{ Math.round(player.elo.overall) }}{{ eloSummary }}
  · очки {{ Math.round(player.points) }}
  <table class="log">
    <tbody>
      <tr v-for="(m, i) in log" :key="i">
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
