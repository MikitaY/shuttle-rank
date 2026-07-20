<script setup>
import { ref, nextTick } from 'vue'
import { CATEGORIES } from '../utils/format.js'

defineProps({
  discipline: String,
  level: String,
  system: String,
  query: String,
  minMatches: Boolean,
})
const emit = defineEmits([
  'update:discipline', 'update:level', 'update:system', 'update:query', 'update:minMatches',
])

const disciplines = CATEGORIES.filter(c => c.group === 'discipline')
const levels = CATEGORIES.filter(c => c.group === 'level')
const systems = [
  { key: 'elo', label: 'Elo' },
  { key: 'points', label: 'Очки' },
]

// On narrow screens the search field collapses behind a magnifier button (see CSS).
const searchOpen = ref(false)
const searchInput = ref(null)
function toggleSearch() {
  searchOpen.value = !searchOpen.value
  if (searchOpen.value) nextTick(() => searchInput.value?.focus())
}
</script>

<template>
  <div class="controls">
    <div class="tabs tabs-wide">
      <button
        v-for="d in disciplines" :key="d.key"
        :class="{ active: discipline === d.key }"
        @click="emit('update:discipline', d.key)"
      >{{ d.label }}</button>
    </div>

    <div class="tabs tabs-wide">
      <button
        v-for="l in levels" :key="l.key"
        :class="{ active: level === l.key }"
        @click="emit('update:level', level === l.key ? null : l.key)"
      >{{ l.label }}</button>
    </div>

    <div class="tabs tabs-wide">
      <button
        v-for="s in systems" :key="s.key"
        :class="{ active: system === s.key }"
        @click="emit('update:system', s.key)"
      >{{ s.label }}</button>
    </div>

    <div class="controls-row">
      <div class="search-wrap" :class="{ open: searchOpen }">
        <button
          type="button" class="search-btn" :class="{ active: query }"
          @click="toggleSearch" aria-label="Поиск игрока"
        >🔍</button>
        <input
          ref="searchInput"
          type="search" placeholder="Поиск игрока…"
          :value="query"
          @input="emit('update:query', $event.target.value)"
        />
      </div>

      <label class="minm">
        <input
          type="checkbox"
          :checked="minMatches"
          @change="emit('update:minMatches', $event.target.checked)"
        />
        от 5 матчей
      </label>
    </div>
  </div>
</template>
