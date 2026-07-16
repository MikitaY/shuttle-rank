<script setup>
import { ref } from 'vue'
import PlayerMatchLog from './PlayerMatchLog.vue'
import { LEVEL_CLASS } from '../utils/format.js'

defineProps({
  rows: { type: Array, required: true },
  sortKey: String,
  sortDir: Number,
})
const emit = defineEmits(['sort'])

// Expanded row with match history (keyed by player name).
const openName = ref(null)
function toggle(name) {
  openName.value = openName.value === name ? null : name
}

const columns = [
  { key: 'rank', label: '#' },
  { key: 'name', label: 'Игрок' },
  { key: 'level', label: 'Уровень' },
  { key: 'rating', label: 'Рейтинг' },
  { key: 'tournaments', label: 'Турниры', cls: 'hide-m' },
  { key: 'matches', label: 'Матчи' },
  { key: 'wl', label: 'В–П', cls: 'hide-m' },
  { key: 'winrate', label: '% побед' },
  { key: 'form', label: 'Форма', cls: 'hide-m' },
]

const pct = winrate => Math.round(winrate * 100)
</script>

<template>
  <table>
    <thead>
      <tr>
        <th
          v-for="c in columns" :key="c.key"
          :class="[c.cls, { sorted: sortKey === c.key }]"
          @click="emit('sort', c.key)"
        >{{ c.label }}</th>
      </tr>
    </thead>
    <tbody>
      <template v-for="(r, i) in rows" :key="r.p.name">
        <tr class="player-row" @click="toggle(r.p.name)">
          <td class="rank num">{{ i + 1 }}</td>
          <td class="name">{{ r.p.name }}</td>
          <td>
            <template v-if="r.p.level">
              <span class="lvl" :class="LEVEL_CLASS[r.p.level]">{{ r.p.level_label }}</span>
              <span v-if="r.p.level_trend === 'up'" class="trend up">▲</span>
              <span v-else-if="r.p.level_trend === 'down'" class="trend down">▼</span>
            </template>
            <template v-else>—</template>
          </td>
          <td class="num rating-val">{{ Math.round(r.rating) }}</td>
          <td class="num hide-m">{{ r.p.tournaments }}</td>
          <td class="num">{{ r.d.matches }}</td>
          <td class="num hide-m">{{ r.d.wins }}–{{ r.d.losses }}</td>
          <td class="num">
            <span class="bar-bg"><span class="bar" :style="{ width: pct(r.d.winrate) + '%' }"></span></span>
            {{ pct(r.d.winrate) }}%
          </td>
          <td class="hide-m">
            <span
              v-for="(x, j) in r.d.form.slice(-5)" :key="j"
              class="chip" :class="x"
            >{{ x === 'W' ? 'В' : 'П' }}</span>
          </td>
        </tr>
        <tr v-if="openName === r.p.name" class="detail">
          <td colspan="9"><PlayerMatchLog :player="r.p" /></td>
        </tr>
      </template>

      <tr v-if="!rows.length">
        <td colspan="9" class="empty">Никого не найдено</td>
      </tr>
    </tbody>
  </table>
</template>
