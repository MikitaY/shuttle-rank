import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// base: './' — relative asset paths, so the site works both from the root and from a
// GitHub Pages subdirectory (username.github.io/shuttle-rank/).
//
// __DATA_VERSION__ stamps the ratings.json request, one value per build. Pages serves the
// data file with Cache-Control: max-age=600, so without the stamp a browser can pair a
// freshly deployed bundle with a ten-minute-old ratings.json — and a data file from before
// the cross-category rating has no `unified` block at all, which reads as an empty table.
// https://vite.dev/config/
export default defineConfig({
  base: './',
  define: { __DATA_VERSION__: JSON.stringify(Date.now().toString(36)) },
  plugins: [vue()],
})
