import { ref, computed, watch, onMounted, onUnmounted } from 'vue'

// The league has hundreds of players, so a table renders in chunks: the first `page` rows,
// then the next chunk once the page is scrolled close to its end. The "показать ещё" button
// does the same thing explicitly — and covers the case where the whole list fits on screen.
export function useLazyRows(rows, page = 40, tailPx = 600) {
  const limit = ref(page)

  // A new filter, sort or mode means a new list — back to the first chunk.
  watch(rows, () => { limit.value = page })

  const visible = computed(() => rows.value.slice(0, limit.value))
  const remaining = computed(() => Math.max(0, rows.value.length - limit.value))
  const more = () => { limit.value += page }

  function onScroll() {
    if (!remaining.value) return
    const scrolled = window.scrollY + window.innerHeight
    if (scrolled >= document.documentElement.scrollHeight - tailPx) more()
  }

  onMounted(() => {
    window.addEventListener('scroll', onScroll, { passive: true })
    window.addEventListener('resize', onScroll, { passive: true })
  })
  onUnmounted(() => {
    window.removeEventListener('scroll', onScroll)
    window.removeEventListener('resize', onScroll)
  })

  return { visible, remaining, more, total: computed(() => rows.value.length) }
}
