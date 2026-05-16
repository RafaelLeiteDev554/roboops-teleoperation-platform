import { createRouter, createWebHistory } from 'vue-router'
import OperatorConsoleView from '../views/OperatorConsoleView.vue'
import SessionsListView from '../views/SessionsListView.vue'
import SessionDetailView from '../views/SessionDetailView.vue'

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'console', component: OperatorConsoleView },
    { path: '/sessions', name: 'sessions', component: SessionsListView },
    { path: '/sessions/:id', name: 'session-detail', component: SessionDetailView, props: true },
  ],
})
