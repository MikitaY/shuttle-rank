<script setup>
import { CATEGORIES } from '../utils/format.js'

defineProps({
  category: String,
  system: String,
  query: String,
  minMatches: Boolean,
})
const emit = defineEmits([
  'update:category', 'update:system', 'update:query', 'update:minMatches',
])

const disciplines = CATEGORIES.filter(c => c.group === 'discipline')
const levels = CATEGORIES.filter(c => c.group === 'level')
const systems = [
  { key: 'elo', label: 'Elo' },
  { key: 'points', label: 'Очки' },
]
</script>

<template>
  <div class="controls">
    <div class="tabs">
      <button
        v-for="d in disciplines" :key="d.key"
        :class="{ active: category === d.key }"
        @click="emit('update:category', d.key)"
      >{{ d.label }}</button>
    </div>

    <div class="tabs">
      <button
        v-for="l in levels" :key="l.key"
        :class="{ active: category === l.key }"
        @click="emit('update:category', l.key)"
      >{{ l.label }}</button>
    </div>

    <div class="tabs">
      <button
        v-for="s in systems" :key="s.key"
        :class="{ active: system === s.key }"
        @click="emit('update:system', s.key)"
      >{{ s.label }}</button>
    </div>

    <input
      type="search" placeholder="Поиск игрока…"
      :value="query"
      @input="emit('update:query', $event.target.value)"
    />

    <label class="minm">
      <input
        type="checkbox"
        :checked="minMatches"
        @change="emit('update:minMatches', $event.target.checked)"
      />
      от 5 матчей
    </label>
  </div>
</template>
