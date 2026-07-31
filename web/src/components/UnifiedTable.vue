<script setup>
import { ref } from 'vue'
import PlayerMatchLog from './PlayerMatchLog.vue'
import { surnameFirst, categoryLabel, plural } from '../utils/format.js'

const props = defineProps({
  rows: { type: Array, required: true },
  sortKey: String,
  sortDir: Number,
  confirmAt: { type: Number, default: 10 },
})
const emit = defineEmits(['sort'])

const openName = ref(null)
function toggle(name) {
  openName.value = openName.value === name ? null : name
}

const columns = [
  { key: 'rank', label: '#' },
  { key: 'name', label: 'Игрок' },
  { key: 'category', label: 'Категория' },
  { key: 'rating', label: 'Рейтинг' },
  { key: 'progress', label: 'До следующей', cls: 'hide-m' },
  { key: 'matches', label: 'Матчи' },
  { key: 'wl', label: 'В–П', cls: 'hide-m' },
  { key: 'winrate', label: '% побед' },
  { key: 'form', label: 'Форма', cls: 'hide-m' },
]

const pct = winrate => Math.round(winrate * 100)

// How much rating is still missing for the next category; null at the top of the ladder.
const toNext = u => u.next_floor != null ? Math.max(0, Math.round(u.next_floor - u.rating)) : null

// A provisional rating can't be promoted yet, so show what it's waiting for instead.
const toConfirm = u => {
  const left = Math.max(0, props.confirmAt - u.matches)
  return `ещё ${left} ${plural(left, ['матч', 'матча', 'матчей'])}`
}
</script>

<template>
  <p class="category-caption">
    Один рейтинг на все дисциплины и уровни. Категория присваивается игроку
    после {{ confirmAt }} сыгранных матчей; до этого она предварительная (?).
  </p>

  <div class="table-wrap">
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
            <td class="name">{{ surnameFirst(r.p.name) }}</td>
            <td>
              <span class="cat" :class="['cat-' + r.u.category, r.u.status]">
                {{ categoryLabel(r.u) }}<template v-if="r.u.provisional">&thinsp;?</template>
              </span>
            </td>
            <td class="num rating-val">{{ Math.round(r.u.rating) }}</td>
            <td class="num hide-m">
              <span v-if="r.u.provisional" class="to-next">{{ toConfirm(r.u) }}</span>
              <template v-else>
                <span class="bar-bg"><span class="bar" :style="{ width: r.u.progress * 100 + '%' }"></span></span>
                <span v-if="toNext(r.u) !== null" class="to-next">{{ r.u.next_category }} −{{ toNext(r.u) }}</span>
                <span v-else class="to-next">верх</span>
              </template>
            </td>
            <td class="num">{{ r.p.matches }}</td>
            <td class="num hide-m">{{ r.p.wins }}–{{ r.p.losses }}</td>
            <td class="num">
              <span class="bar-bg hide-m"><span class="bar" :style="{ width: pct(r.p.winrate) + '%' }"></span></span>
              {{ pct(r.p.winrate) }}%
            </td>
            <td class="hide-m">
              <span
                v-for="(x, j) in r.p.form.slice(-5)" :key="j"
                class="chip" :class="x"
              >{{ x === 'W' ? 'В' : 'П' }}</span>
            </td>
          </tr>
          <tr v-if="openName === r.p.name" class="detail">
            <td colspan="9"><PlayerMatchLog :player="r.p" unified :confirm-at="confirmAt" /></td>
          </tr>
        </template>

        <tr v-if="!rows.length">
          <td colspan="9" class="empty">Никого не найдено</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
