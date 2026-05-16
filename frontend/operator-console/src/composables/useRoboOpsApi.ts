import { ref } from 'vue'

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5157'

const token = ref('')
const role = ref('')

export function useRoboOpsApi() {
  async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(token.value ? { Authorization: `Bearer ${token.value}` } : {}),
        ...options.headers,
      },
    })

    if (!response.ok) {
      throw new Error(`${response.status} ${response.statusText}`)
    }

    return response.json() as Promise<T>
  }

  async function login(username = 'admin', password = 'roboops') {
    const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password }),
    })

    if (!response.ok) {
      throw new Error(`${response.status} ${response.statusText}`)
    }

    const data = await response.json()
    token.value = data.accessToken as string
    role.value = data.role as string
  }

  return {
    token,
    role,
    login,
    request,
  }
}
