<script setup>
import { ref, computed } from 'vue'
import PlayerMatchLog from './PlayerMatchLog.vue'
import { CATEGORIES, surnameFirst } from '../utils/format.js'
import { useLazyRows } from '../utils/useLazyRows.js'

const props = defineProps({
  rows: { type: Array, required: true },
  sortKey: String,
  sortDir: Number,
  discipline: String,
  level: String,
  detailed: { type: Boolean, default: false },
})
const emit = defineEmits(['sort'])

const { visible, remaining, more, total } = useLazyRows(computed(() => props.rows))

// Expanded row with match history (keyed by player name).
const openName = ref(null)
function toggle(name) {
  openName.value = openName.value === name ? null : name
}

const labelOf = key => CATEGORIES.find(c => c.key === key)?.label || key
const categoryLabel = computed(() =>
  props.level ? `${labelOf(props.discipline)} · ${labelOf(props.level)}` : labelOf(props.discipline))

const columns = [
  { key: 'rank', label: '#' },
  { key: 'name', label: 'Игрок' },
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
  <p class="category-caption">Категория: {{ categoryLabel }}</p>

  <div class="table-wrap" :class="{ detailed }">
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
        <template v-for="(r, i) in visible" :key="r.p.name">
          <tr class="player-row" @click="toggle(r.p.name)">
            <td class="rank num">{{ i + 1 }}</td>
            <td class="name">{{ surnameFirst(r.p.name) }}</td>
            <td class="num rating-val">{{ Math.round(r.rating) }}</td>
            <td class="num hide-m">{{ r.p.tournaments }}</td>
            <td class="num">{{ r.d.matches }}</td>
            <td class="num hide-m">{{ r.d.wins }}–{{ r.d.losses }}</td>
            <td class="num">
              <span class="bar-bg hide-m"><span class="bar" :style="{ width: pct(r.d.winrate) + '%' }"></span></span>
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
            <td colspan="8"><PlayerMatchLog :player="r.p" :discipline="discipline" :level="level" /></td>
          </tr>
        </template>

        <tr v-if="remaining" class="more-row">
          <td colspan="8">
            <div class="more-wrap">
              <button type="button" class="more-btn" @click="more">
                Показать ещё {{ Math.min(remaining, 40) }} из {{ remaining }}
              </button>
            </div>
          </td>
        </tr>

        <tr v-if="!total">
          <td colspan="8" class="empty">Никого не найдено</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
